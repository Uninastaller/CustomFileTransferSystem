using Common.Enum;
using Common.Interface;
using Common.Model;
using ConfigManager;
using Logger;
using SslTcpSession.BlockChain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Xml.Linq;

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
        private IWindowEnqueuer? _gui;

        #endregion PrivateFields

        #region ProtectedFields



        #endregion ProtectedFields

        #region Ctor

        public SslDownloadingSession(SslServer server, IWindowEnqueuer? gui) : base(server)
        {
            Log.WriteLog(LogLevel.INFO, $"Guid: {Id}, Starting");

            _gui = gui;

            _flagSwitch.OnNonRegistered(OnNonRegisteredMessage);
            _flagSwitch.Register(SocketMessageFlag.FILE_REQUEST, OnRequestFileHandler);
            _flagSwitch.Register(SocketMessageFlag.FILE_PART_REQUEST, OnRequestFilePartHandler);
            _flagSwitch.Register(SocketMessageFlag.NODE_LIST_REQUEST, OnNodeListRequestHandler);
            _flagSwitch.Register(SocketMessageFlag.PBFT_REQUEST, OnPbftRequestHandler);
            _flagSwitch.Register(SocketMessageFlag.PBFT_PRE_PREPARE, OnPbftPrePrepareHandler);
            _flagSwitch.Register(SocketMessageFlag.PBFT_PREPARE, OnPbftPrepareHandler);
            _flagSwitch.Register(SocketMessageFlag.PBFT_ERROR, OnPbftErrorHandler);
            _flagSwitch.Register(SocketMessageFlag.PBFT_COMMIT, OnPbftCommitHandler);
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

        private void OnPrePreepareBlockForReplica(Block requestedBlock)
        {
            PrePrepareBlockForReplica?.Invoke(requestedBlock);
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

            //OnReceivePbftMessage(message);
            _flagSwitch.Switch(buffer, offset, size);
        }

        protected override void OnError(SocketError error)
        {
            Log.WriteLog(LogLevel.ERROR, $"Ssl session caught an error with code {error}");
        }

        #endregion ProtectedMethods

        #region Events

        public delegate void PrePreepareBlockForReplicaHandler(Block requestedBlock);
        public event PrePreepareBlockForReplicaHandler? PrePrepareBlockForReplica;

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

        private async void OnNodeListRequestHandler(byte[] buffer, long offset, long size)
        {
            if (FlagMessageEvaluator.EvaluateNodeListRequestMessage(buffer, offset, size, out Node? senderNode))
            {
                Log.WriteLog(LogLevel.DEBUG, $"Received NodeList request from client: {Socket.RemoteEndPoint}!");
                SessionState = SessionState.NODE_LIST_SENDING;

                //NodeDiscovery.LoadNodes();

                List<Node> nodes = NodeDiscovery.GetAllNodes().ToList();

                for (int i = 0; i < nodes.Count; i++)
                {
                    FlagMessagesGenerator.GenerateNodeMessage(nodes[i].GetJson(), i == nodes.Count - 1, this);
                }
                //FlagMessagesGenerator.GenerateNodeMessage(NodeDiscovery.GetMyNode().GetJson(), true, this);

                NodeDiscovery.AddNode(senderNode);
                NodeDiscovery.SaveNodes();

                // Start synchronization myself
                if (_gui != null && _gui.IsOpen())
                {
                    await NodeSynchronization.ExecuteSynchronization(_gui);
                }
            }
            else
            {
                Log.WriteLog(LogLevel.WARNING, $"client is sending wrong formats of data, disconnecting!");
                this.Server?.FindSession(this.Id)?.Disconnect();
            }
        }

        private async void OnPbftRequestHandler(byte[] buffer, long offset, long size)
        {
            if (PbftMessageEvaluator.EvaluatePbftRequestMessage(buffer, offset, size, out Block? requestedBlock, out string? synchronizationHash))
            {
                Log.WriteLog(LogLevel.DEBUG, $"Session should be closed by client, but to be sure... disconnecting client!");

                this.Server?.FindSession(this.Id)?.Disconnect();

                // Try find coresponding node
                if (!NodeDiscovery.TryGetNode(requestedBlock.NodeId, out Node? node))
                {
                    Log.WriteLog(LogLevel.WARNING, $"Received request for new block dont have coresponding node with node id: {requestedBlock.NodeId}!" +
                    $" Operation can not be proceed!");
                    return;
                }

                if (PbftAwaiter.BlockNewRequests)
                {
                    Log.WriteLog(LogLevel.WARNING, "Can not validate request bcs of blocking state from BL, sending error response");

                    if (IPAddress.TryParse(node.Address, out IPAddress? senderAddress))
                    {
                        // SEND ERROR
                        await SslPbftTmpClientBusinessLogic.SendPbftErrorAndDispose(senderAddress, node.Port, requestedBlock.Hash, NodeDiscovery.SynchronizationHash,
                            "Can not validate request due to another request in process!", NodeDiscovery.GetMyNode().Id, node.Id);
                    }

                    return;
                }

                // As second, check if hash of active replicas is same as mine
                if (!synchronizationHash.Equals(NodeDiscovery.SynchronizationHash))
                {
                    Log.WriteLog(LogLevel.WARNING, $"Received request for new block, but we have different synchronization hashes!" +
                    $" Application will automatically start synchronization process!");

                    if (_gui == null)
                    {
                        Log.WriteLog(LogLevel.ERROR, "Can not start synchronization, bcs session has null reference to gui!");
                        return;
                    }
                    else
                    {
                        await NodeSynchronization.ExecuteSynchronization(_gui);

                        if (!synchronizationHash.Equals(NodeDiscovery.SynchronizationHash))
                        {
                            Log.WriteLog(LogLevel.WARNING, "SynchronizationHash is still not same, operation can not be proceed! Generating error message to the client replica");

                            if (IPAddress.TryParse(node.Address, out IPAddress? senderAddress))
                            {
                                // SEND ERROR
                                await SslPbftTmpClientBusinessLogic.SendPbftErrorAndDispose(senderAddress, node.Port, requestedBlock.Hash, NodeDiscovery.SynchronizationHash,
                                    "Synchronization hashes are not equal!", NodeDiscovery.GetMyNode().Id, node.Id);
                            }

                            return;
                        }
                    }
                }

                // Check for correct pick of primary replica in current view
                if (!Blockchain.VerifyPrimaryReplica(NodeDiscovery.GetMyNode().Id, requestedBlock.Timestamp))
                {
                    Log.WriteLog(LogLevel.WARNING, $"Unable to verify myself as primary replica in current view! Operation can not be proceed! Sending error response");
                    if (IPAddress.TryParse(node.Address, out IPAddress? senderAddress))
                    {
                        // SEND ERROR
                        await SslPbftTmpClientBusinessLogic.SendPbftErrorAndDispose(senderAddress, node.Port, requestedBlock.Hash, NodeDiscovery.SynchronizationHash,
                            "Unable to verify myself as primary replica in current view!", NodeDiscovery.GetMyNode().Id, node.Id);
                    }
                    return;
                }

                // Check block validity
                BlockValidationResult result = Blockchain.IsNewBlockValid(requestedBlock, node);
                if (result != BlockValidationResult.VALID)
                {
                    Log.WriteLog(LogLevel.WARNING, $"You as primary replica, found requested block as invalid due to: {result}, sending error response!");
                    if (IPAddress.TryParse(node.Address, out IPAddress? senderAddress))
                    {
                        // SEND ERROR
                        await SslPbftTmpClientBusinessLogic.SendPbftErrorAndDispose(senderAddress, node.Port, requestedBlock.Hash, NodeDiscovery.SynchronizationHash,
                            $"I as primary replica, found your request as invalid due to {result}!", NodeDiscovery.GetMyNode().Id, node.Id);
                    }
                    return;
                }

                // PRE-PREPARE
                await SslPbftTmpClientBusinessLogic.MulticastPrePrepareAndDispose(requestedBlock, NodeDiscovery.GetMyNode().Id,
                    requestedBlock.SignAndReturnHash(), synchronizationHash);
            }
            else
            {
                Log.WriteLog(LogLevel.WARNING, $"client is sending wrong formats of data, disconnecting!");
                this.Server?.FindSession(this.Id)?.Disconnect();
            }
        }

        private async void OnPbftPrePrepareHandler(byte[] buffer, long offset, long size)
        {
            if (PbftMessageEvaluator.EvaluatePbftPrePrepareMessage(buffer, offset, size, out Block? requestedBlock,
                out Guid _, out string? signOfPrimaryReplica, out string? synchronizationHash))
            {
                Log.WriteLog(LogLevel.DEBUG, $"Session should be closed by client, but to be sure... disconnecting client!");

                // send block to BL for save in case we will be want to add this block
                OnPrePreepareBlockForReplica(requestedBlock);

                this.Server?.FindSession(this.Id)?.Disconnect();

                // As first, check if hash of active replicas is same as mine
                if (!synchronizationHash.Equals(NodeDiscovery.SynchronizationHash))
                {
                    Log.WriteLog(LogLevel.WARNING, $"Received pre-prepare, but we have different synchronization hashes!" +
                    $" Application will automatically start synchronization process!");

                    if (_gui == null)
                    {
                        Log.WriteLog(LogLevel.ERROR, "Can not start synchronization, bcs session has null reference to gui!");
                        return;
                    }
                    else
                    {
                        await NodeSynchronization.ExecuteSynchronization(_gui);

                        if (!synchronizationHash.Equals(NodeDiscovery.SynchronizationHash))
                        {
                            Log.WriteLog(LogLevel.WARNING, "SynchronizationHash is still not same, operation can not be proceed!");
                            return;
                        }
                    }
                }

                // Try find coresponding node
                if (!NodeDiscovery.TryGetNode(requestedBlock.NodeId, out Node? node))
                {
                    Log.WriteLog(LogLevel.WARNING, $"Received pre-prepare, dont have coresponding node with node id: {requestedBlock.NodeId}!" +
                    $" Operation can not be proceed!");
                    return;
                }

                // Check for correct pick of primary replica in current view
                if (!Blockchain.VerifyPrimaryReplica(requestedBlock.Hash, signOfPrimaryReplica, requestedBlock.Timestamp))
                {
                    Log.WriteLog(LogLevel.WARNING, $"Unable to verify primary replica in current view! Operation can not be proceed!");
                    return;
                }

                // Check block validity
                BlockValidationResult result = Blockchain.IsNewBlockValid(requestedBlock, node);
                if (result != BlockValidationResult.VALID)
                {
                    Log.WriteLog(LogLevel.WARNING, $"You as backup replica, found requested block as invalid due to: {result}");
                    return;
                }

                // PREPARE
                await SslPbftTmpClientBusinessLogic.MulticastPrepareAndDispose(requestedBlock.Hash,
                    requestedBlock.SignAndReturnHash(), NodeDiscovery.SynchronizationHash, NodeDiscovery.GetMyNode().Id);
            }
            else
            {
                Log.WriteLog(LogLevel.WARNING, $"client is sending wrong formats of data, disconnecting!");
                this.Server?.FindSession(this.Id)?.Disconnect();
            }
        }

        private async void OnPbftPrepareHandler(byte[] buffer, long offset, long size)
        {
            if (PbftMessageEvaluator.EvaluatePbftPrepareMessage(buffer, offset, size, out string? hashOfRequest,
                out string? signOfBackupReplica, out string? synchronizationHash, out Guid guidOfBackupReplica))
            {
                Log.WriteLog(LogLevel.DEBUG, $"Session should be closed by client, but to be sure... disconnecting client!");

                this.Server?.FindSession(this.Id)?.Disconnect();

                // As first, check if hash of active replicas is same as mine
                if (!synchronizationHash.Equals(NodeDiscovery.SynchronizationHash))
                {
                    Log.WriteLog(LogLevel.WARNING, $"Received prepare, but we have different synchronization hashes!" +
                    $" Application will automatically start synchronization process!");

                    if (_gui == null)
                    {
                        Log.WriteLog(LogLevel.ERROR, "Can not start synchronization, bcs session has null reference to gui!");
                        return;
                    }
                    else
                    {
                        await NodeSynchronization.ExecuteSynchronization(_gui);

                        if (!synchronizationHash.Equals(NodeDiscovery.SynchronizationHash))
                        {
                            Log.WriteLog(LogLevel.WARNING, "SynchronizationHash is still not same, operation can not be proceed!");
                            return;
                        }
                    }
                }

                // Try find coresponding node
                if (!NodeDiscovery.TryGetNode(guidOfBackupReplica, out Node? node))
                {
                    Log.WriteLog(LogLevel.WARNING, $"Received prepare, but dont have coresponding node with node id: {guidOfBackupReplica}!" +
                    $" Operation can not be proceed!");
                    return;
                }

                // Check for valid sign of backup replica
                if (!Blockchain.VerifyBackupReplica(node, signOfBackupReplica, hashOfRequest))
                {
                    Log.WriteLog(LogLevel.WARNING, $"Unable to verify backup replica! Operation can not be proceed!");
                    return;
                }

            }
            else
            {
                Log.WriteLog(LogLevel.WARNING, $"client is sending wrong formats of data, disconnecting!");
                this.Server?.FindSession(this.Id)?.Disconnect();
            }
        }

        private void OnPbftCommitHandler(byte[] buffer, long offset, long size)
        {
            if (PbftMessageEvaluator.EvaluatePbftCommitMessage(buffer, offset, size, out string? _,
                out string? _, out Guid _))
            {
                Log.WriteLog(LogLevel.DEBUG, $"Session should be closed by client, but to be sure... disconnecting client!");
            }
            else
            {
                Log.WriteLog(LogLevel.WARNING, $"client is sending wrong formats of data, disconnecting!");
            }
            this.Server?.FindSession(this.Id)?.Disconnect();

        }

        private void OnPbftErrorHandler(byte[] buffer, long offset, long size)
        {
            if (PbftMessageEvaluator.EvaluatePbftErrorMessage(buffer, offset, size, out string? hashOfRequest,
                out string? _, out string? _, out Guid _))
            {
                Log.WriteLog(LogLevel.DEBUG, $"Session should be closed by client, but to be sure... disconnecting client!");
            }
            else
            {
                Log.WriteLog(LogLevel.WARNING, $"client is sending wrong formats of data, disconnecting!");
            }
            this.Server?.FindSession(this.Id)?.Disconnect();

        }

        #endregion Events

        #region OverridedMethods



        #endregion OverridedMethods

    }
}

