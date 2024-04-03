using Common;
using Common.Enum;
using Common.Interface;
using Common.Model;
using Common.ThreadMessages;
using ConfigManager;
using Logger;
using SqliteClassLibrary;
using SslTcpSession;
using SslTcpSession.BlockChain;
using SslTcpSession.BlockChain.ThreadMessages;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Authentication;
using System.Text.Json;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using TcpSession;

namespace Client.Windows
{
   /// <summary>
   /// Interaction logic for MainWindow.xaml
   /// </summary>
   public partial class MainWindow : BaseWindowForWPF
   {

      #region Properties

      public ServerSocketState UploadingServerSocketState
      {
         get => _uploadingServerSocketState;
         private set
         {
            if (_uploadingServerSocketState != value)
            {
               _uploadingServerSocketState = value;
               Log.WriteLog(LogLevel.INFO, "Uploading Server Socket State Change To " + value);

               switch (value)
               {
                  case ServerSocketState.STARTED:
                     StartOfUploadingServer();
                     break;
                  case ServerSocketState.STOPPED:
                     StopOfUploadingServer();
                     break;
                  default:
                     break;
               }
            }
         }
      }

      public static IWindowEnqueuer? MainWindowQue {  get; private set; }

      #endregion Properties

      #region UploadingServer

      private void StartOfUploadingServer()
      {
         // Setting color to elipse on Tab header
         elpUploadingServerSocketState.Fill = new SolidColorBrush(Color.FromArgb(0xFF, 0x26, 0x3F, 0x03)); // Using Color class

         // Set starting and disposing toogle button to right positions
         swchSocketDisposeState.IsOnLeft = false;
         swchSocketState.IsOnLeft = false;

         // Start Uploading session refresher
         _uploadingSessionRefresher?.Start();

         // Show message
         ShowTimedMessage($"Uploading {_uploadingServerBussinessLogic?.Type} Soket started!", TimeSpan.FromSeconds(3));
      }

      private void StopOfUploadingServer()
      {
         // Setting color to elipse on Tab header
         elpUploadingServerSocketState.Fill = new SolidColorBrush(Color.FromArgb(0xFF, 0x74, 0x1B, 0x0C)); // Using Color class

         // Set starting toggle button to right position
         swchSocketState.IsOnLeft = true;

         // Stopping Uploading session refresher
         _uploadingSessionRefresher?.Stop();

         // Clearing uploading sessions datagrid
         dtgUploadingSessions.ItemsSource = null;

         // Show message
         ShowTimedMessage($"Uploading {_uploadingServerBussinessLogic?.Type} Socket stopped!", TimeSpan.FromSeconds(3));
      }

      private void CreationOfUploadingSocket()
      {
         Log.WriteLog(LogLevel.DEBUG, "CreationOfUploadingSocket");

         // Filling text blocks with ip and port on UI
         tbServerIpAddressVariable.Text = _uploadingServerBussinessLogic?.Address;
         tbServerPortVariable.Text = _uploadingServerBussinessLogic?.Port.ToString();

         // Set disposing toogle button to right position
         swchSocketDisposeState.IsOnLeft = false;

         // Set Socket type to righ position
         swchSocketType.IsOnLeft = _uploadingServerBussinessLogic?.Type == TypeOfServerSocket.TCP_SERVER ? true : false;

         // Create Uploading session refresher
         _uploadingSessionRefresher?.Dispose();
         _uploadingSessionRefresher = new Timer(1000);
         _uploadingSessionRefresher.Elapsed += UploadingSessionRefresher_Elapsed;

         // Disable choosing between tcp and ssl tcp socket
         swchSocketType.IsEnabled = false;

         // Enabling Starting socket toogle button
         swchSocketState.IsEnabled = true;

         // Show message
         ShowTimedMessage($"Uploading {_uploadingServerBussinessLogic?.Type} Socket Created!", TimeSpan.FromSeconds(3));
      }



      private void DisposeOfUploadingSocket(bool startupCalling = false)
      {
         if (!startupCalling)
            Log.WriteLog(LogLevel.DEBUG, "DisposingServerSocketDisposed");

         // Set UploadingServerSocketState to Stopped
         UploadingServerSocketState = ServerSocketState.STOPPED;

         // Clearing text blocks with ip and port on UI
         tbServerIpAddressVariable.Text = string.Empty;
         tbServerPortVariable.Text = string.Empty;

         // Set disposing toogle button to right position
         swchSocketDisposeState.IsOnLeft = true;

         // Disposing Uploading session refresher
         if (_uploadingSessionRefresher != null)
         {
            _uploadingSessionRefresher.Elapsed -= UploadingSessionRefresher_Elapsed;
            _uploadingSessionRefresher.Dispose();
            _uploadingSessionRefresher = null;
         }

         // Enable choosing between tcp and ssl tcp socket
         swchSocketType.IsEnabled = true;

         // Disabling Starting socket toogle button
         swchSocketState.IsEnabled = false;

         // Show message
         if (!startupCalling)
            ShowTimedMessage("Uploading Server Socket Disposed!", TimeSpan.FromSeconds(3));
      }

      #endregion UploadingServer


      #region PublicFields



