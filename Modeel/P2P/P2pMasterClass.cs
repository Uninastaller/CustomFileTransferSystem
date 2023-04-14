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

    private readonly Dictionary<Guid, ClientBussinesLogic> _clients = new Dictionary<Guid, ClientBussinesLogic>();
    private readonly Dictionary<Guid, ServerBussinesLogic> _servers = new Dictionary<Guid, ServerBussinesLogic>();

    public P2pMasterClass(IWindowEnqueuer gui)
    {
        _gui = gui;
    }

    public void CreateNewServer(ServerBussinesLogic serverBussinesLogic)
    {
        if (!_servers.ContainsKey(serverBussinesLogic.Id))
        {
            _servers.Add(serverBussinesLogic.Id, serverBussinesLogic);
            _gui.BaseMsgEnque(new P2pServersUpdateMessage() { Servers = _servers.Values.ToList() });
        }
    }

    public void CreateNewClient(ClientBussinesLogic clientBussinesLogic)
    {
        if (!_clients.ContainsKey(clientBussinesLogic.Id))
        {
            _clients.Add(clientBussinesLogic.Id, clientBussinesLogic);
            _gui.BaseMsgEnque(new P2pClietsUpdateMessage() { Clients = _clients.Values.ToList() });
        }
    }

    public void CloseAllConnections()
    {
        foreach (ClientBussinesLogic client in _clients.Values)
        {
            client.DisconnectAndStop();
            client.Dispose();
        }
        _clients.Clear();

        foreach (ServerBussinesLogic server in _servers.Values)
        {
            server.Stop();
            server.Dispose();
        }
        _servers.Clear();

        Logger.WriteLog("All connections was closed!", LoggerInfo.P2PSSL);
    }

}
