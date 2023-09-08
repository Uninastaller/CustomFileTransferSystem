using Common.Enum;
using System;

namespace Common.Model
{
   public class ServerDownloadingSessionsInfo
   {
      public Guid Id { get; set; }
      public long BytesSent { get; set; }
      public long BytesReceived { get; set; }
      public string FileNameOfAcceptedfileRequest { get; set; } = string.Empty;
      public SessionState SessionState { get; set; }


      public string DataSendSentFormated => ResourceInformer.BytesToFormatedText(BytesSent);
      public string DataSendReceivedFormated => ResourceInformer.BytesToFormatedText(BytesReceived);
   }
}
