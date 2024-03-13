using Common.Model;
using System;

namespace Common.ThreadMessages
{
   public class RequestForOfferingFilesMessage : MsgBase<RequestForOfferingFilesMessage>
   {
      public RequestForOfferingFilesMessage() : base(typeof(RequestForOfferingFilesMessage))
      {
      }

      public Guid SessionId { get; set; }

    }
}