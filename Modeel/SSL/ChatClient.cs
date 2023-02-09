using Modeel.SSL;
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Modeel.SSL
{
   class ChatClient : SslClient
   {
      public ChatClient(SslContext context, string address, int port) : base(context, address, port) { }

      public void DisconnectAndStop()
      {
         _stop = true;
         DisconnectAsync();
         while (IsConnected)
            Thread.Yield();
      }

      protected override void OnConnected()
      {
         Console.WriteLine($"Chat SSL client connected a new session with Id {Id}");
      }

      protected override void OnHandshaked()
      {
         Console.WriteLine($"Chat SSL client handshaked a new session with Id {Id}");
      }

      protected override void OnDisconnected()
      {
         Console.WriteLine($"Chat SSL client disconnected a session with Id {Id}");

         // Wait for a while...
         Thread.Sleep(1000);

         // Try to connect again
         if (!_stop)
            ConnectAsync();
      }

      protected override void OnReceived(byte[] buffer, long offset, long size)
      {
         string message = Encoding.UTF8.GetString(buffer, (int)offset, (int)size);
         Console.WriteLine(message);
      }

      protected override void OnError(SocketError error)
      {
         Console.WriteLine($"Chat SSL client caught an error with code {error}");
      }

      private bool _stop;
   }
}