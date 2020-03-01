using System;
using System.Threading;
using WebSocketSharp;
using WebSocketSharp.Server;

class WSServer
{
    private WebSocketServer wss;
    private SynchronizationContext context;

    public delegate void dlgOnClientConnect(string id, WebSocket ws);
    public delegate void dlgOnTextData(WebSocket ws, string data);
    public delegate void dlgOnBinaryData(WebSocket ws, byte[] data);
    public delegate void dlgOnClientClose(string id);
    public delegate void dlgOnClientError(string id, Exception e);

    public event dlgOnClientConnect OnClientConnect;
    public event dlgOnTextData OnTextData;
    public event dlgOnBinaryData OnBinaryData;
    public event dlgOnClientClose OnClientClose;
    public event dlgOnClientError OnClientError;

    private class WSBehaviour : WebSocketBehavior
    {
        public delegate void dlgOnClientConnect(string id, WebSocket ws);
        public delegate void dlgOnClientData(WebSocket ws, MessageEventArgs e);
        public delegate void dlgOnClientClose(string id, CloseEventArgs e);
        public delegate void dlgOnClientError(string id, ErrorEventArgs e);

        public event dlgOnClientConnect OnClientConnect;
        public event dlgOnClientData OnClientData;
        public event dlgOnClientClose OnClientClose;
        public event dlgOnClientError OnClientError;

        protected override void OnOpen()
        {
            OnClientConnect.Invoke(ID, Context.WebSocket);
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            OnClientData.Invoke(Context.WebSocket, e);
        }

        protected override void OnClose(CloseEventArgs e)
        {
            OnClientClose(ID, e);
        }

        protected override void OnError(ErrorEventArgs e)
        {
            OnClientError.Invoke(ID, e);
        }
    }

    public WSServer(int port)
    {
        context = SynchronizationContext.Current;

        wss = new WebSocketServer(port);
        wss.AddWebSocketService<WSBehaviour>("/", behaviour =>
        {
            behaviour.OnClientConnect += (id, ws) =>
            {
                context.Post(_ =>
                {
                    OnClientConnect.Invoke(id, ws);
                }, null);
            };
            behaviour.OnClientData += (ws, e) =>
            {
                context.Post(_ =>
                {
                    if (e.IsText)
                        OnTextData.Invoke(ws, e.Data);
                    else if (e.IsBinary)
                        OnBinaryData.Invoke(ws, e.RawData);
                }, null);
            };
            behaviour.OnClientClose += (ws, e) =>
            {
                context.Post(_ =>
                {
                    OnClientClose.Invoke(ws);
                }, null);
            };
            behaviour.OnClientError += (id, e) =>
            {
                context.Post(_ =>
                {
                    OnClientError.Invoke(id, e.Exception);
                }, null);
            };
        });
    }

    public void Start()
    {
        wss.Start();
    }

    public void Stop()
    {
        wss.Stop();
    }
}

public class SignalingMessage
{
    public string type;
    public string sdp;
    public string candidate;
    public string sdpMid;
    public int sdpMLineIndex;

    public SignalingMessage(string type, string sdp)
    {
        this.type = type;
        this.sdp = sdp;
    }

    public SignalingMessage(string candidate, string sdpMid, int sdpMLineIndex)
    {
        this.type = "candidate";
        this.candidate = candidate;
        this.sdpMid = sdpMid;
        this.sdpMLineIndex = sdpMLineIndex;
    }
}
