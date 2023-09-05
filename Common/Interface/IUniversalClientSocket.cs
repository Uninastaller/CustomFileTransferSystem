using Common.Enum;
using System;
using System.Net;

namespace Common.Interface
{
   public interface IUniversalClientSocket
   {
      public Guid Id { get; }
      public TypeOfClientSocket Type { get; }
      EndPoint Endpoint { get; }
      int Port { get; }
      string Address { get; }
      long BytesSent { get; }
      long BytesReceived { get; }
      string TransferSendRateFormatedAsText { get; }
      long TransferSendRate { get; }
      string TransferReceiveRateFormatedAsText { get; }
      long TransferReceiveRate { get; }
      int OptionReceiveBufferSize { get; set; } // = 8192;
      int OptionSendBufferSize { get; set; } // = 8192;
      bool IsDisposed { get; }
      public bool Disconnect();
      public void DisconnectAndStop();
      public bool ConnectAsync();
      public void Dispose();
      public long Send(byte[] buffer);
   }
}
