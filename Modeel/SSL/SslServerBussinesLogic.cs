using Modeel.FastTcp;
using Modeel.Frq;
using Modeel.Log;
using Modeel.Messages;
using Modeel.Model;
using Modeel.Model.Enums;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

        private int _bufferSize = 10; // Number of seconds to consider for the average transfer rate
        private List<long> _byteSendDifferentials = new List<long>(); // Circular buffer to store byte differentials
        private List<long> _byteReceivedDifferentials = new List<long>(); // Circular buffer to store byte differentials

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
                Logger.WriteLog($"Client: {client}, connected to server ", LoggerInfo.sslServer);
            }
            else if (socketState == SocketState.DISCONNECTED && _clients.ContainsKey(sessionId))
            {
                Logger.WriteLog($"Client: {_clients[sessionId]}, disconnected from server ", LoggerInfo.sslServer);
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

            _byteSendDifferentials.Insert(0, BytesSent - _secondOldBytesSent);
            _byteReceivedDifferentials.Insert(0, BytesReceived - _secondOldBytesReceived);

            if (_byteSendDifferentials.Count > _bufferSize)
            {
                _byteSendDifferentials.RemoveAt(_bufferSize);
                _byteReceivedDifferentials.RemoveAt(_bufferSize);
            }

            TransferSendRateFormatedAsText = ResourceInformer.FormatDataTransferRate(_byteSendDifferentials.Sum() / _byteSendDifferentials.Count);
            TransferReceiveRateFormatedAsText = ResourceInformer.FormatDataTransferRate(_byteReceivedDifferentials.Sum() / _byteReceivedDifferentials.Count);
            _secondOldBytesSent = BytesSent;
            _secondOldBytesReceived = BytesReceived;

        }

        private void OnReceiveMessage(SslSession sesion, string message)
        {
            Logger.WriteLog($"Ssl server obtained a message: {message}, from: {sesion.Socket.RemoteEndPoint}", LoggerInfo.socketMessage);            
        }

        /// <summary>
        /// Its called when TcpServerSesion receive FILE_REQUEST message and invoke this event 
        /// </summary>
        /// <param name="session"></param>
        /// <param name="filePath"></param>
        /// <param name="fileSize"></param>
        private void OnClientFileRequest(SslSession session, string filePath, long fileSize)
        {
            Logger.WriteLog($"Request was received for file: {filePath} with size: {fileSize}", LoggerInfo.socketMessage);

            if (File.Exists(filePath) && fileSize == new System.IO.FileInfo(filePath).Length && session is SslServerSession serverSession)
            {
                MessageBoxResult result = MessageBox.Show($"Client: {session.Socket.RemoteEndPoint} is requesting your file: {filePath}, with size of: {fileSize} bytes. \nAllow?", "Request", MessageBoxButton.YesNo, MessageBoxImage.Question);
                //MessageBoxResult result = MessageBoxResult.Yes;
                if (result == MessageBoxResult.Yes)
                {
                    ResourceInformer.GenerateAccept(session);
                    serverSession.RequestAccepted = true;
                    serverSession.FilePathOfAcceptedfileRequest = filePath;
                    return;
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
            if (_timer != null)
            {
                _timer.Elapsed -= OneSecondHandler;
                _timer.Stop();
                _timer.Dispose();
                _timer = null;
            }
            _clients = null;
            _timer = null;
            _gui?.BaseMsgEnque(new DisposeMessage(Id, TypeOfSocket.SERVER));
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
