using System;
using System.Collections;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class test : MonoBehaviour
{
    private VRControls _controls;
    
    private string _serverAddress;
    private HttpClient _client;
    private RaceCar car = new();
    
    public TMP_Text textObject;

    void Awake()
    {
        _controls = new VRControls();
        _controls.OculusTouchControllers.Enable();
    }
    
    private async Task<bool> IsServerOnline()
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            HttpResponseMessage res = await _client.GetAsync("health", cts.Token);
            return res.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            textObject.text = "IsServerOnline check failed: " + ex.Message;
            return false;
        }
    }
    
    private IEnumerator ConnectToServerCoroutine()
    {
        _client = new HttpClient
        {
            BaseAddress = new Uri($"http://{_serverAddress}"),
            Timeout = TimeSpan.FromSeconds(2)
        };

        textObject.text = "trying to connect to server";

        bool online = false;

        while (!online)
        {
            Task<bool> task = IsServerOnline();
            yield return new WaitUntil(() => task.IsCompleted);

            if (task.Exception != null)
            {
                textObject.text = "Server check failed: " + task.Exception.InnerException?.Message;
                yield return new WaitForSeconds(1f);
                continue;
            }

            online = task.Result;
            if (!online)
            {
                textObject.text = "Server is not online yet.";
                yield return new WaitForSeconds(1f);
            }
        }

        textObject.text = "server is online";

        using var clientWebSocket = new ClientWebSocket();
        var uri = new Uri($"ws://{_serverAddress}/send");

        var connectTask = clientWebSocket.ConnectAsync(uri, CancellationToken.None);
        yield return new WaitUntil(() => connectTask.IsCompleted);

        while (clientWebSocket.State == WebSocketState.Open)
        {
            textObject.text = JsonUtility.ToJson(car);

            ArraySegment<byte> buffer = new(Encoding.UTF8.GetBytes(JsonUtility.ToJson(car)));
            var sendTask = clientWebSocket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
            yield return new WaitUntil(() => sendTask.IsCompleted);

            yield return new WaitForSeconds(0.05f);
        }
    }

    
    void Start()
    {
        _serverAddress = "localhost:5000";
        StartCoroutine(ConnectToServerCoroutine());
    }

    public void SetText(String text)
    {
        textObject.text = text;
    }

    // Update is called once per frame
    void Update()
    {
        bool aButtonPressed = _controls.OculusTouchControllers.AButton.WasPressedThisFrame();
        float rightTriggerValue = _controls.OculusTouchControllers.RightTrigger.ReadValue<float>();
        float leftTriggerValue = _controls.OculusTouchControllers.LeftTrigger.ReadValue<float>();
        Vector2 thumbstick = _controls.OculusTouchControllers.RightJoyStick.ReadValue<Vector2>();
        
        car.Throttle = rightTriggerValue;
        
        /*SetText($"AButton pressed : {aButtonPressed}\n" +
                $"Right Trigger Value : {rightTriggerValue:F3}\n" +
                $"Left Trigger Value : {leftTriggerValue:F3}\n" +
                $"Right Joystick Value : X={thumbstick.x:F3}, Y={thumbstick.y:F3}");*/
    }

    void OnDisable()
    {
        _controls.OculusTouchControllers.Disable();
    }
}
