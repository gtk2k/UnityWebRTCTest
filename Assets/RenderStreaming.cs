using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;
using Unity.WebRTC;
using UnityEngine;
using WebSocketSharp;

public class RenderStreaming : MonoBehaviour
{
#pragma warning disable 0649

    [SerializeField, Tooltip("Array to set your own STUN/TURN servers")]
    private RTCIceServer[] iceServers = new RTCIceServer[]
    {
        new RTCIceServer()
        {
            urls = new string[] { "stun:stun.l.google.com:19302" }
        }
    };

    [SerializeField, Tooltip("Streaming size should match display aspect ratio")]
    private Vector2Int streamingSize = new Vector2Int(1920, 1080);

    [SerializeField, Tooltip("Camera to capture video stream")]
    private Camera captureCamera;

#pragma warning restore 0649

    private RTCConfiguration conf;
    private MediaStream videoStream;
    //private MediaStream audioStream;
    private RTCPeerConnection pc;
    private WSServer wss;
    private WebSocket ws;
    private Log log;

    private RTCOfferOptions offerOptions = new RTCOfferOptions
    {
        iceRestart = false,
        offerToReceiveAudio = false,
        offerToReceiveVideo = false
    };

    public void Awake()
    {
        WebRTC.Initialize(EncoderType.Software);
        log = new Log();

        wss = new WSServer(8989);
        wss.OnClientConnect += onClientConnect;
        wss.OnTextData += onTextData;
        wss.OnBinaryData += onBinaryData;
        wss.OnClientClose += onClientClose;
        wss.OnClientError += onClientError;
        wss.Start();
    }

    public void OnDestroy()
    {
        WebRTC.Finalize();
        Audio.Stop();
        ws = null;
        wss.Stop();
    }

    void Start()
    {
        if (captureCamera == null)
        {
            captureCamera = Camera.main;
        }
        videoStream = captureCamera.CaptureStream(streamingSize.x, streamingSize.y, RenderTextureDepth.DEPTH_24);
        //audioStream = Audio.CaptureStream();

        conf = default;
        conf.iceServers = iceServers;
        StartCoroutine(WebRTC.Update());
    }

    private void onClientConnect(string id, WebSocket ws)
    {
        log.Print($"client connect: {id}");
        this.ws = ws;
        setupPeer(ws);
    }

    private void onTextData(WebSocket client, string data)
    {
        var msg = JsonUtility.FromJson<SignalingMessage>(data);
        if (msg.type == "candidate")
        {
            log.Print("on ice candidate");
            RTCIceCandidate candidate;
            candidate.candidate = msg.candidate;
            candidate.sdpMid = msg.sdpMid;
            candidate.sdpMLineIndex = msg.sdpMLineIndex;
            pc.AddIceCandidate(ref candidate);
        }
        else if (msg.type == "answer")
        {
            log.Print("on answer");
            StartCoroutine(proccessAnswer(msg.sdp));
        }
    }

    private void onBinaryData(WebSocket ws, byte[] data)
    {
    }

    private void onClientClose(string id)
    {
        log.Print($"WS Close: {id}");
    }

    private void onClientError(string id, System.Exception e)
    {
        log.Print($"WS Error: id:{id}, error:{e.Message}");
    }

    void setupPeer(WebSocket client)
    {
        pc = new RTCPeerConnection(ref conf);
        pc.OnIceCandidate = candidate =>
        {
            log.Print($"onIceCandidate");
            var sm = new SignalingMessage(candidate.candidate, candidate.sdpMid, candidate.sdpMLineIndex);
            var msg = JsonUtility.ToJson(sm);
            ws.Send(msg);
        };
        foreach (var track in videoStream.GetTracks())
            pc.AddTrack(track);
        //foreach (var track in audioStream.GetTracks())
        //    pc.AddTrack(track);
        StartCoroutine(proccessOffer(client));
    }

    IEnumerator proccessOffer(WebSocket client)
    {
        offerOptions.offerToReceiveAudio = false;
        offerOptions.offerToReceiveVideo = false;
        Debug.Log("Create Offer");
        var op = pc.CreateOffer(ref offerOptions);
        yield return op;

        if (!op.isError)
        {
            var ret = pc.SetLocalDescription(ref op.desc);
            yield return ret;

            if (ret.isError)
                log.Print($"offer setLocalDescription error:{ret.error}");
            else
            {
                var offer = new SignalingMessage("offer", op.desc.sdp);
                var msg = JsonUtility.ToJson(offer);
                Debug.Log("Send Offer");
                client.Send(msg);
            }
        }
        else
        {
            log.Print("create offer error");
        }
    }

    IEnumerator proccessAnswer(string sdp)
    {
        //string pattern = @"(a=fmtp:\d+ .*level-asymmetry-allowed=.*)\r\n";
        //sdp = Regex.Replace(sdp, pattern, "$1;x-google-start-bitrate=16000;x-google-max-bitrate=160000\r\n");
        RTCSessionDescription answer = default;
        answer.type = RTCSdpType.Answer;
        answer.sdp = sdp;
        Debug.Log("Set Remote Answer");
        var ret = pc.SetRemoteDescription(ref answer);
        yield return ret; // ****

        if (ret.isError)
        {
            log.Print($"processAnser error:{ret.error}");
        }
        else
        {
            Debug.Log("Set Remote Answer Success");
        }
    }

    private void Update()
    {
        //var t = typeof(CameraExtension);
        //var fInfo = t.GetField("started", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.GetField);
        //var val = fInfo.GetValue(t);
        //Debug.Log($"CameraExtension.started:{val}");
    }
}