using Modeel.Frq;
using Modeel.Log;
using Modeel.Messages;
using Modeel.Model;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using Timer = System.Timers.Timer;


namespace Modeel.FastTcp
{
   public class ClientBussinesLogic2 : TcpClient, IUniversalClientSocket, ISession
   {

      #region Properties

      public string IpAndPort => Socket.LocalEndPoint?.ToString() ?? string.Empty;
      public TypeOfSocket Type { get; }
      public string TransferSendRateFormatedAsText { get; private set; } = string.Empty;
      public string TransferReceiveRateFormatedAsText { get; private set; } = string.Empty;

      // Transfer flags
      public bool RequestingFile
      {
         get => _requestingFile;

         private set
         {
            _requestingFile = value;
            if (value)
            {
               _requestSended = false;
               _waitingForResponseToRequest = false;
               _requestAccepted = false;
               _waitingForFilePart = false;
            }
         }
      }

      public bool RequestSended
      {
         get => _requestSended;

         private set
         {
            _requestSended = value;
            if (value)
            {
               _requestingFile = false;
               _waitingForResponseToRequest = true;
               _requestAccepted = false;
            }
         }
      }

      public bool WaitingForResponseToRequest
      {
         get => _waitingForResponseToRequest;

         private set
         {
            _waitingForResponseToRequest = value;
            if (!value)
            {
               _requestSended = false;
               _requestingFile = false;
               _requestAccepted = false;
            }
         }
      }

      public bool RequestAccepted
      {
         get => _requestAccepted;

         private set
         {
            _requestAccepted = value;
            if (!value)
            {
               _waitingForResponseToRequest = false;
               _requestSended = false;
               _requestingFile = false;
            }
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

      // Transfer flags
      private bool _requestingFile;
      private bool _requestSended;
      private bool _waitingForResponseToRequest;
      private bool _requestAccepted;

      private bool _waitingForFilePart = false;

      #endregion PrivateFields

      #region Ctor

      public ClientBussinesLogic2(IPAddress address, int port, IWindowEnqueuer gui, string fileName, long fileSize, FileReceiver fileReceiver, int optionReceiveBufferSize = 0x200000, int optionSendBufferSize = 0x200000, bool sessionWithCentralServer = false)
          : this(address, port, gui, optionReceiveBufferSize: optionReceiveBufferSize, optionSendBufferSize: optionSendBufferSize, sessionWithCentralServer: sessionWithCentralServer)
      {
         _requestingFileName = fileName;
         _requestingFileSize = fileSize;
         _fileReceiver = fileReceiver;
         RequestingFile = true;
      }

      public ClientBussinesLogic2(IPAddress address, int port, IWindowEnqueuer gui, int optionReceiveBufferSize = 8192, int optionSendBufferSize = 8192, bool sessionWithCentralServer = false) : base(address, port, optionReceiveBufferSize, optionSendBufferSize)
      {
         Type = TypeOfSocket.TCP;

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

      private void OnRejectHandler(byte[] buffer, long offset, long size)
      {
         Logger.WriteLog($"Reject was received [CLIENT]: {Address}:{Port}", LoggerInfo.socketMessage);

         if (WaitingForResponseToRequest)
         {
            this.DisconnectAndStop();
            Logger.WriteLog("Response was rejected, disconnecting from server! [CLIENT]: {Address}:{Port}", LoggerInfo.warning);
            MessageBox.Show("Request for file was rejected!");
         }
      }

      private void OnAcceptHandler(byte[] buffer, long offset, long size)
      {
         Logger.WriteLog($"Accept was received [CLIENT]: {Address}:{Port}", LoggerInfo.socketMessage);

         if (WaitingForResponseToRequest)
         {
            Logger.WriteLog($"Request for file was accepted! [CLIENT]: {Address}:{Port}", LoggerInfo.fileTransfering);

            // First request for file part



            if (_fileReceiver?.GenerateRequestForFilePart(this) == MethodResult.SUCCES)
            {
               _waitingForFilePart = true;
            }
            else
            {
               _waitingForFilePart = false;
            }
            RequestAccepted = true;
         }
      }

      private void OnFilePartHandler(byte[] buffer, long offset, long size)
      {
         Logger.WriteLog($"File part was received [CLIENT]: {Address}:{Port}", LoggerInfo.socketMessage);

         if (RequestAccepted)
         {
            int partNumber = BitConverter.ToInt32(buffer, (int)offset + 3);
            Logger.WriteLog($"File part No.:{partNumber} was received! [CLIENT]: {Address}:{Port}", LoggerInfo.fileTransfering);
            if (_fileReceiver?.WriteToFile(partNumber, buffer, (int)offset + 3 + sizeof(int), (int)size - 3 - sizeof(int)) == MethodResult.ERROR)
            {

            }

            if (_fileReceiver?.GenerateRequestForFilePart(this) == MethodResult.SUCCES)
            {
               _waitingForFilePart = true;
            }
            else
            {
               _waitingForFilePart = false;
            }
         }
      }

      private void OnNonRegistredMessage()
      {
         this.DisconnectAndStop();
         Logger.WriteLog($"Warning: Non registered message received, disconnecting from server! [CLIENT]: {Address}:{Port}", LoggerInfo.warning);
      }

      #endregion EventHandler

      #region OverridedMethods

      protected override async void OnConnected()
      {
         Logger.WriteLog($"Tcp client connected a new session with Id {Id}", LoggerInfo.tcpClient);

         _gui.BaseMsgEnque(new SocketStateChangeMessage() { SocketState = SocketState.CONNECTED, SessionWithCentralServer = _sessionWithCentralServer });

         if (RequestingFile)
         {
            await Task.Delay(1000);
            if (ResourceInformer.GenerateRequestForFile(_requestingFileName, _requestingFileSize, this) == MethodResult.SUCCES)
               RequestSended = true;
         }
      }

      protected override void OnDisconnected()
      {
         Logger.WriteLog($"Tcp client disconnected from session with Id: {Id}", LoggerInfo.disconnect);

         // Wait for a while...
         Thread.Sleep(1000);

         // Try to connect again
         if (!_stop)
            ConnectAsync();

         _gui.BaseMsgEnque(new SocketStateChangeMessage() { SocketState = SocketState.DISCONNECTED, SessionWithCentralServer = _sessionWithCentralServer });

         _requestSended = false;
         _waitingForResponseToRequest = false;
         _requestAccepted = false;

         if (_waitingForFilePart)
         {
            Logger.WriteLog("Reconnecting to server...", LoggerInfo.disconnect);
            ConnectAsync();
            RequestingFile = true;
         }
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

