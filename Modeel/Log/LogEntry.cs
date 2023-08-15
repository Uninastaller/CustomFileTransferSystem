using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;

namespace Modeel.Log
{
    public class LogEntry
    {
        public string Message { get; set; }
        public LogLevel LogLevel { get; set; }
        public int LineNumber { get; set; }
        public string CallingFilePath { get; set; }
        public string CallingMethod { get; set; }
        public string ThreadName { get; set; }
        public string DateTime { get; set; }
    }
}