      #endregion PublicFields

      #region PrivateFields

      private Timer? _uploadingSessionRefresher;

      private ServerSocketState _uploadingServerSocketState = ServerSocketState.STOPPED;

      private const string _cftsDirectoryName = "CFTS";
      private const string _cftsFileExtensions = ".cfts";

      //private IUniversalClientSocket? _socketToCentralServer;
      private readonly string _certificateNameForCentralServerConnect = "MyTestCertificateClientForCentralServer.pfx";
      private readonly string _certificateNameForP2pAsClient = "MyTestCertificateClient.pfx";
      private readonly string _certificateNameForP2pAsServer = "MyTestCertificateServer.pfx";

      private SslContext _contextForP2pAsClient;
      private SslContext _contextForP2pAsServer;

      private readonly SslContext _contextForCentralServerConnect;

      private readonly ObservableCollection<OfferingFileDto> _offeringFiles = new ObservableCollection<OfferingFileDto>();
      private readonly ObservableCollection<OfferingFileDto> _localOfferingFiles = new ObservableCollection<OfferingFileDto>();

      private ObservableCollection<DownloadModelObject> _downloadModels = new ObservableCollection<DownloadModelObject>();

      private IUniversalServerSocket? _uploadingServerBussinessLogic;

      private IWindowEnqueuer? _nodeSettingsWindow;
      private IWindowEnqueuer? _createBlockRequestWindow;

      private PbftAwaiter? _pbftAwaiter;
      private Block? _pbftCorrespondingBlockForAwaiter;

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
         contract.Add(MsgIds.DisposeMessage, typeof(DisposeMessage));
         contract.Add(MsgIds.ServerSocketStateChangeMessage, typeof(ServerSocketStateChangeMessage));
         contract.Add(MsgIds.CreationMessage, typeof(CreationMessage));
         contract.Add(MsgIds.SesrverDownloadingSessionsInfoMessage, typeof(ServerDownloadingSessionsInfoMessage));
         contract.Add(MsgIds.NodeListReceivedMessage, typeof(NodeListReceivedMessage));
         contract.Add(MsgIds.NodeSettingWindowMessage, typeof(NodeSettingWindowMessage));
         contract.Add(MsgIds.PbftReplicaLogDto, typeof(PbftReplicaLogDto));
         contract.Add(MsgIds.PbftPrePrepareMessageReceivedMessage, typeof(PbftPrePrepareMessageReceivedMessage));
         contract.Add(MsgIds.StartNewDownloadMessage, typeof(StartNewDownloadMessage));

         _contextForCentralServerConnect = new SslContext(SslProtocols.Tls12, Certificats.GetCertificate(_certificateNameForCentralServerConnect, Certificats.CertificateType.ClientConnectionWithCentralServer), (sender, certificate, chain, sslPolicyErrors) => true);
         _contextForP2pAsServer = new SslContext(SslProtocols.Tls12, Certificats.GetCertificate(_certificateNameForP2pAsServer, Certificats.CertificateType.Server), (sender, certificate, chain, sslPolicyErrors) => true);
         _contextForP2pAsClient = new SslContext(SslProtocols.Tls12, Certificats.GetCertificate(_certificateNameForP2pAsClient, Certificats.CertificateType.Client), (sender, certificate, chain, sslPolicyErrors) => true);

         Init();

         tbSuccessMessage.Visibility = Visibility.Collapsed;
         DisposeOfUploadingSocket(startupCalling: true);

         // Set to ssl from the start
         swchSocketType.IsOnLeft = false;

         MainWindowQue = this;
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
          .Case(contract.GetContractId(typeof(DisposeMessage)), (DisposeMessage x) => DisposeMessageHandler(x))
          .Case(contract.GetContractId(typeof(ServerSocketStateChangeMessage)), (ServerSocketStateChangeMessage x) => ServerSocketStateChangeMessageHandler(x))
          .Case(contract.GetContractId(typeof(CreationMessage)), (CreationMessage x) => CreationMessageHandler(x))
          .Case(contract.GetContractId(typeof(ServerDownloadingSessionsInfoMessage)), (ServerDownloadingSessionsInfoMessage x) => ServerDownloadingSessionsInfoMessageHandler(x))
          .Case(contract.GetContractId(typeof(NodeListReceivedMessage)), (NodeListReceivedMessage x) => NodeListReceivedMessageHandler(x.NodeDict))
          .Case(contract.GetContractId(typeof(PbftReplicaLogDto)), (PbftReplicaLogDto x) => PbftReplicaLogDtoHandler(x))
          .Case(contract.GetContractId(typeof(PbftPrePrepareMessageReceivedMessage)), (PbftPrePrepareMessageReceivedMessage x) => PbftPrePrepareMessageReceivedMessageHandler(x))
          .Case(contract.GetContractId(typeof(StartNewDownloadMessage)), (StartNewDownloadMessage x) => StartNewDownloadMessageHandler(x.OfferingFileDto))
          ;

         MyConfigManager.TryGetIntConfigValue("Instance", out int instance);
         tbTitle.Text = $"Custom File Transfer System {{i.{instance}}} [v.{Assembly.GetExecutingAssembly().GetName().Version}]";

