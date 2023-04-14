using Modeel.Messages;
using Modeel.SSL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace Modeel
{
    /// <summary>
    /// Interaction logic for ServerWindow.xaml
    /// </summary>
    public partial class ServerWindow : BaseWindowForWPF
    {
        private ServerBussinesLogic _serverBussinesLogic;

        private readonly int _serverPort = 8080;
        private readonly IPAddress _ipAddress = IPAddress.Loopback;
        private readonly string _certificateName = "MyTestCertificateServer.pfx";
        private Dictionary<Guid, string> _clients = new Dictionary<Guid, string>();

        public ServerWindow()
        {
            InitializeComponent();
            contract.Add(MsgIds.WindowStateSetMessage, typeof(WindowStateSetMessage));
            contract.Add(MsgIds.ClientStateChangeMessage, typeof(ClientStateChangeMessage));

            Init();

            Closed += Window_closedEvent;

            SslContext sslContext = new SslContext(SslProtocols.Tls12, new X509Certificate2(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _certificateName), ""));
            _serverBussinesLogic = new ServerBussinesLogic(sslContext, _ipAddress, _serverPort, this);
        }

        internal void Init()
        {
            msgSwitch
             .Case(contract.GetContractId(typeof(WindowStateSetMessage)), (WindowStateSetMessage x) => WindowStateSetMessageHandler(x))
             .Case(contract.GetContractId(typeof(ClientStateChangeMessage)), (ClientStateChangeMessage x) => ClientStateChangeMessageHandler(x));
        }

        private void WindowStateSetMessageHandler(WindowStateSetMessage message)
        {
            if (WindowState == System.Windows.WindowState.Minimized)
            {
                WindowState = System.Windows.WindowState.Normal;
            }
            Activate();
        }

        private void ClientStateChangeMessageHandler(ClientStateChangeMessage message)
        {
            _clients = message.Clients;
            RefreshClientsView();
        }

        private void RefreshClientsView()
        {
            lvConnectedClients.ItemsSource = _clients.Values.ToList();
        }


        private void Window_closedEvent(object? sender, EventArgs e)
        {
            Closed -= Window_closedEvent;
            _serverBussinesLogic.Stop();
            _serverBussinesLogic.Dispose();
        }
    }
}
