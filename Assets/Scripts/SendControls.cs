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

public class SendControls : MonoBehaviour
{
    private VRControls _controls;
    private HttpClient _client;
    private RaceCar car = new RaceCar();
    
    public string serverAddress;
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
            BaseAddress = new Uri($"http://{serverAddress}"),
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
        var uri = new Uri($"ws://{serverAddress}/oculus");

        var connectTask = clientWebSocket.ConnectAsync(uri, CancellationToken.None);
        yield return new WaitUntil(() => connectTask.IsCompleted);

        while (clientWebSocket.State == WebSocketState.Open)
        {
            SetText(JsonUtility.ToJson(car));

            ArraySegment<byte> buffer = new(Encoding.UTF8.GetBytes(JsonUtility.ToJson(car)));
            var sendTask = clientWebSocket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
            yield return new WaitUntil(() => sendTask.IsCompleted);

            yield return new WaitForSeconds(0.05f);
        }
    }

    
    void Start()
    {
        StartCoroutine(ConnectToServerCoroutine());
    }

    public void SetText(String text)
    {
        textObject.text = text;
    }

    // Update is called once per frame
    void Update()
    {
        float rightTriggerValue = _controls.OculusTouchControllers.RightTrigger.ReadValue<float>();
        float leftTriggerValue = _controls.OculusTouchControllers.LeftTrigger.ReadValue<float>();
        Vector2 thumbstick = _controls.OculusTouchControllers.RightJoyStick.ReadValue<Vector2>();

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

    void OnDisable()
    {
        _controls.OculusTouchControllers.Disable();
    }
}
