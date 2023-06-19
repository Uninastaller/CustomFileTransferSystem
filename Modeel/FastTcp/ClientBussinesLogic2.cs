using Modeel.Frq;
using Modeel.Log;
using Modeel.Messages;
using Modeel.Model;
using Modeel.SSL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer;


namespace Modeel.FastTcp
{
    public class ClientBussinesLogic2 : TcpClient, IUniversalClientSocket, ISession
    {

        #region Properties

        public TypeOfSocket Type { get; }
        public string TransferSendRateFormatedAsText { get; private set; } = string.Empty;
        public string TransferReceiveRateFormatedAsText { get; private set; } = string.Empty;

        #endregion Properties

        #region PrivateFields

        private IWindowEnqueuer _gui;
        private bool _sessionWithCentralServer;

        private bool _stop;

        private Timer? _timer;
        private ulong _timerCounter;

        private long _secondOldBytesSent;
        private long _secondOldBytesReceived;

        private string _requestingFileName = string.Empty;
        private long _requestingFileSize;
        private bool _requestingFile;

        #endregion PrivateFields

        #region Ctor

        public ClientBussinesLogic2(IPAddress address, int port, IWindowEnqueuer gui, string fileName, long fileSize, bool sessionWithCentralServer = false) : this (address, port, gui, sessionWithCentralServer:sessionWithCentralServer)
        {
            _requestingFileName = fileName;
            _requestingFileSize = fileSize;
            _requestingFile = true;
        }

        public ClientBussinesLogic2(IPAddress address, int port, IWindowEnqueuer gui, bool sessionWithCentralServer = false) : base(address, port)
        {
            Type = TypeOfSocket.TCP;

            _sessionWithCentralServer = sessionWithCentralServer;

            ConnectAsync();

            _gui = gui;

            _timer = new Timer(1000); // Set the interval to 1 second
            _timer.Elapsed += OneSecondHandler;
            _timer.Start();
        }

        #endregion Ctor

        #region PublicMethods

        public void DisconnectAndStop()
        {
            _stop = true;

            DisconnectAsync();

            if (Socket.Connected)
            {
                Socket.Shutdown(SocketShutdown.Both);
            }

            if (_timer != null)
            {
                _timer.Elapsed -= OneSecondHandler;
                _timer.Stop();
                _timer.Dispose();
                _timer = null;
            }

            while (IsConnected)
                Thread.Yield();
        }

        #endregion PublicMethods

        #region PrivateMethods



        #endregion PrivateMethods

        #region EventHandler

        private void OneSecondHandler(object? sender, ElapsedEventArgs e)
        {
            _timerCounter++;

            TransferSendRateFormatedAsText = ResourceInformer.FormatDataTransferRate(BytesSent - _secondOldBytesSent);
            TransferReceiveRateFormatedAsText = ResourceInformer.FormatDataTransferRate(BytesReceived - _secondOldBytesReceived);
            _secondOldBytesSent = BytesSent;
            _secondOldBytesReceived = BytesReceived;
        }

        #endregion EventHandler

        #region OverridedMethods

        protected override async void OnConnected()
        {
            Logger.WriteLog($"Tcp client connected a new session with Id {Id}", LoggerInfo.tcpClient);

            _gui.BaseMsgEnque(new SocketStateChangeMessage() { SocketState = SocketState.CONNECTED, SessionWithCentralServer = _sessionWithCentralServer });

            if (_requestingFile)
            {
                await Task.Delay(1000);
                ResourceInformer.GenerateRequest(_requestingFileName, _requestingFileSize, this);
            }
        }

        protected override void OnDisconnected()
        {
            Logger.WriteLog($"Tcp client disconnected from session with Id: {Id}", LoggerInfo.tcpClient);

            // Wait for a while...
            Thread.Sleep(1000);

            // Try to connect again
            if (!_stop)
                ConnectAsync();

            _gui.BaseMsgEnque(new SocketStateChangeMessage() { SocketState = SocketState.DISCONNECTED, SessionWithCentralServer = _sessionWithCentralServer });
        }

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            string message = Encoding.UTF8.GetString(buffer, (int)offset, (int)size);

            _gui.BaseMsgEnque(new MessageReceiveMessage() { Message = message });

            //Logger.WriteLog($"Tcp client obtained a message[{size}]: {message}", LoggerInfo.socketMessage);
            Logger.WriteLog($"Tcp client obtained a message[{size}]", LoggerInfo.socketMessage);
        }

        protected override void OnError(SocketError error)
        {
            Logger.WriteLog($"Tcp client caught an error with code {error}", LoggerInfo.tcpClient);
        }

        #endregion OverridedMethods             

    }
}

