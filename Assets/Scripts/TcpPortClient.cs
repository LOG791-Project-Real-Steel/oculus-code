using System;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class TcpPortClient : MonoBehaviour
{
    TcpClient client;
    TcpClient sender;
    NetworkStream clientStream;
    NetworkStream senderStream;
    byte[] sizeBuffer = new byte[4];
    byte[] frameBuffer;
    
    Texture2D webcamTexture;
    Renderer quadRenderer;
    
    private Controls _controls;
    private RaceCar car = new RaceCar();
    
    private bool isReading = false;
    
    void Start()
    {
        Debug.Log("Blablabla");
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
            sender?.Close();
            client = new TcpClient("192.168.0.104", 9002);
            //sender = new TcpClient("192.168.0.104", 9000);
            
            clientStream = client.GetStream();
            isReading = true;
            
            //senderStream = sender.GetStream();
            
            InvokeRepeating(nameof(ReadFrame), 0f, 0.016f); // ~60 FPS
            InvokeRepeating(nameof(SendControls), 0f, 0.05f);
            Debug.Log("Connected to robot.");
        }
        catch (Exception e)
        {
            Debug.LogWarning("Failed to connect: " + e.Message);
            Invoke(nameof(RetryConnection), 1f); // Retry after 1 second
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

    void ReadFrame()
    {
        if (!isReading || clientStream == null || !clientStream.CanRead) return;

        try
        {
            if (!clientStream.DataAvailable) return;

            clientStream.Read(sizeBuffer, 0, 4);
            int frameSize = BitConverter.ToInt32(sizeBuffer, 0);
            frameSize = System.Net.IPAddress.NetworkToHostOrder(frameSize);

            frameBuffer = new byte[frameSize];
            int totalRead = 0;
            while (totalRead < frameSize)
            {
                int read = clientStream.Read(frameBuffer, totalRead, frameSize - totalRead);
                if (read == 0) throw new Exception("Disconnected");
                totalRead += read;
            }

            webcamTexture.LoadImage(frameBuffer);
            quadRenderer.material.mainTexture = webcamTexture;
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Connection lost: " + ex.Message);
            isReading = false;
            CancelInvoke(nameof(ReadFrame));
            RetryConnection();
        }
    }

    void SendControls()
    {
        Debug.Log("SendControls");
        if (clientStream == null || !clientStream.CanWrite) return;
        
        try
        {
            string json = JsonUtility.ToJson(car) + "\n";
            Debug.Log("jsonCar " + json);
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            clientStream.Write(bytes, 0, bytes.Length);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Failed to send control JSON: " + ex.Message);
            isReading = false;
            CancelInvoke(nameof(SendControls));
            RetryConnection();
        }
    }
    
    void Update()
    {
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
    }

    void OnDestroy()
    {
        clientStream?.Close();
        client?.Close();
        sender?.Close();
    }
}
