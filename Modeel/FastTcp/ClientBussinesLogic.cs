using Modeel.Frq;
using Modeel.Log;
using Modeel.Messages;
using Modeel.Model;
using Modeel.Model.Enums;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Buffer = System.Buffer;
using Timer = System.Timers.Timer;

namespace Modeel.FastTcp
{
    internal class ClientBussinesLogic : IUniversalClientSocket, IDisposable
    {

        #region Properties

        public TypeOfClientSocket Type { get; }
        public string Address => _address.ToString();
        public Guid Id { get; }
        public bool IsConnecting { get; private set; } = false;
        public string TransferSendRateFormatedAsText { get; private set; } = string.Empty;
        public string TransferReceiveRateFormatedAsText { get; private set; } = string.Empty;
        public bool IsDisposed { get; private set; } = false;
        public bool AutoConnect { get; set; } = true;
        public System.Net.Sockets.TcpClient? Socket { get; private set; }
        public long BytesSent { get; private set; }
        public long BytesReceived { get; private set; }
        public int OptionReceiveBufferSize { get; set; } = 8192;
        public int OptionSendBufferSize { get; set; } = 8192;
        public int Port
        {
            get
            {
                if (IsConnected && Socket != null && Socket.Client != null && Socket.Client.LocalEndPoint != null)
                {
                    return ((IPEndPoint)Socket.Client.LocalEndPoint).Port;
                }
                return _port;
            }
            private set
            {
                _port = value;
            }
        }

        public bool IsConnected
        {
            get
            {
                return _isConnected;
            }
            private set
            {
                if (value != _isConnected)
                {
                    _isConnected = value;
                    _gui.BaseMsgEnque(new SocketStateChangeMessage() { SocketState = value ? SocketState.CONNECTED : SocketState.DISCONNECTED, TypeOfSession = _typeOfSession });
                }
            }
        }

        public long TransferSendRate { get; private set; }
        public long TransferReceiveRate { get; private set; }

        #endregion Properties

        #region PrivateFields

        private byte[] _readBuffer = new byte[1000];

        private TypeOfSession _typeOfSession;
        private bool _isConnected = false;

        private readonly IWindowEnqueuer _gui;
        private readonly IPAddress _address;
        private int _port;
        private NetworkStream? _stream;
        private IAsyncResult? result;

        private long _secondOldBytesSent;
        private long _secondOldBytesReceived;


        private readonly Timer _timer;
        private UInt64 _timerCounter;


        string socksHost = "127.0.0.1";
        int socksPort = 9050;
        WebProxy proxy;

        private IPAddress? _publicIpAddress;
        private IPAddress? _localIpAddress;

        #endregion PrivateFields

        #region Ctor

        public ClientBussinesLogic(IPAddress address, int port, IWindowEnqueuer gui, TypeOfSession typeOfSession = TypeOfSession.DOWNLOADING)
        {
            // Create a TCP client and configure the SOCKS proxy
            //System.Net.Sockets.TcpClient tcpClient = new System.Net.Sockets.TcpClient(new IPEndPoint(IPAddress.Parse(socksHost), socksPort));
            // Configure the SOCKS proxy settings
            // Set the SOCKS proxy address and port
            //string socksHost = "127.0.0.1";
            //int socksPort = 9050;
            //proxy = new WebProxy(socksHost, socksPort);
            //proxy.ProxyType = WebProxyType.Socks;



            Id = Guid.NewGuid();
            this.Type = TypeOfClientSocket.TCP_CLIENT;

            _typeOfSession = typeOfSession;

            _gui = gui;
            _address = address;
            _port = port;

            _timer = new Timer(1000); // Set the interval to 1 second
            _timer.Elapsed += OneSecondHandler;
            _timer.Start();

            Init();
        }

        #endregion Ctor

        #region PublicMethods

        public async void Init()
        {
            _publicIpAddress = await NetworkUtils.GetPublicIPAddress();
            _localIpAddress = NetworkUtils.GetLocalIPAddress();
        }

        public virtual bool DisconnectAsync() => Disconnect();

        public long Send(string message)
        {
            return Send(Encoding.UTF8.GetBytes(message));
        }

        public long Send(byte[] buffer)
        {
            long sent = buffer.Length;
            _stream?.Write(buffer);
            return BytesSent += sent;
        }

        public async Task<long> SendAsync(string message)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(message);
            return await SendAsync(buffer);
        }

        public async Task<long> SendAsync(byte[] buffer)
        {
            long sent = buffer.Length;
            if (_stream != null)
            {
                await _stream.WriteAsync(buffer, 0, buffer.Length);
            }
            else
            {
                sent = 0;
            }
            return BytesSent += sent;
        }

