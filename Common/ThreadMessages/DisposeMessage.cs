using Common.Enum;
using Common.Model;
using System;
using System.Net;

namespace Common.ThreadMessages
{
   public class DisposeMessage : MsgBase<DisposeMessage>
   {
      public DisposeMessage(Guid sessionGuid, TypeOfSocket typeOfSocket, TypeOfSession typeOfSession, bool isPurposeFullfilled, EndPoint endPoint) : base(typeof(DisposeMessage))
      {
         SessionGuid = sessionGuid;
         TypeOfSocket = typeOfSocket;
         TypeOfSession = typeOfSession;
         IsPurposeFullfilled = isPurposeFullfilled;
         EndPoint = endPoint;
      }

      public DisposeMessage() : base(typeof(DisposeMessage))
      {

      }

      public Guid SessionGuid { get; set; }
      public TypeOfSocket TypeOfSocket { get; set; }
      public TypeOfSession TypeOfSession { get; set; }
      public bool IsPurposeFullfilled { get; set; }
      public EndPoint? EndPoint { get; set; }

   }
}
