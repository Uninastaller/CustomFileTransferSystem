﻿using Modeel.Frq;
using Modeel.Log;
using Modeel.Messages;
using Modeel.Model;
using Modeel.Model.Enums;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Timers;
using System.Windows.Navigation;
using Timer = System.Timers.Timer;


namespace Modeel.SSL
{
    public class SslClientBussinesLogic : SslClient, IUniversalClientSocket
    {

        #region Properties

        public TypeOfClientSocket Type { get; }
        public string TransferSendRateFormatedAsText { get; private set; } = string.Empty;
        public string TransferReceiveRateFormatedAsText { get; private set; } = string.Empty;

        #endregion Properties

        #region PrivateFields

        private IWindowEnqueuer _gui;
        private bool _sessionWithCentralServer;

        private bool _stop;

        private Timer? _timer;
        private ulong _timerCounter;

        private long _secondOldBytesSent;
        private long _secondOldBytesReceived;

        #endregion PrivateFields

        #region Ctor

        public SslClientBussinesLogic(SslContext context, IPAddress address, int port, IWindowEnqueuer gui, bool sessionWithCentralServer = false) : base(context, address, port)
        {
            Type = TypeOfClientSocket.TCP_CLIENT_SSL;


            _sessionWithCentralServer = sessionWithCentralServer;

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

        private void TestMessage()
        {
            //SendAsync("Hellou from SSlClientBussinesLoggic[1s]");
        }

        #endregion PrivateMethods

        #region EventHandler

        private void OneSecondHandler(object? sender, ElapsedEventArgs e)
        {
            _timerCounter++;

            TransferSendRateFormatedAsText = ResourceInformer.FormatDataTransferRate(BytesSent - _secondOldBytesSent);
            TransferReceiveRateFormatedAsText = ResourceInformer.FormatDataTransferRate(BytesReceived - _secondOldBytesReceived);
            _secondOldBytesSent = BytesSent;
            _secondOldBytesReceived = BytesReceived;

            TestMessage();
        }

        #endregion EventHandler

        #region OverridedMethods

        protected override void OnConnected()
        {
            Logger.WriteLog($"Ssl client connected a new session with Id {Id}", LoggerInfo.sslClient);

            _gui.BaseMsgEnque(new SocketStateChangeMessage() { SocketState = SocketState.CONNECTED, SessionWithCentralServer = _sessionWithCentralServer });
        }

        protected override void OnHandshaked()
        {
            Logger.WriteLog($"Ssl client handshaked a new session with Id {Id}", LoggerInfo.sslClient);
            SendAsync("Hello from SSL client!");
        }

        protected override void OnDisconnected()
        {
            Logger.WriteLog($"Ssl client disconnected from session with Id: {Id}", LoggerInfo.sslClient);

            // Wait for a while...
            Thread.Sleep(1000);

            // Try to connect again
            if (!_stop)
                ConnectAsync();

            _gui.BaseMsgEnque(new SocketStateChangeMessage() { SocketState = SocketState.DISCONNECTED, SessionWithCentralServer = _sessionWithCentralServer });
        }

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            string message = Encoding.UTF8.GetString(buffer, (int)offset, (int)size);
            //Logger.WriteLog($"Ssl client obtained a message[{size}]: {message}", LoggerInfo.socketMessage);
            Logger.WriteLog($"Ssl client obtained a message[{size}]", LoggerInfo.socketMessage);
        }

        protected override void OnError(SocketError error)
        {
            Logger.WriteLog($"Ssl client caught an error with code {error}", LoggerInfo.sslClient);
        }

        #endregion OverridedMethods             

    }
}