         LoadLocalOfferingFiles();
         dtgOfferingFiles.ItemsSource = _offeringFiles;
         dtgLocalOfferingFiles.ItemsSource = _localOfferingFiles;
         dtgDownloading.ItemsSource = _downloadModels;
         dtgNodes.ItemsSource = NodeDiscovery.GetAllNodes();

         PbftMessageEvaluator.ReceivePbftMessage += PbftReplicaLogDtoHandler;
         SslPbftTmpClientBusinessLogic.ReceivePbftMessage += PbftReplicaLogDtoHandler;
         Blockchain.DownloadFile += OnDownloadFile;
      }

      private void LoadLocalOfferingFiles()
      {
         Log.WriteLog(LogLevel.DEBUG, "LoadLocalOfferingFiles");

         _localOfferingFiles.Clear();

         string filesDirectory = Path.Combine(MyConfigManager.GetConfigStringValue("DownloadingDirectory"), _cftsDirectoryName);
         if (Directory.Exists(filesDirectory))
         {
            string[] files = Directory.GetFiles(filesDirectory);

            // Filter the list to include only files with the desired extension
            var filteredFiles = files.Where(f => Path.GetExtension(f).Equals(_cftsFileExtensions)).ToList();

            for (int i = 0; i < files.Length; i++)
            {
               string jsonString = File.ReadAllText(files[i]);    // Read content of file

               Log.WriteLog(LogLevel.INFO, $"Reading content of file: {files[i]}, content: {jsonString}");
               // Validate if content is valid json
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
         string filesDirectory = Path.Combine(MyConfigManager.GetConfigStringValue("DownloadingDirectory"), _cftsDirectoryName);
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

      private void ServerDownloadingSessionsInfoMessageHandler(ServerDownloadingSessionsInfoMessage message)
      {
         //Log.WriteLog(LogLevel.DEBUG, "ServerDownloadingSessionsInfoMessageHandler");

         if (tbMyTabControl.SelectedIndex == 3)
         {
            dtgUploadingSessions.ItemsSource = message.ServerDownloadingSessionsInfo;
         }
      }

      private void NodeListReceivedMessageHandler(Dictionary<Guid, Node> nodeDict)
      {
         Log.WriteLog(LogLevel.DEBUG, "NodeListReceivedMessageHandler");
         NodeDiscovery.UpdateNodeList(nodeDict);
         NodeDiscovery.SaveNodes();
         //ShowTimedMessageAndEnableUI("NodeList received!", TimeSpan.FromSeconds(2), dtgNodes);
         ShowTimedMessage("NodeList received!", TimeSpan.FromSeconds(2));
      }

      private void PbftPrePrepareMessageReceivedMessageHandler(PbftPrePrepareMessageReceivedMessage message)
      {
         Log.WriteLog(LogLevel.DEBUG, $"Ssl server obtained a pbft replica log");

         if (!PbftAwaiter.BlockNewRequests)
         {
            _pbftCorrespondingBlockForAwaiter = message.RequestedBlock;
            _pbftAwaiter = new PbftAwaiter(NodeDiscovery.GetAllCurrentlyVerifiedActiveNodeGuids().Select(x => x.ToString()), message.RequestedBlock.Hash);
         }
      }

      private void StartNewDownloadMessageHandler(OfferingFileDto offeringFileDto)
      {
         Log.WriteLog(LogLevel.DEBUG, $"StartNewDownloadMessageHandler for file name: {offeringFileDto.FileName}");

         DownloadingStart(offeringFileDto);
      }

      private void OnDownloadFile(OfferingFileDto offeringFileDto)
      {
         this.BaseMsgEnque(new StartNewDownloadMessage(offeringFileDto));
      }

      private async void PbftReplicaLogDtoHandler(PbftReplicaLogDto log)
      {
         Log.WriteLog(LogLevel.DEBUG, $"Ssl server obtained a pbft replica log with flag: {log.MessageType}, direction: {log.MessageDirection}");

         if (log.MessageDirection == MessageDirection.RECEIVED)
         {
            switch (log.MessageType)
            {
               case SocketMessageFlag.PBFT_PREPARE:
               case SocketMessageFlag.PBFT_COMMIT:

                  if (_pbftAwaiter != null && log.HashOfRequest.Equals(_pbftAwaiter.HashOfRequest))
                  {
                     _pbftAwaiter.ReceivedMessage(log.SenderId, log.MessageType);

                     ActionRequired actionRequired = _pbftAwaiter.CheckActionRequired();
                     Log.WriteLog(LogLevel.INFO, $"PBFT awaiter returned action needed to: {actionRequired}");

                     if (actionRequired == ActionRequired.SEND_COMMIT)
                     {
                        _pbftAwaiter.CommitSent();

                        await SslPbftTmpClientBusinessLogic.MulticastCommitAndDispose(_pbftAwaiter.HashOfRequest,
                            Certificats.SignString(Certificats.GetCertificate("ReplicaXY", Certificats.CertificateType.Node), _pbftAwaiter.HashOfRequest), NodeDiscovery.GetMyNode().Id);
                     }
                     else if (actionRequired == ActionRequired.ADD_BLOCK_TO_BLOCKCHAIN)
                     {
                        if (_pbftCorrespondingBlockForAwaiter != null && _pbftAwaiter.HashOfRequest.Equals(_pbftCorrespondingBlockForAwaiter.Hash))
                        {
                           Blockchain.AddBlockAfterConsensus(_pbftCorrespondingBlockForAwaiter);
                           _pbftAwaiter.BlockAdded();
                           _pbftAwaiter = null;

                           SqliteDataAccessReplicaLog.InsertNewBlockAsync(_pbftCorrespondingBlockForAwaiter);

                           if (_pbftCorrespondingBlockForAwaiter.Transaction == TransactionType.ADD_FILE &&
                              _pbftCorrespondingBlockForAwaiter.NodeId == NodeDiscovery.GetMyNode().Id)
                           {
                              // ITS ME! I SHOULD CONNECT TO SOME REPLICA TO DOWNLOAD FILE FROM HIM (IF I DONT HAVE HIM)

                              bool haveFile = DataEncryptor.FindEncryptedFileByIdAndCheckHisSizeAndHash(_pbftCorrespondingBlockForAwaiter.FileID,
                              MyConfigManager.GetConfigStringValue("BlockchainFileDirectoryDownload"),
                              _pbftCorrespondingBlockForAwaiter.FileSize, _pbftCorrespondingBlockForAwaiter.FileHash);

                              if (!haveFile && NodeDiscovery.TryGetNode(_pbftCorrespondingBlockForAwaiter.NodeId, out Node? node))
                              {
                                 this.BaseMsgEnque(new StartNewDownloadMessage(new OfferingFileDto($"{node.Address}:{node.Port}", TypeOfServerSocket.TCP_SERVER_SSL)
                                 {
                                    FileName = _pbftCorrespondingBlockForAwaiter.FileIDAsString,
                                    FileSize = _pbftCorrespondingBlockForAwaiter.FileSize,
                                 }));
                              }
                           }
                        }
                     }
                  }
                  break;
               default:
                  break;
            }
         }

         await SqliteDataAccessReplicaLog.InsertLogAsync(log);
      }

      private void ServerSocketStateChangeMessageHandler(ServerSocketStateChangeMessage message)
      {
         Log.WriteLog(LogLevel.DEBUG, $"ServerSocketStateChangeMessageHandler TypeOfSession: {message.TypeOfSession}, ServerSocketState: {message.ServerSocketState}");

         if (message.TypeOfSession == TypeOfSession.DOWNLOADING)
         {
            UploadingServerSocketState = message.ServerSocketState;
         }
      }

      private void CreationMessageHandler(CreationMessage message)
      {
         Log.WriteLog(LogLevel.DEBUG, $"CreationMessageHandler id:{message.SessionGuid}, TypeOfSocket: {message.TypeOfSocket}, TypeOfSession: {message.TypeOfSession}");

         if (message.TypeOfSession == TypeOfSession.DOWNLOADING && message.TypeOfSocket == TypeOfSocket.SERVER)
         {
            // Uploading server socket created
            CreationOfUploadingSocket();
         }
      }

      private void DisposeMessageHandler(DisposeMessage message)
      {
         Log.WriteLog(LogLevel.DEBUG, $"DisposeMessageHandler " +
             $"id:{message.SessionGuid}, TypeOfSocket: {message.TypeOfSocket}, TypeOfSession: {message.TypeOfSession}, IsPurposeFullfilled: {message.IsPurposeFullfilled}");

         if (message.TypeOfSession == TypeOfSession.DOWNLOADING && message.TypeOfSocket == TypeOfSocket.CLIENT)
         {
            // Loop through each downloadModel
            foreach (var downloadModel in _downloadModels)
            {
               // Find the client with the matching Id
               var clientToRemove = downloadModel.Clients.FirstOrDefault(client => (client.Id == message.SessionGuid && client.IsDisposed));

               // If found, remove it from the list
               if (clientToRemove != null)
               {
                  downloadModel.Clients.Remove(clientToRemove);
                  if (message.IsPurposeFullfilled)
                  {
                     downloadModel.RefreshWaitForLast = true;
                     downloadModel.IsDownloading = false;
                  }
                  Log.WriteLog(LogLevel.DEBUG, $"Client socket: {clientToRemove.Id} disposed for downloading: {downloadModel.FileIndentificator}, with IsPurposeFullfilled: {message.IsPurposeFullfilled}");
               }
            }
         }
         if (message.TypeOfSession == TypeOfSession.DOWNLOADING && message.TypeOfSocket == TypeOfSocket.SERVER)
         {
            // Uploading server socket disposed
            DisposeOfUploadingSocket();
         }
         else if (message.TypeOfSession == TypeOfSession.NODE_DISCOVERY && message.TypeOfSocket == TypeOfSocket.CLIENT)
         {
            NodeSynchronization.ReleaseSem();

            if (!message.IsPurposeFullfilled)
            {
               ShowTimedMessage("NodeList not received, connection to node could not be made!", TimeSpan.FromSeconds(4));
            }
            else if (!string.IsNullOrEmpty(message.Address))
            {
               // add node to currently verified as active
               NodeDiscovery.UpdateCurrentlyVerifiedActiveNodeList(message.Address, message.Port);
            }
         }
      }

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

      private async Task ReloadReplicaLogsToDatagrid()
      {
         List<PbftReplicaLogDto> replicaLogs = await SqliteDataAccessReplicaLog.GetAllLogsAsync();
         dtgReplicaLogs.ItemsSource = replicaLogs;
      }

      private async Task ReloadBlockchainFromDb()
      {
         List<Block> blockchain = await SqliteDataAccessReplicaLog.GetAllBlocksAsync();
         Blockchain.LoadedChainFromDb(blockchain);
         dtgBlockchain.ItemsSource = blockchain.ToList();
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
            string fileDirectory = Path.Combine(MyConfigManager.GetConfigStringValue("DownloadingDirectory"), _cftsDirectoryName);

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
            if (IPAddress.TryParse(MyConfigManager.GetConfigStringValue("CentralServerIpAddress"), out IPAddress? cetralServerIpAdress)
                && cetralServerIpAdress != null
                && MyConfigManager.TryGetIntConfigValue("CentralServerPort", out Int32 centralServerPort))
            {
               button.IsEnabled = false;
               new SslClientBussinesLogic(_contextForCentralServerConnect, cetralServerIpAdress, centralServerPort, this,
               typeOfSession: TypeOfSession.DOWNLOADING_OFFERING_FILES_SESSION_WITH_CENTRAL_SERVER, optionReceiveBufferSize: 0x2000, optionSendBufferSize: 0x2000);
            }
         }
      }


      private void btnUploadOfferingFilesToCentralServer_Click(object sender, RoutedEventArgs e)
      {
         if (sender is Button button)
         {
            if (IPAddress.TryParse(MyConfigManager.GetConfigStringValue("CentralServerIpAddress"), out IPAddress? cetralServerIpAdress)
                && cetralServerIpAdress != null
                && MyConfigManager.TryGetIntConfigValue("CentralServerPort", out Int32 centralServerPort))
            {
               button.IsEnabled = false;
               new SslClientBussinesLogic(_contextForCentralServerConnect, cetralServerIpAdress, centralServerPort, this,
                   typeOfSession: TypeOfSession.UPDATING_OFFERING_FILES_SESSION_WITH_CENTRAL_SERVER, optionReceiveBufferSize: 0x2000, optionSendBufferSize: 0x2000);
            }
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

            string uploadingDirectory = MyConfigManager.GetConfigStringValue("UploadingDirectory");
            if (!string.IsNullOrEmpty(uploadingDirectory))
            {
               if (!Directory.Exists(uploadingDirectory))
               {
                  Directory.CreateDirectory(uploadingDirectory);
               }
               Process.Start("explorer.exe", uploadingDirectory);
            }
         }
      }

      private void btnDownloadingDirectory_Click(object sender, RoutedEventArgs e)
      {
         if (sender is Button button)
         {
            Log.WriteLog(LogLevel.DEBUG, button.Name);
            string downloadingDirectory = MyConfigManager.GetConfigStringValue("DownloadingDirectory");
            if (!string.IsNullOrEmpty(downloadingDirectory))
            {
               if (!Directory.Exists(downloadingDirectory))
               {
                  Directory.CreateDirectory(downloadingDirectory);
               }
               Process.Start("explorer.exe", downloadingDirectory);
            }
         }
      }

      private void btnStartDownloading_Click(object sender, RoutedEventArgs e)
      {
         if (sender is Button button && button.Tag is OfferingFileDto offeringFileDto)
         {
            button.IsEnabled = false;
            Log.WriteLog(LogLevel.DEBUG, button.Name);

            DownloadingStart(offeringFileDto);
            ShowTimedMessageAndEnableUI("Start of Downloading operation done!", TimeSpan.FromSeconds(2), button);
         }
      }

      private void DownloadingStart(OfferingFileDto offeringFileDto)
      {
         DownloadModelObject? downloadModelObject = _downloadModels.FirstOrDefault(x => x.FileIndentificator.Equals(offeringFileDto.OfferingFileIdentificator));
         if (downloadModelObject == null)
         {
            Log.WriteLog(LogLevel.DEBUG, $"Creating new Download model fo identificator: {offeringFileDto.OfferingFileIdentificator}");
            NewDownloadingStart(offeringFileDto);
         }
         else
         {
            Log.WriteLog(LogLevel.DEBUG, $"Downloading already exist: {offeringFileDto.OfferingFileIdentificator}, refreshing cliets");
            ExistingDownloadingStart(downloadModelObject, offeringFileDto);
         }
      }

      private void ExistingDownloadingStart(DownloadModelObject downloadModelObject, OfferingFileDto offeringFileDto)
      {
         foreach (KeyValuePair<string, EndpointProperties> endPointsAndProperties in offeringFileDto.EndpointsAndProperties)
         {
            TryCreateNewDownloadingClientBusinessLogic(downloadModelObject, endPointsAndProperties.Key, endPointsAndProperties.Value.TypeOfServerSocket);
         }
      }

      private void NewDownloadingStart(OfferingFileDto offeringFileDto)
      {
         string? downloadingDirectory;
         if (!Guid.TryParse(offeringFileDto.FileName, out _))
         {
            downloadingDirectory = ConfigurationManager.AppSettings["DownloadingDirectory"];
         }
         else
         {
            downloadingDirectory = ConfigurationManager.AppSettings["BlockchainFileDirectoryDownload"];
         }

         if (string.IsNullOrEmpty(downloadingDirectory))
         {
            Log.WriteLog(LogLevel.ERROR, $"Empty BlockchainFileDirectoryDownload or DownloadingDirectory!");
            return;
         }

         // Create downloading directory if not exist
         if (!Directory.Exists(downloadingDirectory))
         {
            Directory.CreateDirectory(downloadingDirectory);
         }

         // Create FileReceiver
         FileReceiver fileReceiver = FileReceiver.GetFileReceiver($@"{downloadingDirectory}\{Path.GetFileName(offeringFileDto.FileName)}", offeringFileDto.FileSize);

         // Create DownloadModel
         DownloadModelObject downloadModelObject = new DownloadModelObject(fileReceiver, offeringFileDto.OfferingFileIdentificator);

         // Create Client bussiness logics
         foreach (KeyValuePair<string, EndpointProperties> endPointsAndProperties in offeringFileDto.EndpointsAndProperties)
         {
            TryCreateNewDownloadingClientBusinessLogic(downloadModelObject, endPointsAndProperties.Key, endPointsAndProperties.Value.TypeOfServerSocket);
         }

         _downloadModels.Add(downloadModelObject);
      }

      private void TryCreateNewDownloadingClientBusinessLogic(DownloadModelObject downloadModelObject, string ipAddressAndPort, TypeOfServerSocket typeOfServerSocket)
      {
         if (NetworkUtils.TryGetIPEndPointFromString(ipAddressAndPort, out IPEndPoint iPEndPoint))
         {
            if (!downloadModelObject.Clients.Any(client => client.Endpoint.ToString().Equals(iPEndPoint.ToString())))
            {
               if (typeOfServerSocket == TypeOfServerSocket.TCP_SERVER)
               {
                  Log.WriteLog(LogLevel.DEBUG, $"Creating new Tcp Download client BussinesLogic for endpoint: {iPEndPoint}");
                  IUniversalClientSocket socket = new ClientBussinesLogic2(iPEndPoint.Address, iPEndPoint.Port, this, downloadModelObject.FileReceiver.FileName,
                     downloadModelObject.FileReceiver.FileSize, downloadModelObject.FileReceiver, downloadModelObject.FileReceiver.PartSize * 2, downloadModelObject.FileReceiver.PartSize * 2,
                     typeOfSession: TypeOfSession.DOWNLOADING);

                  downloadModelObject.Clients.Add(socket);
                  downloadModelObject.IsDownloading = true;
                  //_p2PMasterClass.CreateNewClient(socket);
               }
               if (typeOfServerSocket == TypeOfServerSocket.TCP_SERVER_SSL)
               {
                  Log.WriteLog(LogLevel.DEBUG, $"Creating new Ssl Download client BussinesLogic for endpoint: {iPEndPoint}");
                  IUniversalClientSocket socket = new SslClientBussinesLogic(_contextForP2pAsClient, iPEndPoint.Address, iPEndPoint.Port, this, downloadModelObject.FileReceiver.FileName,
                     downloadModelObject.FileReceiver.FileSize, downloadModelObject.FileReceiver, downloadModelObject.FileReceiver.PartSize * 2, downloadModelObject.FileReceiver.PartSize * 2,
                     typeOfSession: TypeOfSession.DOWNLOADING);

                  downloadModelObject.Clients.Add(socket);
                  downloadModelObject.IsDownloading = true;
                  //_p2PMasterClass.CreateNewClient(socket);
               }
            }
         }
      }

      private void btnPauseDownload_Click(object sender, RoutedEventArgs e)
      {
         if (sender is Button button && tbMyTabControl.SelectedIndex == 0 && dtgDownloading.SelectedItem is DownloadModelObject downloadModelObject)
         {
            Log.WriteLog(LogLevel.DEBUG, button.Name);
            downloadModelObject.IsDownloading = false;
         }
      }

      private void swchSocketDisposeState_Switched(object sender, EventArgs e)
      {
         if (!(sender is CustomSwitchWithText customSwitchWithText)) return;

         Log.WriteLog(LogLevel.DEBUG, customSwitchWithText.Name + ", IsOnLeft: " + customSwitchWithText.IsOnLeft);

         bool starting = customSwitchWithText.IsOnLeft;

         TypeOfServerSocket typeOfServerSocket = swchSocketType.IsOnLeft ? TypeOfServerSocket.TCP_SERVER : TypeOfServerSocket.TCP_SERVER_SSL;

         // Starting socket
         if (starting && (_uploadingServerBussinessLogic == null || !_uploadingServerBussinessLogic.IsStarted))
         {

            // Check for valid ip address
            IPAddress? iPAddress = NetworkUtils.GetLocalIPAddress();
            if (iPAddress == null)
            {
               Log.WriteLog(LogLevel.ERROR, "Invalid ipAdress!");
               return;
            }

            // Check for valid port
            if (!MyConfigManager.TryGetIntConfigValue("UploadingServerPort", out Int32 port))
            {
               Log.WriteLog(LogLevel.ERROR, "Invalid port!");
               return;
            }

            // Check if there is already server socket, and if is, then if he have same address, port and type
            if (_uploadingServerBussinessLogic != null && iPAddress.ToString().Equals(_uploadingServerBussinessLogic.Address) && _uploadingServerBussinessLogic.Port == port
                && typeOfServerSocket == _uploadingServerBussinessLogic.Type)
            {
               // Socket we need already exist
               if (!_uploadingServerBussinessLogic.IsStarted)
               {
                  // Start, if its not started
                  //_uploadingServerBussinessLogic.Start();
                  Log.WriteLog(LogLevel.INFO, $"Socket for uploading with ip: {iPAddress}, port: {port}, type: {_uploadingServerBussinessLogic.Type} exist and stopped!");
               }
               else
               {
                  Log.WriteLog(LogLevel.INFO, $"Socket for uploading with ip: {iPAddress}, port: {port}, type: {_uploadingServerBussinessLogic.Type} exist and already started!");
               }
            }
            else
            {
               // There is no server socket, or we want different parameters
               // Clear presious socket if exist
               _uploadingServerBussinessLogic?.Stop();
               _uploadingServerBussinessLogic?.Dispose();


               // Check for valid free port
               if (!NetworkUtils.IsPortFree(port, iPAddress))
               {
                  Log.WriteLog(LogLevel.WARNING, $"Port: {port} is not free!");
                  port = NetworkUtils.GetRandomFreePort(iPAddress);
               }

               // Choosing between tcp and ssltcp to create
               if (typeOfServerSocket == TypeOfServerSocket.TCP_SERVER)
               {
                  // TCP
                  Log.WriteLog(LogLevel.INFO, $"Starting Tcp socket in ip: {iPAddress}, port: {port}");
                  _uploadingServerBussinessLogic = new ServerBussinesLogic2(iPAddress, port, this, optionAcceptorBacklog: 2);
               }
               else
               {
                  // SSL
                  Log.WriteLog(LogLevel.INFO, $"Starting SSl Tcp socket in ip: {iPAddress}, port: {port}");
                  _uploadingServerBussinessLogic = new SslServerBussinessLogic(_contextForP2pAsServer, iPAddress, port, this, optionAcceptorBacklog: 2);
               }
            }
         }
         else
         {
            // Stopping and disposing socket
            _uploadingServerBussinessLogic?.Stop();
            _uploadingServerBussinessLogic?.Dispose();
            _uploadingServerBussinessLogic = null;
         }
      }

      private void swchSocketType_Switched(object sender, EventArgs e)
      {
         if (!(sender is CustomSwitchWithText customSwitchWithText)) return;
         Log.WriteLog(LogLevel.DEBUG, customSwitchWithText.Name + ", IsOnLeft: " + customSwitchWithText.IsOnLeft);

         customSwitchWithText.IsOnLeft = !customSwitchWithText.IsOnLeft;
      }


      private void swchSocketState_Switched(object sender, EventArgs e)
      {
         if (!(sender is CustomSwitchWithText customSwitchWithText)) return;
         Log.WriteLog(LogLevel.DEBUG, customSwitchWithText.Name + ", IsOnLeft: " + customSwitchWithText.IsOnLeft);
         if (_uploadingServerBussinessLogic != null)
         {
            bool start = customSwitchWithText.IsOnLeft;

            if (!start)
            {
               _uploadingServerBussinessLogic.Stop();
            }
            else
            {
               _uploadingServerBussinessLogic.Start();
            }
         }
      }

      private void UploadingSessionRefresher_Elapsed(object? sender, ElapsedEventArgs e)
      {
         if (_uploadingServerBussinessLogic != null)
         {
            this.BaseMsgEnque(new ServerDownloadingSessionsInfoMessage(_uploadingServerBussinessLogic.GetDownloadingSessionsInfo()));
         }
      }

      private void btnReloadNodesFile_Click(object sender, RoutedEventArgs e)
      {
         if (sender is Button button)
         {
            Log.WriteLog(LogLevel.DEBUG, button.Name);

            NodeDiscovery.LoadNodes();
            dtgNodes.ItemsSource = null;
            dtgNodes.ItemsSource = NodeDiscovery.GetNodesForGrid();

            ShowTimedMessage("Nodes reloaded from local file!", TimeSpan.FromSeconds(2));
         }
      }

      private void btnNodeDiscovery_Click(object sender, RoutedEventArgs e)
      {
         if (sender is Button button && button.Tag is Node node)
         {
            if (IPAddress.TryParse(node.Address, out IPAddress? nodeIpAdress) && nodeIpAdress != null)
            {
               dtgNodes.IsEnabled = false;
               new SslClientBussinesLogic(_contextForP2pAsClient, nodeIpAdress, node.Port, this,
                   typeOfSession: TypeOfSession.NODE_DISCOVERY, optionReceiveBufferSize: 0x2000, optionSendBufferSize: 0x2000);
            }
         }
      }

      private async void btnNodeSynchronization_Click(object sender, RoutedEventArgs e)
      {
         if (sender is Button button)
         {
            Log.WriteLog(LogLevel.DEBUG, button.Name);

            dtgNodes.IsEnabled = false;
            btnNodeSynchronization.IsEnabled = false;
            await NodeSynchronization.ExecuteSynchronization(this);
            ShowTimedMessage("Synchronization Complete!", TimeSpan.FromSeconds(5));
            dtgNodes.IsEnabled = true;
            btnNodeSynchronization.IsEnabled = true;
         }
      }

      private void btnSettings_Click(object sender, RoutedEventArgs e)
      {
         MyConfigManager.OpenConfigFile();
      }

      private void btnReloadLocalOfferingFilesFile_Click(object sender, RoutedEventArgs e)
      {
         LoadLocalOfferingFiles();
      }


      private void btnMyNode_Click(object sender, RoutedEventArgs e)
      {
         if (sender is Button button)
         {
            Log.WriteLog(LogLevel.DEBUG, button.Name);

            NodeSettingWindowMessage message = new NodeSettingWindowMessage(NodeDiscovery.GetMyNode(), NodeSettingsWindowState.MY_NODE);

            if (_nodeSettingsWindow == null || !_nodeSettingsWindow.IsOpen())
            {
               _nodeSettingsWindow = BaseWindowForWPF.CreateWindow<NodeSettingsWindow>(() => new NodeSettingsWindow(message));
            }
            else
            {
               _nodeSettingsWindow.BaseMsgEnque(message);
               _nodeSettingsWindow.BaseMsgEnque(new WindowStateSetMessage());
            }
         }
      }

      private void btnAddNode_Click(object sender, RoutedEventArgs e)
      {
         if (sender is Button button)
         {
            Log.WriteLog(LogLevel.DEBUG, button.Name);
            NodeSettingWindowMessage message = new NodeSettingWindowMessage(new Node(), NodeSettingsWindowState.ADD_FOREIGN_NODE);

            if (_nodeSettingsWindow == null || !_nodeSettingsWindow.IsOpen())
            {
               _nodeSettingsWindow = BaseWindowForWPF.CreateWindow<NodeSettingsWindow>(() => new NodeSettingsWindow(message));
            }
            else
            {
               _nodeSettingsWindow.BaseMsgEnque(message);
               _nodeSettingsWindow.BaseMsgEnque(new WindowStateSetMessage());
            }
         }
      }


      private void btnNodeModify_Click(object sender, RoutedEventArgs e)
      {
         if (sender is Button button && button.Tag is NodeForGrid node)
         {
            Log.WriteLog(LogLevel.DEBUG, button.Name);
            NodeSettingWindowMessage message = new NodeSettingWindowMessage(new Node(node), NodeSettingsWindowState.CHANGE_FOREIGN_NODE);

            if (_nodeSettingsWindow == null || !_nodeSettingsWindow.IsOpen())
            {
               _nodeSettingsWindow = BaseWindowForWPF.CreateWindow<NodeSettingsWindow>(() => new NodeSettingsWindow(message));
            }
            else
            {
               _nodeSettingsWindow.BaseMsgEnque(message);
               _nodeSettingsWindow.BaseMsgEnque(new WindowStateSetMessage());
            }
         }
      }

      private void btnNodeRemove_Click(object sender, RoutedEventArgs e)
      {
         if (sender is Button button && button.Tag is NodeForGrid node)
         {
            Log.WriteLog(LogLevel.DEBUG, button.Name);
            NodeDiscovery.RemoveNode(node.Id);
            NodeDiscovery.SaveNodes();
         }
      }

      private async void btnReloadReplicaLogs_Click(object sender, RoutedEventArgs e)
      {
         if (sender is Button button)
         {
            button.IsEnabled = false;
            Log.WriteLog(LogLevel.DEBUG, button.Name);
            await ReloadReplicaLogsToDatagrid();
            ShowTimedMessageAndEnableUI("Replica logs reloaded!", TimeSpan.FromSeconds(3), button);
         }
      }

      private async void btnReloadBlockchain_Click(object sender, RoutedEventArgs e)
      {
         if (sender is Button button)
         {
            button.IsEnabled = false;
            Log.WriteLog(LogLevel.DEBUG, button.Name);
            await ReloadBlockchainFromDb();
            ShowTimedMessageAndEnableUI("Blockchain reloaded!", TimeSpan.FromSeconds(3), button);
         }
      }

      private void btnCreateBlockRequest_Click(object sender, RoutedEventArgs e)
      {
         if (sender is Button button)
         {
            Log.WriteLog(LogLevel.DEBUG, button.Name);

            if (_createBlockRequestWindow == null || !_createBlockRequestWindow.IsOpen())
            {
               _createBlockRequestWindow = BaseWindowForWPF.CreateWindow<CreateBlockRequestWindow>();
            }
            else
            {
               _createBlockRequestWindow.BaseMsgEnque(new WindowStateSetMessage());
            }
         }
      }

      #endregion Events

      #region OverridedMethods



      #endregion OverridedMethods

   }
}
