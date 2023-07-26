using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using Modeel.Model.Enums;

namespace Modeel.Model
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
