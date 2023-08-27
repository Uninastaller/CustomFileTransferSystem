using Common.Interface;
using Common.Model;
using Common.ThreadMessages;
using Logger;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace Modeel
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : BaseWindowForWPF
    {

        //private IWindowEnqueuer? _serverWindow;
        private IWindowEnqueuer? _clientWindow;
        //private MemoryLeakTestFormDefault? _form;

        public MainWindow()
        {
            Thread.CurrentThread.Name = $"{this.GetType().Name}";

            //Logger.WriteLog(LogLevel.Debug, "START OF PROGRAM");

            contract.Add(MsgIds.DisposeMessage, typeof(DisposeMessage));

            InitializeComponent();

            _ = BaseWindowForWPF.CreateWindow<CFTS>();
            //_ = BaseWindowForWPF.CreateWindow<TorOnionInterfaceWindow>();
        }

        //private void btServerWindow_Click(object sender, RoutedEventArgs e)
        //{
        //    //_form = null;
        //    //_clientWindow = null;
        //    //GC.Collect();
        //    //GC.WaitForPendingFinalizers();
        //    //GC.Collect();

        //    if (sender is Button button)
        //    {
        //        Log.WriteLog(LogLevel.DEBUG, button.Name);

        //        OpenServerWindow();
        //    }
        //}

        //private void OpenServerWindow()
        //{
        //    if (_serverWindow == null || !_serverWindow.IsOpen())
        //    {
        //        _serverWindow = BaseWindowForWPF.CreateWindow<ServerWindow>();
        //    }
        //    else
        //    {
        //        _serverWindow.BaseMsgEnque(new WindowStateSetMessage());
        //    }
        //}

        private void btClientWindow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                Log.WriteLog(LogLevel.DEBUG, button.Name);

                OpenClientWindow();
            }
        }

        private void OpenClientWindow()
        {
            //if (_clientWindow == null || !_clientWindow.IsOpen())
            //{
            //    _clientWindow = BaseWindowForWPF.CreateWindow<ClientWindow>();
            //}
            //else
            //{
            //    _clientWindow.BaseMsgEnque(new WindowStateSetMessage());
            //}
            _ = BaseWindowForWPF.CreateWindow<ClientWindow>();
            //_form = new MemoryLeakTestFormDefault();
            //_form.Show();
        }
    }
}
