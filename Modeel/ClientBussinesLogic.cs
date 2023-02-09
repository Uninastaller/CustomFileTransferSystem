using Modeel.SSL;
using System;
using System.IO;
using System.Net;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace Modeel
{
   public class ClientBussinesLogic
   {
      TcpClientSSL _tcpClientSSL;
      private readonly int _serverPort = 8080;
      private readonly string _serverIp = "127.0.0.1";
      private readonly IPAddress _ipAddress = IPAddress.Loopback;
      private readonly string _certificateName = "MyTestCertificateClient.pfx";
      private readonly X509Certificate2 _certificate;

      public ClientBussinesLogic()
      {
         //_certificate = new X509Certificate2(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _certificateName), "", X509KeyStorageFlags.MachineKeySet);

         //_tcpClientSSL = new TcpClientSSL(_ipAddress, _serverPort, _certificate);
         //_tcpClientSSL.SendMessage("haha");


         SslContext context = new SslContext(SslProtocols.Tls12, new X509Certificate2(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _certificateName), ""), (sender, certificate, chain, sslPolicyErrors) => true);

         // Create a new SSL chat client
         ChatClient client = new ChatClient(context, _serverIp, _serverPort);

         client.Connect();

         client.Send("haha");

      }
   }
}
