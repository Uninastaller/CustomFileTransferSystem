using Common.Enum;
using Common.Interface;
using Common.Model;
using Logger;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Timers;
using Timer = System.Timers.Timer;

namespace TcpSession
{
    public class ServerBussinesLogic : IUniversalServerSocket
    {

        #region Properties

        public string TransferSendRateFormatedAsText { get; private set; } = string.Empty;
        public string TransferReceiveRateFormatedAsText { get; private set; } = string.Empty;

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
        public TypeOfServerSocket Type { get; }

        public long TransferSendRate => 0;

        public long TransferReceiveRate => 0;

        #endregion Properties

        #region PublicFields



        #endregion PublicFields

        #region PrivateFields

        private TcpListener? _listener;
        private bool _isAccepting;
        private bool _isStarted;
        private IPAddress _address;
        private IWindowEnqueuer? _gui;
        private Stopwatch? _stopwatch = new Stopwatch();

        private Dictionary<Guid, System.Net.Sockets.TcpClient> _clients = new Dictionary<Guid, System.Net.Sockets.TcpClient>();

        private Timer? _timer;
        private UInt64 _timerCounter;

        private long _secondOldBytesSent;
        private long _secondOldBytesReceived;

        #endregion PrivateFields

        #region Ctor

        public ServerBussinesLogic(string address, int port, IWindowEnqueuer gui, int optionAcceptorBacklog) : this(IPAddress.Parse(address), port, gui, optionAcceptorBacklog: optionAcceptorBacklog) { }

        public ServerBussinesLogic(IPAddress address, int port, IWindowEnqueuer gui, int optionAcceptorBacklog = 1024)
        {
            this.Type = TypeOfServerSocket.TCP_SERVER;
            _gui = gui;

            Id = Guid.NewGuid();
            _address = address;
            Port = port;
            OptionAcceptorBacklog = optionAcceptorBacklog;

            _timer = new Timer(1000); // Set the interval to 1 second
            _timer.Elapsed += OneSecondHandler;
            _timer.Start();
        }

        #endregion Ctor

        #region PublicMethods

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

        public void SendMessage(System.Net.Sockets.TcpClient client, string message)
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            SendMessage(client, data, 0, data.Length);
        }

        public void SendMessage(System.Net.Sockets.TcpClient client, byte[] data, int index, int lenght)
        {
            if (!client.Connected) return;

            NetworkStream stream = client.GetStream();
            stream.BeginWrite(data, index, lenght, SendMessageCallback, client);
            BytesSent += lenght;
        }

        #endregion PublicMethods

        #region PrivateMethods

        private void TestMessage()
        {
            foreach (KeyValuePair<Guid, System.Net.Sockets.TcpClient> client in _clients)
            {
                //SendMessage(client.Value, "Hellou from ServerBussinesLoggic[1s]");
            }
        }

        private void DisconnectAll()
        {
            foreach (KeyValuePair<Guid, System.Net.Sockets.TcpClient> keyValuePair in _clients)
            {
                keyValuePair.Value.Close();
            }
        }

        private void ClientConnected(System.Net.Sockets.TcpClient client)
        {
            OnConnected(client);

            SendMessage(client, "Hello from Tcp server!");
            Test1BigFile(client);
        }

        private void ReceiveMessage(System.Net.Sockets.TcpClient client, byte[] receivedData)
        {
            OnReceiveMessage(client, receivedData);

            string message = Encoding.UTF8.GetString(receivedData);

            Log.WriteLog(LogLevel.DEBUG, $"Tcp server obtained a message: {message}, from: {client.Client.RemoteEndPoint}");
        }

