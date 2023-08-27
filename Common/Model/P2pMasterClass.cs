using Common.Interface;
using Common.ThreadMessages;
using Logger;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Common.Model
{

    public class P2pMasterClass : IP2pMasterClass
    {
        private const long _kiloByte = 0x400;   //KB
        private const long _megaByte = 0x100000; //MB

        private IWindowEnqueuer _gui;

        private readonly Dictionary<Guid, IUniversalClientSocket> _clients = new Dictionary<Guid, IUniversalClientSocket>();
        private readonly Dictionary<Guid, IUniversalServerSocket> _servers = new Dictionary<Guid, IUniversalServerSocket>();

        public P2pMasterClass(IWindowEnqueuer gui)
        {
            _gui = gui;
        }

        public long GetTotalUploadingSpeedOfAllRunningServersInBytes()
        {
            return _servers.Sum(server => server.Value.TransferSendRate);
        }

        public long GetTotalUploadingSpeedOfAllRunningServersInKiloBytes()
        {
            return GetTotalUploadingSpeedOfAllRunningServersInBytes() / _kiloByte;
        }

        public long GetTotalUploadingSpeedOfAllRunningServersInMegaBytes()
        {
            return GetTotalUploadingSpeedOfAllRunningServersInBytes() / _megaByte;
        }

        public long GetTotalDownloadingSpeedOfAllRunningClientsInBytes()
        {
            return _clients.Sum(client => client.Value.TransferReceiveRate);
        }

        public long GetTotalDownloadingSpeedOfAllRunningClientsInKiloBytes()
        {
            return GetTotalDownloadingSpeedOfAllRunningClientsInBytes() / _kiloByte;
        }

        public long GetTotalDownloadingSpeedOfAllRunningClientsInMegaBytes()
        {
            return GetTotalDownloadingSpeedOfAllRunningClientsInBytes() / _megaByte;
        }

        public void CreateNewServer(IUniversalServerSocket socketServer)
        {
            if (!_servers.ContainsKey(socketServer.Id))
            {
                _servers.Add(socketServer.Id, socketServer);
                _gui.BaseMsgEnque(new P2pServersUpdateMessage() { Servers = _servers.Values.ToList() });
            }
        }

        public void CreateNewClient(IUniversalClientSocket socketClient)
        {
            if (!_clients.ContainsKey(socketClient.Id))
            {
                _clients.Add(socketClient.Id, socketClient);
                _gui.BaseMsgEnque(new P2pClietsUpdateMessage() { Clients = _clients.Values.ToList() });
            }
        }

        public void RemoveServer(IUniversalServerSocket socketServer)
        {
            if (_servers.ContainsKey(socketServer.Id))
            {
                _servers.Remove(socketServer.Id);
                _gui.BaseMsgEnque(new P2pServersUpdateMessage() { Servers = _servers.Values.ToList() });
            }
        }

        public void RemoveClient(IUniversalClientSocket socketClient)
        {
            if (_clients.ContainsKey(socketClient.Id))
            {
                _clients.Remove(socketClient.Id);
                _gui.BaseMsgEnque(new P2pClietsUpdateMessage() { Clients = _clients.Values.ToList() });
            }
        }

        public void CloseAllConnections()
        {
            foreach (IUniversalClientSocket client in _clients.Values)
            {
                client.DisconnectAndStop();
                client.Dispose();
            }
            _clients.Clear();

            foreach (IUniversalServerSocket server in _servers.Values)
            {
                server.Stop();
                server.Dispose();
            }
            _servers.Clear();

            Log.WriteLog(LogLevel.DEBUG, "All connections was closed!");
        }

    }
}