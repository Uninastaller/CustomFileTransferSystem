using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Modeel.Model;

namespace Modeel.P2P
{
    public interface IP2pMasterClass
    {
        public void CloseAllConnections();
        public void CreateNewServer(IUniversalServerSocket socketServer);
        public void CreateNewClient(IUniversalClientSocket socketClient);
        public void RemoveServer(IUniversalServerSocket socketServer);
        public void RemoveClient(IUniversalClientSocket socketClient);
    }
}
