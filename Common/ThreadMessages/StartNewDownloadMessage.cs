using Common.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.ThreadMessages
{
   public class StartNewDownloadMessage : MsgBase<StartNewDownloadMessage>
   {
      public StartNewDownloadMessage(OfferingFileDto offeringFileDto) : base(typeof(StartNewDownloadMessage))
      {
         OfferingFileDto = offeringFileDto;
      }

      public OfferingFileDto OfferingFileDto { get; set; }
   }
}