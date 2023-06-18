using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modeel.Model
{
    public interface ISession
    {
        public bool SendAsync(byte[] buffer, long offset, long size);
    }
}
