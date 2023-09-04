using Common.Enum;
using Common.Interface;
using Common.Model;
using Common.ThreadMessages;
using Logger;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using Timer = System.Timers.Timer;


namespace SslTcpSession
{
    public class SslClientBussinesLogic : SslClient, IUniversalClientSocket, ISession
    {

        #region Properties

        public string IpAndPort => Socket.LocalEndPoint?.ToString() ?? string.Empty;
        public TypeOfClientSocket Type { get; }
        public string TransferSendRateFormatedAsText => ResourceInformer.FormatDataTransferRate(TransferSendRate);
        public string TransferReceiveRateFormatedAsText => ResourceInformer.FormatDataTransferRate(TransferReceiveRate);

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

        public long TransferSendRate { get; private set; }
        public long TransferReceiveRate { get; private set; }

        #endregion Properties

        #region PrivateFields

        private const int _flagBytesCount = 3;

        private IWindowEnqueuer _gui;
        private TypeOfSession _typeOfSession;

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

        private long _disconnectTime = 0;

        #endregion PrivateFields

        #region Ctor

        public SslClientBussinesLogic(SslContext context, IPAddress address, int port, IWindowEnqueuer gui, string fileName, long fileSize, FileReceiver fileReceiver, int optionReceiveBufferSize = 0x200000, int optionSendBufferSize = 0x200000, TypeOfSession typeOfSession = TypeOfSession.DOWNLOADING)
            : this(context, address, port, gui, optionReceiveBufferSize: optionReceiveBufferSize, optionSendBufferSize: optionSendBufferSize, typeOfSession: typeOfSession)
        {
            _requestingFileName = fileName;
            _requestingFileSize = fileSize;
            _fileReceiver = fileReceiver;

            _flagSwitch.SetCaching(fileReceiver.PartSize, OnFilePartHandler);

            State = ClientBussinesLogicState.REQUESTING_FILE;
        }

