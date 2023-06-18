using Modeel.FastTcp;
using Modeel.Frq;
using Modeel.Log;
using Modeel.Messages;
using Modeel.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Timers;
using System.Windows;
using Timer = System.Timers.Timer;


namespace Modeel.SSL
{
    public class SslServerBussinesLogic : SslServer, IUniversalServerSocket
    {

        #region Properties

        public TypeOfSocket Type { get; }
        public string TransferSendRateFormatedAsText { get; private set; } = string.Empty;
        public string TransferReceiveRateFormatedAsText { get; private set; } = string.Empty;

        #endregion Properties

        #region PublicFields



        #endregion PublicFields

        #region PrivateFields

        private IWindowEnqueuer? _gui;
        /// <summary>
        /// value: IpAddress
        /// </summary>
        private Dictionary<Guid, string>? _clients = new Dictionary<Guid, string>();

        private Timer? _timer;
        private ulong _timerCounter;

        private long _secondOldBytesSent;
        private long _secondOldBytesReceived;

        #endregion PrivateFields

        #region Ctor

        public SslServerBussinesLogic(SslContext context, IPAddress address, int port, IWindowEnqueuer gui, int optionAcceptorBacklog = 1024) : base(context, address, port, optionAcceptorBacklog)
        {
            Type = TypeOfSocket.TCP_SSL;

            _gui = gui;
            Start();

            _timer = new Timer(1000); // Set the interval to 1 second
            _timer.Elapsed += OneSecondHandler;
            _timer.Start();
        }

        #endregion Ctor

        #region PublicMethods



        #endregion PublicMethods

        #region PrivateMethods

        private void TestMessage()
        {
            //Multicast("Hellou from SSlServerBussinesLoggic[1s]");  //async
        }

        private void ClientStateChange(SocketState socketState, string? client, Guid sessionId)
        {
            if (_clients == null) return;

            if (socketState == SocketState.CONNECTED && !_clients.ContainsKey(sessionId) && client != null)
            {
                _clients.Add(sessionId, client);
                Logger.WriteLog($"Client: {client}, connected to server ", LoggerInfo.sslServer);
            }
            else if (socketState == SocketState.DISCONNECTED && _clients.ContainsKey(sessionId))
            {
                Logger.WriteLog($"Client: {_clients[sessionId]}, disconnected from server ", LoggerInfo.sslServer);
                _clients.Remove(sessionId);
            }
        }

        private void Test1BigFile(SslSession session)
        {
            ResourceInformer.SendFile("C:\\Users\\tomas\\Downloads\\The.Office.US.S05.Season.5.Complete.720p.NF.WEB.x264-maximersk [mrsktv]\\The.Office.US.S05E15.720p.NF.WEB.x264-MRSK.mkv", session);
        }

        #endregion PrivateMethods

        #region ProtectedMethods



        #endregion ProtectedMethods

        #region EventHandler

        private void OneSecondHandler(object? sender, ElapsedEventArgs e)
        {
            _timerCounter++;

            TransferSendRateFormatedAsText = ResourceInformer.FormatDataTransferRate(BytesSent - _secondOldBytesSent);
            TransferReceiveRateFormatedAsText = ResourceInformer.FormatDataTransferRate(BytesReceived - _secondOldBytesReceived);
            _secondOldBytesSent = BytesSent;
            _secondOldBytesReceived = BytesReceived;

            TestMessage();
        }

        private void OnReceiveMessage(SslSession sesion, string message)
        {
            Logger.WriteLog($"Tcp server obtained a message: {message}, from: {sesion.Socket.RemoteEndPoint}", LoggerInfo.socketMessage);            
        }

        #endregion EventHandler

        #region OverridedMethods

        protected override void OnDispose()
        {
            if (_timer != null)
            {
                _timer.Elapsed -= OneSecondHandler;
                _timer.Stop();
                _timer.Dispose();
                _timer = null;
            }
            _clients = null;
            _timer = null;
            _gui = null;
        }

        protected override SslSession CreateSession() { return new SslServerSession(this); }

        protected override void OnError(SocketError error)
        {
            Logger.WriteLog($"Ssl server caught an error with code {error}", LoggerInfo.tcpServer);
        }

        private void OnClientDisconnected(SslSession session)
        {
            if (session is SslServerSession serverSession)
            {
                serverSession.ReceiveMessage -= OnReceiveMessage;
                serverSession.ClientDisconnected -= OnClientDisconnected;
            }

            ClientStateChange(SocketState.DISCONNECTED, null, session.Id);
            if (_clients != null && _gui != null)
                _gui.BaseMsgEnque(new ClientStateChangeMessage() { Clients = _clients });
        }

        protected override void OnConnected(SslSession session)
        {
            if (session is SslServerSession serverSession)
            {
                serverSession.ReceiveMessage += OnReceiveMessage;
                serverSession.ClientDisconnected += OnClientDisconnected;

                Test1BigFile(session);
            }

            ClientStateChange(SocketState.CONNECTED, session.Socket?.RemoteEndPoint?.ToString(), session.Id);
            if (_clients != null && _gui != null)
                _gui.BaseMsgEnque(new ClientStateChangeMessage() { Clients = _clients });
        }

        #endregion OverridedMethods

    }
}
