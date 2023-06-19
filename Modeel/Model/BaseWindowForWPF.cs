using Modeel.Frq;
using Modeel.Log;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace Modeel.Model
{
    public abstract class BaseWindowForWPF : Window, IWindowEnqueuer
    {
        protected ConcurrentQueue<BaseMsg> _concurrentQueue = new ConcurrentQueue<BaseMsg>();
        protected AutoResetEvent _autoResetEvent = new AutoResetEvent(false);

        protected IContractType contract;
        protected NumberSwitch msgSwitch { get; set; }

        protected Thread _windowWorkerThread;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public BaseWindowForWPF()
        {
            contract = ContractType.GetInstance();
            msgSwitch = new NumberSwitch();
            _windowWorkerThread = new Thread(() => HandleMessage(_cancellationTokenSource.Token));
            _windowWorkerThread.Start();
            Closed += Window_closedEvent;
        }

        private void HandleMessage(CancellationToken cancellationToken)
        {
            Thread.CurrentThread.Name = $"{GetType().Name}_WorkerThread";
            Logger.WriteLog("WorkerThread starting", LoggerInfo.methodEntry);

            while (!cancellationToken.IsCancellationRequested)
            {
                int loopFlag = WaitHandle.WaitAny(new[] { _autoResetEvent, cancellationToken.WaitHandle }, 1000, false);                // TimeOut
                // Check if Cancel has been signalized
                if (loopFlag == 1)
                {
                    while (!_concurrentQueue.IsEmpty)
                    {
                        _concurrentQueue.TryDequeue(out BaseMsg? _);
                    }
                    break;
                }
                else if (loopFlag == WaitHandle.WaitTimeout)
                {
                    // Timeout occurred, continue looping.
                    continue;
                }
                else
                {
                    Action handlingMessageInGuiThread = HandlingMessageInGuiThread;
                    Dispatcher.BeginInvoke(handlingMessageInGuiThread);
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
                Dispatcher.Run();
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
            _cancellationTokenSource.Cancel();
            Logger.WriteLog("Window and his threads closing", LoggerInfo.windowClosed, sender?.GetType().Name);
            _windowWorkerThread.Join();
            Dispatcher.CurrentDispatcher.BeginInvokeShutdown(DispatcherPriority.Background);
        }

        public void BaseMsgEnque(BaseMsg baseMsg)
        {
            Logger.WriteLog("Sending message", LoggerInfo.msgSendLocal, baseMsg.GetType().Name);
            _concurrentQueue.Enqueue(baseMsg);
            _autoResetEvent.Set();
        }

        public bool IsOpen()
        {
            return !_cancellationTokenSource.IsCancellationRequested;
        }
    }
}
