using Modeel.Frq;
using Modeel.Log;
using Modeel.Messages;
using Modeel.Model;
using Modeel.Model.Enums;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Timers;
using System.Windows;
using Timer = System.Timers.Timer;


namespace Modeel.FastTcp
{
    public class ClientBussinesLogic2 : TcpClient, IUniversalClientSocket, ISession
    {

        #region Properties

        public string IpAndPort => Socket.LocalEndPoint?.ToString() ?? string.Empty;
        public TypeOfClientSocket Type { get; }
        public string TransferSendRateFormatedAsText { get; private set; } = string.Empty;
        public string TransferReceiveRateFormatedAsText { get; private set; } = string.Empty;
        public ClientBussinesLogicState State
        {
            get
            {
                return _state;
            }
            set
            {
                _state = value;
            }
        }

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

        private readonly FileReceiver? _fileReceiver;

        private ClientBussinesLogicState _state = ClientBussinesLogicState.NONE;

        private long _assignedFilePart;

        #endregion PrivateFields

        #region Ctor

        public ClientBussinesLogic2(IPAddress address, int port, IWindowEnqueuer gui, string fileName, long fileSize, FileReceiver fileReceiver, int optionReceiveBufferSize = 0x200000, int optionSendBufferSize = 0x200000, bool sessionWithCentralServer = false)
            : this(address, port, gui, optionReceiveBufferSize: optionReceiveBufferSize, optionSendBufferSize: optionSendBufferSize, sessionWithCentralServer: sessionWithCentralServer)
        {
            _requestingFileName = fileName;
            _requestingFileSize = fileSize;
            _fileReceiver = fileReceiver;
            //RequestingFile = true;

            State = ClientBussinesLogicState.REQUESTING_FILE;
        }

