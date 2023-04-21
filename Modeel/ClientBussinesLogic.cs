using Modeel.Frq;
using Modeel.Messages;
using Modeel.SSL;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;

namespace Modeel
{
    public class ClientBussinesLogic : SslClient, IUniversalClientSocket
    {
        private IWindowEnqueuer _gui;
        private bool _sessionWithCentralServer;

        public ClientBussinesLogic(SslContext context, IPAddress address, int port, IWindowEnqueuer gui, bool sessionWithCentralServer = false) : base(context, address, port)
        {
            _sessionWithCentralServer = sessionWithCentralServer;

            //Connect();
            ConnectAsync();

            _gui = gui;
        }

        public void DisconnectAndStop()
        {   
            _stop = true;
            DisconnectAsync();
            while (IsConnected)
                Thread.Yield();
        }

        protected override void OnConnected()
        {
            Logger.WriteLog($"Tcp client connected a new session with Id {Id}", LoggerInfo.tcpClient);

            if(_sessionWithCentralServer)
            _gui.BaseMsgEnque(new SocketStateChangeMessage() { SocketState = SocketState.CONNECTED });
        }

        protected override void OnHandshaked()
        {
            Logger.WriteLog($"Tcp client handshaked a new session with Id {Id}", LoggerInfo.tcpClient);
            Send("Hello from SSL client!");
        }

        protected override void OnDisconnected()
        {
            Logger.WriteLog($"Tcp client disconnected from session with Id: {Id}", LoggerInfo.tcpClient);

            // Wait for a while...
            Thread.Sleep(1000);

            // Try to connect again
            if (!_stop)
                ConnectAsync();

            if(_sessionWithCentralServer)
            _gui.BaseMsgEnque(new SocketStateChangeMessage() { SocketState = SocketState.DISCONNECTED });
        }

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            string message = Encoding.UTF8.GetString(buffer, (int)offset, (int)size);
            Logger.WriteLog($"Tcp client obtained a message: HAHA, from: {Endpoint}", LoggerInfo.socketMessage);
        }

        protected override void OnError(SocketError error)
        {
            Logger.WriteLog($"Tcp client caught an error with code {error}", LoggerInfo.tcpClient);
        }

        private bool _stop;
    }
}

