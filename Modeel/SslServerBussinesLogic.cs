using Modeel.Frq;
using Modeel.Messages;
using Modeel.SSL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Timers;
using Timer = System.Timers.Timer;


namespace Modeel
{

    public class SslServerBussinesLogic : SslServer, IUniversalServerSocket
    {
        private IWindowEnqueuer? _gui;
        private Stopwatch? _stopwatch = new Stopwatch();
        private Dictionary<Guid, string>? _clients = new Dictionary<Guid, string>();
        public TypeOfSocket Type { get; }
        public string TransferRateFormatedAsText { get; private set; } = string.Empty;


        private Timer? _timer;
        private UInt64 _timerCounter;

        private const int _kilobyte = 1024;
        private const int _megabyte = _kilobyte * 1024;
        private double _transferRate;
        private string _unit = string.Empty;
        private long _secondOldBytesSent;

        public SslServerBussinesLogic(SslContext context, IPAddress address, int port, IWindowEnqueuer gui, int optionAcceptorBacklog = 1024) : base(context, address, port, optionAcceptorBacklog)
        {
            this.Type = TypeOfSocket.TCP_SSL;

            _gui = gui;
            Start();

            _timer = new Timer(1000); // Set the interval to 1 second
            _timer.Elapsed += OneSecondHandler;
            _timer.Start();
        }

        private void OneSecondHandler(object? sender, ElapsedEventArgs e)
        {
            _timerCounter++;
            FormatDataTransferRate(BytesSent + BytesReceived - _secondOldBytesSent);
            _secondOldBytesSent = BytesSent + BytesReceived;
        }

        protected override void OnDispose()
        {
            if (_timer != null)
            {
                _timer.Elapsed -= OneSecondHandler;
                _timer.Stop();
                _timer.Dispose();
                _timer = null;
            }
            _clients = null;
            _stopwatch = null;
            _timer = null;
            _gui = null;
        }            

        public void FormatDataTransferRate(long bytesSent)
        {
            if (bytesSent < _kilobyte)
            {
                _transferRate = bytesSent;
                _unit = "B/s";
            }
            else if (bytesSent < _megabyte)
            {
                _transferRate = (double)bytesSent / _kilobyte;
                _unit = "KB/s";
            }
            else
            {
                _transferRate = (double)bytesSent / _megabyte;
                _unit = "MB/s";
            }

            TransferRateFormatedAsText = $"{_transferRate:F2} {_unit}";
        }

        protected override SslSession CreateSession() { return new ServerSession(this); }

        protected override void OnError(SocketError error)
        {
            Logger.WriteLog($"Tcp server caught an error with code {error}", LoggerInfo.tcpServer);
        }

        private void OnClientDisconnected(SslSession session)
        {
            if (session is ServerSession serverSession)
            {
                serverSession.ReceiveMessage -= OnReceiveMessage;
                serverSession.ClientDisconnected -= OnClientDisconnected;
            }

            ClientStateChange(SocketState.DISCONNECTED, null, session.Id);
            if(_clients != null && _gui != null)
                _gui.BaseMsgEnque(new ClientStateChangeMessage() { Clients = _clients });
        }

        protected override void OnConnected(SslSession session)
        {
            if (session is ServerSession serverSession)
            {
                serverSession.ReceiveMessage += OnReceiveMessage;
                serverSession.ClientDisconnected += OnClientDisconnected;
            }

            ClientStateChange(SocketState.CONNECTED, session.Socket?.RemoteEndPoint?.ToString(), session.Id);
            if (_clients != null && _gui != null)
                _gui.BaseMsgEnque(new ClientStateChangeMessage() { Clients = _clients });
        }

        private void OnReceiveMessage(SslSession sesion, string message)
        {
            Logger.WriteLog($"Tcp server obtained a message: {message}, from: {sesion.Socket.RemoteEndPoint}", LoggerInfo.socketMessage);

            _stopwatch?.Start();
            SendFile("C:\\Users\\tomas\\Downloads\\The.Office.US.S05.Season.5.Complete.720p.NF.WEB.x264-maximersk [mrsktv]\\The.Office.US.S05E15.720p.NF.WEB.x264-MRSK.mkv", sesion);
            _stopwatch?.Stop();
            TimeSpan elapsedTime = _stopwatch != null ? _stopwatch.Elapsed : TimeSpan.Zero;
            Logger.WriteLog($"File transfer completed in {elapsedTime.TotalSeconds} seconds.", LoggerInfo.P2PSSL);
        }

        private void ClientStateChange(SocketState socketState, string? client, Guid sessionId)
        {
            if (_clients == null)return;
            
            if (socketState == SocketState.CONNECTED && !_clients.ContainsKey(sessionId) && client != null)
            {
                _clients.Add(sessionId, client);
                Logger.WriteLog($"Client: {client}, connected to server ", LoggerInfo.tcpServer);
            }
            else if (socketState == SocketState.DISCONNECTED && _clients.ContainsKey(sessionId))
            {
                Logger.WriteLog($"Client: {_clients[sessionId]}, disconnected from server ", LoggerInfo.tcpServer);
                _clients.Remove(sessionId);
            }
        }

        private void SendFile(string filePath, SslSession session)
        {
            // Open the file for reading
            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                // Choose an appropriate buffer size based on the file size and system resources
                int bufferSize = ResourceInformer.CalculateBufferSize(fileStream.Length);
                Logger.WriteLog($"Fille buffer choosed for: {bufferSize}", LoggerInfo.P2PSSL);

                byte[] buffer = new byte[bufferSize];
                int bytesRead;
                while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    // Send the bytes read from the file over the network stream
                    //SslSession session = FindSession(_clients.ElementAt(0).Key);
                    session.Send(buffer, 0, bytesRead);
                }
            }
        }
    }
}
