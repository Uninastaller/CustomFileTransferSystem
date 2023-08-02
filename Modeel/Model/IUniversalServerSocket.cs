using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Modeel.Model.Enums;

namespace Modeel.Model
{
    public interface IUniversalServerSocket
    {
        public Guid Id { get; }
        public TypeOfServerSocket Type { get; }
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
        string TransferSendRateFormatedAsText { get; }
        long TransferSendRate { get; }
        string TransferReceiveRateFormatedAsText { get; }
        long TransferReceiveRate { get; }
        public bool Stop();
        public void Dispose();
        public bool Restart();
        public bool Start();
    }
}
