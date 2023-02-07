using Modeel.Frq;
using System.Threading;
using System.Windows;

namespace Modeel
{
   /// <summary>
   /// Interaction logic for MainWindow.xaml
   /// </summary>
   public partial class MainWindow : BaseWindowForWPF
   {

      private IWindowEnqueuer? _testWindow;
      private IWindowEnqueuer? _testWindow2;

      public MainWindow()
      {
         Thread.CurrentThread.Name = $"{this.GetType().Name}";

         Logger.WriteLog("START OF PROGRAM", LoggerInfo.methodEntry);
         InitializeComponent();
      }

      private void MyButton_Click(object sender, RoutedEventArgs e)
      {
         Logger.WriteLog("MyButton_Click", LoggerInfo.methodEntry);
         OpenTestWindow();
      }

      private void OpenTestWindow()
      {
         if (_testWindow == null || !_testWindow.IsOpen())
         {
            _testWindow = BaseWindowForWPF.CreateWindow<TestWindow>();
         }
         else
         {
            _testWindow.BaseMsgEnque(new TestMessage() { TestString = "Test" });
         }
      }

      private void MyButton2_Click(object sender, RoutedEventArgs e)
      {
         Logger.WriteLog("MyButton2_Click", LoggerInfo.methodEntry);
         OpenTestWindow2();
      }

      private void OpenTestWindow2()
      {
         if (_testWindow2 == null || !_testWindow2.IsOpen())
         {
            _testWindow2 = BaseWindowForWPF.CreateWindow<TestWindow2>();
         }
         else
         {
            _testWindow2.BaseMsgEnque(new TestMessage() { TestString = "Test2" });
         }
      }
   }
}