        private void Test1BigFile(System.Net.Sockets.TcpClient client)
        {
            //_stopwatch?.Start();
            //SendFile("C:\\Users\\tomas\\Downloads\\The.Office.US.S05.Season.5.Complete.720p.NF.WEB.x264-maximersk [mrsktv]\\The.Office.US.S05E15.720p.NF.WEB.x264-MRSK.mkv", client);
            //_stopwatch?.Stop();
            //TimeSpan elapsedTime = _stopwatch != null ? _stopwatch.Elapsed : TimeSpan.Zero;
            //Logger.WriteLog($"File transfer completed in {elapsedTime.TotalSeconds} seconds.", LoggerInfo.socketMessage);
            //MessageBox.Show("File transfer completed");
        }

        private void SendFile(string filePath, System.Net.Sockets.TcpClient client)
        {
            //// Open the file for reading
            //using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            //{
            //    // Choose an appropriate buffer size based on the file size and system resources
            //    int bufferSize = ResourceInformer.CalculateBufferSize(fileStream.Length);
            //    Logger.WriteLog($"Fille buffer choosed for: {bufferSize}", LoggerInfo.socketMessage);

            //    byte[] buffer = new byte[bufferSize];
            //    int bytesRead;
            //    while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
            //    {
            //        // Send the bytes read from the file over the network stream
            //        //SslSession session = FindSession(_clients.ElementAt(0).Key);
            //        SendMessage(client, buffer, 0, bytesRead);
            //        Logger.WriteLog($"Reading bytes from file and sending: {bytesRead}", LoggerInfo.socketMessage);
            //    }
            //}
        }

        private void RemoveClientFromDict(Guid clientId)
        {
            if (_clients.ContainsKey(clientId))
            {
                _clients.Remove(clientId);
            }
        }

        #endregion PrivateMethods

        #region ProtectedMethods



        #endregion ProtectedMethods

        #region EventHandler

        public event Action<System.Net.Sockets.TcpClient> OnClientDisconnected = delegate { };
        public event Action<System.Net.Sockets.TcpClient> OnConnected = delegate { };
        public event Action<System.Net.Sockets.TcpClient, byte[]> OnReceiveMessage = delegate { };

        private void OneSecondHandler(object? sender, ElapsedEventArgs e)
        {
            _timerCounter++;

            TransferSendRateFormatedAsText = ResourceInformer.FormatDataTransferRate(BytesSent - _secondOldBytesSent);
            TransferReceiveRateFormatedAsText = ResourceInformer.FormatDataTransferRate(BytesReceived - _secondOldBytesReceived);
            _secondOldBytesSent = BytesSent;
            _secondOldBytesReceived = BytesReceived;

            TestMessage();
        }

        private void SendMessageCallback(IAsyncResult ar)
        {
            if (ar.AsyncState == null) return;
            System.Net.Sockets.TcpClient client = (System.Net.Sockets.TcpClient)ar.AsyncState;
            NetworkStream stream = client.GetStream();
            stream.EndWrite(ar);
        }

        private void AcceptClientCallback(IAsyncResult ar)
        {
            if (!_isAccepting || _listener == null) return;

            System.Net.Sockets.TcpClient client = _listener.EndAcceptTcpClient(ar);
            Guid clientId = Guid.NewGuid();
            _clients.Add(clientId, client);
            ClientConnected(client);

            byte[] buffer = new byte[OptionReceiveBufferSize];
            NetworkStream stream = client.GetStream();
            stream.BeginRead(buffer, 0, buffer.Length, ReceiveMessageCallback, new Tuple<System.Net.Sockets.TcpClient, NetworkStream, Guid, byte[]>(client, stream, clientId, buffer));

            _listener.BeginAcceptTcpClient(AcceptClientCallback, null);
        }

        private void ReceiveMessageCallback(IAsyncResult ar)
        {
            if (ar.AsyncState == null) return;
            Tuple<System.Net.Sockets.TcpClient, NetworkStream, Guid, byte[]> state = (Tuple<System.Net.Sockets.TcpClient, NetworkStream, Guid, byte[]>)ar.AsyncState;
            System.Net.Sockets.TcpClient client = state.Item1;
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

        #endregion EventHandler

        #region OverridedMethods



        #endregion OverridedMethods

    }
}
