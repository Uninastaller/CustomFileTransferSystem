using Modeel.Frq;
using Modeel.Messages;
using Modeel.SSL;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Modeel
{

    public class ServerBussinesLogic : SslServer
    {
        private IWindowEnqueuer _gui;
        private readonly Dictionary<Guid, string> _clients = new Dictionary<Guid, string>();

        public ServerBussinesLogic(SslContext context, IPAddress address, int port, IWindowEnqueuer gui) : base(context, address, port)
        {
            _gui = gui;
            Start();
        }

        protected override SslSession CreateSession() { return new ServerSession(this); }

        protected override void OnError(SocketError error)
        {
            Logger.WriteLog($"Tcp server caught an error with code {error}", LoggerInfo.tcpServer);
        }

        private void OnClientDisconnected(SslSession session)
        {
            if (session is ServerSession serverSession)
            {
                serverSession.ReceiveMessage -= OnReceiveMessage;
                serverSession.ClientDisconnected -= OnClientDisconnected;
            }

            ClientStateChange(SocketState.DISCONNECTED, null, session.Id);
            _gui.BaseMsgEnque(new ClientStateChangeMessage() { Clients = _clients }); 
        }

        protected override void OnConnected(SslSession session)
        {
            if (session is ServerSession serverSession)
            {
                serverSession.ReceiveMessage += OnReceiveMessage;
                serverSession.ClientDisconnected += OnClientDisconnected;
            }

            ClientStateChange(SocketState.CONNECTED, session.Socket?.RemoteEndPoint?.ToString(), session.Id);
            _gui.BaseMsgEnque(new ClientStateChangeMessage() { Clients = _clients });
        }

        private void OnReceiveMessage(SslSession sesion, string message)
        {
            Logger.WriteLog($"Tcp server obtained a message: {message}, from: {sesion.Socket.RemoteEndPoint}", LoggerInfo.tcpServer);
        }

        private void ClientStateChange(SocketState socketState, string? client, Guid sessionId)
        {
            if (socketState == SocketState.CONNECTED && !_clients.ContainsKey(sessionId) && client != null)
            {
                _clients.Add(sessionId, client);
                Logger.WriteLog($"Client: {client}, connected to server ", LoggerInfo.tcpServer);
            }
            else if (socketState == SocketState.DISCONNECTED && _clients.ContainsKey(sessionId))
            {
                Logger.WriteLog($"Client: {_clients[sessionId]}, disconnected from server ", LoggerInfo.tcpServer);
                _clients.Remove(sessionId);
            }
        }
    }
}
