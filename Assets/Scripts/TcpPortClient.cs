using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class TcpPortClient : MonoBehaviour
{
    public string robotIp = "10.0.0.5";
    public bool kpi = false;
    
    TcpClient client;
    TcpClient ping;
    NetworkStream clientStream;
    NetworkStream pingStream;
    byte[] sizeBuffer = new byte[4];
    byte[] frameBuffer;
    
    Texture2D webcamTexture;
    Renderer quadRenderer;
    
    private Controls _controls;
    private RaceCar car = new RaceCar();

    private bool isReading = false;
    private Queue<VideoFrame> _frameQueue = new Queue<VideoFrame>();
    
    private List<Stat> display_frame_delays = new List<Stat>();
    private List<Stat> send_controls_delays = new List<Stat>();
    private List<Stat> fps_over_time = new List<Stat>();
    
    private int numberOfVideoFramesReceived = 0;
    
    void Start()
    {
        quadRenderer = GetComponent<Renderer>();
        webcamTexture = new Texture2D(2, 2);
        
        _controls = new Controls();
        _controls.OculusTouchControllers.Enable();

        StartCoroutine(RetryConnectionLoop());
    }
    
    IEnumerator RetryConnectionLoop()
    {
        while (true)
        {
            if (client == null || !client.Connected || (kpi && ping == null || !ping.Connected))
            {
                Debug.Log("Attempting to connect to robot...");
                try
                {
                    client?.Close();
                    ping?.Close();

                    client = new TcpClient(robotIp, 9002);
                    if (kpi) ping = new TcpClient(robotIp, 9003);

                    clientStream = client.GetStream();
                    if (kpi) pingStream = ping.GetStream();

                    isReading = true;

                    StartCoroutine(ReadFramesCoroutine());
                    if (kpi) StartCoroutine(ReadAndSendPingCoroutine());
                    InvokeRepeating(nameof(SendControls), 0f, 0.05f);
                    InvokeRepeating(nameof(CollectFps), 0f, 1.0f);

                    Debug.Log("Connected to robot.");
                    yield break; // Stop retry loop once connected
                }
                catch (Exception e)
                {
                    Debug.LogWarning("Connection failed: " + e.Message);
                }
            }

            yield return new WaitForSeconds(5f);
        }
    }
    
    IEnumerator ReadAndSendPingCoroutine()
    {
        byte[] buffer = new byte[1024];

        while (isReading && pingStream != null)
        {
            if (pingStream.DataAvailable)
            {
                int bytesRead = pingStream.Read(buffer, 0, buffer.Length);
                if (bytesRead > 0)
                {
                    string incoming = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    foreach (string line in incoming.Split('\n'))
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        var msg = JsonUtility.FromJson<PingPongMessage>(line);
                        if (msg.type == "ping")
                        {
                            var pong = new PingPongMessage { type = "pong", timestamp = msg.timestamp };
                            string pongJson = JsonUtility.ToJson(pong) + "\n";
                            byte[] pongBytes = Encoding.UTF8.GetBytes(pongJson);
                            pingStream.Write(pongBytes, 0, pongBytes.Length);
                            pingStream.Flush();
                            Debug.Log("Pong sent: " + pongJson);
                        }
                    }
                }
            }

            yield return null;
        }

        // If we exit loop, something failed
        StopCoroutine(RetryConnectionLoop());
        StartCoroutine(RetryConnectionLoop());
    }
    
    IEnumerator ReadFramesCoroutine()
    {
        while (isReading && clientStream != null && clientStream.CanRead)
        {
            if (clientStream.DataAvailable)
            {
                
                // Read 4-byte frame size
                int readSize = clientStream.Read(sizeBuffer, 0, 4);
                if (readSize != 4)
                {
                    Debug.LogWarning("Failed to read frame size");
                    break;
                }

                int frameSize = BitConverter.ToInt32(sizeBuffer, 0);
                frameSize = System.Net.IPAddress.NetworkToHostOrder(frameSize);

                if (frameBuffer == null || frameBuffer.Length != frameSize)
                {
                    frameBuffer = new byte[frameSize]; // only reallocate if needed
                }
                int totalRead = 0;
                while (totalRead < frameSize)
                {
                    int read = clientStream.Read(frameBuffer, totalRead, frameSize - totalRead);
                    if (read == 0)
                    {
                        Debug.LogWarning("Disconnected during frame read");
                        isReading = false;
                        StopCoroutine(RetryConnectionLoop());
                        StartCoroutine(RetryConnectionLoop());
                        yield break;
                    }
                    totalRead += read;
                }

                lock (_frameQueue)
                {
                    _frameQueue.Enqueue(new VideoFrame(frameBuffer, DateTimeOffset.Now.ToUnixTimeMilliseconds()));
                    numberOfVideoFramesReceived++;
                }
            }

            yield return null; // Wait one frame before checking again
        }
    }


    void SendControls()
    {
        if (clientStream == null || !clientStream.CanWrite) return;
        
        try
        {
            string json = JsonUtility.ToJson(car) + "\n";
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            clientStream.Write(bytes, 0, bytes.Length);
            
            DateTimeOffset now = DateTimeOffset.Now;
            send_controls_delays.Add(new Stat(now.ToUnixTimeMilliseconds(), (int)(now - car.GetInputTimestamp()).TotalMilliseconds));
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Failed to send control JSON: " + ex.Message);
            isReading = false;
            CancelInvoke(nameof(SendControls));
            StopCoroutine(RetryConnectionLoop());
            StartCoroutine(RetryConnectionLoop());
        }
    }

    void CollectFps()
    {
        fps_over_time.Add(new Stat(DateTimeOffset.Now.ToUnixTimeMilliseconds(), numberOfVideoFramesReceived));
        numberOfVideoFramesReceived = 0;
    }
    
    void Update()
    {
        DateTimeOffset time = DateTimeOffset.Now;
        float rightTriggerValue = _controls.OculusTouchControllers.Forward.ReadValue<float>();
        float leftTriggerValue = _controls.OculusTouchControllers.Backward.ReadValue<float>();
        Vector2 thumbstick = _controls.OculusTouchControllers.Turn.ReadValue<Vector2>();

        if (rightTriggerValue >= leftTriggerValue)
        {
            car.Throttle = -rightTriggerValue;
        }
        else
        {
            car.Throttle = leftTriggerValue;
        }
        
        car.Steering = -thumbstick.x;
        car.SetInputTimestamp(time);
        
        // Apply video frame
        lock (_frameQueue)
        {
            if (_frameQueue.Count > 0)
            {
                var frame = _frameQueue.Dequeue();
                webcamTexture.LoadImage(frame.jpegData);
                quadRenderer.material.mainTexture = webcamTexture;

                long now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                display_frame_delays.Add(new Stat(now, (int)(now - frame.receivedTimestamp)));
            }
        }
    }
    
    void WriteDelaysToFile()
    {
        string framePath = Application.persistentDataPath + "/frame_delays.csv";
        string controlPath = Application.persistentDataPath + "/control_delays.csv";
        string fpsPath = Application.persistentDataPath + "/fps_over_time.csv";

        System.IO.File.WriteAllLines(framePath,
            new string[] { "timestamp,delay" }
                .Concat(display_frame_delays.Select(d => $"{d.timestamp},{d.value}")));

        System.IO.File.WriteAllLines(controlPath,
            new string[] { "timestamp,delay" }
                .Concat(send_controls_delays.Select(d => $"{d.timestamp},{d.value}")));
        
        System.IO.File.WriteAllLines(fpsPath,
            new string[] { "timestamp,fps" }
                .Concat(fps_over_time.Select(d => $"{d.timestamp},{d.value}")));

        Debug.Log($"Delays written to:\n{framePath}\n{controlPath}\n{fpsPath}");
    }
    
    void SendCsvFiles()
    {
        try
        {
            using (TcpClient csvClient = new TcpClient(robotIp, 9004))
            using (NetworkStream ns = csvClient.GetStream())
            {
                string[] filenames = {
                    Application.persistentDataPath + "/frame_delays.csv",
                    Application.persistentDataPath + "/control_delays.csv",
                    Application.persistentDataPath + "/fps_over_time.csv"
                };

                foreach (string fullPath in filenames)
                {
                    string fname = System.IO.Path.GetFileName(fullPath);
                    byte[] fnameBytes = Encoding.UTF8.GetBytes(fname);
                    byte[] fnameLen = BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder(fnameBytes.Length));
                    ns.Write(fnameLen, 0, 4);
                    ns.Write(fnameBytes, 0, fnameBytes.Length);

                    byte[] fileBytes = System.IO.File.ReadAllBytes(fullPath);
                    byte[] fileLen = BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder(fileBytes.Length));
                    ns.Write(fileLen, 0, 4);
                    ns.Write(fileBytes, 0, fileBytes.Length);
                }

                Debug.Log("CSV files sent to robot.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Failed to send CSVs: " + ex.Message);
        }
    }



    void OnDestroy()
    {
        StopAllCoroutines();
        clientStream?.Close();
        client?.Close();
        pingStream?.Close();
        ping?.Close();
        
        WriteDelaysToFile();
        //SendCsvFiles(); NOT FUNCTIONAL RN
    }
}

[Serializable]
internal class PingPongMessage
{
    public string type;
    public ulong timestamp;
}


internal class VideoFrame
{
    public byte[] jpegData;
    public long receivedTimestamp;

    public VideoFrame(byte[] jpegData, long receivedTimestamp)
    {
        this.jpegData = jpegData;
        this.receivedTimestamp = receivedTimestamp;
    }
}

internal class Stat
{
    public long timestamp;
    public int value;

    public Stat(long timestamp, int value)
    {
        this.timestamp = timestamp;
        this.value = value;
    }
}
