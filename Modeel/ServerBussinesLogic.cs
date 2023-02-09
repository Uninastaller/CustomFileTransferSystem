using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Modeel
{

    public class ServerBussinesLogic
    {
        private Server _server;
        private TcpServerSSL _tcpServerSSL;
        private readonly int _serverPort = 8080;
        private readonly string _serverIp = "127.0.0.1";
        private readonly IPAddress _ipAddress;
        private readonly string _certificateName = "C:\\Users\\tomas\\source\\repos\\Modeel\\MyTestCertificate.pfx";
        private readonly X509Certificate2 _certificate;

        public ServerBussinesLogic()
        {
            SecureString password = new SecureString();
            foreach (char c in "7E59A722F2B6AC8399AC4283C06D6BDD")
            {
                password.AppendChar(c);
            }
            password.MakeReadOnly();

            _ipAddress = IPAddress.Loopback;

            _certificate = new X509Certificate2(_certificateName, "", X509KeyStorageFlags.MachineKeySet);
            _tcpServerSSL = new TcpServerSSL(_ipAddress, _serverPort, _certificate);
            _tcpServerSSL.Start();
            //_server = new Server(_ipAddress, _serverPort);
        }
    }
}
