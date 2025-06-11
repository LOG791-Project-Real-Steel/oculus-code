using NativeWebSocket;
using UnityEngine;
using System;
using System.Collections;

public class WebcamStreamClient : MonoBehaviour
{
    WebSocket websocket;
    Texture2D webcamTexture;
    Renderer quadRenderer;

    void Start()
    {
        quadRenderer = GetComponent<Renderer>();
        webcamTexture = new Texture2D(2, 2); // initial size
        Connect();
    }

    async void Connect()
    {
        websocket = new WebSocket("ws://10.192.160.51:8765/"); // replace with your PC IP
        // websocket = new WebSocket("ws://localhost:8765/"); // replace with your PC IP

        websocket.OnMessage += (bytes) =>
        {
            string base64 = System.Text.Encoding.UTF8.GetString(bytes);
            byte[] imageData = Convert.FromBase64String(base64);
            webcamTexture.LoadImage(imageData);
            quadRenderer.material.mainTexture = webcamTexture;
        };

        await websocket.Connect();
    }

    void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        websocket?.DispatchMessageQueue();
#endif
    }

    private async void OnApplicationQuit()
    {
        await websocket.Close();
    }
}