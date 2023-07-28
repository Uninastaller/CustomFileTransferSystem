using Modeel.Log;
using Modeel.Model;
using Modeel.Model.Enums;
using Modeel.SSL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

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
            Logger.WriteLog($"Guid: {Id}, Starting");
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
            Console.WriteLine($"Tcp session with Id {Id} disconnected!");
        }

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            _flagSwitch.Switch(buffer, offset, size);
        }

        protected override void OnError(SocketError error)
        {
            Console.WriteLine($"Tcp session caught an error with code {error}");
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
            Logger.WriteLog($"Warning: Non registered message received, disconnecting client!", LoggerInfo.warning);
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
                Logger.WriteLog($"Warning: client is sending wrong formats of data, disconnecting!", LoggerInfo.warning);
            }
        }

        private void OnRequestFilePartHandler(byte[] buffer, long offset, long size)
        {
            string message = Encoding.UTF8.GetString(buffer, (int)offset, (int)size);
            string[] messageParts = message.Split(ResourceInformer.messageConnector, StringSplitOptions.None);

            if (long.TryParse(messageParts[1], out long filePartNumber) && int.TryParse(messageParts[2], out int partSize) && RequestAccepted) // ak by som sa rozhodol ze nie kazdy part ma rovnaku velkost, musi sa poslat aj zaciatok partu
            {
                Logger.WriteLog($"Received file part request for part: {filePartNumber}, from client: {Socket.RemoteEndPoint}!", LoggerInfo.fileTransfering);
                ResourceInformer.GenerateFilePart(FilePathOfAcceptedfileRequest, this, filePartNumber, partSize);
            }
            else
            {
                this.Server.FindSession(this.Id).Disconnect();
                Logger.WriteLog($"Warning: client is sending wrong formats of data, disconnecting!", LoggerInfo.warning);
            }
        }

        #endregion Events

        #region OverridedMethods



        #endregion OverridedMethods

    }
}