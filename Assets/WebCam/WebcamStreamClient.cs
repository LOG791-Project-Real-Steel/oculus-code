using NativeWebSocket;
using UnityEngine;
using System.Collections;

public class WebcamStreamClient : MonoBehaviour
{
    WebSocket websocket;
    Texture2D webcamTexture;
    Renderer quadRenderer;

    void Start()
    {
        quadRenderer = GetComponent<Renderer>();
        webcamTexture = new Texture2D(2, 2); // Will auto-resize
        Connect();
    }

    async void Connect()
    {
        websocket = new WebSocket("ws://74.56.22.147:8765/"); // home server ip

        websocket.OnMessage += (bytes) =>
        {
            // UTILISE LE JPEG RAW pu de conversion 
            webcamTexture.LoadImage(bytes);
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
