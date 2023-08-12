using Modeel.FastTcp;
using Modeel.Log;
using Modeel.Messages;
using Modeel.Model;
using Modeel.P2P;
using Modeel.SSL;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Modeel
{
    /// <summary>
    /// Interaction logic for CFTS.xaml
    /// </summary>
    public partial class CFTS : BaseWindowForWPF
    {

        #region Properties



        #endregion Properties

        #region PublicFields



        #endregion PublicFields

        #region PrivateFields

        private List<RequestModelObject> _requestModels = new List<RequestModelObject>();
        private readonly IP2pMasterClass _p2PMasterClass;
        //private List<IUniversalClientSocket> _p2pClients = new List<IUniversalClientSocket>();
        //private FileReceiver? fileReceiver;
        private List<DownloadModelObject> _downloadModels = new List<DownloadModelObject>();
        //private AutoRefreshingCollection<DownloadModelObject> _downloadModels = new AutoRefreshingCollection<DownloadModelObject>();
        private SslContext _contextForP2pAsClient;
        private SslContext _contextForP2pAsServer;
        private readonly string _certificateNameForP2pAsClient = "MyTestCertificateClient.pfx";
        private readonly string _certificateNameForP2pAsServer = "MyTestCertificateServer.pfx";

        private Timer? _timer;


        #endregion PrivateFields

        #region Ctor

        public CFTS()
        {
            InitializeComponent();
            contract.Add(MsgIds.SocketStateChangeMessage, typeof(SocketStateChangeMessage));
            contract.Add(MsgIds.P2pClietsUpdateMessage, typeof(P2pClietsUpdateMessage));
            contract.Add(MsgIds.RefreshTablesMessage, typeof(RefreshTablesMessage));

            Init();

            LoadRequestFromConfig();

            dgRequests.ItemsSource = _requestModels;
            dgDownloading.ItemsSource = _downloadModels;
            _p2PMasterClass = new P2pMasterClass(this);

            Closed += Window_closedEvent;

            _contextForP2pAsClient = new SslContext(SslProtocols.Tls12, new X509Certificate2(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _certificateNameForP2pAsClient), ""), (sender, certificate, chain, sslPolicyErrors) => true);
            _contextForP2pAsServer = new SslContext(SslProtocols.Tls12, new X509Certificate2(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _certificateNameForP2pAsServer), ""), (sender, certificate, chain, sslPolicyErrors) => true);

            // anonymous method that will be called every time the timer elapses
            // there is a need to transfer calling to ui thread, so we are sending message to ourself
            _timer = new Timer(1000); // Set the interval to 1 second
            _timer.Elapsed += Timer_elapsed;
            _timer.Start();
        }

        #endregion Ctor

        internal void Init()
        {
            msgSwitch
             .Case(contract.GetContractId(typeof(SocketStateChangeMessage)), (SocketStateChangeMessage x) => SocketStateChangeMessageHandler(x))
             .Case(contract.GetContractId(typeof(P2pClietsUpdateMessage)), (P2pClietsUpdateMessage x) => P2pClietsUpdateMessageHandler(x))
             .Case(contract.GetContractId(typeof(RefreshTablesMessage)), (RefreshTablesMessage x) => RefreshTablesMessageHandler())
             .Case(contract.GetContractId(typeof(DisposeMessage)), (DisposeMessage x) => DisposeMessageHandler(x))
             ;
        }

        #region PublicMethods



        #endregion PublicMethods

        #region PrivateMethods

        private void DisposeMessageHandler(DisposeMessage disposeMessage)
        {
            DownloadModelObject? downloadModelObject = _downloadModels.FirstOrDefault(x => x.Clients.Any(client => client.Id == disposeMessage.SessionGuid));
            downloadModelObject?.Clients.RemoveAll(client => client.Id == disposeMessage.SessionGuid);
        }

        private void RefreshTablesMessageHandler()
        {
            //dgDownloading.ItemsSource = null;
            //dgDownloading.ItemsSource = _downloadModels;
            ICollectionView dataGridCollectionView = CollectionViewSource.GetDefaultView(dgDownloading.ItemsSource);
            dataGridCollectionView.Refresh();
        }

        private void LoadRequestFromConfig()
        {
            RequestModelObject request = new RequestModelObject();
            request.FilePath = Settings.Default.File1Name;
            request.FileSize = Settings.Default.File1Size;
            request.Clients.Add(new BaseClient() { IpAddress = Settings.Default.File1IpAddress1, Port = Settings.Default.File1Port1 });
            request.Clients.Add(new BaseClient() { IpAddress = Settings.Default.File1IpAddress2, Port = Settings.Default.File1Port2 , UseThisClient = false});

            RequestModelObject request2 = new RequestModelObject();
            request2.FilePath = Settings.Default.File2Name;
            request2.FileSize = Settings.Default.File2Size;
            request2.Clients.Add(new BaseClient() { IpAddress = Settings.Default.File2IpAddress1, Port = Settings.Default.File2Port1, TypeOfSocket = Model.Enums.TypeOfClientSocket.TCP_CLIENT_SSL });

            RequestModelObject request3 = new RequestModelObject();
            request3.FilePath = Settings.Default.File3Name;
            request3.FileSize = Settings.Default.File3Size;
            request3.Clients.Add(new BaseClient() { IpAddress = Settings.Default.File3IpAddress1, Port = Settings.Default.File3Port1, TypeOfSocket = Model.Enums.TypeOfClientSocket.TCP_CLIENT });

            _requestModels.Add(request);
            _requestModels.Add(request2);
            _requestModels.Add(request3);
        }

        private void P2pClietsUpdateMessageHandler(P2pClietsUpdateMessage message)
        {
            //_p2pClients = message.Clients;
        }

        private void SocketStateChangeMessageHandler(SocketStateChangeMessage message)
        {

        }

        #endregion PrivateMethods

        #region ProtectedMethods



        #endregion ProtectedMethods

        #region EventHandler

        private void Timer_elapsed(object? sender, ElapsedEventArgs e)
        {
            this.BaseMsgEnque(new RefreshTablesMessage());
        }

        private void Window_closedEvent(object? sender, EventArgs e)
        {

            if (_timer != null)
            {
                _timer.Elapsed -= Timer_elapsed;
                _timer.Stop();
                _timer.Dispose();
                _timer = null;
            }


            _downloadModels.Clear();
            _p2PMasterClass.CloseAllConnections();

            Closed -= Window_closedEvent;
        }

        private void btnRequest_Click(object sender, RoutedEventArgs e)
        {
            Button? b = sender as Button;

            if (b?.Tag is RequestModelObject requestModel && requestModel.Clients.Any(client => client.UseThisClient == true))
            {
                //int megabyte = 0x100000;
                //int filePartSize = megabyte;
                //FileReceiver fileReceiver = new FileReceiver(requestModel.FileSize, filePartSize, Path.GetFileName(requestModel.FilePath));
                FileReceiver fileReceiver = GetFileReceiver($@"{ConfigurationManager.AppSettings["DownloadingDirectory"]}\{Path.GetFileName(requestModel.FilePath)}", requestModel.FileSize);
                DownloadModelObject downloadModelObject = new DownloadModelObject(fileReceiver);

                foreach (BaseClient baseClient in requestModel.Clients)
                {
                    if (baseClient.UseThisClient && IPAddress.TryParse(baseClient.IpAddress, out IPAddress? iPAddress))
                    {
                        if (baseClient.TypeOfSocket == Model.Enums.TypeOfClientSocket.TCP_CLIENT)
                        {
                            IUniversalClientSocket socket = new ClientBussinesLogic2(iPAddress, baseClient.Port, this, requestModel.FilePath, requestModel.FileSize, fileReceiver, fileReceiver.PartSize * 2, fileReceiver.PartSize * 2);
                            downloadModelObject.Clients.Add(socket);
                            _p2PMasterClass.CreateNewClient(socket);
                        }
                        else if (baseClient.TypeOfSocket == Model.Enums.TypeOfClientSocket.TCP_CLIENT_SSL)
                        {
                            IUniversalClientSocket socket = new SslClientBussinesLogic(_contextForP2pAsClient, iPAddress, baseClient.Port, this, requestModel.FilePath, requestModel.FileSize, fileReceiver, (int)fileReceiver.PartSize * 2, (int)fileReceiver.PartSize * 2);
                            downloadModelObject.Clients.Add(socket);
                            _p2PMasterClass.CreateNewClient(socket);
                        }
                    }
                }

                _downloadModels.Add(downloadModelObject);

            }
        }

        /// <summary>
        /// Look if there is Downloading status file, we need to know if we starting download from beggining or continuing 
        /// </summary>
        /// <param name="downloadingFile"></param>
        /// <param name="fileSize"></param>
        /// <returns></returns>
        private FileReceiver GetFileReceiver(string downloadingFile, long fileSize)
        {
            string downloadingStatusFile = Path.ChangeExtension(downloadingFile, ".cfts");

            if (DownloadingStatusFileController.CheckForValidStatusFile(downloadingStatusFile))
            {
                DownloadStatus downloadStatus = DownloadingStatusFileController.LoadStatusFile(downloadingStatusFile);

                if (downloadStatus.FileSize == fileSize)
                {
                    if (FileReceiver.CalculateTotalPartsCount(downloadStatus.FileSize, (int)downloadStatus.PartSize) == downloadStatus.TotalParts)
                    {
                        if (FileReceiver.CalculateLastPartSize(downloadStatus.FileSize, (int)downloadStatus.PartSize) == downloadStatus.LastPartSize)
                        {
                            return new FileReceiver(downloadStatus.ReceivedParts, downloadStatus.TotalParts, downloadStatus.FileSize, downloadStatus.PartSize, downloadStatus.LastPartSize, downloadingFile);
                        }
                        else
                        {
                            Logger.WriteLog("Saved Status File has different last part size, starting downloading from beginning", LoggerInfo.downloadingStatusFile);
                        }
                    }
                    else
                    {
                        Logger.WriteLog("Saved Status File has different total file part count, starting downloading from beginning", LoggerInfo.downloadingStatusFile);
                    }
                }
                else
                {
                    Logger.WriteLog("Saved Status File has different file size, starting downloading from beginning", LoggerInfo.downloadingStatusFile);
                }
            }
            else
            {
                Logger.WriteLog("Saved Status File is Invalid, starting downloading from beginning", LoggerInfo.downloadingStatusFile);
            }
            int megabyte = 0x100000;
            int filePartSize = megabyte;
            return new FileReceiver(fileSize, filePartSize, downloadingFile);
        }

        private void btnExpandColapse_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button b)
            {
                DataGridRow row = DataGridRow.GetRowContainingElement(b);
                if (row != null)
                {
                    if (row.DetailsVisibility == Visibility.Visible)
                    {
                        row.DetailsVisibility = Visibility.Collapsed;
                    }
                    else
                    {
                        row.DetailsVisibility = Visibility.Visible;
                    }
                }
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

        #endregion EventHandler

        #region OverridedMethods



        #endregion OverridedMethods

    }
}
