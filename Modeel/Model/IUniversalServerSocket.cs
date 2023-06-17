using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modeel.Model
{
    public interface IUniversalServerSocket
    {
        public Guid Id { get; }
        public TypeOfSocket Type { get; }
        long ConnectedSessions { get; }
        bool IsAccepting { get; }
        bool IsStarted { get; }
        int Port { get; }
        string Address { get; }
        long BytesSent { get; }
        long BytesReceived { get; }
        int OptionAcceptorBacklog { get; set; } // = 1024;
        int OptionReceiveBufferSize { get; set; } // = 8192;
        int OptionSendBufferSize { get; set; } // = 8192;
        string TransferRateFormatedAsText { get; }

        public bool Stop();
        public void Dispose();
        public bool Restart();
        public bool Start();
    }
}
