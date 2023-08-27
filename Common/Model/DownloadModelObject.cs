using Common.Interface;
using System.Collections.Generic;
using System.Linq;

namespace Common.Model
{
    public class DownloadModelObject
    {
        public List<IUniversalClientSocket> Clients { get; set; } = new List<IUniversalClientSocket>();
        public FileReceiver FileReceiver { get; set; }
        public string TransferReceiveRateFormatedAsText => ResourceInformer.FormatDataTransferRate(Clients.Sum(client => client.TransferReceiveRate));

        public DownloadModelObject(FileReceiver fileReceiver)
        {
            FileReceiver = fileReceiver;
        }
    }
}
