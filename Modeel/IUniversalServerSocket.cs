using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modeel
{
    public interface IUniversalServerSocket
    {
        public Guid Id { get; }
        public TypeOfSocket Type { get; }
        public bool Stop();
        public void Dispose();
        public bool Restart();
        public bool Start();
    }
}
