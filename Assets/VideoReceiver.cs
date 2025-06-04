using System;
using System.Text;
using UnityEngine;
using NativeWebSocket;

public class VideoReceiver : MonoBehaviour
{
    public Renderer targetRenderer;  // assign in Unity to the Quad/Plane
    private WebSocket websocket;
    private Texture2D texture;

    private void Start()
    {
        texture = new Texture2D(2, 2);
        ConnectToServer();
    }

    private async void ConnectToServer()
    {
        websocket = new WebSocket("ws://localhost:8080");

        websocket.OnMessage += (bytes) =>
        {
            try
            {
                string base64Image = Encoding.UTF8.GetString(bytes);
                byte[] imageData = Convert.FromBase64String(base64Image);

                texture.LoadImage(imageData);
                targetRenderer.material.mainTexture = texture;
            }
            catch (Exception e)
            {
                Debug.Log("Error decoding image: " + e.Message);
            }
        };

        await websocket.Connect();
    }

    private void Update()
    {
        websocket?.DispatchMessageQueue();
    }

    private async void OnApplicationQuit()
    {
        await websocket.Close();
    }
}
