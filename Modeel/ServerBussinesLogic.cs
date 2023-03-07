using Modeel.Frq;
using Modeel.SSL;
using System;
using System.Net;
using System.Net.Sockets;

namespace Modeel
{

    public class ServerBussinesLogic : SslServer
    {
        private IWindowEnqueuer _gui;
        public ServerBussinesLogic(SslContext context, IPAddress address, int port, IWindowEnqueuer gui) : base(context, address, port)
        {
            _gui = gui;
            Start();
        }

        protected override SslSession CreateSession() { return new ServerSession(this); }

        protected override void OnError(SocketError error)
        {
            Console.WriteLine($"Chat SSL server caught an error with code {error}");
        }

        private void OnClientDisconnected(SslSession session)
        {

            if (session is ServerSession serverSession)
            {
                serverSession.ReceiveMessage -= OnReceiveMessage;
                serverSession.ClientDisconnected -= OnClientDisconnected;
            }

            ClientStateChangeMessage message = new ClientStateChangeMessage()
            {
                State = ClientState.DISCONNECTED,
                SessionId = session.Id
            };
            _gui.BaseMsgEnque(message);
        }

        protected override void OnConnected(SslSession session)
        {
            if (session is ServerSession serverSession)
            {
                serverSession.ReceiveMessage += OnReceiveMessage;
                serverSession.ClientDisconnected += OnClientDisconnected;
            }

            ClientStateChangeMessage message = new ClientStateChangeMessage()
            {
                Client = session.Socket?.RemoteEndPoint?.ToString(),
                State = ClientState.CONNECTED,
                SessionId = session.Id
            };
            _gui.BaseMsgEnque(message);

        }

        private void OnReceiveMessage(SslSession sesion, string message)
        {

        }
    }
}
