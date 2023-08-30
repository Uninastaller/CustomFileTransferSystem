using Common.Enum;
using Common.Model;
using Logger;
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
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

        #endregion PrivateMethods

        #region ProtectedMethods

        protected override void OnHandshaked()
        {
            Log.WriteLog(LogLevel.INFO, $"Ssl session with Id {Id} handshaked!");
        }

        protected override async void OnConnected()
        {
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

        private void OnOfferingFileHandler(byte[] buffer, long offset, long size)
        {
            if(FlagMessageEvaluator.EvaluateOfferingFile(buffer, offset, size, out OfferingFileDto? offeringFileDto))
            {

            }
        }


        #endregion Events

        #region OverridedMethods



        #endregion OverridedMethods

    }
}

