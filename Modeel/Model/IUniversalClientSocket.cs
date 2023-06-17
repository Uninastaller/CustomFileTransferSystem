using Modeel.SSL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modeel.Model
{
    public interface IUniversalClientSocket
    {
        public Guid Id { get; }
        public TypeOfSocket Type { get; }
        int Port { get; }
        string Address { get; }
        long BytesSent { get; }
        long BytesReceived { get; }
        string TransferRateFormatedAsText { get; }
        public bool Disconnect();
        public void DisconnectAndStop();
        public bool ConnectAsync();
        public void Dispose();
        public long Send(byte[] buffer);
    }
}
