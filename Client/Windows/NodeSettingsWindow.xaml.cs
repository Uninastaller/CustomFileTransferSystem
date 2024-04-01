using Common.Enum;
using Common.Model;
using Common.ThreadMessages;
using ConfigManager;
using Logger;
using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace Client.Windows
{
    /// <summary>
    /// Interaction logic for NodeSettingsWindow.xaml
    /// </summary>
    public partial class NodeSettingsWindow : BaseWindowForWPF
    {

        #region Properties

        public NodeSettingsWindowState State { get; private set; }

        #endregion Properties

        #region PrivateFields


        #endregion PrivateFields

        #region Ctor

        public NodeSettingsWindow(NodeSettingWindowMessage message) : this()
        {
            SetWindow(message);
        }

        public NodeSettingsWindow()
        {
            InitializeComponent();
            Init();

            if (MyConfigManager.TryGetBoolConfigValue("EnableDynamicGradients", out bool enableDynamicGradients) && enableDynamicGradients)
            {
                WindowDesignSet();
            }

            tbSuccessMessage.Visibility = Visibility.Collapsed;
        }

        #endregion Ctor

        #region Events

        private void Border_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void btnClose_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.Close();
        }


        private void btnNewGuid_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                Log.WriteLog(LogLevel.DEBUG, button.Name);
                tbId.Text = Guid.NewGuid().ToString();
            }
        }

        private void btnReloadIpAddress_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                Log.WriteLog(LogLevel.DEBUG, button.Name);
                tbIpAddress.Text = NetworkUtils.GetLocalIPAddress().ToString();
            }
        }


        private void btnReloadPort_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                Log.WriteLog(LogLevel.DEBUG, button.Name);
                if (!MyConfigManager.TryGetIntConfigValue("UploadingServerPort", out Int32 port))
                {
                    Log.WriteLog(LogLevel.ERROR, "Invalid port!");
                    return;
                }
                tbPort.Text = port.ToString();
            }
        }


        private void btnReloadPublicKey_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                Log.WriteLog(LogLevel.DEBUG, button.Name);
                tbPublicKey.Text = Certificats.ExportPublicKeyToJSON(Certificats.GetCertificate("NodeXY", Certificats.CertificateType.Node));
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (Guid.TryParse(tbId.Text, out Guid guid))
            {
                if (int.TryParse(tbPort.Text, out int port))
                {
                    if (IPAddress.TryParse(tbIpAddress.Text, out _))
                    {
                        Node node = new Node()
                        {
                            Id = guid,
                            Port = port,
                            Address = tbIpAddress.Text,
                            PublicKey = tbPublicKey.Text,
                        };

                        if (State == NodeSettingsWindowState.MY_NODE)
                        {
                            NodeDiscovery.UpdateAndSaveMyNode(node);
                        }
                        else if (State == NodeSettingsWindowState.ADD_FOREIGN_NODE || State == NodeSettingsWindowState.CHANGE_FOREIGN_NODE)
                        {
                            NodeDiscovery.AddNode(node);
                            NodeDiscovery.SaveNodes();
                        }

                        this.Close();
                    }
                    else
                    {
                        ShowTimedMessage("Invalid ip address!", TimeSpan.FromSeconds(3));
                    }
                }
                else
                {
                    ShowTimedMessage("Invalid port!", TimeSpan.FromSeconds(3));
                }
            }
            else
            {
                ShowTimedMessage("Invalid ID!", TimeSpan.FromSeconds(3));
            }
        }

        #endregion Events

        #region PrivateMethods

        private void SetWindow(NodeSettingWindowMessage message)
        {
            SetFieldsByNode(message.Node);
            State = message.State;
            lbTitle.Content = State.ToString();

            if (message.State == NodeSettingsWindowState.MY_NODE)
            {
                tbIpAddress.IsEnabled = false;
                tbPort.IsEnabled = false;
                tbPublicKey.IsEnabled = false;
                btnNewGuid.Visibility = Visibility.Visible;
                btnReloadIpAddress.Visibility = Visibility.Visible;
                btnReloadPort.Visibility = Visibility.Visible;
                btnReloadPublicKey.Visibility = Visibility.Visible;
                tbId.Visibility = Visibility.Visible;
                tblId.Visibility = Visibility.Visible;
                tbIpAddress.Visibility = Visibility.Visible;
                tblIpAddress.Visibility = Visibility.Visible;
                tbPort.Visibility = Visibility.Visible;
                tblPort.Visibility = Visibility.Visible;
                tbPublicKey.Visibility = Visibility.Visible;
                tblPublickKey.Visibility = Visibility.Visible;
            }
            else if (message.State == NodeSettingsWindowState.CHANGE_FOREIGN_NODE || message.State == NodeSettingsWindowState.ADD_FOREIGN_NODE)
            {
                tbIpAddress.IsEnabled = true;
                tbPort.IsEnabled = true;
                btnNewGuid.Visibility = Visibility.Collapsed;
                btnReloadIpAddress.Visibility = Visibility.Collapsed;
                btnReloadPort.Visibility = Visibility.Collapsed;
                btnReloadPublicKey.Visibility = Visibility.Collapsed;
                tbId.Visibility = Visibility.Collapsed;
                tblId.Visibility = Visibility.Collapsed;
                tbIpAddress.Visibility = Visibility.Visible;
                tblIpAddress.Visibility = Visibility.Visible;
                tbPort.Visibility = Visibility.Visible;
                tblPort.Visibility = Visibility.Visible;
                tbPublicKey.Visibility = Visibility.Collapsed;
                tblPublickKey.Visibility = Visibility.Collapsed;
            }
        }

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

        private void Init()
        {
            msgSwitch
             .Case(contract.GetContractId(typeof(WindowStateSetMessage)), (WindowStateSetMessage x) => WindowStateSetMessageHandler(x))
             .Case(contract.GetContractId(typeof(NodeSettingWindowMessage)), (NodeSettingWindowMessage x) => NodeSettingWindowMessageHandler(x))
             ;
        }

        private void WindowStateSetMessageHandler(WindowStateSetMessage message)
        {
            if (WindowState == System.Windows.WindowState.Minimized)
            {
                WindowState = System.Windows.WindowState.Normal;
            }
            Activate();
        }

        private void NodeSettingWindowMessageHandler(NodeSettingWindowMessage message)
        {
            SetWindow(message);
        }

        private void SetFieldsByNode(Node node)
        {
            tbId.Text = node.Id.ToString();
            tbIpAddress.Text = node.Address;
            tbPort.Text = node.Port.ToString();
            tbPublicKey.Text = node.PublicKey;
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

        #endregion PrivateMethods
    }
}