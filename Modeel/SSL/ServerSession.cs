using System;
using System.Net.Sockets;
using System.Text;

namespace Modeel.SSL
{
    class ServerSession : SslSession
    {

        public delegate void ReceiveMessageEventHandler(SslSession sender, string message);
        public event ReceiveMessageEventHandler ReceiveMessage;

        public delegate void ClientDisconnectedHandler(SslSession sender);
        public event ClientDisconnectedHandler ClientDisconnected;

        public ServerSession(SslServer server) : base(server) { }

        //protected override void OnConnected()
        //{
        //    Console.WriteLine($"Chat SSL session with Id {Id} connected!");
        //}

        protected void OnClientDisconnected()
        {
            ClientDisconnected?.Invoke(this);
        }

        protected void OnReceiveMessage(string message)
        {
            ReceiveMessage?.Invoke(this, message);
        }

        protected override void OnHandshaked()
        {
            Console.WriteLine($"Chat SSL session with Id {Id} handshaked!");

            // Send invite message
            string message = "Hello from SSL server!";
            Send(message);
        }

        protected override void OnDisconnected()
        {
            OnClientDisconnected();
            Console.WriteLine($"Chat SSL session with Id {Id} disconnected!");
        }

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            string message = Encoding.UTF8.GetString(buffer, (int)offset, (int)size);

            //// Multicast message to all connected sessions
            //Server.Multicast(message);

            OnReceiveMessage(message);
        }

        protected override void OnError(SocketError error)
        {
            Console.WriteLine($"Chat SSL session caught an error with code {error}");
        }
    }
}