        public ClientBussinesLogic2(IPAddress address, int port, IWindowEnqueuer gui, int optionReceiveBufferSize = 8192, int optionSendBufferSize = 8192, bool sessionWithCentralServer = false) : base(address, port, optionReceiveBufferSize, optionSendBufferSize)
        {
            Type = TypeOfClientSocket.TCP_CLIENT;

            _sessionWithCentralServer = sessionWithCentralServer;

            _flagSwitch.OnNonRegistered(OnNonRegistredMessage);
            _flagSwitch.Register(SocketMessageFlag.REJECT, OnRejectHandler);
            _flagSwitch.Register(SocketMessageFlag.ACCEPT, OnAcceptHandler);
            _flagSwitch.Register(SocketMessageFlag.FILE_PART, OnFilePartHandler);

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

        private void RequestFilePart()
        {
            _assignedFilePart = _fileReceiver.AssignmentOfFilePart();
            if (_assignedFilePart == -1)
            {
                State = ClientBussinesLogicState.NONE;
                Logger.WriteLog("File is completly transfered", LoggerInfo.fileTransfering);
                this.Dispose();
                return;
            }

            MethodResult result = _fileReceiver.GenerateRequestForFilePart(this, _assignedFilePart);

            switch (result)
            {
                case MethodResult.SUCCES:
                    State = ClientBussinesLogicState.WAITING_FOR_FILE_PART;
                    break;
                case MethodResult.ERROR:
                    State = ClientBussinesLogicState.REQUEST_ACCEPTED;
                    Logger.WriteLog($"Error in generating request for file part, switching to state: {State}!", LoggerInfo.fileTransfering);
                    break;
            }
        }

        private void RequestFile()
        {
            if (ResourceInformer.GenerateRequestForFile(_requestingFileName, _requestingFileSize, this) == MethodResult.SUCCES)
                State = ClientBussinesLogicState.REQUEST_SENDED;
        }

        #endregion PrivateMethods

        #region EventHandler

        private void OneSecondHandler(object? sender, ElapsedEventArgs e)
        {
            _timerCounter++;

            if (IsConnected)
                if (State == ClientBussinesLogicState.REQUESTING_FILE)
                {
                    RequestFile();
                }
                else if (State == ClientBussinesLogicState.REQUEST_ACCEPTED)
                {
                    RequestFilePart();
                }


            TransferSendRateFormatedAsText = ResourceInformer.FormatDataTransferRate(BytesSent - _secondOldBytesSent);
            TransferReceiveRateFormatedAsText = ResourceInformer.FormatDataTransferRate(BytesReceived - _secondOldBytesReceived);
            _secondOldBytesSent = BytesSent;
            _secondOldBytesReceived = BytesReceived;
        }

        private void OnRejectHandler(byte[] buffer, long offset, long size)
        {
            Logger.WriteLog($"Reject was received [CLIENT]: {Address}:{Port}", LoggerInfo.socketMessage);

            if (State == ClientBussinesLogicState.REQUEST_SENDED)
            {
                Logger.WriteLog("Response was rejected, disconnecting from server and disposing client! [CLIENT]: {Address}:{Port}", LoggerInfo.warning);
                MessageBox.Show("Request for file was rejected!");
                this.Dispose();
            }
        }

        private void OnAcceptHandler(byte[] buffer, long offset, long size)
        {
            Logger.WriteLog($"Accept was received [CLIENT]: {Address}:{Port}", LoggerInfo.socketMessage);

            if (State == ClientBussinesLogicState.REQUEST_SENDED)
            {
                Logger.WriteLog($"Request for file was accepted! [CLIENT]: {Address}:{Port}", LoggerInfo.fileTransfering);

                // First request for file part
                RequestFilePart();
            }
        }

        private void OnFilePartHandler(byte[] buffer, long offset, long size)
        {
            Logger.WriteLog($"File part was received [CLIENT]: {Address}:{Port}", LoggerInfo.socketMessage);

            if (State == ClientBussinesLogicState.WAITING_FOR_FILE_PART)
            {
                int partNumber = BitConverter.ToInt32(buffer, (int)offset + 3);
                Logger.WriteLog($"File part No.:{partNumber} was received! [CLIENT]: {Address}:{Port}", LoggerInfo.fileTransfering);
                if (_fileReceiver?.WriteToFile(partNumber, buffer, (int)offset + 3 + sizeof(int), (int)size - 3 - sizeof(int)) == MethodResult.ERROR)
                {

                }

                RequestFilePart();
            }
        }

        private void OnNonRegistredMessage()
        {
            this.Disconnect();
            Logger.WriteLog($"Warning: Non registered message received, disconnecting from server! [CLIENT]: {Address}:{Port}", LoggerInfo.warning);
        }

        #endregion EventHandler

        #region OverridedMethods

        protected override void Dispose(bool disposingManagedResources)
        {
            DisconnectAndStop();
            base.Dispose(disposingManagedResources);

            _gui.BaseMsgEnque(new DisposeMessage(Id, TypeOfSocket.CLIENT));
        }

        protected override void OnConnected()
        {
            Logger.WriteLog($"Tcp client connected a new session with Id {Id}", LoggerInfo.tcpClient);

            if (_fileReceiver != null && !_fileReceiver.DownloadDone)
            {
                Thread.Sleep(100);
                RequestFile();
            }

            _gui.BaseMsgEnque(new SocketStateChangeMessage() { SocketState = SocketState.CONNECTED, SessionWithCentralServer = _sessionWithCentralServer });
        }

        protected override void OnDisconnected()
        {
            Logger.WriteLog($"Tcp client disconnected from session with Id: {Id}", LoggerInfo.disconnect);

            if (_assignedFilePart != -1)
            {
                _fileReceiver?.ReAssignFilePart(_assignedFilePart);
                _assignedFilePart = 0;
            }

            // Wait for a while...
            Thread.Sleep(1000);

            // Try to connect again
            if (!_stop)
                ConnectAsync();

            _gui.BaseMsgEnque(new SocketStateChangeMessage() { SocketState = SocketState.DISCONNECTED, SessionWithCentralServer = _sessionWithCentralServer });

            State = ClientBussinesLogicState.NONE;
        }

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            _flagSwitch.Switch(buffer, offset, size);
            //string message = Encoding.UTF8.GetString(buffer, (int)offset, (int)size);

            //_gui.BaseMsgEnque(new MessageReceiveMessage() { Message = message });

            //Logger.WriteLog($"Tcp client obtained a message[{size}]: {message}", LoggerInfo.socketMessage);
            //Logger.WriteLog($"Tcp client obtained a message[{size}]", LoggerInfo.socketMessage);
        }

        protected override void OnError(SocketError error)
        {
            Logger.WriteLog($"Tcp client caught an error with code {error}", LoggerInfo.tcpClient);
        }

        #endregion OverridedMethods             

    }
}

