using Modeel.Frq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Modeel
{
   internal class TestMessage : MsgBase<TestMessage>
   {
      public TestMessage() : base(typeof(TestMessage))
      {
      }
      public string? TestString { get; set; }
   }
}
