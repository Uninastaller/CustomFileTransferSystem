using Modeel.Frq;
using Modeel.Messages;
using Modeel.SSL;
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


namespace Modeel
{
    public class SslClientBussinesLogic : SslClient, IUniversalClientSocket
    {
        private IWindowEnqueuer _gui;
        private bool _sessionWithCentralServer;

        public TypeOfSocket Type { get; }
        public string TransferRateFormatedAsText { get; private set; } = string.Empty;

        private bool _stop;

        private Timer? _timer;
        private UInt64 _timerCounter;

        private const int kilobyte = 1024;
        private const int megabyte = kilobyte * 1024;
        private double transferRate;
        private string unit = string.Empty;
        private long SecondOldBytesSent;

        public SslClientBussinesLogic(SslContext context, IPAddress address, int port, IWindowEnqueuer gui, bool sessionWithCentralServer = false) : base(context, address, port)
        {
            this.Type = TypeOfSocket.TCP_SSL;


            _sessionWithCentralServer = sessionWithCentralServer;

            //Connect();
            ConnectAsync();

            _gui = gui;

            _timer = new Timer(1000); // Set the interval to 1 second
            _timer.Elapsed += OneSecondHandler;
            _timer.Start();
        }

        private void OneSecondHandler(object? sender, ElapsedEventArgs e)
        {
            _timerCounter++;
            FormatDataTransferRate(BytesSent + BytesReceived - SecondOldBytesSent);
            SecondOldBytesSent = BytesSent + BytesReceived;
        }

        public void FormatDataTransferRate(long bytesSent)
        {
            if (bytesSent < kilobyte)
            {
                transferRate = bytesSent;
                unit = "B/s";
            }
            else if (bytesSent < megabyte)
            {
                transferRate = (double)bytesSent / kilobyte;
                unit = "KB/s";
            }
            else
            {
                transferRate = (double)bytesSent / megabyte;
                unit = "MB/s";
            }

            TransferRateFormatedAsText = $"{transferRate:F2} {unit}";
        }

        public void DisconnectAndStop()
        {   
            _stop = true;

            DisconnectAsync();

            Socket.Shutdown(SocketShutdown.Both);

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

        protected override void OnConnected()
        {
            Logger.WriteLog($"Tcp client connected a new session with Id {Id}", LoggerInfo.tcpClient);

            if(_sessionWithCentralServer)
            _gui.BaseMsgEnque(new SocketStateChangeMessage() { SocketState = SocketState.CONNECTED });
        }

        protected override void OnHandshaked()
        {
            Logger.WriteLog($"Tcp client handshaked a new session with Id {Id}", LoggerInfo.tcpClient);
            Send("Hello from SSL client!");
        }

        protected override void OnDisconnected()
        {
            Logger.WriteLog($"Tcp client disconnected from session with Id: {Id}", LoggerInfo.tcpClient);

            // Wait for a while...
            Thread.Sleep(1000);

            // Try to connect again
            if (!_stop)
                ConnectAsync();

            if(_sessionWithCentralServer)
            _gui.BaseMsgEnque(new SocketStateChangeMessage() { SocketState = SocketState.DISCONNECTED });
        }

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            string message = Encoding.UTF8.GetString(buffer, (int)offset, (int)size);
            Logger.WriteLog($"Tcp client obtained a message: HAHA, from: {Endpoint}", LoggerInfo.socketMessage);
        }

        protected override void OnError(SocketError error)
        {
            Logger.WriteLog($"Tcp client caught an error with code {error}", LoggerInfo.tcpClient);
        }
    }
}

