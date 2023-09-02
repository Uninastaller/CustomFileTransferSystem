using Common.Model;
using Common.ThreadMessages;
using ConfigManager;
using System;
using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

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

            if (MyConfigManager.TryGetBoolConfigValue("EnableDynamicGradients", out bool enableDynamicGradients) && enableDynamicGradients)
            {
                WindowDesignSet();
            }

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
