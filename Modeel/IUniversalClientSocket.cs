using Modeel.SSL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modeel
{
    public interface IUniversalClientSocket
    {
        public Guid Id { get; }
        public TypeOfSocket Type { get; }
        public bool Disconnect();
        public void DisconnectAndStop();
        public bool ConnectAsync();
        public void Dispose();
        public long Send(byte[] buffer);
    }
}
