using Common.Enum;
using Common.Interface;
using Common.Model;
using Common.ThreadMessages;
using Logger;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Timers;
using System.Windows;
using Timer = System.Timers.Timer;

namespace TcpSession
{
   public class ServerBussinesLogic2 : TcpServer, IUniversalServerSocket
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
      private Dictionary<Guid, ServerClientsModel>? _clients = new Dictionary<Guid, ServerClientsModel>();

      private Timer? _timer;
      private ulong _timerCounter;

      private long _secondOldBytesSent;
      private long _secondOldBytesReceived;

      private TypeOfSession _typeOfSession;

      #endregion PrivateFields

      #region Ctor

      public ServerBussinesLogic2(IPAddress address, int port, IWindowEnqueuer gui, int optionReceiveBufferSize = 0x200000, int optionSendBufferSize = 0x200000, int optionAcceptorBacklog = 1024, TypeOfSession typeOfSession = TypeOfSession.DOWNLOADING) : base(address, port, optionReceiveBufferSize, optionSendBufferSize, optionAcceptorBacklog)
      {
         Type = TypeOfServerSocket.TCP_SERVER;
         _gui = gui;

         _timer = new Timer(1000); // Set the interval to 1 second
         _timer.Elapsed += OneSecondHandler;
         _timer.Start();
         _typeOfSession = typeOfSession;

         _gui?.BaseMsgEnque(new CreationMessage(Id, TypeOfSocket.SERVER, _typeOfSession));
      }

      #endregion Ctor

      #region PublicMethods

      public void DisconnectSession(Guid sessionId)
      {
         FindSession(sessionId)?.Disconnect();
      }

      public List<ServerDownloadingSessionsInfo> GetDownloadingSessionsInfo()
      {
         List<ServerDownloadingSessionsInfo> list = new List<ServerDownloadingSessionsInfo>();

         foreach (TcpSession session in Sessions.Values)
         {
            if (session is TcpDownloadingSession downloadingSession)
            {
               list.Add(new ServerDownloadingSessionsInfo()
               {
                  Id = session.Id,
                  SessionState = downloadingSession.SessionState,
                  FileNameOfAcceptedfileRequest = downloadingSession.FileNameOfAcceptedfileRequest,
                  BytesReceived = session.BytesReceived,
                  BytesSent = session.BytesSent,
               });
            }
         }
         return list;
      }

      public ISession? GetSessionById(Guid sessionID)
      {
         return this.FindSession(sessionID);
      }

      #endregion PublicMethods

      #region PrivateMethods

