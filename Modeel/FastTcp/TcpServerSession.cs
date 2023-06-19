using Modeel.Log;
using Modeel.Model;
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

            _flagSwitch.OnNonRegistered(OnNonRegistredMessage);
            _flagSwitch.Register(SocketMessageFlag.REQUEST, OnRegisterHandler);

        }

        #endregion Ctor

        #region PublicMethods



        #endregion PublicMethods

        #region PrivateMethods

        private void OnNonRegistredMessage()
        {
            this.Server.FindSession(this.Id).Disconnect();
            Logger.WriteLog($"Warning: Non registered message received, disconnecting client!", LoggerInfo.warning);
        }

        private void OnRegisterHandler(byte[] buffer, long offset, long size)
        {
            string message = Encoding.UTF8.GetString(buffer, (int)offset, (int)size);
            OnReceiveMessage(message);
        }

        #endregion PrivateMethods

        #region ProtectedMethods

        protected void OnClientDisconnected()
        {
            ClientDisconnected?.Invoke(this);
        }

        protected void OnReceiveMessage(string message)
        {
            ReceiveMessage?.Invoke(this, message);
        }

        protected override void OnDisconnected()
        {
            OnClientDisconnected();
            Console.WriteLine($"Tcp session with Id {Id} disconnected!");
        }

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            if (size < 3)
            {
                Logger.WriteLog($"Warning, received message with too few bytes, size: {size}", LoggerInfo.warning);
                return;
            }

            _flagSwitch.Switch(buffer.Skip((int)offset).Take(3).ToArray(), buffer, offset, size);
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
        public event ReceiveMessageEventHandler ReceiveMessage;

        public delegate void ClientDisconnectedHandler(TcpSession sender);
        public event ClientDisconnectedHandler ClientDisconnected;

        #endregion Events

        #region OverridedMethods



        #endregion OverridedMethods
        
    }
}