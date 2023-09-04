using Common.Enum;
using Common.Model;
using Common.ThreadMessages;
using ConfigManager;
using Logger;
using SslTcpSession;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace Client.Windows
{
   /// <summary>
   /// Interaction logic for MainWindow.xaml
   /// </summary>
   public partial class MainWindow : BaseWindowForWPF
   {

      #region Properties



      #endregion Properties

      #region PublicFields



      #endregion PublicFields

      #region PrivateFields

      private const string _cftsDirectoryName = "CFTS";
      private const string _cftsFileExtensions = ".cfts";

      //private IUniversalClientSocket? _socketToCentralServer;
      private readonly string _certificateNameForCentralServerConnect = "MyTestCertificateClient.pfx";
      private readonly SslContext _contextForCentralServerConnect;
      private IPAddress _centralServerIpAddress = NetworkUtils.GetLocalIPAddress() ?? IPAddress.Loopback;
      private int _centralServerPort = 34258;

      private readonly ObservableCollection<OfferingFileDto> _offeringFiles = new ObservableCollection<OfferingFileDto>();
      private readonly ObservableCollection<OfferingFileDto> _localOfferingFiles = new ObservableCollection<OfferingFileDto>();

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

         contract.Add(MsgIds.ClientSocketStateChangeMessage, typeof(ClientSocketStateChangeMessage));
         contract.Add(MsgIds.OfferingFilesReceivedMessage, typeof(OfferingFilesReceivedMessage));

         if (MyConfigManager.TryGetConfigValue<Int32>("CeentralServerPort", out Int32 centralServerPort))
         {
            _centralServerPort = centralServerPort;
         }

         _contextForCentralServerConnect = new SslContext(SslProtocols.Tls12, new X509Certificate2(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _certificateNameForCentralServerConnect), ""), (sender, certificate, chain, sslPolicyErrors) => true);

         Init();

         tbSuccessMessage.Visibility = Visibility.Collapsed;
      }

      #endregion Ctor

      #region PublicMethods



      #endregion PublicMethods

      #region PrivateMethods

      private void Init()
      {
         msgSwitch
          .Case(contract.GetContractId(typeof(ClientSocketStateChangeMessage)), (ClientSocketStateChangeMessage x) => ClientSocketStateChangeMessageHandler(x))
          .Case(contract.GetContractId(typeof(OfferingFilesReceivedMessage)), (OfferingFilesReceivedMessage x) => OfferingFilesReceivedMessageHandler(x.OfferingFiles))
          ;

         tbTitle.Text = $"Custom File Transfer System [v.{Assembly.GetExecutingAssembly().GetName().Version}]";

         LoadLocalOfferingFiles();
         dtgOfferingFiles.ItemsSource = _offeringFiles;
         dtgLocalOfferingFiles.ItemsSource = _localOfferingFiles;
      }

      private void LoadLocalOfferingFiles()
      {
         Log.WriteLog(LogLevel.DEBUG, "LoadLocalOfferingFiles");
         string filesDirectory = Path.Combine(MyConfigManager.GetConfigValue("DownloadingDirectory"), _cftsDirectoryName);
         if (Directory.Exists(filesDirectory))
         {
            string[] files = Directory.GetFiles(filesDirectory);

            // Filter the list to include only files with the desired extension
            var filteredFiles = files.Where(f => Path.GetExtension(f).Equals(_cftsFileExtensions)).ToList();

            for (int i = 0; i < files.Length; i++)
            {
               string jsonString = File.ReadAllText(files[i]);    // Read content of file

               Log.WriteLog(LogLevel.INFO, $"Reading content of file: {files[i]}, content: {jsonString}");
               // Validate if conten is valid json
               try
               {
                  // Attempt to parse the JSON string
                  OfferingFileDto? offeringFileDto = OfferingFileDto.ToObjectFromJson(jsonString);
                  if (offeringFileDto != null)
                  {
                     TryAddLocalOfferingFile(offeringFileDto);
                  }

               }
               catch (JsonException ex)
               {
                  // Parsing failed, so the JSON is not valid
                  Log.WriteLog(LogLevel.WARNING, "Content is invalid! " + ex.Message);
               }
            }
         }
      }

      private void TryAddLocalOfferingFile(OfferingFileDto offeringFileDto)
      {
         if (!_localOfferingFiles.Contains(offeringFileDto))
         {
            OfferingFileDto? oldOfferingFileWithSameIdentificator = _localOfferingFiles.FirstOrDefault(x => x.OfferingFileIdentificator.Equals(offeringFileDto.OfferingFileIdentificator));
            if (oldOfferingFileWithSameIdentificator != null)
            {
               _localOfferingFiles.Remove(oldOfferingFileWithSameIdentificator);
            }
            _localOfferingFiles.Add(offeringFileDto);
         }
      }

      private void TryDeleteLocalOfferingFile(OfferingFileDto offeringFileDto)
      {
         // Delete from memmory of the program
         if (_localOfferingFiles.Contains(offeringFileDto))
         {
            _localOfferingFiles.Remove(offeringFileDto);
         }

         // Delete from saved file
         string filesDirectory = Path.Combine(MyConfigManager.GetConfigValue("DownloadingDirectory"), _cftsDirectoryName);
         string fileName = offeringFileDto.OfferingFileIdentificator + _cftsFileExtensions;
         string filePath = Path.Combine(filesDirectory, fileName);
         if (File.Exists(filePath))
         {
            File.Delete(filePath);
         }
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

      private void ClientSocketStateChangeMessageHandler(ClientSocketStateChangeMessage message)
      {
         Log.WriteLog(LogLevel.DEBUG, "ClientSocketStateChangeMessageHandler");
         if (message.ClientSocketState == ClientSocketState.CONNECTED)
         {
            if (message.TypeOfSession == TypeOfSession.DOWNLOADING_OFFERING_FILES_SESSION_WITH_CENTRAL_SERVER)
            {
               _offeringFiles.Clear();
            }

         }
         else if (message.ClientSocketState == ClientSocketState.DISCONNECTED)
         {
            if (message.TypeOfSession == TypeOfSession.UPDATING_OFFERING_FILES_SESSION_WITH_CENTRAL_SERVER)
            {
               ShowTimedMessageAndEnableUI("Offering files uploading operations ended!", TimeSpan.FromSeconds(4), btnUploadOfferingFilesToCentralServer);
            }
            if (message.TypeOfSession == TypeOfSession.DOWNLOADING_OFFERING_FILES_SESSION_WITH_CENTRAL_SERVER)
            {
               ShowTimedMessageAndEnableUI("Offering files downloading operation ended!", TimeSpan.FromSeconds(4), btnDownloadOfferingFilesFromCetnralServer);
            }
         }
      }

      private void OfferingFilesReceivedMessageHandler(List<OfferingFileDto> offeringFiles)
      {
         Log.WriteLog(LogLevel.DEBUG, "OfferingFilesReceivedMessageHandler");
         foreach (var offeringFile in offeringFiles)
         {
            _offeringFiles.Add(offeringFile);
         }
      }

      #endregion PrivateMethods

      #region ProtectedMethods



      #endregion ProtectedMethods

      #region Events

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


      private void btnSaveFileIdentificator_Click(object sender, RoutedEventArgs e)
      {
         if (sender is Button button && button.Tag is OfferingFileDto offeringFileDto)
         {
            button.IsEnabled = false;
            Log.WriteLog(LogLevel.DEBUG, button.Name);

            // Add him to memmory of running program
            TryAddLocalOfferingFile(offeringFileDto);

            // Save him for later
            string fileName = offeringFileDto.OfferingFileIdentificator + _cftsFileExtensions;
            string fileDirectory = Path.Combine(MyConfigManager.GetConfigValue("DownloadingDirectory"), _cftsDirectoryName);

            if (!Directory.Exists(fileDirectory))
            {
               Directory.CreateDirectory(fileDirectory);
            }
            File.WriteAllText(Path.Combine(fileDirectory, fileName), offeringFileDto.GetJson());
            ShowTimedMessageAndEnableUI("File Identificator Saved!", TimeSpan.FromSeconds(2), button);
         }
      }

      private void btnDownloadOfferingFilesFromCetnralServer_Click(object sender, RoutedEventArgs e)
      {
         if (sender is Button button)
         {
            button.IsEnabled = false;
            Log.WriteLog(LogLevel.DEBUG, button.Name);
            new SslClientBussinesLogic(_contextForCentralServerConnect, _centralServerIpAddress, _centralServerPort, this,
                typeOfSession: TypeOfSession.DOWNLOADING_OFFERING_FILES_SESSION_WITH_CENTRAL_SERVER, optionReceiveBufferSize: 0x2000, optionSendBufferSize: 0x2000);
         }
      }


      private void btnUploadOfferingFilesToCentralServer_Click(object sender, RoutedEventArgs e)
      {
         if (sender is Button button)
         {
            button.IsEnabled = false;
            Log.WriteLog(LogLevel.DEBUG, button.Name);
            new SslClientBussinesLogic(_contextForCentralServerConnect, _centralServerIpAddress, _centralServerPort, this,
                typeOfSession: TypeOfSession.UPDATING_OFFERING_FILES_SESSION_WITH_CENTRAL_SERVER, optionReceiveBufferSize: 0x2000, optionSendBufferSize: 0x2000);
         }
      }


      private void btnDeleteFileIdentificator_Click(object sender, RoutedEventArgs e)
      {
         if (sender is Button button && button.Tag is OfferingFileDto offeringFileDto)
         {
            button.IsEnabled = false;
            Log.WriteLog(LogLevel.DEBUG, button.Name);

            TryDeleteLocalOfferingFile(offeringFileDto);
            ShowTimedMessageAndEnableUI("File Identificator Removed!", TimeSpan.FromSeconds(2), button);
         }
      }

      private void btnUploadingDirectory_Click(object sender, RoutedEventArgs e)
      {
         if (sender is Button button)
         {
            Log.WriteLog(LogLevel.DEBUG, button.Name);

            string uploadingDirectory = MyConfigManager.GetConfigValue("UploadingDirectory");
            if (!string.IsNullOrEmpty(uploadingDirectory) && Directory.Exists(uploadingDirectory))
            {
               Process.Start("explorer.exe", uploadingDirectory);
            }
         }
      }

      private void btnDownloadingDirectory_Click(object sender, RoutedEventArgs e)
      {
         if (sender is Button button)
         {
            Log.WriteLog(LogLevel.DEBUG, button.Name);
            string downloadingDirectory = MyConfigManager.GetConfigValue("DownloadingDirectory");
            if (!string.IsNullOrEmpty(downloadingDirectory) && Directory.Exists(downloadingDirectory))
            {
               Process.Start("explorer.exe", downloadingDirectory);
            }
         }
      }

      #endregion Events

      #region OverridedMethods



      #endregion OverridedMethods

   }
}
