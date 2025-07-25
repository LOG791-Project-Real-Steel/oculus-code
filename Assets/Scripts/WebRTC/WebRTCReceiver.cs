using UnityEngine;
using Unity.WebRTC;
using System;
using System.Collections;
using System.Text;
using NativeWebSocket;

public class WebRTCReceiver : MonoBehaviour
{
    [Header("Target texture (Oculus-compatible)")]
    public Renderer targetRenderer;

    private RTCPeerConnection peerConnection;
    private VideoStreamTrack videoTrack;
    private WebSocket websocket;

    private bool isConnected = false;

    private void Start()
    {
        Debug.Log("🟢 WebRTCReceiver starting...");
        SetupConnection();
        StartCoroutine(RetryConnect());
    }

    private void SetupConnection()
    {
        var config = new RTCConfiguration
        {
            iceServers = new[] {
                new RTCIceServer { urls = new[] { "stun:stun.l.google.com:19302" } }
            }
        };

        peerConnection = new RTCPeerConnection(ref config);

        peerConnection.OnTrack = e =>
        {
            Debug.Log($"🎥 Track received: {e.Track.Kind}");

            if (e.Track is VideoStreamTrack track)
            {
                videoTrack = track;
                videoTrack.OnVideoReceived += tex =>
                {
                    Debug.Log("📷 Video frame received.");
                    if (targetRenderer != null && tex != null)
                    {
                        targetRenderer.material.mainTexture = tex;
                    }
                };
            }
        };

        peerConnection.OnIceCandidate = candidate =>
        {
            if (websocket != null && websocket.State == WebSocketState.Open)
            {
                var message = new
                {
                    to = "robot",
                    type = "candidate",
                    data = new
                    {
                        candidate = candidate.Candidate,
                        sdpMid = candidate.SdpMid,
                        sdpMLineIndex = candidate.SdpMLineIndex
                    }
                };
                websocket.SendText(JsonUtility.ToJson(message));
                Debug.Log("❄ ICE candidate sent.");
            }
        };
    }

    private IEnumerator RetryConnect()
    {
        while (!isConnected)
        {
            Debug.Log("🔁 Attempting to connect to signaling server...");
            ConnectToSignalingServer();
            yield return new WaitForSeconds(2);
        }
    }

    private async void ConnectToSignalingServer()
    {
        websocket = new WebSocket("ws://localhost:5000/robot/signaling2");

        websocket.OnOpen += () =>
        {
            Debug.Log("✅ WebSocket connection opened.");
            websocket.SendText("{\"role\": \"unity\"}");
            isConnected = true;
        };

        websocket.OnMessage += (bytes) =>
        {
            var json = Encoding.UTF8.GetString(bytes);
            Debug.Log("📨 Message received: " + json);
            var msg = JsonUtility.FromJson<SignalingMessage>(json);

            if (msg.type == "offer")
            {
                var desc = new RTCSessionDescription
                {
                    type = RTCSdpType.Offer,
                    sdp = msg.sdp
                };

                StartCoroutine(HandleOffer(desc));
            }
            else if (msg.type == "candidate")
            {
                var candidate = new RTCIceCandidate(new RTCIceCandidateInit
                {
                    candidate = msg.data.candidate,
                    sdpMid = msg.data.sdpMid,
                    sdpMLineIndex = msg.data.sdpMLineIndex
                });
                peerConnection.AddIceCandidate(candidate);
                Debug.Log("📡 ICE candidate added.");
            }
        };

        websocket.OnClose += (e) =>
        {
            Debug.Log("🔌 WebSocket closed");
            isConnected = false;
        };

        websocket.OnError += (e) =>
        {
            Debug.LogError("❌ WebSocket error: " + e);
            isConnected = false;
        };

        await websocket.Connect();
    }

    private IEnumerator HandleOffer(RTCSessionDescription offerDesc)
    {
        Debug.Log("📥 Handling SDP offer...");
        var op = peerConnection.SetRemoteDescription(ref offerDesc);
        yield return new WaitUntil(() => op.IsDone);

        if (op.IsError)
        {
            Debug.LogError("❌ SetRemoteDescription failed: " + op.Error.message);
            yield break;
        }

        var answerOp = peerConnection.CreateAnswer();
        yield return new WaitUntil(() => answerOp.IsDone);

        if (answerOp.IsError)
        {
            Debug.LogError("❌ CreateAnswer failed: " + answerOp.Error.message);
            yield break;
        }

        var answer = answerOp.Desc;

        var setLocalOp = peerConnection.SetLocalDescription(ref answer);
        yield return new WaitUntil(() => setLocalOp.IsDone);

        if (setLocalOp.IsError)
        {
            Debug.LogError("❌ SetLocalDescription failed: " + setLocalOp.Error.message);
            yield break;
        }

        var answerMsg = new
        {
            to = "robot",
            type = "answer",
            sdp = answer.sdp
        };

        websocket.SendText(JsonUtility.ToJson(answerMsg));
        Debug.Log("📤 Sent SDP answer.");
    }

    private void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        websocket?.DispatchMessageQueue();
#endif
    }

    private void OnDestroy()
    {
        Debug.Log("🧹 Cleaning up...");
        videoTrack?.Dispose();
        peerConnection?.Close();
        peerConnection?.Dispose();
    }

    [Serializable]
    public class SignalingMessage
    {
        public string type;
        public string sdp;
        public CandidateData data;
    }

    [Serializable]
    public class CandidateData
    {
        public string candidate;
        public string sdpMid;
        public int sdpMLineIndex;
    }
}
