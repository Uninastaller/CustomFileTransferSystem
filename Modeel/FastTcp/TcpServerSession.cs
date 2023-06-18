using Modeel.SSL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Modeel.FastTcp
{
    public class TcpServerSession : TcpSession
    {

        public delegate void ReceiveMessageEventHandler(TcpSession sender, string message);
        public event ReceiveMessageEventHandler ReceiveMessage;

        public delegate void ClientDisconnectedHandler(TcpSession sender);
        public event ClientDisconnectedHandler ClientDisconnected;

        public TcpServerSession(TcpServer server) : base(server) { }

        //protected override void OnConnected()
        //{
        //    Console.WriteLine($Tcp session with Id {Id} connected!");
        //}

        protected void OnClientDisconnected()
        {
            ClientDisconnected?.Invoke(this);
        }

        protected void OnReceiveMessage(string message)
        {
            ReceiveMessage?.Invoke(this, message);
        }

        protected override void OnDisconnected()
        {
            OnClientDisconnected();
            Console.WriteLine($"Tcp session with Id {Id} disconnected!");
        }

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            string message = Encoding.UTF8.GetString(buffer, (int)offset, (int)size);

            OnReceiveMessage(message);
        }

        protected override void OnError(SocketError error)
        {
            Console.WriteLine($"SSL session caught an error with code {error}");
        }
    }
}