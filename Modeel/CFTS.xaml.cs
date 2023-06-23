using Modeel.FastTcp;
using Modeel.Messages;
using Modeel.Model;
using Modeel.P2P;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;

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
      private List<IUniversalClientSocket> _p2pClients = new List<IUniversalClientSocket>();
      private FileReceiver? _fileReceiver;

      #endregion PrivateFields

      #region Ctor

      public CFTS()
      {
         InitializeComponent();
         contract.Add(MsgIds.SocketStateChangeMessage, typeof(SocketStateChangeMessage));
         contract.Add(MsgIds.P2pClietsUpdateMessage, typeof(P2pClietsUpdateMessage));

         Init();

         LoadRequestFromConfig();

         dgRequests.ItemsSource = _requestModels;
         _p2PMasterClass = new P2pMasterClass(this);

         Closed += Window_closedEvent;
      }

      #endregion Ctor

      internal void Init()
      {
         msgSwitch
          .Case(contract.GetContractId(typeof(SocketStateChangeMessage)), (SocketStateChangeMessage x) => SocketStateChangeMessageHandler(x))
          .Case(contract.GetContractId(typeof(P2pClietsUpdateMessage)), (P2pClietsUpdateMessage x) => P2pClietsUpdateMessageHandler(x))
          ;
      }

      #region PublicMethods



      #endregion PublicMethods

      #region PrivateMethods

      private void LoadRequestFromConfig()
      {
         RequestModelObject request = new RequestModelObject();
         request.FilePath = Settings.Default.File1Name;
         request.FileSize = Settings.Default.File1Size;
         request.Clients.Add(new BaseClient() { IpAddress = Settings.Default.File1IpAddress1, Port = Settings.Default.File1Port1 });
         //request.Clients.Add(new BaseClient() { IpAddress = Settings.Default.File1IpAddress2, Port = Settings.Default.File1Port2 });
         _requestModels.Add(request);
      }

      private void P2pClietsUpdateMessageHandler(P2pClietsUpdateMessage message)
      {
         _p2pClients = message.Clients;
      }

      private void SocketStateChangeMessageHandler(SocketStateChangeMessage message)
      {

      }

      #endregion PrivateMethods

      #region ProtectedMethods



      #endregion ProtectedMethods

      #region EventHandler

      private void Window_closedEvent(object? sender, EventArgs e)
      {
         Closed -= Window_closedEvent;

         _p2PMasterClass.CloseAllConnections();
         _p2pClients.Clear();

      }

      private void btnRequest_Click(object sender, RoutedEventArgs e)
      {
         Button? b = sender as Button;

         if (b?.Tag is RequestModelObject requestModel)
         {
            int megabyte = 0x100000;
            int filePartSize = megabyte;
            _fileReceiver = new FileReceiver(requestModel.FileSize, filePartSize, Path.GetFileName(requestModel.FilePath));

            foreach (BaseClient client in requestModel.Clients)
            {
               if (IPAddress.TryParse(client.IpAddress, out IPAddress? iPAddress))
               {
                  _p2PMasterClass.CreateNewClient(new ClientBussinesLogic2(iPAddress, client.Port, this, requestModel.FilePath, requestModel.FileSize, _fileReceiver, filePartSize*2, filePartSize*2));
               }
            }
         }
      }

      #endregion EventHandler

      #region OverridedMethods



      #endregion OverridedMethods

   }
}
