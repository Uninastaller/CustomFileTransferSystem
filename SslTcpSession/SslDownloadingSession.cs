using Common.Enum;
using Common.Model;
using ConfigManager;
using Logger;
using SslTcpSession.BlockChain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;

namespace SslTcpSession
{
   class SslDownloadingSession : SslSession
   {

      #region Properties

      public bool RequestAccepted { get; set; } = false;
      public string FileNameOfAcceptedfileRequest { get; set; } = string.Empty;
      public SessionState SessionState
      {
         get => _sessionState;

         set
         {
            if (value != _sessionState)
            {
               _sessionState = value;
               ServerSessionStateChange?.Invoke(this, value);
            }
         }
      }

      #endregion Properties

      #region PublicFields


      #endregion PublicFields

      #region PrivateFields

      private SessionState _sessionState = SessionState.NONE;

      #endregion PrivateFields

      #region ProtectedFields



      #endregion ProtectedFields

      #region Ctor

      public SslDownloadingSession(SslServer server) : base(server)
      {
         Log.WriteLog(LogLevel.INFO, $"Guid: {Id}, Starting");

         _flagSwitch.OnNonRegistered(OnNonRegisteredMessage);
         _flagSwitch.Register(SocketMessageFlag.FILE_REQUEST, OnRequestFileHandler);
         _flagSwitch.Register(SocketMessageFlag.FILE_PART_REQUEST, OnRequestFilePartHandler);
         _flagSwitch.Register(SocketMessageFlag.NODE_LIST_REQUEST, OnNodeListRequestHandler);
         _flagSwitch.Register(SocketMessageFlag.PBFT_REQUEST, OnPbftRequestHandler);

      }

      #endregion Ctor

      #region PublicMethods



      #endregion PublicMethods

      #region PrivateMethods

      private void OnClientFileRequest(string filePath, long fileSize)
      {
         ClientFileRequest?.Invoke(this, filePath, fileSize);
      }

      private void OnClientDisconnected()
      {
         ClientDisconnected?.Invoke(this);
      }

      private void OnReceiveMessage(string message)
      {
         ReceiveMessage?.Invoke(this, message);
      }

      #endregion PrivateMethods

      #region ProtectedMethods

      protected override void OnHandshaked()
      {
         Log.WriteLog(LogLevel.INFO, $"Ssl session with Id {Id} handshaked!");

         //// Send invite message
         //string message = "Hello from SSL server!";
         //Send(message);
      }

      protected override void OnDisconnected()
      {
         OnClientDisconnected();
         Log.WriteLog(LogLevel.INFO, $"Ssl session with Id {Id} disconnected!");
      }

      protected override void OnReceived(byte[] buffer, long offset, long size)
      {
         //string message = Encoding.UTF8.GetString(buffer, (int)offset, (int)size);

         //OnReceiveMessage(message);
         _flagSwitch.Switch(buffer, offset, size);
      }

      protected override void OnError(SocketError error)
      {
         Log.WriteLog(LogLevel.ERROR, $"Ssl session caught an error with code {error}");
      }

      #endregion ProtectedMethods

      #region Events

      public delegate void ReceiveMessageEventHandler(SslSession sender, string message);
      public event ReceiveMessageEventHandler? ReceiveMessage;

      public delegate void ClientDisconnectedHandler(SslSession sender);
      public event ClientDisconnectedHandler? ClientDisconnected;

      public delegate void ClientFileRequestHandler(SslSession sender, string filePath, long fileSize);
      public event ClientFileRequestHandler? ClientFileRequest;

      public delegate void ServerSessionStateChangeEventHandler(SslSession sender, SessionState serverSessionState);
      public event ServerSessionStateChangeEventHandler? ServerSessionStateChange;

      private void OnNonRegisteredMessage(string message)
      {
         Log.WriteLog(LogLevel.WARNING, $"Non registered message received, disconnecting client!");
         SessionState = SessionState.NONE;
         this.Server?.FindSession(this.Id)?.Disconnect();
      }

