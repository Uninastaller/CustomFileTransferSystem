using Common.Enum;
using Common.Model;
using Common.ThreadMessages;
using ConfigManager;
using Logger;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace Client.Windows
{
   /// <summary>
   /// Interaction logic for CreateBlockRequestWindow.xaml
   /// </summary>
   public partial class OfferingFileSettingsWindow : BaseWindowForWPF
   {
      #region PublicFields

      #endregion PublicFields

      #region Ctor

      public OfferingFileSettingsWindow(OfferingFileDto offeringFileDto) : this()
      {
         SetWindow(offeringFileDto);
      }

      public OfferingFileSettingsWindow()
      {
         InitializeComponent();
         Init();

         dtgEndpoints.ItemsSource = new List<EndpointDisplay>();

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

      private void btnSave_Click(object sender, RoutedEventArgs e)
      {
         if (sender is Button button)
         {
            button.IsEnabled = false;
            Log.WriteLog(LogLevel.DEBUG, button.Name);

            if (!long.TryParse(tbFileSize.Text, out long size))
            {
               ShowTimedMessageAndEnableUI("Invalid size!", TimeSpan.FromSeconds(2), button);
               return;
            }

            OfferingFileDto offeringFileDto = new OfferingFileDto();
            foreach (EndpointDisplay display in dtgEndpoints.ItemsSource)
            {
               if (!int.TryParse(display.Port, out _))
               {
                  ShowTimedMessageAndEnableUI($"Invalid port: {display.Port}!", TimeSpan.FromSeconds(2), button);
                  return;
               }
               string key = $"{display.IPAddress}:{display.Port}";
               if (!offeringFileDto.EndpointsAndProperties.ContainsKey(key))
               {
                  offeringFileDto.EndpointsAndProperties.Add(key, new EndpointProperties
                  {
                     Grade = 0,
                     TypeOfServerSocket = display.SocketType
                  });
               }
            }
            offeringFileDto.FileName = tbFileName.Text;
            offeringFileDto.FileSize = size;

            // Save him for later
            string fileName = offeringFileDto.OfferingFileIdentificator + MainWindow.cftsFileExtensions;
            string fileDirectory = Path.Combine(MyConfigManager.GetConfigStringValue("DownloadingDirectory"), MainWindow.cftsDirectoryName);

            if (!Directory.Exists(fileDirectory))
            {
               Directory.CreateDirectory(fileDirectory);
            }
            File.WriteAllText(Path.Combine(fileDirectory, fileName), offeringFileDto.GetJson());
            ShowTimedMessageAndEnableUI("File Identificator Saved!", TimeSpan.FromSeconds(2), button);
         }
      }

      #endregion Events

      #region PrivateMethods

      private void SetWindow(OfferingFileDto o)
      {
         tbFileName.Text = o.FileName;
         tbFileSize.Text = o.FileSize.ToString();

         dtgEndpoints.ItemsSource = o.EndpointsAndProperties.Select(kvp =>
         {
            var parts = kvp.Key.Split(':');
            return new EndpointDisplay
            {
               IPAddress = parts.Length > 0 ? parts[0] : string.Empty,
               Port = parts.Length > 1 ? parts[1] : string.Empty,
               SocketType = kvp.Value.TypeOfServerSocket
            };
         }).ToList();
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
          .Case(contract.GetContractId(typeof(StartNewDownloadMessage)), (StartNewDownloadMessage x) => SetWindow(x.OfferingFileDto))
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

      private class EndpointDisplay
      {
         public string IPAddress { get; set; }
         public string Port { get; set; }
         public TypeOfServerSocket SocketType { get; set; }
      }

   }
}