        public bool ConnectAsync()
        {

            if (IsConnecting || IsConnected)
            {
                return false;
            }

            try
            {
                //AutoConnect = false;

                // Set IsConnecting to true to indicate that the client is attempting to connect
                IsConnecting = true;

                Socket = new System.Net.Sockets.TcpClient();

                // Start connecting to the server asynchronously
                //result = Socket.BeginConnect(_address, _port, null, null);
                result = Socket.BeginConnect(_address, socksPort, null, null);

                // Wait up to 5 seconds for the connection to complete
                bool success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(5));

                if (success)
                {
                    // End the asynchronous connection attempt and get a NetworkStream object from the TcpClient
                    Socket.EndConnect(result);
                    _stream = Socket.GetStream();

                    // Start an asynchronous read operation to receive data from the server
                    //_stream.BeginRead(_readBuffer, 0, _readBuffer.Length, HandleReceivedData, null);

                    OnConnected();

                    return true;
                }
                else
                {
                    Logger.WriteLog(LogLevel.DEBUG, "Connection attempt timed out.");
                    AutoConnect = true;
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(LogLevel.ERROR, $"Failed to connect to server: {ex.Message}");

                // Set IsConnecting to false to indicate that the client is not connecting
                IsConnecting = false;

                AutoConnect = true;
                return false;
            }
        }

        public bool Disconnect()
        {

            if (!IsConnected && !IsConnecting)
                return false;

            // Cancel connecting operation
            if (IsConnecting && result != null)
                Socket?.EndConnect(result);

            // Reset connecting flag
            IsConnecting = false;

            try
            {
                // Close the network stream and TcpClient
                _stream?.Close();
                _stream?.Dispose();
                _stream = null;
                Socket?.Close();
                Socket?.Dispose();
                Socket = null;
                IsConnected = false;
                return true;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(LogLevel.ERROR, $"Failed to disconnect from server: {ex.Message}");
                return false;
            }
        }

        public void DisconnectAndStop()
        {
            AutoConnect = false;
            DisconnectAsync();
            while (IsConnected)
                Thread.Yield();
        }

