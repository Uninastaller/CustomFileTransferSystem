using System;
using System.Collections.ObjectModel; // Required for ObservableCollection
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Timers;
using Common.Interface;
using Common.ThreadMessages;

namespace Common.Model
{
   public class DownloadModelObject : INotifyPropertyChanged, IDisposable
   {
      public ObservableCollection<IUniversalClientSocket> Clients { get; }
      public FileReceiver FileReceiver { get; }
      public string FileIndentificator { get; }
      public bool IsDownloading
      {
         get => _isDownloading;
         set
         {
            if (_isDownloading != value)
            {
               if (value)
               {
                  FileReceiver.StartTimer();
                  StartRefresher();
               }
               else
               {
                  FileReceiver.PauseTimer();
                  CleanupRefresher();
                  foreach (var client in Clients)
                  {
                     client.Dispose();
                  }
               }
               _isDownloading = value;
               OnPropertyChanged(nameof(IsDownloading));
            }
         }
      }

      private bool _isDownloading;

      public string TransferReceiveRateFormatedAsText
          => ResourceInformer.FormatDataTransferRate(Clients.Sum(client => client.TransferReceiveRate));

      public DownloadModelObject(FileReceiver fileReceiver, string fileIndentificator)
      {
         FileReceiver = fileReceiver;
         FileIndentificator = fileIndentificator;
         Clients = new ObservableCollection<IUniversalClientSocket>();
      }





















      private Timer? _timer;
      public event PropertyChangedEventHandler? PropertyChanged;
      private void StartRefresher()
      {
         // Should not happend that he will exist before we create new one, but to be safe, if exist, unsubscribe event
         CleanupRefresher();

         _timer = new Timer(1000); // Set the interval to 1 second
         _timer.Elapsed += Timer_elapsed;
         _timer.Start();
      }
      private void CleanupRefresher()
      {
         // Unsubscribe from the Elapsed event
         if (_timer != null)
         {
            _timer.Elapsed -= Timer_elapsed;
            // Stop and dispose of the timer
            _timer?.Stop();
            _timer?.Dispose();
            _timer = null;
         }         
      }
      private void OnPropertyChanged(string propertyName)
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
      }
      private void Timer_elapsed(object? sender, ElapsedEventArgs e)
      {
         OnPropertyChanged(nameof(TransferReceiveRateFormatedAsText));
         OnPropertyChanged(nameof(FileReceiver));
      }
      public void Dispose()
      {
         CleanupRefresher();
         foreach (var client in Clients)
         {
            client.Dispose();
         }
      }

      ~DownloadModelObject()
      {
         // Finalizer
         Dispose();
      }
   }
}
