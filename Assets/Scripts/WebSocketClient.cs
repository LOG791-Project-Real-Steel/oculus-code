using NativeWebSocket;
using UnityEngine;
using System.Collections;
using System.Text;
using TMPro;

public class WebSocketClient : MonoBehaviour
{
    WebSocket websocket;
    Texture2D webcamTexture;
    Renderer quadRenderer;
    
    private Coroutine sendLoop;
    private Controls _controls;
    private RaceCar car = new RaceCar();
    public Renderer cylinderRenderer;

    void Start()
    {
        quadRenderer = GetComponent<Renderer>();
        webcamTexture = new Texture2D(2, 2); // Will auto-resize
        _controls = new Controls();
        _controls.OculusTouchControllers.Enable();
        Connect();
    }

    async void Connect()
    {
        //websocket = new WebSocket("ws://74.56.22.147:8765/oculus"); // home server ip
        // websocket = new WebSocket("ws://localhost:8765"); //Pour tester connecte a un serveur local
        websocket = new WebSocket("ws://localhost:5000/robot/signaling2");

        websocket.OnOpen += () =>
        {
            Debug.Log("Connection opened!");
            sendLoop = StartCoroutine(SendDataLoop());
        };
        
        websocket.OnMessage += (bytes) =>
        {
            // UTILISE LE JPEG RAW pu de conversion 
            webcamTexture.LoadImage(bytes);
            //quadRenderer.material.mainTexture = webcamTexture;
            cylinderRenderer.material.mainTexture = webcamTexture;
        };
        
        websocket.OnError += (e) =>
        {
            Debug.Log("Error! " + e);
        };

        if (websocket != null)
        {
            await websocket.Connect();
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

            yield return new WaitForSeconds(0.05f); // 100ms
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
#endif
    }

    private async void OnApplicationQuit()
    {
        if (websocket != null)
        {
            await websocket.Close();
        }
    }
}


public class FollowCameraBillboard : MonoBehaviour
{
    public Transform cameraTransform;
    public float distance = 2f;

    void LateUpdate()
    {
        if (!cameraTransform) return;

        transform.position = cameraTransform.position + cameraTransform.forward * distance;
        transform.LookAt(cameraTransform);
        transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
    }
}
