﻿using Modeel.FastTcp;
using Modeel.Log;
using Modeel.Messages;
using Modeel.Model;
using Modeel.Model.Enums;
using Modeel.P2P;
using Modeel.SSL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Timer = System.Timers.Timer;

namespace Modeel
{
    /// <summary>
    /// Interaction logic for ClientWindow.xaml
    /// </summary>
    public partial class ClientWindow : BaseWindowForWPF
    {
        private readonly SslClientBussinesLogic _clientBussinesLogic;
        private readonly IP2pMasterClass _p2PMasterClass;

        private readonly int _serverPort = 8080;
        private readonly IPAddress _ipAddress = IPAddress.Loopback;
        private readonly string _certificateNameForCentralServerConnect = "MyTestCertificateClient.pfx";
        private readonly string _certificateNameForP2pAsClient = "MyTestCertificateClient.pfx";
        private readonly string _certificateNameForP2pAsServer = "MyTestCertificateServer.pfx";
        private SslContext _contextForCentralServerConnect;
        private SslContext _contextForP2pAsClient;
        private SslContext _contextForP2pAsServer;

        private List<IUniversalClientSocket> _p2pClients = new List<IUniversalClientSocket>();
        private List<IUniversalServerSocket> _p2pServers = new List<IUniversalServerSocket>();

        private Timer? _timer;

        #region PrivateFields

        private IPAddress? _publicIpAddress;
        private IPAddress? _localIpAddress;

        #endregion PrivateFields

        #region Properties

        public static int GetRandomFreePort
        {
            get
            {
                var listener = new TcpListener(IPAddress.Loopback, 0);
                listener.Start();
                var port = ((IPEndPoint)listener.LocalEndpoint).Port;
                listener.Stop();
                Logger.WriteLog($"Random free port generated: {port}", LoggerInfo.P2PSSL);
                return port;
            }
        }


        public int P2pPort
        {
            get
            {
                if (int.TryParse(tbP2pIpPort.Text, out int port))
                {
                    return port;
                }
                else
                {
                    return GetRandomFreePort;
                }
            }
        }

        public IPAddress P2pIpAddress
        {
            get
            {
                if (IPAddress.TryParse(tbP2pIpAddress.Text, out IPAddress? iPAddress))
                {
                    return iPAddress;
                }
                else
                {
                    return IPAddress.Loopback;
                }
            }
        }

        #endregion Properties

        #region Ctor
        public ClientWindow()
        {
            InitializeComponent();
            contract.Add(MsgIds.WindowStateSetMessage, typeof(WindowStateSetMessage));
            contract.Add(MsgIds.SocketStateChangeMessage, typeof(SocketStateChangeMessage));
            contract.Add(MsgIds.P2pClietsUpdateMessage, typeof(P2pClietsUpdateMessage));
            contract.Add(MsgIds.P2pServersUpdateMessage, typeof(P2pServersUpdateMessage));
            contract.Add(MsgIds.RefreshTablesMessage, typeof(RefreshTablesMessage));
            contract.Add(MsgIds.DisposeMessage, typeof(DisposeMessage));

            Init();

            Closed += Window_closedEvent;

            _contextForCentralServerConnect = new SslContext(SslProtocols.Tls12, new X509Certificate2(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _certificateNameForCentralServerConnect), ""), (sender, certificate, chain, sslPolicyErrors) => true);
            //_clientBussinesLogic = new SslClientBussinesLogic(_contextForCentralServerConnect, _ipAddress, _serverPort, this, sessionWithCentralServer: true);

