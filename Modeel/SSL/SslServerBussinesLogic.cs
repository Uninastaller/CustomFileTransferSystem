using Modeel.Frq;
using Modeel.Log;
using Modeel.Messages;
using Modeel.Model;
using Modeel.Model.Enums;
using System;
using System.Collections.Generic;
using System.Configuration;
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

        public TypeOfServerSocket Type { get; }
        public string TransferSendRateFormatedAsText => ResourceInformer.FormatDataTransferRate(TransferSendRate);
        public string TransferReceiveRateFormatedAsText => ResourceInformer.FormatDataTransferRate(TransferReceiveRate);
        public long TransferSendRate { get; private set; }
        public long TransferReceiveRate { get; private set; }

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

        public SslServerBussinesLogic(SslContext context, IPAddress address, int port, IWindowEnqueuer gui, int optionReceiveBufferSize = 0x200000, int optionSendBufferSize = 0x200000, int optionAcceptorBacklog = 1024) : base(context, address, port, optionReceiveBufferSize, optionSendBufferSize, optionAcceptorBacklog)
        {
            Type = TypeOfServerSocket.TCP_SERVER_SSL;

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

        private void ClientStateChange(SocketState socketState, string? client, Guid sessionId)
        {
            if (_clients == null) return;

            if (socketState == SocketState.CONNECTED && !_clients.ContainsKey(sessionId) && client != null)
            {
                _clients.Add(sessionId, client);
                Logger.WriteLog(LogLevel.DEBUG, $"Client: {client}, connected to server");
            }
            else if (socketState == SocketState.DISCONNECTED && _clients.ContainsKey(sessionId))
            {
                Logger.WriteLog(LogLevel.DEBUG, $"Client: {_clients[sessionId]}, disconnected from server");
                _clients.Remove(sessionId);
            }
        }

        #endregion PrivateMethods

        #region ProtectedMethods



        #endregion ProtectedMethods

        #region EventHandler

        private void OneSecondHandler(object? sender, ElapsedEventArgs e)
        {
            _timerCounter++;

            TransferSendRate = BytesSent - _secondOldBytesSent;
            TransferReceiveRate = BytesReceived - _secondOldBytesReceived;
            _secondOldBytesSent = BytesSent;
            _secondOldBytesReceived = BytesReceived;
        }

        private void OnReceiveMessage(SslSession sesion, string message)
        {
            Logger.WriteLog(LogLevel.DEBUG, $"Ssl server obtained a message: {message}, from: {sesion.Socket.RemoteEndPoint}");
        }

        /// <summary>
        /// Its called when TcpServerSesion receive FILE_REQUEST message and invoke this event 
        /// </summary>
        /// <param name="session"></param>
        /// <param name="filePath"></param>
        /// <param name="fileSize"></param>
        private void OnClientFileRequest(SslSession session, string filePath, long fileSize)
        {
            Logger.WriteLog(LogLevel.DEBUG, $"Request was received for file: {filePath} with size: {fileSize}");

            string? uploadingDirectory = ConfigurationManager.AppSettings["UploadingDirectory"];
            if (uploadingDirectory != null)
            {
                if (!Directory.Exists(uploadingDirectory))
                {
                    Directory.CreateDirectory(uploadingDirectory);
                }

                filePath = $@"{uploadingDirectory}\{Path.GetFileName(filePath)}";

                if (File.Exists(filePath) && fileSize == new System.IO.FileInfo(filePath).Length && session is SslServerSession serverSession)
                {
                    //MessageBoxResult result = MessageBox.Show($"Client: {session.Socket.RemoteEndPoint} is requesting your file: {filePath}, with size of: {fileSize} bytes. \nAllow?", "Request", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    MessageBoxResult result = MessageBoxResult.Yes;
                    if (result == MessageBoxResult.Yes)
                    {
                        ResourceInformer.GenerateAccept(session);
                        serverSession.RequestAccepted = true;
                        serverSession.FilePathOfAcceptedfileRequest = filePath;
                        return;
                    }
                }
            }

            ResourceInformer.GenerateReject(session);
            session.Disconnect();
            session.Dispose();
        }

        #endregion EventHandler

        #region OverridedMethods

        protected override void OnDispose()
        {
            _gui?.BaseMsgEnque(new DisposeMessage(Id, TypeOfSocket.SERVER));

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
            Logger.WriteLog(LogLevel.ERROR, $"Ssl server caught an error with code {error}");
        }

        private void OnClientDisconnected(SslSession session)
        {
            if (session is SslServerSession serverSession)
            {
                serverSession.ReceiveMessage -= OnReceiveMessage;
                serverSession.ClientDisconnected -= OnClientDisconnected;
                serverSession.ClientFileRequest -= OnClientFileRequest;
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
                serverSession.ClientFileRequest += OnClientFileRequest;
            }

            ClientStateChange(SocketState.CONNECTED, session.Socket?.RemoteEndPoint?.ToString(), session.Id);
            if (_clients != null && _gui != null)
                _gui.BaseMsgEnque(new ClientStateChangeMessage() { Clients = _clients });
        }

        #endregion OverridedMethods

    }
}
