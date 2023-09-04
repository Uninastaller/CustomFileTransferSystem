using Common.Model;
using System.Collections.Generic;

namespace Common.ThreadMessages
{
    public class OfferingFilesReceivedMessage : MsgBase<OfferingFilesReceivedMessage>
    {
        public OfferingFilesReceivedMessage(List<OfferingFileDto> offeringFiles) : base(typeof(OfferingFilesReceivedMessage))
        {
            OfferingFiles = offeringFiles;
        }

        public List<OfferingFileDto> OfferingFiles { get; set; }
    }
}
