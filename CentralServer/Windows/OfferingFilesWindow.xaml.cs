using Common.Model;
using Common.ThreadMessages;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;

namespace CentralServer.Windows
{
    /// <summary>
    /// Interaction logic for OfferingFilesWindow.xaml
    /// </summary>
    public partial class OfferingFilesWindow : BaseWindowForWPF
    {
        #region Properties



        #endregion Properties

        #region PublicFields



        #endregion PublicFields

        #region PrivateFields



        #endregion PrivateFields

        #region ProtectedFields



        #endregion ProtectedFields

        #region Ctor

        public OfferingFilesWindow()
        {
            InitializeComponent();
            contract.Add(MsgIds.WindowStateSetMessage, typeof(WindowStateSetMessage));

            _ = RefreshDataAsync(); // Initialize with the method

            Init();
        }

        #endregion Ctor

        #region PublicMethods



        #endregion PublicMethods

        #region PrivateMethods

        private void Init()
        {
            msgSwitch
             .Case(contract.GetContractId(typeof(WindowStateSetMessage)), (WindowStateSetMessage x) => WindowStateSetMessageHandler(x))
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

        // Separated refresh logic into its own async method
        private async Task RefreshDataAsync()
        {
            var offeringFiles = await SqliteDataAccess.GetAllOfferingFilesWithOnlyJsonGradesAsync(); // Await here
            dtgOfferingFiles.ItemsSource = offeringFiles; // No need for explicit casting
        }

        #endregion PrivateMethods

        #region ProtectedMethods



        #endregion ProtectedMethods

        #region Events

        private async void btnRefreshData_Click(object sender, RoutedEventArgs e)
        {
            await RefreshDataAsync(); // Use await here
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

        #endregion Events

        #region OverridedMethods



        #endregion OverridedMethods
        
    }
}
