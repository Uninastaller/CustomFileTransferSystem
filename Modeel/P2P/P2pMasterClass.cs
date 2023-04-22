using Modeel;
using Modeel.Frq;
using Modeel.Messages;
using Modeel.P2P;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

public class P2pMasterClass : IP2pMasterClass
{
    private IWindowEnqueuer _gui;

    private readonly Dictionary<Guid, IUniversalClientSocket> _clients = new Dictionary<Guid, IUniversalClientSocket>();
    private readonly Dictionary<Guid, SslServerBussinesLogic> _servers = new Dictionary<Guid, SslServerBussinesLogic>();

    public P2pMasterClass(IWindowEnqueuer gui)
    {
        _gui = gui;
    }

    public void CreateNewServer(SslServerBussinesLogic serverBussinesLogic)
    {
        if (!_servers.ContainsKey(serverBussinesLogic.Id))
        {
            _servers.Add(serverBussinesLogic.Id, serverBussinesLogic);
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

    public void CloseAllConnections()
    {
        foreach (IUniversalClientSocket client in _clients.Values)
        {
            client.DisconnectAndStop();
            client.Dispose();
        }
        _clients.Clear();

        foreach (SslServerBussinesLogic server in _servers.Values)
        {
            server.Stop();
            server.Dispose();
        }
        _servers.Clear();

        Logger.WriteLog("All connections was closed!", LoggerInfo.P2PSSL);
    }

}
