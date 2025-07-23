using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class TcpPortClient : MonoBehaviour
{
    TcpClient client;
    NetworkStream clientStream;
    byte[] sizeBuffer = new byte[4];

    Texture2D webcamTexture;
    Renderer quadRenderer;

    private Controls _controls;
    private RaceCar car = new RaceCar();

    private bool isReading = false;
    private ConcurrentQueue<byte[]> frameQueue = new ConcurrentQueue<byte[]>();

    void Start()
    {
        quadRenderer = GetComponent<Renderer>();
        webcamTexture = new Texture2D(2, 2);

        _controls = new Controls();
        _controls.OculusTouchControllers.Enable();

        ConnectToRobot();
    }

    void ConnectToRobot()
    {
        try
        {
            client?.Close();
            client = new TcpClient("192.168.0.104", 9002);
            clientStream = client.GetStream();
            isReading = true;

            Task.Run(() => ReceiveLoop());
            InvokeRepeating(nameof(SendControls), 0f, 0.05f);

            Debug.Log("Connected to robot.");
        }
        catch (Exception e)
        {
            Debug.LogWarning("Failed to connect: " + e.Message);
            Invoke(nameof(RetryConnection), 1f);
        }
    }

    void RetryConnection()
    {
        if (client is null || !client.Connected)
        {
            Debug.Log("Retrying connection...");
            ConnectToRobot();
        }
    }

    void ReceiveLoop()
    {
        while (isReading && clientStream != null && clientStream.CanRead)
        {
            try
            {
                clientStream.Read(sizeBuffer, 0, 4);
                int frameSize = BitConverter.ToInt32(sizeBuffer, 0);
                frameSize = System.Net.IPAddress.NetworkToHostOrder(frameSize);

                byte[] frameBuffer = new byte[frameSize];
                int totalRead = 0;
                while (totalRead < frameSize)
                {
                    int read = clientStream.Read(frameBuffer, totalRead, frameSize - totalRead);
                    if (read == 0) throw new Exception("Disconnected");
                    totalRead += read;
                }

                frameQueue.Enqueue(frameBuffer);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Connection lost: " + ex.Message);
                isReading = false;
                RetryConnection();
                break;
            }
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
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Failed to send control JSON: " + ex.Message);
            isReading = false;
            RetryConnection();
        }
    }

    void Update()
    {
        float rightTriggerValue = _controls.OculusTouchControllers.Forward.ReadValue<float>();
        float leftTriggerValue = _controls.OculusTouchControllers.Backward.ReadValue<float>();
        Vector2 thumbstick = _controls.OculusTouchControllers.Turn.ReadValue<Vector2>();

        car.Throttle = rightTriggerValue >= leftTriggerValue ? -rightTriggerValue : leftTriggerValue;
        car.Steering = -thumbstick.x;

        if (frameQueue.TryDequeue(out var frameBytes))
        {
            webcamTexture.LoadImage(frameBytes);
            quadRenderer.material.mainTexture = webcamTexture;
        }
    }

    void OnDestroy()
    {
        clientStream?.Close();
        client?.Close();
    }
}
