using Modeel.Frq;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace Modeel
{
    public class BaseWindowForWPF : Window, IWindowEnqueuer
    {
        protected ConcurrentQueue<BaseMsg> _concurrentQueue = new ConcurrentQueue<BaseMsg>();
        protected AutoResetEvent _autoResetEvent = new AutoResetEvent(false);

        protected IContractType contract;
        protected NumberSwitch msgSwitch { get; set; }

        protected Thread _windowWorkerThread;
        protected bool _loopFlag = true;

        public BaseWindowForWPF()
        {
            contract = ContractType.GetInstance();
            msgSwitch = new NumberSwitch();
            _windowWorkerThread = new Thread(HandleMessage);
            _windowWorkerThread.Start();
            Closed += Window_closedEvent;
        }

        private void HandleMessage()
        {
            Thread.CurrentThread.Name = $"{this.GetType().Name}_WorkerThread";
            Logger.WriteLog("WorkerThread starting", LoggerInfo.methodEntry);

            while (true)
            {
                bool isSignaled = _autoResetEvent.WaitOne(1000);
                // TimeOut
                // Check if Cancel has been pressed
                if (!_loopFlag)
                {
                    while (!_concurrentQueue.IsEmpty)
                    {
                        _concurrentQueue.TryDequeue(out BaseMsg? baseMsgFromQuee);
                    }
                    break;
                }
                if (isSignaled)
                {
                    Action handlingMessageInGuiThread = HandlingMessageInGuiThread;
                    this.Dispatcher.Invoke(handlingMessageInGuiThread);
                }
                // Check if Cancel has been pressed
                else if (!_loopFlag)
                {
                    break;
                }
            }
            Logger.WriteLog("WorkerThread closing", LoggerInfo.methodExit);
        }

        private void HandlingMessageInGuiThread()
        {
            while (!_concurrentQueue.IsEmpty)
            {
                if (_concurrentQueue.TryDequeue(out BaseMsg? baseMsgFromQueue))
                {
                    Logger.WriteLog("New message received", LoggerInfo.msgReceivLocal, baseMsgFromQueue.GetType().Name);
                    // calling of registered method for ai
                    msgSwitch.Switch(baseMsgFromQueue.ai, baseMsgFromQueue);
                }
            }
        }

        public static IWindowEnqueuer? CreateWindow<T>() where T : BaseWindowForWPF, new()
        {
            T? window = null;

            //http://reedcopsey.com/2011/11/28/launching-a-wpf-window-in-a-separate-thread-part-1/
            Thread newWindowThread = new Thread(new ThreadStart(() =>
            {
                // Create our context, and install it:
                SynchronizationContext.SetSynchronizationContext(
                 new DispatcherSynchronizationContext(
                     Dispatcher.CurrentDispatcher));

                window = new();
                window.Title = Thread.CurrentThread.Name = $"{typeof(T).Name}";
                window.Show();

                // Start the Dispatcher Processing
                System.Windows.Threading.Dispatcher.Run();
            }));

            newWindowThread.SetApartmentState(ApartmentState.STA);
            // Make the thread a background thread
            newWindowThread.IsBackground = true;
            // Start the thread
            newWindowThread.Start();

            while (window == null)
            {
                Thread.Sleep(50);
            }
            Logger.WriteLog("New window created", LoggerInfo.windowCreated, typeof(T).Name);
            return window;
        }

        private void Window_closedEvent(object? sender, EventArgs e)
        {
            Closed -= Window_closedEvent;
            _loopFlag = false;
            Logger.WriteLog("Window and his threads closing", LoggerInfo.windowClosed, sender?.GetType().Name);
            System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvokeShutdown(DispatcherPriority.Background);
        }

        public void BaseMsgEnque(BaseMsg baseMsg)
        {
            Logger.WriteLog("Sending message", LoggerInfo.msgSendLocal, baseMsg.GetType().Name);
            _concurrentQueue.Enqueue(baseMsg);
            _autoResetEvent.Set();
        }

        public bool IsOpen()
        {
            if (!_loopFlag)
            {
                return _loopFlag;
            }
            return _loopFlag;
        }
    }
}
