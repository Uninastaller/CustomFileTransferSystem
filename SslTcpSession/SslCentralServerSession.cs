using CentralServer;
using Common.Enum;
using Common.Model;
using ConfigManager;
using Logger;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading.Tasks;

namespace SslTcpSession
{
    class SslCentralServerSession : SslSession
    {

        #region Properties

        public ServerSessionState ServerSessionState
        {
            get => _serverSessionState;

            set
            {
                if (value != _serverSessionState)
                {
                    _serverSessionState = value;
                    ServerSessionStateChange?.Invoke(this, value);
                }
            }
        }

        #endregion Properties

        #region PublicFields


        #endregion PublicFields

        #region PrivateFields

        private ServerSessionState _serverSessionState = ServerSessionState.NONE;

        #endregion PrivateFields

        #region ProtectedFields



        #endregion ProtectedFields

        #region Ctor

        public SslCentralServerSession(SslServer server) : base(server)
        {
            Log.WriteLog(LogLevel.INFO, $"Guid: {Id}, Starting");

            _flagSwitch.OnNonRegistered(OnNonRegistredMessage);
            _flagSwitch.Register(SocketMessageFlag.OFFERING_FILE, OnOfferingFileHandler);
        }

        #endregion Ctor

        #region PublicMethods



        #endregion PublicMethods

        #region PrivateMethods

        private void OnClientDisconnected()
        {
            ClientDisconnected?.Invoke(this);
        }

        private async Task OnNewOfferingFileReceived(List<OfferingFileDto?> offeringFileDtos)
        {
            foreach (OfferingFileDto? offeringFileDto in offeringFileDtos)
            {
                if (offeringFileDto != null)
                {
                    await SqliteDataAccess.InsertOrUpdateOfferingFileDtoAsync(offeringFileDto);
                }
            }
        }

        #endregion PrivateMethods

        #region ProtectedMethods

        protected async override void OnHandshaked()
        {
            Log.WriteLog(LogLevel.INFO, $"Ssl session with Id {Id} handshaked!");

            int maxRepeatCounter = 3;
            await Task.Delay(100);

            // Staf to do after 200ms => waiting without blocking thread
            while (FlagMessagesGenerator.GenerateOfferingFilesRequest(this) == MethodResult.ERROR && maxRepeatCounter-- >= 0)
            {
                await Task.Delay(200);
            }

            if (maxRepeatCounter < -1)
            {
                Disconnect();
            }
        }

        protected override void OnDisconnected()
        {
            OnClientDisconnected();
            Log.WriteLog(LogLevel.INFO, $"Ssl session with Id {Id} disconnected!");
        }

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            _flagSwitch.Switch(buffer, offset, size);
        }

        protected override void OnError(SocketError error)
        {
            Log.WriteLog(LogLevel.ERROR, $"Ssl session caught an error with code {error}");
        }

        #endregion ProtectedMethods

        #region Events

        public delegate void ClientDisconnectedHandler(SslSession sender);
        public event ClientDisconnectedHandler? ClientDisconnected;

        public delegate void ServerSessionStateChangeEventHandler(SslSession sender, ServerSessionState serverSessionState);
        public event ServerSessionStateChangeEventHandler? ServerSessionStateChange;

        private void OnNonRegistredMessage(string message)
        {
            ServerSessionState = ServerSessionState.NONE;
            this.Server?.FindSession(this.Id)?.Disconnect();
            Log.WriteLog(LogLevel.WARNING, $"Warning: Non registered message received, disconnecting client!");
        }

        private async void OnOfferingFileHandler(byte[] buffer, long offset, long size)
        {
            if (FlagMessageEvaluator.EvaluateOfferingFileMessage(buffer, offset, size, out List<OfferingFileDto?> offeringFileDto))
            {
                await OnNewOfferingFileReceived(offeringFileDto);
            }
        }


        #endregion Events

        #region OverridedMethods



        #endregion OverridedMethods

    }
}

