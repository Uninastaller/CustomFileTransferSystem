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
            _flagSwitch.Register(SocketMessageFlag.OFFERING_FILES_REQUEST, OnOfferingFilesRequestHandler);
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

        private async Task SendOfferingFilesToClient()
        {
            ServerSessionState = ServerSessionState.OFFERING_FILES_SENDING;
            List<OfferingFileDto> offeringFiles = await SqliteDataAccess.GetAllOfferingFilesWithGradesAsync();
            for (int i = 0; i < offeringFiles.Count; i++)
            {
                FlagMessagesGenerator.GenerateOfferingFile(offeringFiles[i].GetJson(), i == offeringFiles.Count - 1, this);
            }
        }

        #endregion PrivateMethods

        #region ProtectedMethods

        protected override void OnHandshaked()
        {
            Log.WriteLog(LogLevel.INFO, $"Ssl session with Id {Id} handshaked!");

            //int maxRepeatCounter = 3;
            //await Task.Delay(100);

            //ServerSessionState = ServerSessionState.OFFERING_FILES_RECEIVING;

            //// Staf to do after 200ms => waiting without blocking thread
            //while (FlagMessagesGenerator.GenerateOfferingFilesRequest(this) == MethodResult.ERROR && maxRepeatCounter-- >= 0)
            //{
            //    await Task.Delay(200);
            //}

            //if (maxRepeatCounter < -1)
            //{
            //    Disconnect();
            //}
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
            Log.WriteLog(LogLevel.WARNING, $"Non registered message received, disconnecting client!");
        }

        private async void OnOfferingFileHandler(byte[] buffer, long offset, long size)
        {

            if (ServerSessionState == ServerSessionState.NONE)
            {
                ServerSessionState = ServerSessionState.OFFERING_FILES_RECEIVING;
            }

            if (ServerSessionState == ServerSessionState.OFFERING_FILES_RECEIVING)
            {
                if (FlagMessageEvaluator.EvaluateOfferingFileMessage(buffer, offset, size, out List<OfferingFileDto?> offeringFileDto, out bool endOfMessageGroup))
                {
                    await OnNewOfferingFileReceived(offeringFileDto);
                    if (endOfMessageGroup)
                    {
                        // Client should disconnect automatically after send all data, but if no, manually destroy session
                        Log.WriteLog(LogLevel.INFO, $"All Offering File was receiver! Destroying session");
                        Disconnect();
                    }
                }
            }
            else
            {
                Log.WriteLog(LogLevel.WARNING, $"Offering File received, but session is not in default state, so message can not be proceed!");
            }
        }

        /// <summary>
        /// If server have no offering files, so server will not automatically send him offering files bcs of leak end of pessage group. He send offering files request to get offering files from server
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="size"></param>
        private async void OnOfferingFilesRequestHandler(byte[] buffer, long offset, long size)
        {
            Log.WriteLog(LogLevel.DEBUG, $"Offering File request received");
            if (ServerSessionState == ServerSessionState.NONE)
            {
                await SendOfferingFilesToClient();
                Log.WriteLog(LogLevel.INFO, $"All Offering File was sended to client! Destroying session");
                // Client should disconnect automatically after send all data, but if no, manually destroy session
                Disconnect();
            }
            else
            {
                Log.WriteLog(LogLevel.WARNING, $"Offering File request received, but session is not in default state for this message, so message will not be proceed!");
            }
        }

        #endregion Events

        #region OverridedMethods



        #endregion OverridedMethods

    }
}

