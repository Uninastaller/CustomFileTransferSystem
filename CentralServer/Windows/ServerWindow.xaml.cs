using Common.Interface;
using Common.Model;
using Common.ThreadMessages;
using Logger;
using SslTcpSession;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Controls;

namespace CentralServer.Windows
{
    /// <summary>
    /// Interaction logic for ServerWindow.xaml
    /// </summary>
    public partial class ServerWindow : BaseWindowForWPF
    {
        private SslServerBussinesLogic _serverBussinesLogic;

        private readonly int _serverPort = 8080;
        private readonly IPAddress _ipAddress = IPAddress.Loopback;
        private readonly string _certificateName = "MyTestCertificateServer.pfx";
        private Dictionary<Guid, ServerClientsModel> _clients = new Dictionary<Guid, ServerClientsModel>();

        public ServerWindow()
        {
            InitializeComponent();
            contract.Add(MsgIds.WindowStateSetMessage, typeof(WindowStateSetMessage));
            contract.Add(MsgIds.ClientStateChangeMessage, typeof(ClientStateChangeMessage));

            Init();

            Closed += Window_closedEvent;

            SslContext sslContext = new SslContext(SslProtocols.Tls12, new X509Certificate2(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _certificateName), ""));
            _serverBussinesLogic = new SslServerBussinesLogic(sslContext, _ipAddress, _serverPort, this);
        }

        internal void Init()
        {

            tbTitle.Text = $"ESTE NEVIEM [v.{Assembly.GetExecutingAssembly().GetName().Version}]";

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
            RefreshClientsDatagrid();
        }

        private void RefreshClientsDatagrid()
        {
            dtgClients.ItemsSource = null;
            dtgClients.ItemsSource = _clients.Values.ToList();
        }

        private void Window_closedEvent(object? sender, EventArgs e)
        {
            Closed -= Window_closedEvent;
            _serverBussinesLogic.Stop();
            _serverBussinesLogic.Dispose();
        }

        private void Border_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {

            this.DragMove();
        }

        private void btnClose_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnDisconnect_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is ServerClientsModel serverClientsModel)
            {
                Log.WriteLog(LogLevel.DEBUG, button.Name);
                _serverBussinesLogic.FindSession(serverClientsModel.SessionGuid).Disconnect();
            }
        }
    }
}