      private void ClientStateChange(ClientSocketState socketState, string? client, Guid sessionId, SessionState serverSessionState = SessionState.NONE)
      {
         if (_clients == null) return;

         if (socketState == ClientSocketState.CONNECTED && !_clients.ContainsKey(sessionId) && client != null)
         {
            _clients.Add(sessionId, new ServerClientsModel() { SessionGuid = sessionId, RemoteEndpoint = client, ServerSessionState = serverSessionState });
            Log.WriteLog(LogLevel.DEBUG, $"Client: {client}, connected to server");
         }
         else if (socketState == ClientSocketState.DISCONNECTED && _clients.ContainsKey(sessionId))
         {
            Log.WriteLog(LogLevel.DEBUG, $"Client: {_clients[sessionId]}, disconnected from server");
            _clients.Remove(sessionId);
         }
         else if (socketState == ClientSocketState.INNER_STATE_CHANGE && _clients.ContainsKey(sessionId))
         {
            _clients[sessionId].ServerSessionState = serverSessionState;
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

      private void OnReceiveMessage(TcpSession sesion, string message)
      {
         Log.WriteLog(LogLevel.DEBUG, $"Tcp server obtained a message: {message}, from: {sesion.Socket.RemoteEndPoint}");
         //Logger.WriteLog($"Tcp server obtained a message, from: {sesion.Socket.RemoteEndPoint}", LoggerInfo.socketMessage);
      }

      /// <summary>
      /// Its called when TcpServerSesion receive FILE_REQUEST message and invoke this event 
      /// </summary>
      /// <param name="session"></param>
      /// <param name="filePath"></param>
      /// <param name="fileSize"></param>
      private void OnClientFileRequest(TcpSession session, string filePath, long fileSize)
      {
         Log.WriteLog(LogLevel.DEBUG, $"Request was received for file: {filePath} with size: {fileSize}");

         string? uploadingDirectory = ConfigurationManager.AppSettings["UploadingDirectory"];
         if (uploadingDirectory != null)
         {
            if (!Directory.Exists(uploadingDirectory))
            {
               Directory.CreateDirectory(uploadingDirectory);
            }

            filePath = $@"{uploadingDirectory}\{Path.GetFileName(filePath)}";

            if (File.Exists(filePath) && fileSize == new System.IO.FileInfo(filePath).Length && session is TcpDownloadingSession serverSession)
            {
               //MessageBoxResult result = MessageBox.Show($"Client: {session.Socket.RemoteEndPoint} is requesting your file: {filePath}, with size of: {fileSize} bytes. \nAllow?", "Request", MessageBoxButton.YesNo, MessageBoxImage.Question);
               MessageBoxResult result = MessageBoxResult.Yes;
               if (result == MessageBoxResult.Yes)
               {
                  FlagMessagesGenerator.GenerateAccept(session);
                  serverSession.RequestAccepted = true;
                  serverSession.FileNameOfAcceptedfileRequest = filePath;
                  return;
               }
            }
         }

         FlagMessagesGenerator.GenerateReject(session);
         session.Disconnect();
         session.Dispose();
      }

      private void OnServerSessionStateChange(TcpSession session, SessionState serverSessionState)
      {
         //Log.WriteLog(LogLevel.DEBUG, $"OnServerSessionStateChange: {serverSessionState}, on: {sesion.Socket.RemoteEndPoint}");
         ClientStateChange(ClientSocketState.INNER_STATE_CHANGE, null, session.Id, serverSessionState);
         if (_clients != null && _gui != null)
            _gui.BaseMsgEnque(new ClientsStateChangeMessage() { Clients = _clients });
      }

      #endregion EventHandler

      #region OverridedMethods

      protected override void OnDispose()
      {
         _gui?.BaseMsgEnque(new DisposeMessage(Id, TypeOfSocket.SERVER, TypeOfSession.DOWNLOADING, true));

         if (_timer != null)
         {
            _timer.Elapsed -= OneSecondHandler;
            _timer.Stop();
            _timer.Dispose();
            _timer = null;
         }
         _clients?.Clear();
         _clients = null;
         _timer = null;
         _gui = null;
      }

      protected override TcpSession CreateSession() { return new TcpDownloadingSession(this); }

      protected override void OnError(SocketError error)
      {
         Log.WriteLog(LogLevel.ERROR, $"Tcp server caught an error with code {error}");
      }

      private void OnClientDisconnected(TcpSession session)
      {
         if (session is TcpDownloadingSession serverSession)
         {
            serverSession.ReceiveMessage -= OnReceiveMessage;
            serverSession.ClientDisconnected -= OnClientDisconnected;
            serverSession.ClientFileRequest -= OnClientFileRequest;
            serverSession.ServerSessionStateChange -= OnServerSessionStateChange;
         }

         ClientStateChange(ClientSocketState.DISCONNECTED, null, session.Id);
         if (_clients != null && _gui != null)
            _gui.BaseMsgEnque(new ClientsStateChangeMessage() { Clients = _clients });
      }

      protected override void OnConnected(TcpSession session)
      {
         if (session is TcpDownloadingSession serverSession)
         {
            serverSession.ReceiveMessage += OnReceiveMessage;
            serverSession.ClientDisconnected += OnClientDisconnected;
            serverSession.ClientFileRequest += OnClientFileRequest;
            serverSession.ServerSessionStateChange += OnServerSessionStateChange;
         }

         ClientStateChange(ClientSocketState.CONNECTED, session.Socket?.RemoteEndPoint?.ToString(), session.Id);
         if (_clients != null && _gui != null)
            _gui.BaseMsgEnque(new ClientsStateChangeMessage() { Clients = _clients });
      }

      protected override void OnStarted()
      {
         _gui?.BaseMsgEnque(new ServerSocketStateChangeMessage() { ServerSocketState = ServerSocketState.STARTED, TypeOfSession = _typeOfSession });
      }

      protected override void OnStopped()
      {
         _gui?.BaseMsgEnque(new ServerSocketStateChangeMessage() { ServerSocketState = ServerSocketState.STOPPED, TypeOfSession = _typeOfSession });
      }

      #endregion OverridedMethods

   }
}
