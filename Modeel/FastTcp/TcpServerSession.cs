using Common.Enum;
using Common.Model;
using Logger;
using System;
using System.Net.Sockets;
using System.Text;

namespace Modeel.FastTcp
{
    public class TcpServerSession : TcpSession
    {

        #region Properties

        public bool RequestAccepted { get; set; } = false;
        public string FilePathOfAcceptedfileRequest { get; set; } = string.Empty;

        #endregion Properties

        #region PublicFields



        #endregion PublicFields

        #region PrivateFields



        #endregion PrivateFields

        #region ProtectedFields



        #endregion ProtectedFields

        #region Ctor

        public TcpServerSession(TcpServer server) : base(server)
        {
            Log.WriteLog(LogLevel.INFO, $"Guid: {Id}, Starting");

            _flagSwitch.OnNonRegistered(OnNonRegistredMessage);
            _flagSwitch.Register(SocketMessageFlag.FILE_REQUEST, OnRequestFileHandler);
            _flagSwitch.Register(SocketMessageFlag.FILE_PART_REQUEST, OnRequestFilePartHandler);
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

        protected override void OnDisconnected()
        {
            OnClientDisconnected();
            Log.WriteLog(LogLevel.INFO, $"Tcp session with Id {Id} disconnected!");
        }

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            _flagSwitch.Switch(buffer, offset, size);
        }

        protected override void OnError(SocketError error)
        {
            Log.WriteLog(LogLevel.ERROR, $"Tcp session caught an error with code {error}");
        }

        //protected override void OnConnected()
        //{
        //    Console.WriteLine($Tcp session with Id {Id} connected!");
        //}

        #endregion ProtectedMethods

        #region Events

        public delegate void ReceiveMessageEventHandler(TcpSession sender, string message);
        public event ReceiveMessageEventHandler? ReceiveMessage;

        public delegate void ClientDisconnectedHandler(TcpSession sender);
        public event ClientDisconnectedHandler? ClientDisconnected;

        public delegate void ClientFileRequestHandler(TcpSession sender, string filePath, long fileSize);
        public event ClientFileRequestHandler? ClientFileRequest;

        private void OnNonRegistredMessage(string message)
        {
            this.Server.FindSession(this.Id).Disconnect();
            Log.WriteLog(LogLevel.WARNING, $"Warning: Non registered message received, disconnecting client!");
        }

        private void OnRequestFileHandler(byte[] buffer, long offset, long size)
        {
            string message = Encoding.UTF8.GetString(buffer, (int)offset, (int)size);
            string[] messageParts = message.Split(ResourceInformer.messageConnector, StringSplitOptions.None);

            if (long.TryParse(messageParts[2], out long fileSize))
            {
                OnClientFileRequest(messageParts[1], fileSize);
            }
            else
            {
                this.Server.FindSession(this.Id).Disconnect();
                Log.WriteLog(LogLevel.WARNING, $"Warning: client is sending wrong formats of data, disconnecting!");
            }
        }

        private void OnRequestFilePartHandler(byte[] buffer, long offset, long size)
        {
            string message = Encoding.UTF8.GetString(buffer, (int)offset, (int)size);
            string[] messageParts = message.Split(ResourceInformer.messageConnector, StringSplitOptions.None);

            if (long.TryParse(messageParts[1], out long filePartNumber) && int.TryParse(messageParts[2], out int partSize) && RequestAccepted) // ak by som sa rozhodol ze nie kazdy part ma rovnaku velkost, musi sa poslat aj zaciatok partu
            {
                Log.WriteLog(LogLevel.DEBUG, $"Received file part request for part: {filePartNumber}, with size: {partSize}, from client: {Socket.RemoteEndPoint}!");
                ResourceInformer.GenerateFilePart(FilePathOfAcceptedfileRequest, this, filePartNumber, partSize);
            }
            else
            {
                this.Server.FindSession(this.Id).Disconnect();
                Log.WriteLog(LogLevel.WARNING, $"Warning: client is sending wrong formats of data, disconnecting!");
            }
        }

        #endregion Events

        #region OverridedMethods



        #endregion OverridedMethods

    }
}