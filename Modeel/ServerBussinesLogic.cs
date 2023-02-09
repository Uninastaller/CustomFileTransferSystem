using Modeel.SSL;
using System;
using System.IO;
using System.Net;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace Modeel
{

   public class ServerBussinesLogic
   {
      //private Server _server;
      private TcpServerSSL _tcpServerSSL;
      private readonly int _serverPort = 8080;
      //private readonly string _serverIp = "127.0.0.1";
      private readonly IPAddress _ipAddress = IPAddress.Loopback;
      private readonly string _certificateName = "MyTestCertificateServer.pfx";
      private readonly X509Certificate2 _certificate;

      public ServerBussinesLogic()
      {
         //SecureString password = new SecureString();
         //foreach (char c in "7E59A722F2B6AC8399AC4283C06D6BDD")
         //{
         //   password.AppendChar(c);
         //}
         //password.MakeReadOnly();

         //_certificate = new X509Certificate2(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _certificateName), "", X509KeyStorageFlags.MachineKeySet);
         //_tcpServerSSL = new TcpServerSSL(_ipAddress, _serverPort, _certificate);
         //_tcpServerSSL.Start();
         //_server = new Server(_ipAddress, _serverPort);

         SslContext context = new SslContext(SslProtocols.Tls12, new X509Certificate2(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _certificateName), ""));

         // Create a new SSL chat server
         ChatServer server = new ChatServer(context, _ipAddress, _serverPort);

         server.Start();
      }
   }
}
