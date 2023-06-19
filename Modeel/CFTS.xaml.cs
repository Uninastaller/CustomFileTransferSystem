using Modeel.FastTcp;
using Modeel.Frq;
using Modeel.Messages;
using Modeel.Model;
using Modeel.P2P;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

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
            _requestModels.Add(new RequestModelObject()
            {
                FileName = Settings.Default.File1Name,
                FileSize = Settings.Default.File1Size,
                IpAddress = Settings.Default.File1IpAddress,
                Port = Settings.Default.File1Port
            });
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

        #endregion EventHandler

        #region OverridedMethods



        #endregion OverridedMethods

        private void btnRequest_Click(object sender, RoutedEventArgs e)
        {
            Button? b = sender as Button;
            if (b?.Tag is RequestModelObject requestModel && IPAddress.TryParse(requestModel.IpAddress, out IPAddress? iPAddress))
            {
                _p2PMasterClass.CreateNewClient(new ClientBussinesLogic2(iPAddress, requestModel.Port, this, requestModel.FileName, requestModel.FileSize));
            }
        }
    }
}
