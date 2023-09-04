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
using System.Net;
using System.Reflection;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace CentralServer.Windows
{
   /// <summary>
   /// Interaction logic for MainWindow.xaml
   /// </summary>
   public partial class MainWindow : BaseWindowForWPF
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
                  ShowTimedMessage("Server started!", TimeSpan.FromSeconds(2));
                  break;
               case ServerSocketState.STOPPED:
                  elpServerStatus.Fill = new SolidColorBrush(Color.FromArgb(0xFF, 0x74, 0x1B, 0x0C)); // Using Color class
                  ShowTimedMessage("Server stopped!", TimeSpan.FromSeconds(2));
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

      private readonly IUniversalServerSocket _serverBussinesLogic;

      private readonly int _serverPort = 34258;
      private readonly IPAddress _serverIpAddress = NetworkUtils.GetLocalIPAddress() ?? IPAddress.Loopback;
      private readonly string _certificateName = "MyTestCertificateServer.pfx";
      private ServerSocketState _centralServersocketState = ServerSocketState.STOPPED;

      private IWindowEnqueuer? _offeringFilesWindow;
      private IWindowEnqueuer? _clientsWindow;

      private Dictionary<Guid, ServerClientsModel> _clients = new Dictionary<Guid, ServerClientsModel>();

      #endregion PrivateFields

      #region ProtectedFields



      #endregion ProtectedFields

      #region Ctor

      public MainWindow()
      {
         InitializeComponent();

         if (MyConfigManager.TryGetBoolConfigValue("EnableDynamicGradients", out bool enableDynamicGradients) && enableDynamicGradients)
         {
            WindowDesignSet();
         }

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

         tbSuccessMessage.Visibility = Visibility.Collapsed;

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

      #region TemplateMethods

      private void WindowDesignSet()
      {
         // Generate random points
         Random rand = new Random();
         double startX = rand.NextDouble();
         double startY = rand.NextDouble();
         double endX = rand.NextDouble();
         double endY = rand.NextDouble();

         // Generate random offsets
         double[] randomOffsets = new double[4] { rand.NextDouble(), rand.NextDouble(), rand.NextDouble(), rand.NextDouble() };

         // Sort them to ensure they are in ascending order
         Array.Sort(randomOffsets);

         // Create new LinearGradientBrush
         LinearGradientBrush newBrush = new LinearGradientBrush()
         {
            StartPoint = new Point(startX, startY),
            EndPoint = new Point(endX, endY),
         };

         // Add GradientStops to the LinearGradientBrush
         newBrush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#4e3926"), randomOffsets[0]));
         newBrush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#453a26"), randomOffsets[1]));
         newBrush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#753b22"), randomOffsets[2]));
         newBrush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#383838"), randomOffsets[3]));

         brdSecond.BorderBrush = newBrush;
         gdMain.Background = newBrush;
      }

      private void ShowTimedMessageAndEnableUI(string message, TimeSpan duration, UIElement componentToEnable)
      {
         // Initialize and configure the DispatcherTimer
         DispatcherTimer timer = new DispatcherTimer();
         timer.Interval = duration;

         // Show the message
         tbSuccessMessage.Visibility = Visibility.Visible;
         tbSuccessMessage.Text = message;

         // Subscribe to the Tick event
         EventHandler tickEventHandler = null;
         tickEventHandler = (sender, e) =>
         {
            // Hide the message
            tbSuccessMessage.Visibility = Visibility.Collapsed;
            tbSuccessMessage.Text = "";

            // Stop the timer
            timer.Stop();

            // Unsubscribe from Tick event
            timer.Tick -= tickEventHandler;

            // Dispose the timer
            timer = null;
         };

         // Enable the component
         componentToEnable.IsEnabled = true;

         timer.Tick += tickEventHandler;

         // Start the timer
         timer.Start();
      }

      private void ShowTimedMessage(string message, TimeSpan duration)
      {
         // Initialize and configure the DispatcherTimer
         DispatcherTimer? timer = new DispatcherTimer();
         timer.Interval = duration;

         // Show the message
         tbSuccessMessage.Visibility = Visibility.Visible;
         tbSuccessMessage.Text = message;

         // Subscribe to the Tick event
         EventHandler? tickEventHandler = null;
         tickEventHandler = (sender, e) =>
         {
            // Hide the message
            tbSuccessMessage.Visibility = Visibility.Collapsed;
            tbSuccessMessage.Text = "";

            // Stop the timer
            timer.Stop();

            // Unsubscribe from Tick event
            timer.Tick -= tickEventHandler;

            // Dispose the timer
            timer = null;
         };
         timer.Tick += tickEventHandler;

         // Start the timer
         timer.Start();
      }

      #endregion TemplateMethods

      private void ClientStateChangeMessageHandler(ClientStateChangeMessage message)
      {
         _clients = message.Clients;
         if (_clientsWindow != null && _clientsWindow.IsOpen())
         {
            _clientsWindow.BaseMsgEnque(message);
         }
      }

      private void ServerSocketStateChangeMessageHandler(ServerSocketStateChangeMessage message)
      {
         CentralServerSocketState = message.ServerSocketState;
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
         Environment.Exit(0);
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

      private void btnClientsWindow_Click(object sender, RoutedEventArgs e)
      {
         if (sender is Button button)
         {
            Log.WriteLog(LogLevel.DEBUG, button.Name);
            if (_clientsWindow == null || !_clientsWindow.IsOpen())
            {
               _clientsWindow = BaseWindowForWPF.CreateWindow<ClientsWindow>(() => new ClientsWindow(_serverBussinesLogic));
               _clientsWindow?.BaseMsgEnque(new ClientStateChangeMessage() { Clients = _clients });
            }
            else
            {
               _clientsWindow.BaseMsgEnque(new WindowStateSetMessage());
            }
         }
      }

      #endregion Events

      #region OverridedMethods



      #endregion OverridedMethods

   }
}