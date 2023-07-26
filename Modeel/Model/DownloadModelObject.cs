using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Modeel.Model.Enums;

namespace Modeel.Model
{
    public class DownloadModelObject
    {
        public List<IUniversalClientSocket> Clients { get; set; } = new List<IUniversalClientSocket>();
        public FileReceiver FileReceiver { get; set; }

        public DownloadModelObject(FileReceiver fileReceiver)
        {
            FileReceiver = fileReceiver;
        }
    }
}
