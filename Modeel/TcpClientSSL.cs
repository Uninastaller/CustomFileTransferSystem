using System.Net;
using System;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;

namespace Modeel
{
   public class TcpClientSSL
   {
      public delegate void ConnectedEventHandler(object sender, EventArgs e);
      public event ConnectedEventHandler Connected;

      public delegate void DisconnectedEventHandler(object sender, EventArgs e);
      public event DisconnectedEventHandler Disconnected;

      public delegate void MessageReceivedEventHandler(object sender, string message);
      public event MessageReceivedEventHandler MessageReceived;

      private TcpClient client;
      private SslStream sslStream;

      public TcpClientSSL(IPAddress serverAddress, int serverPort, X509Certificate2 clientCertificate)
      {
         client = new TcpClient();
         client.Connect(serverAddress, serverPort);

         sslStream = new SslStream(client.GetStream(), false, new RemoteCertificateValidationCallback(ValidateServerCertificate), null);
         sslStream.AuthenticateAsClient(serverAddress.ToString(), new X509Certificate2Collection(clientCertificate), SslProtocols.Tls12, false);

         Connected?.Invoke(this, EventArgs.Empty);

         BeginReceive();
      }

      private bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
      {
         return true;
      }

      private void BeginReceive()
      {
         byte[] buffer = new byte[1024];
         sslStream.BeginRead(buffer, 0, buffer.Length, ReceiveCallback, buffer);
      }

      private void ReceiveCallback(IAsyncResult result)
      {
         int bytesRead = sslStream.EndRead(result);
         if (bytesRead > 0)
         {
            byte[] buffer = (byte[])result.AsyncState;
            string message = System.Text.Encoding.UTF8.GetString(buffer, 0, bytesRead);

            MessageReceived?.Invoke(this, message);

            BeginReceive();
         }
         else
         {
            Disconnected?.Invoke(this, EventArgs.Empty);
         }
      }

      public void SendMessage(string message)
      {
         byte[] buffer = System.Text.Encoding.UTF8.GetBytes(message);
         sslStream.Write(buffer);
      }
   }
}