using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Modeel
{
    class Server
    {
        private Socket? _serverSocket;
        private IPAddress _ipAddress;
        private IPEndPoint? _localEndPoint;

        private Dictionary<Socket, byte[]> bufferAndSocketHolder = new Dictionary<Socket, byte[]>();

        private const int BUFFER_SIZE = 2048;

        public Server(IPAddress ipAddress, int port)
        {
            _ipAddress = ipAddress;
            ConfigureSocket(port);
        }

        private void ConfigureSocket(int port)
        {
            Set(port);
            Create();
            Bind();
            Listen();
        }

        private void Set(int port)
        {
            if (_ipAddress != null)
            {
                _localEndPoint = new IPEndPoint(_ipAddress, port);
            }
        }

        private void Create()
        {
            if (_ipAddress != null)
            {
                try
                {
                    _serverSocket = new Socket(_ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                }
                catch (SocketException)
                {
                    ErrorWithStarting("Failed to create socket", false);
                }
                Console.WriteLine("Socket created");
            }
        }

        private void Bind()
        {
            if (_serverSocket != null && _localEndPoint != null)
            {
                try
                {
                    _serverSocket.Bind(_localEndPoint);
                }
                catch (SocketException)
                {
                    ErrorWithStarting("Error with binding", true);
                }
                Console.WriteLine("Socket bound");
            }
            else
            {
                ErrorWithStarting("ServerSocket or LocalEndPoint is null", true);
            }
        }

        private void Listen()
        {
            if (_serverSocket != null)
            {
                _serverSocket.Listen(10);
                Console.WriteLine("Socket listening");
                _serverSocket.BeginAccept(new AsyncCallback(AcceptCallback), null);
            }
            else
            {
                ErrorWithStarting("ServerSocket is null", true);
            }
        }

        private void AcceptCallback(IAsyncResult AR)
        {
            if (_serverSocket != null)
            {
                Socket socket;
                try
                {
                    socket = _serverSocket.EndAccept(AR);
                }
                catch (ObjectDisposedException)
                {
                    return;
                }

                bufferAndSocketHolder.Add(socket, new byte[BUFFER_SIZE]);

                socket.BeginReceive(bufferAndSocketHolder[socket], 0, BUFFER_SIZE, SocketFlags.None, new AsyncCallback(ReceiveCallback), socket);
                Console.WriteLine("Client {0} - connected", socket.Handle);
                _serverSocket.BeginAccept(AcceptCallback, null);
            }
            else
            {
                ErrorWithStarting("ServerSocket is null", true);
            }
        }

        private void ReceiveCallback(IAsyncResult AR)
        {
            Console.Write("[THREAD {0}] ", Thread.CurrentThread.ManagedThreadId);
            Socket? socket = AR.AsyncState as Socket;
            if (socket != null)
            {
                int amountOfBytes;
                try
                {
                    amountOfBytes = socket.EndReceive(AR);
                }
                catch (SocketException)
                {
                    Console.WriteLine("Client {0} - forcefully disconnected", socket.Handle);
                    SocketSocket(socket);
                    return;
                }

                byte[] msg = new byte[amountOfBytes];
                Array.Copy(bufferAndSocketHolder[socket], msg, amountOfBytes);

                string data = Encoding.ASCII.GetString(msg);
                Console.WriteLine("[Client " + socket.Handle + "] " + data);
                EvaluateOfReceivedData(data, socket);
            }
        }

        private void EvaluateOfReceivedData(string data, Socket socket)
        {

        }

        private void Send(string data, Socket socket)
        {
            byte[] msg = Encoding.ASCII.GetBytes(data);
            socket.Send(msg);
            socket.BeginReceive(bufferAndSocketHolder[socket], 0, BUFFER_SIZE, SocketFlags.None, new AsyncCallback(ReceiveCallback), socket);

        }

        private void CloseAllSockets()
        {
            foreach (Socket socket in bufferAndSocketHolder.Keys)
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }
        }

        private void SocketSocket(Socket socket)
        {
            socket.Close();
            bufferAndSocketHolder.Remove(socket);
        }

        private void ErrorWithStarting(string message, bool socketClose)
        {
            if (socketClose && _serverSocket != null) _serverSocket.Close();
            Console.WriteLine(message);
            Environment.Exit(0);
        }
    }
}