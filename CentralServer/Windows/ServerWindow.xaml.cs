using Common.Enum;
using Common.Interface;
using Common.Model;
using Common.ThreadMessages;
using ConfigManager;
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
using System.Windows.Media;

namespace CentralServer.Windows
{
    /// <summary>
    /// Interaction logic for ServerWindow.xaml
    /// </summary>
    public partial class ServerWindow : BaseWindowForWPF
    {
        #region Properties

        public ServerSocketState CentralServerSocketState
        {
            get => _centralServersocketState;
            set
            {
                _centralServersocketState = value;
                Log.WriteLog(LogLevel.INFO, "Central Server Socket State Change To " + value);

                switch (value)
                {
                    case ServerSocketState.STARTED:
                        elpServerStatus.Fill = new SolidColorBrush(Color.FromArgb(0xFF, 0x26, 0x3F, 0x03)); // Using Color class
                        break;
                    case ServerSocketState.STOPPED:
                        elpServerStatus.Fill = new SolidColorBrush(Color.FromArgb(0xFF, 0x74, 0x1B, 0x0C)); // Using Color class
                        break;
                    default:
                        break;
                }
            }
        }

        #endregion Properties

        #region PublicFields



        #endregion PublicFields

        #region PrivateFields

        private IUniversalServerSocket _serverBussinesLogic;

        private readonly int _serverPort = 34258;
        private readonly IPAddress _serverIpAddress = NetworkUtils.GetLocalIPAddress() ?? IPAddress.Loopback;
        private readonly string _certificateName = "MyTestCertificateServer.pfx";
        private Dictionary<Guid, ServerClientsModel> _clients = new Dictionary<Guid, ServerClientsModel>();
        private ServerSocketState _centralServersocketState = ServerSocketState.STOPPED;

        private IWindowEnqueuer? _offeringFilesWindow;

        #endregion PrivateFields

        #region ProtectedFields



        #endregion ProtectedFields

        #region Ctor

        public ServerWindow()
        {
            InitializeComponent();
            contract.Add(MsgIds.ClientStateChangeMessage, typeof(ClientStateChangeMessage));
            contract.Add(MsgIds.ServerSocketStateChangeMessage, typeof(ServerSocketStateChangeMessage));

            if (MyConfigManager.TryGetConfigValue<Int32>("CeentralServerPort", out Int32 serverPort))
            {
                _serverPort = serverPort;
            }            

            Init();

            Closed += Window_closedEvent;

            SslContext sslContext = new SslContext(SslProtocols.Tls12, new X509Certificate2(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _certificateName), ""));
            _serverBussinesLogic = new SslServerBussinesLogic(sslContext, _serverIpAddress, _serverPort, this, 0x2000, 0x2000, typeOfSession: TypeOfSession.SESSION_WITH_CENTRAL_SERVER);
        }

        #endregion Ctor

        #region PublicMethods



        #endregion PublicMethods

        #region PrivateMethods

        private void Init()
        {

            tbTitle.Text = $"Custom File Transfer System [v.{Assembly.GetExecutingAssembly().GetName().Version}]";
            tbServerIpAddressVariable.Text = _serverIpAddress.ToString();
            tbServerPortVariable.Text = _serverPort.ToString();

            msgSwitch
             .Case(contract.GetContractId(typeof(ClientStateChangeMessage)), (ClientStateChangeMessage x) => ClientStateChangeMessageHandler(x))
             .Case(contract.GetContractId(typeof(ServerSocketStateChangeMessage)), (ServerSocketStateChangeMessage x) => ServerSocketStateChangeMessageHandler(x));
        }

        private void ClientStateChangeMessageHandler(ClientStateChangeMessage message)
        {
            _clients = message.Clients;
            RefreshClientsDatagrid();
        }

        private void ServerSocketStateChangeMessageHandler(ServerSocketStateChangeMessage message)
        {
            CentralServerSocketState = message.ServerSocketState;
        }

        private void RefreshClientsDatagrid()
        {
            dtgClients.ItemsSource = null;
            dtgClients.ItemsSource = _clients.Values.ToList();
        }

        #endregion PrivateMethods

        #region ProtectedMethods



        #endregion ProtectedMethods

        #region Events

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


        private void btnMinimize_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.WindowState = System.Windows.WindowState.Minimized;
        }

        private void btnMaximize_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (this.WindowState == System.Windows.WindowState.Maximized)
            {
                this.WindowState = System.Windows.WindowState.Normal; // Restore window size
            }
            else
            {
                this.WindowState = System.Windows.WindowState.Maximized; // Maximize window
            }
        }

        private void btnDisconnect_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is ServerClientsModel serverClientsModel)
            {
                Log.WriteLog(LogLevel.DEBUG, button.Name);
                _serverBussinesLogic.DisconnectSession(serverClientsModel.SessionGuid);
            }
        }

        private void btnStartServer_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                Log.WriteLog(LogLevel.DEBUG, button.Name);
                _serverBussinesLogic.Start();
            }
        }

        private void btnRestartServer_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                Log.WriteLog(LogLevel.DEBUG, button.Name);
                _serverBussinesLogic.Restart();
            }
        }

        private void btnStopServer_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                Log.WriteLog(LogLevel.DEBUG, button.Name);
                _serverBussinesLogic.Stop();
            }
        }

        private void btnOfferingFilesWindow_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                Log.WriteLog(LogLevel.DEBUG, button.Name);
                if (_offeringFilesWindow == null || !_offeringFilesWindow.IsOpen())
                {
                    _offeringFilesWindow = BaseWindowForWPF.CreateWindow<OfferingFilesWindow>();
                }
                else
                {
                    _offeringFilesWindow.BaseMsgEnque(new WindowStateSetMessage());
                }
            }
        }

        #endregion Events

        #region OverridedMethods



        #endregion OverridedMethods

    }
}