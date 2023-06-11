using Modeel.Frq;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Timers;
using Timer = System.Timers.Timer;

namespace Modeel.FastTcp
{
    public class ServerBussinesLogic : IUniversalServerSocket
    {
        private TcpListener? _listener;
        private bool _isAccepting;
        private bool _isStarted;
        private IPAddress _address;
        private IWindowEnqueuer? _gui;

        private Timer? _timer;
        private UInt64 _timerCounter;

        private const int _kilobyte = 1024;
        private const int _megabyte = _kilobyte * 1024;
        private double _transferRate;
        private string _unit = string.Empty;
        private long _secondOldBytesSent;

        public string TransferRateFormatedAsText { get; private set; } = string.Empty;

        public Guid Id { get; }
        public long ConnectedSessions { get; private set; }
        public bool IsAccepting => _isAccepting;
        public bool IsStarted => _isStarted;
        public int Port { get; }
        public string Address => _address.ToString();
        public long BytesSent { get; private set; }
        public long BytesReceived { get; private set; }
        public int OptionAcceptorBacklog { get; set; } = 1024;
        public int OptionReceiveBufferSize { get; set; } = 8192;
        public int OptionSendBufferSize { get; set; } = 8192;
        public TypeOfSocket Type { get; }

        public ServerBussinesLogic(string address, int port, IWindowEnqueuer gui, int optionAcceptorBacklog) : this(IPAddress.Parse(address), port, gui, optionAcceptorBacklog: optionAcceptorBacklog) { }

        public ServerBussinesLogic(IPAddress address, int port, IWindowEnqueuer gui, int optionAcceptorBacklog = 1024)
        {
            this.Type = TypeOfSocket.TCP;
            _gui = gui;

            Id = Guid.NewGuid();
            _address = address;
            Port = port;
            OptionAcceptorBacklog = optionAcceptorBacklog;

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

        public bool Stop()
        {
            if (!_isStarted) return false;

            _listener?.Stop();
            _isStarted = false;
            _isAccepting = false;
            return true;
        }

        public void Dispose()
        {
            Stop();
        }

        public bool Restart()
        {
            Stop();
            return Start();
        }

        public bool Start()
        {
            if (_isStarted) return false;

            _listener = new TcpListener(_address, Port);
            _listener.Start(OptionAcceptorBacklog);
            _isStarted = true;
            _isAccepting = true;
            _listener.BeginAcceptTcpClient(AcceptClientCallback, null);

            return true;
        }

        private void AcceptClientCallback(IAsyncResult ar)
        {
            if (!_isAccepting || _listener == null) return;

            TcpClient client = _listener.EndAcceptTcpClient(ar);
            ConnectedSessions++;
            OnConnected(client);

            byte[] buffer = new byte[OptionReceiveBufferSize];
            NetworkStream stream = client.GetStream();
            stream.BeginRead(buffer, 0, buffer.Length, ReceiveMessageCallback, new Tuple<TcpClient, NetworkStream>(client, stream));

            _listener.BeginAcceptTcpClient(AcceptClientCallback, null);
        }

        private void ReceiveMessageCallback(IAsyncResult ar)
        {
            if (ar.AsyncState == null) return;
            Tuple<TcpClient, NetworkStream> state = (Tuple<TcpClient, NetworkStream>)ar.AsyncState;
            TcpClient client = state.Item1;
            NetworkStream stream = state.Item2;

            byte[] buffer = new byte[OptionReceiveBufferSize]; // Add this line to declare the buffer

            int bytesRead = stream.EndRead(ar);
            if (bytesRead <= 0)
            {
                OnClientDisconnected(client);
                ConnectedSessions--;
                client.Close();
                return;
            }

            byte[] receivedData = new byte[bytesRead];
            Array.Copy(buffer, receivedData, bytesRead);
            BytesReceived += bytesRead;
            OnReceiveMessage(client, receivedData);
            stream.BeginRead(buffer, 0, buffer.Length, ReceiveMessageCallback, state);
        }

        public event Action<TcpClient> OnClientDisconnected = delegate { };
        public event Action<TcpClient> OnConnected = delegate { };
        public event Action<TcpClient, byte[]> OnReceiveMessage = delegate { };

        public void SendMessage(TcpClient client, string message)
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            SendMessage(client, data);
        }

        public void SendMessage(TcpClient client, byte[] data)
        {
            if (!client.Connected) return;

            NetworkStream stream = client.GetStream();
            stream.BeginWrite(data, 0, data.Length, SendMessageCallback, client);
            BytesSent += data.Length;
        }

        private void SendMessageCallback(IAsyncResult ar)
        {
            if (ar.AsyncState == null) return;
            TcpClient client = (TcpClient)ar.AsyncState;
            NetworkStream stream = client.GetStream();
            stream.EndWrite(ar);
        }
    }

}
