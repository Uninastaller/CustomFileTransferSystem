﻿using Common.Interface;
using Logger;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace Common.Model
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
            Log.WriteLog(LogLevel.DEBUG, "WorkerThread starting");

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
            Log.WriteLog(LogLevel.DEBUG, "WorkerThread closing");
        }

        private void HandlingMessageInGuiThread()
        {
            while (!_concurrentQueue.IsEmpty)
            {
                if (_concurrentQueue.TryDequeue(out BaseMsg? baseMsgFromQueue))
                {
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
            Log.WriteLog(LogLevel.DEBUG, $"New window created with type of: {typeof(T).Name}");
            return window;
        }

        public static IWindowEnqueuer? CreateWindow<T>(Func<T> factoryMethod) where T : BaseWindowForWPF
        {
            T? window = null;

            Thread newWindowThread = new Thread(new ThreadStart(() =>
            {
                SynchronizationContext.SetSynchronizationContext(
                    new DispatcherSynchronizationContext(
                        Dispatcher.CurrentDispatcher));

                window = factoryMethod();

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
            Log.WriteLog(LogLevel.DEBUG, $"New window created with type of: {typeof(T).Name}");
            return window;
        }


        private void Window_closedEvent(object? sender, EventArgs e)
        {
            Closed -= Window_closedEvent;
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            Log.WriteLog(LogLevel.DEBUG, $"Window of type: {sender?.GetType().Name} and his threads are closing");
            _windowWorkerThread.Join();
            Dispatcher.CurrentDispatcher.BeginInvokeShutdown(DispatcherPriority.Background);
        }

        public void BaseMsgEnque(BaseMsg baseMsg)
        {
            //Logger.WriteLog("Sending message", LoggerInfo.msgSendLocal, baseMsg.GetType().Name);
            _concurrentQueue.Enqueue(baseMsg);
            _autoResetEvent.Set();
        }

        public bool IsOpen()
        {
            return !_cancellationTokenSource.IsCancellationRequested;
        }
    }
}
