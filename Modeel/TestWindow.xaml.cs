using Modeel.Frq;
using System.Threading;

namespace Modeel
{
   /// <summary>
   /// Interaction logic for TestWindow.xaml
   /// </summary>
   public partial class TestWindow : BaseWindowForWPF
   {
      public TestWindow()
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
