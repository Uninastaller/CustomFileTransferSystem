using Modeel.FastTcp;
using Modeel.Frq;
using Modeel.Model;
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

        #endregion PrivateFields

        #region Ctor

        public CFTS()
        {
            InitializeComponent();

            LoadRequestFromConfig();

            dgRequests.ItemsSource = _requestModels;
        }

        #endregion Ctor

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

        #endregion PrivateMethods

        #region ProtectedMethods



        #endregion ProtectedMethods

        #region EventHandler



        #endregion EventHandler

        #region OverridedMethods



        #endregion OverridedMethods

        private void btnRequest_Click(object sender, RoutedEventArgs e)
        {
            Button? b = sender as Button;
            if (b?.Tag is RequestModelObject requestModel && IPAddress.TryParse(requestModel.IpAddress, out IPAddress? iPAddress))
            {
                new ClientBussinesLogic2(iPAddress, requestModel.Port, this, requestModel.FileName, requestModel.FileSize);
            }
        }
    }
}