            _contextForP2pAsClient = new SslContext(SslProtocols.Tls12, new X509Certificate2(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _certificateNameForP2pAsClient), ""), (sender, certificate, chain, sslPolicyErrors) => true);
            _contextForP2pAsServer = new SslContext(SslProtocols.Tls12, new X509Certificate2(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _certificateNameForP2pAsServer), ""), (sender, certificate, chain, sslPolicyErrors) => true);

            _p2PMasterClass = new P2pMasterClass(this);

            dgP2pCliets.ItemsSource = _p2pClients;
            dgP2pServers.ItemsSource = _p2pServers;


            // anonymous method that will be called every time the timer elapses
            // there is a need to transfer calling to ui thread, so we are sending message to ourself
            _timer = new Timer(1000); // Set the interval to 1 second
            _timer.Elapsed += Timer_elapsed;
            _timer.Start();
        }

        #endregion Ctor

        internal async void Init()
        {
            msgSwitch
             .Case(contract.GetContractId(typeof(WindowStateSetMessage)), (WindowStateSetMessage x) => WindowStateSetMessageHandler(x))
             .Case(contract.GetContractId(typeof(SocketStateChangeMessage)), (SocketStateChangeMessage x) => SocketStateChangeMessageHandler(x))
             .Case(contract.GetContractId(typeof(P2pClietsUpdateMessage)), (P2pClietsUpdateMessage x) => P2pClietsUpdateMessageHandler(x))
             .Case(contract.GetContractId(typeof(P2pServersUpdateMessage)), (P2pServersUpdateMessage x) => P2pServersUpdateMessageHandler(x))
             .Case(contract.GetContractId(typeof(RefreshTablesMessage)), (RefreshTablesMessage x) => RefreshTablesMessageHandler())
             .Case(contract.GetContractId(typeof(DisposeMessage)), (DisposeMessage x) => DisposeMessageMessageHandler(x))
             ;

            _publicIpAddress = await NetworkUtils.GetPublicIPAddress();
            _localIpAddress = NetworkUtils.GetLocalIPAddress();
            tbP2pIpAddress.Text = _localIpAddress?.ToString();
        }

        #region PrivateMethods

        private void RefreshTablesMessageHandler()
        {
            dgP2pServers.ItemsSource = null;
            dgP2pServers.ItemsSource = _p2pServers;

            dgP2pCliets.ItemsSource = null;
            dgP2pCliets.ItemsSource = _p2pClients;
        }

        private void P2pClietsUpdateMessageHandler(P2pClietsUpdateMessage message)
        {
            _p2pClients = message.Clients;
            dgP2pCliets.ItemsSource = _p2pClients;

        }

        private void P2pServersUpdateMessageHandler(P2pServersUpdateMessage message)
        {
            _p2pServers = message.Servers;
            dgP2pServers.ItemsSource = _p2pServers;

        }

        private void DisposeMessageMessageHandler(DisposeMessage message)
        {
            if (message.TypeOfSocket == TypeOfSocket.SERVER)
            {
                IUniversalServerSocket? server = _p2pServers.FirstOrDefault(x => x.Id == message.SessionGuid);
                if (server != null)
                {
                    _p2PMasterClass.RemoveServer(server);
                }
            }
            else
            {
                IUniversalClientSocket? client = _p2pClients.FirstOrDefault(x => x.Id == message.SessionGuid);
                if (client != null)
                {
                    _p2PMasterClass.RemoveClient(client);
                }
            }

        }

        private void SocketStateChangeMessageHandler(SocketStateChangeMessage message)
        {
            if (message.TypeOfSession == TypeOfSession.SESSION_WITH_CENTRAL_SERVER)
            {
                if (message.SocketState == SocketState.CONNECTED)
                {
                    rtgServerStatus.Fill = new SolidColorBrush(Colors.Green);
                }
                else if (message.SocketState == SocketState.DISCONNECTED)
                {
                    rtgServerStatus.Fill = new SolidColorBrush(Colors.Red);
                }
            }
        }

        private void WindowStateSetMessageHandler(WindowStateSetMessage message)
        {
            if (WindowState == System.Windows.WindowState.Minimized)
            {
                WindowState = System.Windows.WindowState.Normal;
            }
            Activate();
        }

        private static bool IsPortFree(int port)
        {
            bool isFree = true;

            try
            {
                var listener = new TcpListener(IPAddress.Loopback, port);
                listener.Start();
                listener.Stop();
            }
            catch (SocketException)
            {
                isFree = false;
            }

            return isFree;
        }

        #endregion PrivateMethods

        #region EventHandlers

        private void Window_closedEvent(object? sender, EventArgs e)
        {
            Closed -= Window_closedEvent;

            if (_timer != null)
            {
                _timer.Elapsed -= Timer_elapsed;
                _timer.Stop();
                _timer.Dispose();
                _timer = null;
            }

            _clientBussinesLogic?.DisconnectAndStop();
            _clientBussinesLogic?.Dispose();
            _p2PMasterClass.CloseAllConnections();
            _p2pClients.Clear();
            _p2pServers.Clear();
            dgP2pCliets.ItemsSource = null;
            dgP2pServers.ItemsSource = null;
        }

        private void Timer_elapsed(object? sender, ElapsedEventArgs e)
        {
            this.BaseMsgEnque(new RefreshTablesMessage());
        }

        private void btnP2pListen_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (!IsPortFree(P2pPort))
            {
                Logger.WriteLog($"Port: {P2pPort} is not free!", LoggerInfo.P2PSSL);

                // just for testing create on another free port
                _p2PMasterClass.CreateNewServer(new SslServerBussinesLogic(_contextForP2pAsServer, P2pIpAddress, GetRandomFreePort, this, optionAcceptorBacklog: 1));

                return;
            }
            _p2PMasterClass.CreateNewServer(new SslServerBussinesLogic(_contextForP2pAsServer, P2pIpAddress, P2pPort, this, optionAcceptorBacklog: 1));
        }

        private void btnP2pConnect_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            _p2PMasterClass.CreateNewClient(new SslClientBussinesLogic(_contextForP2pAsClient, P2pIpAddress, P2pPort, this));
        }

        private void btnStopServer_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Button? b = sender as Button;
            if (b?.Tag is IUniversalServerSocket server)
            {
                server.Stop();
            }
        }

        private void btnStartServer_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Button? b = sender as Button;
            if (b?.Tag is IUniversalServerSocket server)
            {
                server.Start();
            }
        }

        private void btnRestartServer_Click(object sender, RoutedEventArgs e)
        {
            Button? b = sender as Button;
            if (b?.Tag is IUniversalServerSocket server)
            {
                server.Restart();
            }
        }

        private void btnDisposeServer_Click(object sender, RoutedEventArgs e)
        {
            Button? b = sender as Button;
            if (b?.Tag is IUniversalServerSocket server)
            {
                server.Dispose();
            }
        }

        private void btnDisconnectFromServer_Click(object sender, RoutedEventArgs e)
        {
            Button? b = sender as Button;
            if (b?.Tag is IUniversalClientSocket client)
            {
                client.Disconnect();
            }
        }

        private void btnDisconnectAndStop_Click(object sender, RoutedEventArgs e)
        {
            Button? b = sender as Button;
            if (b?.Tag is IUniversalClientSocket client)
            {
                client.DisconnectAndStop();
            }
        }

        private void btnConnectAsyncToServer_Click(object sender, RoutedEventArgs e)
        {
            Button? b = sender as Button;
            if (b?.Tag is IUniversalClientSocket client)
            {
                client.ConnectAsync();
            }
        }

        private void btnDisposeClient_Click(object sender, RoutedEventArgs e)
        {
            Button? b = sender as Button;
            if (b?.Tag is IUniversalClientSocket client)
            {
                client.Dispose();
            }            
        }

        private async void btnP2pListenFast_Click(object sender, RoutedEventArgs e)
        {
            if (!IsPortFree(P2pPort))
            {
                Logger.WriteLog($"Port: {P2pPort} is not free!", LoggerInfo.P2P);
                // just for testing create on another free port
                _p2PMasterClass.CreateNewServer(new ServerBussinesLogic2(_localIpAddress ?? P2pIpAddress, GetRandomFreePort, this, optionAcceptorBacklog: 1));
                return;
            }
            //_p2PMasterClass.CreateNewServer(new ServerBussinesLogic2(P2pIpAddress, P2pPort, this, optionAcceptorBacklog: 1));
            _p2PMasterClass.CreateNewServer(new ServerBussinesLogic2(_localIpAddress ?? P2pIpAddress, P2pPort, this, optionAcceptorBacklog: 1));
        }

        private void btnP2pConnectFast_Click(object sender, RoutedEventArgs e)
        {
            //_p2PMasterClass.CreateNewClient(new ClientBussinesLogic2(P2pIpAddress, P2pPort, this));
            _p2PMasterClass.CreateNewClient(new ClientBussinesLogic(IPAddress.Parse("127.0.0.1"), P2pPort, this));

        }

        #endregion EventHandlers

    }
}