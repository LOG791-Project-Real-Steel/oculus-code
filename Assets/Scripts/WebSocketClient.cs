using System;
using NativeWebSocket;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using TMPro;

public class WebSocketClient : MonoBehaviour
{
    public string robotIp = "100.80.175.10";
    public bool kpi = false;

    WebSocket websocket; // Main websocket for video/data
    WebSocket pingSocket; // Second websocket for ping/pong
    Texture2D webcamTexture;
    Renderer quadRenderer;

    private Coroutine sendLoop;
    private Controls _controls;
    private RaceCar car = new RaceCar();

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
        Connect();
        if (kpi) ConnectPingSocket();
    }

    async void Connect()
    {
        websocket = new WebSocket("ws://74.56.22.147:8764/oculus");

        websocket.OnOpen += () =>
        {
            Debug.Log("Main websocket connected!");
            sendLoop = StartCoroutine(SendDataLoop());
        };

        websocket.OnMessage += (bytes) =>
        {
            long start = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            
            webcamTexture.LoadImage(bytes);
            quadRenderer.material.mainTexture = webcamTexture;
            
            long now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            display_frame_delays.Add(new Stat(now, (int)(now - start)));
            numberOfVideoFramesReceived++;
        };

        websocket.OnError += (e) => { Debug.Log("Main websocket error: " + e); };

        if (websocket != null)
        {
            await websocket.Connect();
        }
    }

    async void ConnectPingSocket()
    {
        InvokeRepeating(nameof(CollectFps), 0, 1.0f);
        
        pingSocket = new WebSocket("ws://74.56.22.147:8764/oculus/ping");

        pingSocket.OnOpen += () => { Debug.Log("Ping socket connected!"); };

        pingSocket.OnMessage += (bytes) =>
        {
            string message = Encoding.UTF8.GetString(bytes);
            Debug.Log("Ping socket received: " + message);
            PingPongMessage json = JsonUtility.FromJson<PingPongMessage>(message);

            if (json.type == "ping")
            {
                Debug.Log("Responding with pong...");
                json.type = "pong";
                pingSocket.Send(Encoding.UTF8.GetBytes(JsonUtility.ToJson(json)));
            }
        };

        pingSocket.OnError += (e) => { Debug.Log("Ping socket error: " + e); };

        if (pingSocket != null)
        {
            await pingSocket.Connect();
        }
    }

    private IEnumerator SendDataLoop()
    {
        while (true)
        {
            string json = JsonUtility.ToJson(car);
            byte[] bytes = Encoding.UTF8.GetBytes(json);

            if (websocket.State == WebSocketState.Open)
            {
                websocket.Send(bytes);
            }

            if (kpi)
            {
                long now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                send_controls_delays.Add(new Stat(now, (int)(now - car.Timestamp)));
            }
            
            yield return new WaitForSeconds(0.05f);
        }
    }
    
    void CollectFps()
    {
        fps_over_time.Add(new Stat(DateTimeOffset.Now.ToUnixTimeMilliseconds(), numberOfVideoFramesReceived));
        numberOfVideoFramesReceived = 0;
    }

    void Update()
    {
        DateTimeOffset start = DateTimeOffset.Now;
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
        car.Timestamp = start.ToUnixTimeMilliseconds();

#if !UNITY_WEBGL || UNITY_EDITOR
        websocket?.DispatchMessageQueue();
        pingSocket?.DispatchMessageQueue();
#endif
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
                string[] filenames =
                {
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

    private void OnDestroy()
    {
        if (kpi)
        {
            WriteDelaysToFile();
            SendCsvFiles();
        }
    }

    private async void OnApplicationQuit()
    {
        if (websocket != null)
        {
            await websocket.Close();
        }

        if (pingSocket != null)
        {
            await pingSocket.Close();
        }
    }

    class PingPongMessage
    {
        public string type;
        public string timestamp;

        public PingPongMessage()
        {
        }

        public PingPongMessage(string type, string timestamp)
        {
            this.type = type;
            this.timestamp = timestamp;
        }
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