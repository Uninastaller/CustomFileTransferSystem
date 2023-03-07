using System;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;

namespace Modeel
{
    internal class TcpServerSSL_NON
    {
        private TcpListener _listener;
        private X509Certificate2 _certificate;
        private SslStream _sslStream;

        public delegate void ClientConnectedEventHandler(TcpClient client);
        public event ClientConnectedEventHandler ClientConnected;

        public delegate void ClientDisconnectedEventHandler(TcpClient client);
        public event ClientDisconnectedEventHandler ClientDisconnected;

        public delegate void MessageReceivedEventHandler(TcpClient client, string message);
        public event MessageReceivedEventHandler MessageReceived;

        public TcpServerSSL_NON(IPAddress address, int port, X509Certificate2 certificate)
        {
            _listener = new TcpListener(address, port);
            _certificate = certificate;
        }

        public void Start()
        {
            _listener.Start();

            //while (true)
            //{
                //TcpClient client = _listener.AcceptTcpClient();
                _listener.BeginAcceptTcpClient(new AsyncCallback(AcceptCallback), null);
                //Thread clientThread = new Thread(() => HandleClient(client));
                //clientThread.Start();
            //}
        }

        private void AcceptCallback(IAsyncResult AR)
        {
            TcpClient client;
            try
            {
                client = _listener.EndAcceptTcpClient(AR);
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            Thread clientThread = new Thread(() => HandleClient(client));
            clientThread.Start();
            _listener.BeginAcceptTcpClient(new AsyncCallback(AcceptCallback), null);
        }

        private void HandleClient(TcpClient client)
        {
            _sslStream = new SslStream(client.GetStream(), false);
            _sslStream.AuthenticateAsServer(_certificate, false, SslProtocols.Tls, true);

            ClientConnected?.Invoke(client);

            while (true)
            {
                try
                {
                    byte[] buffer = new byte[1024];
                    int bytesReceived = _sslStream.Read(buffer, 0, buffer.Length);

                    if (bytesReceived == 0)
                    {
                        break;
                    }

                    string message = Encoding.UTF8.GetString(buffer, 0, bytesReceived);
                    MessageReceived?.Invoke(client, message);
                }
                catch (Exception)
                {
                    break;
                }
            }

            ClientDisconnected?.Invoke(client);
            _sslStream.Close();
            client.Close();
        }

        public void SendMessageToClient(TcpClient client, string message)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(message);
            _sslStream.Write(buffer);
        }

        public void SendMessageToAllClients(string message)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(message);

            //foreach (TcpClient c in _listener.GetClients())
            //{
            //    SslStream stream = new SslStream(c.GetStream(), false);
            //    stream.Write(buffer);
            //}
        }
    }
}
