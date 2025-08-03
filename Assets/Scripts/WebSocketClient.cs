using NativeWebSocket;
using UnityEngine;
using System.Collections;
using System.Text;
using TMPro;

public class WebSocketClient : MonoBehaviour
{
    WebSocket websocket;       // Main websocket for video/data
    WebSocket pingSocket;      // Second websocket for ping/pong
    Texture2D webcamTexture;
    Renderer quadRenderer;
    
    private Coroutine sendLoop;
    private Controls _controls;
    private RaceCar car = new RaceCar();

    void Start()
    {
        quadRenderer = GetComponent<Renderer>();
        webcamTexture = new Texture2D(2, 2);
        _controls = new Controls();
        _controls.OculusTouchControllers.Enable();
        Connect();
        ConnectPingSocket();
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
            webcamTexture.LoadImage(bytes);
            quadRenderer.material.mainTexture = webcamTexture;
        };

        websocket.OnError += (e) =>
        {
            Debug.Log("Main websocket error: " + e);
        };

        if (websocket != null)
        {
            await websocket.Connect();
        }
    }

    async void ConnectPingSocket()
    {
        pingSocket = new WebSocket("ws://74.56.22.147:8764/oculus/ping");

        pingSocket.OnOpen += () =>
        {
            Debug.Log("Ping socket connected!");
        };

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

        pingSocket.OnError += (e) =>
        {
            Debug.Log("Ping socket error: " + e);
        };

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

            yield return new WaitForSeconds(0.05f);
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

#if !UNITY_WEBGL || UNITY_EDITOR
        websocket?.DispatchMessageQueue();
        pingSocket?.DispatchMessageQueue();
#endif
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
