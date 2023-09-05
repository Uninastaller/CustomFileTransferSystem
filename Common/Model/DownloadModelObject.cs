using System.Collections.ObjectModel; // Required for ObservableCollection
using System.Linq;
using Common.Interface;

namespace Common.Model
{
   public class DownloadModelObject
   {
      public ObservableCollection<IUniversalClientSocket> Clients { get; }
      public FileReceiver FileReceiver { get; }
      public string FileIndentificator { get; }

      public string TransferReceiveRateFormatedAsText
          => ResourceInformer.FormatDataTransferRate(Clients.Sum(client => client.TransferReceiveRate));

      public DownloadModelObject(FileReceiver fileReceiver, string fileIndentificator)
      {
         FileReceiver = fileReceiver;
         FileIndentificator = fileIndentificator;
         Clients = new ObservableCollection<IUniversalClientSocket>();
      }
   }
}
