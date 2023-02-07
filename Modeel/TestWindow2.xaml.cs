using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml.Linq;

namespace Modeel
{
   /// <summary>
   /// Interaction logic for TestWindow2.xaml
   /// </summary>
   public partial class TestWindow2 : BaseWindowForWPF
   {
      public TestWindow2()
      {
         InitializeComponent();
         contract.Add(MsgIds.TestMessage, typeof(TestMessage));

         Init();
      }
      internal void Init()
      {
         msgSwitch
          .Case(contract.GetContractId(typeof(TestMessage)), (TestMessage x) => TestMessageHandler(x));
      }

      private void TestMessageHandler(TestMessage message)
      {
         if (WindowState == System.Windows.WindowState.Minimized)
         {
            WindowState = System.Windows.WindowState.Normal;
         }
         Activate();
      }
   }
}