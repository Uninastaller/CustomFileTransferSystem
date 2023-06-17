using Modeel.Frq;
using Modeel.SSL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
        private Stopwatch? _stopwatch = new Stopwatch();

        private Dictionary<Guid, TcpClient> _clients = new Dictionary<Guid, TcpClient>();

        private Timer? _timer;
        private UInt64 _timerCounter;

        private const int _kilobyte = 1024;
        private const int _megabyte = _kilobyte * 1024;
        private double _transferRate;
        private string _unit = string.Empty;
        private long _secondOldBytesSent;

        public string TransferRateFormatedAsText { get; private set; } = string.Empty;

        public Guid Id { get; }
        public long ConnectedSessions => _clients.Count;
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

            TestMessage();
        }

        private void TestMessage()
        {
            foreach (KeyValuePair<Guid, TcpClient> client in _clients)
            {
                SendMessage(client.Value, "Hellou from ServerBussinesLoggic[1s]");
            }
        }

        private void FormatDataTransferRate(long bytesSent)
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
            DisconnectAll();
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

        private void DisconnectAll()
        {
            foreach (KeyValuePair<Guid, TcpClient> keyValuePair in _clients)
            {
                keyValuePair.Value.Close();
            }
        }

        private void AcceptClientCallback(IAsyncResult ar)
        {
            if (!_isAccepting || _listener == null) return;

            TcpClient client = _listener.EndAcceptTcpClient(ar);
            Guid clientId = Guid.NewGuid();
            _clients.Add(clientId, client);
            OnConnected(client);

            byte[] buffer = new byte[OptionReceiveBufferSize];
            NetworkStream stream = client.GetStream();
            stream.BeginRead(buffer, 0, buffer.Length, ReceiveMessageCallback, new Tuple<TcpClient, NetworkStream, Guid, byte[]>(client, stream, clientId, buffer));

            _listener.BeginAcceptTcpClient(AcceptClientCallback, null);
        }

        private void ReceiveMessageCallback(IAsyncResult ar)
        {
            if (ar.AsyncState == null) return;
            Tuple<TcpClient, NetworkStream, Guid, byte[]> state = (Tuple<TcpClient, NetworkStream, Guid, byte[]>)ar.AsyncState;
            TcpClient client = state.Item1;
            NetworkStream stream = state.Item2;
            Guid clientId = state.Item3;
            byte[] buffer = state.Item4;

            if (!stream.CanRead)
            {
                RemoveClientFromDict(clientId);
                OnClientDisconnected(client);
                client.Close();
                return;
            }

            //byte[] buffer = new byte[OptionReceiveBufferSize]; // Add this line to declare the buffer

            int bytesRead = stream.EndRead(ar);
            if (bytesRead <= 0)
            {
                RemoveClientFromDict(clientId);
                OnClientDisconnected(client);
                client.Close();
                return;
            }

            byte[] receivedData = new byte[bytesRead];
            Array.Copy(buffer, receivedData, bytesRead);
            BytesReceived += bytesRead;
            ReceiveMessage(client, receivedData);
            stream.BeginRead(buffer, 0, buffer.Length, ReceiveMessageCallback, state);
        }

        private void ReceiveMessage(TcpClient client, byte[] receivedData)
        {
            OnReceiveMessage(client, receivedData);

            string message = Encoding.UTF8.GetString(receivedData);

            Logger.WriteLog($"Tcp server obtained a message: {message}, from: {client.Client.RemoteEndPoint}", LoggerInfo.socketMessage);

            return;


            _stopwatch?.Start();
            SendFile("C:\\Users\\tomas\\Downloads\\The.Office.US.S05.Season.5.Complete.720p.NF.WEB.x264-maximersk [mrsktv]\\The.Office.US.S05E15.720p.NF.WEB.x264-MRSK.mkv", client);
            _stopwatch?.Stop();
            TimeSpan elapsedTime = _stopwatch != null ? _stopwatch.Elapsed : TimeSpan.Zero;
            Logger.WriteLog($"File transfer completed in {elapsedTime.TotalSeconds} seconds.", LoggerInfo.P2PSSL);
        }

        private void SendFile(string filePath, TcpClient client)
        {
            // Open the file for reading
            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                // Choose an appropriate buffer size based on the file size and system resources
                int bufferSize = ResourceInformer.CalculateBufferSize(fileStream.Length);
                Logger.WriteLog($"Fille buffer choosed for: {bufferSize}", LoggerInfo.P2P);

                byte[] buffer = new byte[bufferSize];
                int bytesRead;
                while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    // Send the bytes read from the file over the network stream
                    //SslSession session = FindSession(_clients.ElementAt(0).Key);
                    SendMessage(client, buffer, 0, bytesRead);
                }
            }
        }

        private void RemoveClientFromDict(Guid clientId)
        {
            if (_clients.ContainsKey(clientId))
            {
                _clients.Remove(clientId);
            }
        }

        public event Action<TcpClient> OnClientDisconnected = delegate { };
        public event Action<TcpClient> OnConnected = delegate { };
        public event Action<TcpClient, byte[]> OnReceiveMessage = delegate { };

        public void SendMessage(TcpClient client, string message)
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            SendMessage(client, data, 0, data.Length);
        }

        public void SendMessage(TcpClient client, byte[] data, int index, int lenght)
        {
            if (!client.Connected) return;

            NetworkStream stream = client.GetStream();
            stream.BeginWrite(data, index, lenght, SendMessageCallback, client);
            BytesSent += lenght;
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