        public void Dispose()
        {
            _timer.Elapsed -= OneSecondHandler;
            _timer.Stop();
            _timer.Dispose();
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion PublicMethods

        #region PrivateMethods

        private void TestMessage()
        {
            //_ = SendAsync("Hellou from ClientBussinesLoggic[1s]");
        }

        private async void StartReceivingData()
        {
            try
            {
                while (IsConnected)
                {
                    //await ReceiveDataAsync();
                    await Task.Run(() => ReceiveDataAsync());
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(LogLevel.ERROR, $"Failed to start receiving data: {ex.Message}");
                Disconnect();
            }
        }

        private void OnConnected()
        {

            // Send a request to connect to the target host and port through the SOCKS proxy
            //string targetHost = "127.0.0.1";
            int targetPort = _port;
            //IPAddress ipAddress;
            //bool isIp = IPAddress.TryParse(targetHost, out ipAddress);

            byte[] request4 = new byte[9];
            request4[0] = 0x04;
            request4[1] = 0x01;
            request4[2] = (byte)((targetPort >> 8) & 0xFF); // Port high byte
            request4[3] = (byte)(targetPort & 0xFF);        // Port low byte
            _publicIpAddress?.GetAddressBytes().CopyTo(request4, 4);
            request4[8] = 0x00; // Null terminator


            byte[] request5 = new byte[10];
            request5[0] = 0x05;
            request5[1] = 0x00;
            request5[2] = 0x00;











            //// Set the SOCKS version based on whether the targetHost is an IP address or a domain name
            //byte socksVersion = (byte)(isIp ? 0x04 : 0x05);

            //// Send a request to connect to the target host and port through the SOCKS proxy
            //byte[] request;
            //if (socksVersion == 0x04)
            //{
            //    // SOCKS4 request
            //    request = new byte[9];
            //    request[0] = 0x04; // SOCKS version 4
            //    request[1] = 0x01; // Command: Connect
            //    request[2] = (byte)((targetPort >> 8) & 0xFF); // Port high byte
            //    request[3] = (byte)(targetPort & 0xFF);        // Port low byte
            //    ipAddress.GetAddressBytes().CopyTo(request, 4);
            //    request[8] = 0x00; // Null terminator
            //}
            //else
            //{
            //    // SOCKS5 request
            //    request = new byte[10];
            //    request[0] = 0x05; // SOCKS version 5
            //    request[1] = 0x01; // Command: Connect
            //    request[2] = 0x00; // Reserved (must be 0x00)

            //    if (isIp)
            //    {
            //        ipAddress.GetAddressBytes().CopyTo(request, 3);
            //    }
            //    else
            //    {
            //        // Use domain name
            //        request[3] = 0x03; // Address type: Domain name
            //        byte[] domainBytes = System.Text.Encoding.ASCII.GetBytes(targetHost);
            //        request[4] = (byte)domainBytes.Length;
            //        domainBytes.CopyTo(request, 5);
            //    }

            //    request[request.Length - 2] = (byte)((targetPort >> 8) & 0xFF); // Port high byte
            //    request[request.Length - 1] = (byte)(targetPort & 0xFF);        // Port low byte
            //}

            Send(request4);
            //NetworkStream proxyStream = Socket.GetStream();
            //proxyStream.Write(request, 0, request.Length);

            //Send($"CONNECT {_address}:{_port} HTTP/1.1<CR><LF>");
            //Send($"<CR><LF>");

            //Send($"socks5h://localhost:{_port}");


            IsConnecting = false;
            IsConnected = true;
            StartReceivingData();
        }

        private void OnMessageReceived(byte[] buffer)
        {
            string message = Encoding.UTF8.GetString(buffer);

            //_gui.BaseMsgEnque(new MessageReceiveMessage() { Message = message });

            //Logger.WriteLog($"Tcp client obtained a message[{buffer.Length}]: {message}", LoggerInfo.socketMessage);
            Logger.WriteLog(LogLevel.DEBUG, $"Tcp client obtained a message[{buffer.Length}]");
        }

        #endregion PrivateMethods

        #region ProtectedMethods

        protected virtual void Dispose(bool disposingManagedResources)
        {
            // The idea here is that Dispose(Boolean) knows whether it is
            // being called to do explicit cleanup (the Boolean is true)
            // versus being called due to a garbage collection (the Boolean
            // is false). This distinction is useful because, when being
            // disposed explicitly, the Dispose(Boolean) method can safely
            // execute code using reference type fields that refer to other
            // objects knowing for sure that these other objects have not been
            // finalized or disposed of yet. When the Boolean is false,
            // the Dispose(Boolean) method should not execute code that
            // refer to reference type fields because those objects may
            // have already been finalized."

            Socket?.Close();
            Socket = null;
            _stream?.Dispose();
            _stream = null;

            if (!IsDisposed)
            {
                if (disposingManagedResources)
                {
                    // Dispose managed resources here...
                    DisconnectAsync();
                }

                // Dispose unmanaged resources here...

                // Set large fields to null here...

                // Mark as disposed.
                IsDisposed = true;
            }
        }

        #endregion ProtectedMethods

        #region EventHandlers

        private void OneSecondHandler(object? sender, ElapsedEventArgs e)
        {
            _timerCounter++;

            TransferSendRate = BytesSent - _secondOldBytesSent;
            TransferReceiveRate = BytesReceived - _secondOldBytesReceived;
            TransferSendRateFormatedAsText = ResourceInformer.FormatDataTransferRate(TransferSendRate);
            TransferReceiveRateFormatedAsText = ResourceInformer.FormatDataTransferRate(TransferReceiveRate);
            _secondOldBytesSent = BytesSent;
            _secondOldBytesReceived = BytesReceived;

            if (_timerCounter % 10 == 0)
            {
                if (AutoConnect)
                {
                    ConnectAsync();
                }
            }
            TestMessage();
        }

        private async Task ReceiveDataAsync()
        {
            try
            {
                while (IsConnected)
                {
                    int bytesRead = await _stream.ReadAsync(_readBuffer, 0, _readBuffer.Length);

                    if (bytesRead > 0)
                    {
                        byte[] buffer = new byte[bytesRead];
                        Array.Copy(_readBuffer, buffer, bytesRead);
                        BytesReceived += bytesRead;
                        OnMessageReceived(buffer);
                    }
                    else
                    {
                        // The server has closed the connection
                        Disconnect();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(LogLevel.ERROR, $"Failed to receive data from server: {ex.Message}");
                Disconnect();
            }
        }

        //private void HandleReceivedData(IAsyncResult ar)
        //{
        //    try
        //    {
        //        // End the read operation and get the number of bytes read
        //        if (_stream == null) return;

        //        int bytesRead = _stream.EndRead(ar);

        //        if (bytesRead > 0)
        //        {
        //            // Get the data from the buffer
        //            byte[] buffer = new byte[bytesRead];
        //            Array.Copy(_readBuffer, buffer, bytesRead);
        //            BytesReceived += bytesRead;
        //            // Process the received data here...                   
        //            OnMessageReceived(buffer);
        //            // Start another asynchronous read operation to receive more data
        //            _stream.BeginRead(_readBuffer, 0, _readBuffer.Length, HandleReceivedData, null);
        //        }
        //        else
        //        {
        //            // The server has closed the connection
        //            Disconnect();
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.WriteLog($"Failed to receive data from server: {ex.Message}");
        //        Disconnect();
        //    }
        //}

        #endregion EventHandlers

    }
}
