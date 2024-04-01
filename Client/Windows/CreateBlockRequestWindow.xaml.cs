using Common.Enum;
using Common.Model;
using Common.ThreadMessages;
using ConfigManager;
using Logger;
using SslTcpSession.BlockChain;
using System;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace Client.Windows
{
    /// <summary>
    /// Interaction logic for CreateBlockRequestWindow.xaml
    /// </summary>
    public partial class CreateBlockRequestWindow : BaseWindowForWPF
    {
        #region PrivatFields

        private readonly Block _requestingBlock = new Block();
        private TransactionType? _selectedTransactionType = null;

        #endregion PrivatFields

        #region Ctor

        public CreateBlockRequestWindow()
        {
            InitializeComponent();
            Init();

            if (MyConfigManager.TryGetBoolConfigValue("EnableDynamicGradients", out bool enableDynamicGradients) && enableDynamicGradients)
            {
                WindowDesignSet();
            }

            tbSuccessMessage.Visibility = Visibility.Collapsed;

            SetWindow();
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

        private void cbTransaction_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (sender is ComboBox cb)
            {
                Log.WriteLog(LogLevel.DEBUG, cb.Name);

                int selectedIndex = cb.SelectedIndex;
                if (selectedIndex == -1)
                {
                    return;
                }

                _selectedTransactionType = (TransactionType)selectedIndex;
                SetWindow();
            }
        }

        private async void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                button.IsEnabled = false;
                Log.WriteLog(LogLevel.DEBUG, button.Name);

                BlockValidationResult result;

                switch (_selectedTransactionType)
                {
                    case TransactionType.ADD_FILE:
                        break;
                    case TransactionType.ADD_FILE_REQUEST:

                        if (!File.Exists(tbChooseFile.Text))
                        {
                            ShowTimedMessageAndEnableUI("Chosen file do not exist!", TimeSpan.FromSeconds(3), button);
                            return;
                        }

                        result = await Blockchain.Add_AddFileRequest(tbChooseFile.Text);
                        if (result == BlockValidationResult.VALID)
                        {
                            button.Visibility = Visibility.Collapsed;
                        }
                        ShowTimedMessageAndEnableUI(result.ToString(), TimeSpan.FromSeconds(10), button);

                        break;
                    case TransactionType.REMOVE_FILE:
                        break;
                    case TransactionType.REMOVE_FILE_REQUEST:
                        break;
                    case TransactionType.ADD_CREDIT:

                        if (!double.TryParse(tbCreditToAdd.Text, out double creditToAdd))
                        {
                            ShowTimedMessageAndEnableUI("Invalid credit to add", TimeSpan.FromSeconds(3), button);
                            return;
                        }
                        result = await Blockchain.Add_AddCredit(creditToAdd);
                        if (result == BlockValidationResult.VALID)
                        {
                            button.Visibility = Visibility.Collapsed;
                        }
                        ShowTimedMessageAndEnableUI(result.ToString(), TimeSpan.FromSeconds(10), button);
                        return;

                    case null:
                        ShowTimedMessageAndEnableUI("Select transaction!", TimeSpan.FromSeconds(3), button);
                        break;
                }
            }
        }

        private void DropArea_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void DropArea_Drop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files == null || files.Length != 1)
            {
                return;
            }

            tbChooseFile.Text = files[0];
            tblPriceOfRequest.Text = $"Price of request: {Blockchain.CalculatePriceOfFile(tbChooseFile.Text, out _)}";
        }
        private void btnChooseFile_Click(object sender, RoutedEventArgs e)
        {
            var fileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "All files (*.*)|*.*"
            };
            bool? result = fileDialog.ShowDialog();

            if (result == true)
            {
                tbChooseFile.Text = fileDialog.FileName;

                tblPriceOfRequest.Text = $"Price of request: {Blockchain.CalculatePriceOfFile(tbChooseFile.Text, out _)}";
            }
        }

        #endregion Events

        #region PrivateMethods

        private void SetWindow()
        {
            switch (_selectedTransactionType)
            {
                case TransactionType.ADD_FILE:
                    tbCreditToAdd.Visibility = Visibility.Collapsed;
                    tblCreditToAdd.Visibility = Visibility.Collapsed;

                    tblChooseFile.Visibility = Visibility.Collapsed;
                    gdChooseFile.Visibility = Visibility.Collapsed;
                    tbChooseFile.Visibility = Visibility.Collapsed;
                    btnChooseFile.Visibility = Visibility.Collapsed;
                    tblPriceOfRequest.Visibility = Visibility.Collapsed;
                    break;
                case TransactionType.ADD_FILE_REQUEST:
                    tbCreditToAdd.Visibility = Visibility.Collapsed;
                    tblCreditToAdd.Visibility = Visibility.Collapsed;

                    tblChooseFile.Visibility = Visibility.Visible;
                    gdChooseFile.Visibility = Visibility.Visible;
                    tbChooseFile.Visibility = Visibility.Visible;
                    btnChooseFile.Visibility = Visibility.Visible;
                    tblPriceOfRequest.Visibility = Visibility.Visible;
                    break;
                case TransactionType.REMOVE_FILE:
                    tbCreditToAdd.Visibility = Visibility.Collapsed;
                    tblCreditToAdd.Visibility = Visibility.Collapsed;

                    tblChooseFile.Visibility = Visibility.Collapsed;
                    gdChooseFile.Visibility = Visibility.Collapsed;
                    tbChooseFile.Visibility = Visibility.Collapsed;
                    btnChooseFile.Visibility = Visibility.Collapsed;
                    tblPriceOfRequest.Visibility = Visibility.Collapsed;
                    break;
                case TransactionType.REMOVE_FILE_REQUEST:
                    tbCreditToAdd.Visibility = Visibility.Collapsed;
                    tblCreditToAdd.Visibility = Visibility.Collapsed;

                    tblChooseFile.Visibility = Visibility.Collapsed;
                    gdChooseFile.Visibility = Visibility.Collapsed;
                    tbChooseFile.Visibility = Visibility.Collapsed;
                    btnChooseFile.Visibility = Visibility.Collapsed;
                    tblPriceOfRequest.Visibility = Visibility.Collapsed;
                    break;
                case TransactionType.ADD_CREDIT:
                    tbCreditToAdd.Visibility = Visibility.Visible;
                    tblCreditToAdd.Visibility = Visibility.Visible;

                    tblChooseFile.Visibility = Visibility.Collapsed;
                    gdChooseFile.Visibility = Visibility.Collapsed;
                    tbChooseFile.Visibility = Visibility.Collapsed;
                    btnChooseFile.Visibility = Visibility.Collapsed;
                    tblPriceOfRequest.Visibility = Visibility.Collapsed;
                    break;
                case null:
                    tbCreditToAdd.Visibility = Visibility.Collapsed;
                    tblCreditToAdd.Visibility = Visibility.Collapsed;

                    tblChooseFile.Visibility = Visibility.Collapsed;
                    gdChooseFile.Visibility = Visibility.Collapsed;
                    tbChooseFile.Visibility = Visibility.Collapsed;
                    btnChooseFile.Visibility = Visibility.Collapsed;
                    tblPriceOfRequest.Visibility = Visibility.Collapsed;
                    break;
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
             ;

            cbTransaction.ItemsSource = Enum.GetValues(typeof(TransactionType));
        }

        private void WindowStateSetMessageHandler(WindowStateSetMessage message)
        {
            if (WindowState == System.Windows.WindowState.Minimized)
            {
                WindowState = System.Windows.WindowState.Normal;
            }
            Activate();
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


                // Enable the component
                componentToEnable.IsEnabled = true;
            };

            timer.Tick += tickEventHandler;

            // Start the timer
            timer.Start();
        }

        #endregion PrivateMethods

    }
}