      private void OnRequestFileHandler(byte[] buffer, long offset, long size)
      {
         if (FlagMessageEvaluator.EvaluateRequestFileMessage(buffer, offset, size, out string fileName, out Int64 fileSize))
         {
            OnClientFileRequest(fileName, fileSize);
            SessionState = SessionState.FILE_REQUEST;
         }
         else
         {
            this.Server?.FindSession(this.Id)?.Disconnect();
            Log.WriteLog(LogLevel.WARNING, $"client is sending wrong formats of data, disconnecting!");
         }
      }

      private void OnRequestFilePartHandler(byte[] buffer, long offset, long size)
      {
         if (RequestAccepted && FlagMessageEvaluator.EvaluateRequestFilePartMessage(buffer, offset, size, out Int64 filePartNumber, out Int32 partSize))
         {
            Log.WriteLog(LogLevel.DEBUG, $"Received file part request for part: {filePartNumber}, with size: {partSize}, from client: {Socket.RemoteEndPoint}!");
            FlagMessagesGenerator.GenerateFilePart(FileNameOfAcceptedfileRequest, this, filePartNumber, partSize);
            SessionState = SessionState.FILE_PART_REQUEST;
         }
         else
         {
            this.Server?.FindSession(this.Id)?.Disconnect();
            Log.WriteLog(LogLevel.WARNING, $"client is sending wrong formats of data, disconnecting!");
         }
      }

      private void OnNodeListRequestHandler(byte[] buffer, long offset, long size)
      {
         if (FlagMessageEvaluator.EvaluateNodeListRequestMessage(buffer, offset, size, out Node? senderNode))
         {
            Log.WriteLog(LogLevel.DEBUG, $"Received NodeList request from client: {Socket.RemoteEndPoint}!");
            SessionState = SessionState.NODE_LIST_SENDING;

            //NodeDiscovery.LoadNodes();

            List<Node> nodes = NodeDiscovery.GetAllNodes().ToList();

            for (int i = 0; i < nodes.Count; i++)
            {
               FlagMessagesGenerator.GenerateNodeMessage(nodes[i].GetJson(), false, this);
            }
            FlagMessagesGenerator.GenerateNodeMessage(NodeDiscovery.GetMyNode().GetJson(), true, this);

            NodeDiscovery.AddNode(senderNode);
            NodeDiscovery.SaveNodes();
         }
         else
         {
            Log.WriteLog(LogLevel.WARNING, $"client is sending wrong formats of data, disconnecting!");
            this.Server?.FindSession(this.Id)?.Disconnect();
         }
      }

      private void OnPbftRequestHandler(byte[] buffer, long offset, long size)
      {
         if (PbftMessageEvaluator.EvaluatePbftRequestMessage(buffer, offset, size, out Block? receivedBlock, out string? hashOfActiveReplicas))
         {
            Log.WriteLog(LogLevel.DEBUG, $"Received request for new block request from client: {Socket.RemoteEndPoint}!" +
                $" with hash of active replics: {hashOfActiveReplicas}. " +
                $"Session should be closed by client, but to be sure... disconnecting client!");
            this.Server?.FindSession(this.Id)?.Disconnect();


            // Try find coresponding node
            if (!NodeDiscovery.TryGetNode(receivedBlock.NodeId, out Node? node))
            {
               Log.WriteLog(LogLevel.WARNING, $"Received request for new block dont have coresponding node with node id: {receivedBlock.NodeId}!" +
               $" Operation can not be proceed!");
               return;
            }

            // Check for correct pick of primary replica in current view
            if (!Blockchain.VerifyPrimaryReplica(NodeDiscovery.GetMyNode().Id, view:0))
            {
               Log.WriteLog(LogLevel.WARNING, $"Unable to verify myself as primary replica in current view! Operation can not be proceed!");
               return;
            }

            // Check block validity
            BlockValidationResult result = Blockchain.IsNewBlockValid(receivedBlock, node);
            if (result != BlockValidationResult.VALID){
               Log.WriteLog(LogLevel.WARNING, $"You as primary replica, foun requested block as invalid due to: {result}");
               return;
            }

         }
         else
         {
            Log.WriteLog(LogLevel.WARNING, $"client is sending wrong formats of data, disconnecting!");
            this.Server?.FindSession(this.Id)?.Disconnect();
         }
      }

      #endregion Events

      #region OverridedMethods



      #endregion OverridedMethods

   }
}

