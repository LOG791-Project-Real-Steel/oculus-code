using UnityEngine;
using UnityEngine.UI;
using Unity.WebRTC;
using System.Text;
using System;
using System.Collections;
using NativeWebSocket;

public class WebRTCReceiver : MonoBehaviour
{
    [Header("Target UI to show stream")]
    public RawImage targetDisplay;

    private RTCPeerConnection peerConnection;
    private VideoStreamTrack videoTrack;
    private WebSocket websocket;

    void Start()
    {
        SetupConnection();
        ConnectToSignalingServer();
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
            if (e.Track is VideoStreamTrack track)
            {
                videoTrack = track;
                videoTrack.OnVideoReceived += tex =>
                {
                    targetDisplay.texture = tex;
                };
            }
        };

        peerConnection.OnIceCandidate = candidate =>
        {
            if (websocket != null && websocket.State == WebSocketState.Open)
            {
                var message = new
                {
                    type = "ice",
                    candidate = candidate.Candidate,
                    sdpMid = candidate.SdpMid,
                    sdpMLineIndex = candidate.SdpMLineIndex
                };
                websocket.SendText(JsonUtility.ToJson(message));
            }
        };
    }

    private async void ConnectToSignalingServer()
    {
        websocket = new WebSocket("ws://localhost:8888");

        websocket.OnOpen += () =>
        {
            Debug.Log("Connected to signaling server");
            websocket.SendText("{\"role\": \"unity\"}");
        };

        websocket.OnMessage += (bytes) =>
        {
            var json = Encoding.UTF8.GetString(bytes);
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
            else if (msg.type == "ice")
            {
                var candidate = new RTCIceCandidate(new RTCIceCandidateInit
                {
                    candidate = msg.candidate,
                    sdpMid = msg.sdpMid,
                    sdpMLineIndex = msg.sdpMLineIndex
                });
                peerConnection.AddIceCandidate(candidate);
            }
        };

        websocket.OnError += (e) => Debug.LogError("WebSocket error: " + e);
        websocket.OnClose += (e) => Debug.Log("WebSocket closed");

        await websocket.Connect();
    }

    private IEnumerator HandleOffer(RTCSessionDescription offerDesc)
    {
        var op = peerConnection.SetRemoteDescription(ref offerDesc);
        yield return new WaitUntil(() => op.IsDone);

        if (op.IsError)
        {
            Debug.LogError("SetRemoteDescription failed: " + op.Error.message);
            yield break;
        }

        var answerOp = peerConnection.CreateAnswer();
        yield return new WaitUntil(() => answerOp.IsDone);

        if (answerOp.IsError)
        {
            Debug.LogError("CreateAnswer failed: " + answerOp.Error.message);
            yield break;
        }

        var answer = answerOp.Desc;

        var setLocalOp = peerConnection.SetLocalDescription(ref answer);
        yield return new WaitUntil(() => setLocalOp.IsDone);

        if (setLocalOp.IsError)
        {
            Debug.LogError("SetLocalDescription failed: " + setLocalOp.Error.message);
            yield break;
        }

        var answerMsg = new SignalingMessage
        {
            type = "answer",
            sdp = answer.sdp
        };

        websocket.SendText(JsonUtility.ToJson(answerMsg));
    }

    private void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        websocket?.DispatchMessageQueue();
#endif
    }

    private void OnDestroy()
    {
        videoTrack?.Dispose();
        peerConnection?.Close();
        peerConnection?.Dispose();
    }

    [Serializable]
    public class SignalingMessage
    {
        public string type;
        public string sdp;
        public string candidate;
        public string sdpMid;
        public int sdpMLineIndex;
    }
}
