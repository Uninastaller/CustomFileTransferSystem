using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modeel
{
    public class ClientBussinesLogic
    {
        TcpClientSSL _tcpClientSSL;
        private readonly int _serverPort = 8080;
        private readonly string _serverIp = "127.0.0.1";

        public ClientBussinesLogic()
        {
            _tcpClientSSL = new TcpClientSSL(_serverIp, _serverPort);
            _tcpClientSSL.SendMessage("haha");
        }
    }
}