        public SslClientBussinesLogic(SslContext context, IPAddress address, int port, IWindowEnqueuer gui, int optionReceiveBufferSize = 8192, int optionSendBufferSize = 8192, TypeOfSession typeOfSession = TypeOfSession.DOWNLOADING) : base(context, address, port, optionReceiveBufferSize, optionSendBufferSize)
        {
            Type = TypeOfClientSocket.TCP_CLIENT_SSL;

            _typeOfSession = typeOfSession;

            _flagSwitch.OnNonRegistered(OnNonRegistredMessage);

            if (typeOfSession == TypeOfSession.DOWNLOADING)
            {
                _flagSwitch.Register(SocketMessageFlag.REJECT, OnRejectHandler);
                _flagSwitch.Register(SocketMessageFlag.ACCEPT, OnAcceptHandler);
                _flagSwitch.Register(SocketMessageFlag.FILE_PART, OnFilePartHandler);
            }
            else if (typeOfSession == TypeOfSession.DOWNLOADING_OFFERING_FILES_SESSION_WITH_CENTRAL_SERVER)
            {
                _flagSwitch.Register(SocketMessageFlag.OFFERING_FILE, OnOfferingFileHandler);
            }


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

        private void CreateAndSendOfferingFilesToCentralServer()
        {
            Log.WriteLog(LogLevel.DEBUG, "CreateAndSendOfferingFilesToCentralServer");
            State = ClientBussinesLogicState.OFFERING_FILES_SENDING;
            ResourceInformer.CreateAndSendOfferingFilesToCentralServer(NetworkUtils.GetLocalIPAddress() ?? IPAddress.Loopback, 34259, this);
            Log.WriteLog(LogLevel.INFO, $"All Offering Files sended, disposing socket!");
            Dispose();
        }

        private void CreateRequestForOfferingFilesToCentralServer()
        {
            Log.WriteLog(LogLevel.DEBUG, "CreateRequestForOfferingFilesToCentralServer");
            State = ClientBussinesLogicState.OFFERING_FILES_RECEIVING;
            FlagMessagesGenerator.GenerateOfferingFilesRequest(this);
        }

        private void RequestFilePart()
        {
            _assignedFilePart = _fileReceiver.AssignmentOfFilePart();
            if (_assignedFilePart == -1)
            {
                State = ClientBussinesLogicState.NONE;
                Log.WriteLog(LogLevel.DEBUG, "File is completly transfered");
                this.Dispose();
                return;
            }
            else if (_assignedFilePart + 1 == _fileReceiver.TotalParts)
            {
                _flagSwitch.SetLastPartSize(_fileReceiver.LastPartSize);
            }

            MethodResult result = _fileReceiver.GenerateRequestForFilePart(this, _assignedFilePart);

            switch (result)
            {
                case MethodResult.SUCCES:
                    State = ClientBussinesLogicState.WAITING_FOR_FILE_PART;
                    break;
                case MethodResult.ERROR:
                    State = ClientBussinesLogicState.REQUEST_ACCEPTED;
                    Log.WriteLog(LogLevel.WARNING, $"Problem in generating request for file part, switching to state: {State}! Automatically retrie in few seconds");
                    break;
            }
        }

        private async void RequestFilePartAsync()
        {
            _assignedFilePart = _fileReceiver.AssignmentOfFilePart();
            if (_assignedFilePart == -1)
            {
                State = ClientBussinesLogicState.NONE;
                Log.WriteLog(LogLevel.DEBUG, "File is completly transfered");
                this.Dispose();
                return;
            }
            else if (_assignedFilePart + 1 == _fileReceiver.TotalParts)
            {
                // Las part to download remaining
                _flagSwitch.SetLastPartSize(_fileReceiver.LastPartSize);
            }

            MethodResult result = await Task.Run(() => _fileReceiver.GenerateRequestForFilePart(this, _assignedFilePart));

            switch (result)
            {
                case MethodResult.SUCCES:
                    State = ClientBussinesLogicState.WAITING_FOR_FILE_PART;
                    break;
                case MethodResult.ERROR:
                    State = ClientBussinesLogicState.REQUEST_ACCEPTED;
                    Log.WriteLog(LogLevel.WARNING, $"Problem in generating request for file part, switching to state: {State}! Automatically retrie in few seconds");
                    break;
            }
        }

        private void RequestFile()
        {
            if (FlagMessagesGenerator.GenerateRequestForFile(_requestingFileName, _requestingFileSize, this) == MethodResult.SUCCES)
                State = ClientBussinesLogicState.REQUEST_SENDED;
        }

        #endregion PrivateMethods

        #region EventHandler

        private void OneSecondHandler(object? sender, ElapsedEventArgs e)
        {
            _timerCounter++;

            if (IsConnected)
            {
                if (State == ClientBussinesLogicState.REQUESTING_FILE)
                {
                    RequestFile();
                }
                else if (State == ClientBussinesLogicState.REQUEST_ACCEPTED)
                {
                    RequestFilePart();
                }
            }
            else if(++_disconnectTime == 10)
            {
                Log.WriteLog(LogLevel.WARNING, "Unable to connect to the server. Disposing socked!");
                Dispose();
            }

            TransferSendRate = BytesSent - _secondOldBytesSent;
            TransferReceiveRate = BytesReceived - _secondOldBytesReceived;
            _secondOldBytesSent = BytesSent;
            _secondOldBytesReceived = BytesReceived;
        }

        private void OnRejectHandler(byte[] buffer, long offset, long size)
        {
            Log.WriteLog(LogLevel.DEBUG, $"Reject was received [CLIENT]: {Address}:{Port}");

            if (State == ClientBussinesLogicState.REQUEST_SENDED)
            {
                Log.WriteLog(LogLevel.DEBUG, "Response was rejected, disconnecting from server and disposing client! [CLIENT]: {Address}:{Port}");
                MessageBox.Show("Request for file was rejected!");
                this.Dispose();
            }
        }

        private void OnOfferingFileHandler(byte[] buffer, long offset, long size)
        {
            if (State == ClientBussinesLogicState.OFFERING_FILES_RECEIVING)
            {
                State = ClientBussinesLogicState.OFFERING_FILES_RECEIVING;
                if (FlagMessageEvaluator.EvaluateOfferingFileMessage(buffer, offset, size, out List<OfferingFileDto> offeringFileDto, out bool endOfMessageGroup))
                {
                    _gui?.BaseMsgEnque(new OfferingFilesReceivedMessage(offeringFileDto));
                    if (endOfMessageGroup)
                    {
                        Dispose();
                        Log.WriteLog(LogLevel.INFO, $"All Offering Files received, disposing socket!");
                    }
                }
            }
            else
            {
                Log.WriteLog(LogLevel.WARNING, $"Offering File received, but session is not in default state, so message can not be proceed!");
            }
        }

        private void OnAcceptHandler(byte[] buffer, long offset, long size)
        {
            Log.WriteLog(LogLevel.DEBUG, $"Accept was received [CLIENT]: {Address}:{Port}");

            if (State == ClientBussinesLogicState.REQUEST_SENDED)
            {
                Log.WriteLog(LogLevel.DEBUG, $"Request for file was accepted! [CLIENT]: {Address}:{Port}");

                // First request for file part
                RequestFilePart();
            }
        }

        private void OnFilePartHandler(byte[] buffer, long offset, long size)
        {
            Log.WriteLog(LogLevel.DEBUG, $"File part was received [CLIENT]: {Address}:{Port}");

            if (State == ClientBussinesLogicState.WAITING_FOR_FILE_PART)
            {
                RequestFilePartAsync();

                long partNumber = BitConverter.ToInt64(buffer, (int)offset + _flagBytesCount);
                Log.WriteLog(LogLevel.DEBUG, $"File part No.:{partNumber} was received! [CLIENT]: {Address}:{Port}");
                if (_fileReceiver.WriteToFile(partNumber, buffer, (int)offset + _flagBytesCount + sizeof(long), (int)size - _flagBytesCount - sizeof(long)) == MethodResult.ERROR)
                {
                    Log.WriteLog(LogLevel.INFO, $"Part number {partNumber}, returning to state waiting for asignment, becouse we were unable to save him");
                }

                //RequestFilePart();
            }
        }

        private void OnNonRegistredMessage(string message)
        {
            if (_typeOfSession == TypeOfSession.TOR_CONTROL_SESSION)
            {
                _gui.BaseMsgEnque(new MessageReceiveMessage() { Message = message });
                Log.WriteLog(LogLevel.DEBUG, $"Tor cotroller obtained a message[{message.Length}]: {message}");
            }
            else
            {
                this.Disconnect();
                Log.WriteLog(LogLevel.WARNING, $"Non registered message received, disconnecting from server! [CLIENT]: {Address}:{Port}");
            }
        }

        #endregion EventHandler

        #region OverridedMethods

        protected override void Dispose(bool disposingManagedResources)
        {
            Log.WriteLog(LogLevel.DEBUG, $"Ssl client with Id {Id} is being disposed");

            _gui.BaseMsgEnque(new DisposeMessage(Id, TypeOfSocket.CLIENT));

            TransferReceiveRate = 0;
            TransferSendRate = 0;
            DisconnectAndStop();
            base.Dispose(disposingManagedResources);
        }

        protected override void OnConnected()
        {
            Log.WriteLog(LogLevel.DEBUG, $"Ssl client connected a new session with Id {Id}");
            _disconnectTime = 0;
            _gui.BaseMsgEnque(new ClientSocketStateChangeMessage() { ClientSocketState = ClientSocketState.CONNECTED, TypeOfSession = _typeOfSession });
        }

        protected async override void OnHandshaked()
        {
            Log.WriteLog(LogLevel.DEBUG, $"Ssl client handshaked a new session with Id {Id}");
            switch (_typeOfSession)
            {
                case TypeOfSession.DOWNLOADING:
                    if (_fileReceiver != null && !_fileReceiver.NoPartsForAsignmentLeft)
                    {
                        RequestFile();
                    }
                    break;
                case TypeOfSession.UPDATING_OFFERING_FILES_SESSION_WITH_CENTRAL_SERVER:
                    CreateAndSendOfferingFilesToCentralServer();
                    break;
                case TypeOfSession.DOWNLOADING_OFFERING_FILES_SESSION_WITH_CENTRAL_SERVER:
                    CreateRequestForOfferingFilesToCentralServer();
                    break;
                default:
                    break;
            }
            
        }

        protected override void OnDisconnected()
        {
            Log.WriteLog(LogLevel.DEBUG, $"Ssl client disconnected from session with Id: {Id}");

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

            _gui.BaseMsgEnque(new ClientSocketStateChangeMessage() { ClientSocketState = ClientSocketState.DISCONNECTED, TypeOfSession = _typeOfSession });
        }

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            _flagSwitch.Switch(buffer, offset, size);
            //string message = Encoding.UTF8.GetString(buffer, (int)offset, (int)size);
            ////Logger.WriteLog($"Ssl client obtained a message[{size}]: {message}", LoggerInfo.socketMessage);
            //Logger.WriteLog($"Ssl client obtained a message[{size}]", LoggerInfo.socketMessage);
        }

        protected override void OnError(SocketError error)
        {
            Log.WriteLog(LogLevel.ERROR, $"Ssl client caught an error with code {error}");
        }

        #endregion OverridedMethods             

    }
}

