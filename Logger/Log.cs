using ConfigManager;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Logger
{
    public static class Log
    {
        private static readonly string _headerLine = "Time;Line;Filename;Method name;Thread name;Level;Message";
        private static readonly object _lockObect = new object();
        private static readonly int _megaByte = 0x100000;

        private static readonly List<LogEntry> _bufferedLogEntries = new List<LogEntry>();

        private static ConcurrentQueue<LogEntry> _concurrentQueue = new ConcurrentQueue<LogEntry>();
        private static AutoResetEvent _autoResetEvent = new AutoResetEvent(false);
        private static Thread? _workingThread;
        private static CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();


        private static readonly string _configPath = Assembly.GetExecutingAssembly().Location;

        #region Config

        private static bool _useAsynchronousLogging = true;
        private static bool _enableLogging = true;
        private static int _sizeLimitInMB = 10;
        private static string _loggingDirectory = string.Empty;
        private static int _bufferSize = 50;

        #endregion Config

        private static string _logFilePathAndName = string.Empty;

        public static void StartApplication()
        {

            LoadSettingsFromConfig();

            lock (_lockObect)
            {
                ZipExistingLogFile();
                CreateNewLogFile();
            }
            _workingThread = new Thread(() => HandleMessage(_cancellationTokenSource.Token));
            _workingThread.IsBackground = true;
            _workingThread.Start();

            MyConfigManager.ConfigChanged += OnConfigFileChange;
        }

        private static void OnConfigFileChange(object? sender, EventArgs e)
        {
            Log.WriteLog(LogLevel.INFO, "Config Changed");
            LoadSettingsFromConfig();
        }

        private static void LoadSettingsFromConfig()
        {
            if (MyConfigManager.TryGetConfigValue<bool>("UseAsynchronousLogging", out bool useAsynchronousLogging))
            {
                _useAsynchronousLogging = useAsynchronousLogging;
            }

            if (MyConfigManager.TryGetConfigValue<bool>("EnableLogging", out bool enableLogging))
            {
                _enableLogging = enableLogging;
            }

            if (MyConfigManager.TryGetConfigValue<Int32>("SizeLimitInMB", out Int32 sizeLimitInMB))
            {
                _sizeLimitInMB = sizeLimitInMB;
            }

            if (MyConfigManager.TryGetConfigValue<Int32>("BufferSize", out Int32 bufferSize))
            {
                _bufferSize = bufferSize;
            }

            _loggingDirectory = Path.Combine(MyConfigManager.GetConfigValue("LoggingDirectory"), System.AppDomain.CurrentDomain.FriendlyName);
            _logFilePathAndName = Path.Combine(_loggingDirectory, "Active.csv");
        }

        public static void EndApplication()
        {
            MyConfigManager.ConfigChanged -= OnConfigFileChange;
            _cancellationTokenSource.Cancel();
            _workingThread?.Join();
        }

        public static void WriteLog(LogLevel logLevel, string message = "", [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string callingFilePath = "", [CallerMemberName] string callingMethod = "")
        {
            if (_enableLogging)
            {
                if (_useAsynchronousLogging)
                {
                    _concurrentQueue.Enqueue(new LogEntry()
                    {
                        Message = message,
                        LogLevel = logLevel,
                        LineNumber = lineNumber,
                        CallingFilePath = callingFilePath,
                        CallingMethod = callingMethod,
                        ThreadName = Thread.CurrentThread.Name ?? string.Empty,
                        DateTime = DateTime.Now.ToString("HH:mm:ss:fff")
                    }); ;
                    _autoResetEvent.Set();
                }
                else
                {
                    lock (_lockObect)
                    {
                        WriteLogSync(logLevel, message, lineNumber, callingFilePath, callingMethod, Thread.CurrentThread.Name ?? string.Empty, DateTime.Now.ToString("HH:mm:ss:fff"));
                    }
                }
            }
        }

        static void HandleMessage(CancellationToken cancellationToken)
        {
            Thread.CurrentThread.Name = $"{nameof(Log)}_WorkerThread";

            while (!cancellationToken.IsCancellationRequested)
            {
                int loopFlag = WaitHandle.WaitAny(new[] { _autoResetEvent, cancellationToken.WaitHandle }, 1000, false);                // TimeOut
                                                                                                                                        // Check if Cancel has been signalized
                if (loopFlag == 1)
                {
                    // Empty buffer before exiting
                    FlushBuffer();

                    // Empty queue
                    while (!_concurrentQueue.IsEmpty)
                    {
                        _concurrentQueue.TryDequeue(out LogEntry? _);
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
                    if (_concurrentQueue.Count > 0)
                    {

                        while (_concurrentQueue.TryDequeue(out LogEntry? logEntry))
                        {
                            // Buffer the log entry
                            _bufferedLogEntries.Add(logEntry);

                            // If buffer is full, flush it to disk
                            if (_bufferedLogEntries.Count >= _bufferSize)
                            {
                                FlushBuffer();
                            }
                        }
                    }
                }
            }
        }

        private static void CreateNewLogFile()
        {
            try
            {
                if (!Directory.Exists(_loggingDirectory))
                {
                    Directory.CreateDirectory(_loggingDirectory);
                }

                using (StreamWriter writer = new StreamWriter(_logFilePathAndName, false)) // Pass 'false' to create a new file and overwrite if it already exists
                {
                    writer.WriteLine(_headerLine);
                }

            }
            catch (Exception ex)
            {
                // Handle the exception by logging the error message or taking appropriate action.
                // For example, you could log to a separate error log or silently ignore the error.
            }
        }

        private static void ZipExistingLogFile()
        {
            try
            {
                if (File.Exists(_logFilePathAndName))
                {
                    string zipFileName = Path.Combine(_loggingDirectory, string.Format("log_{0:yyyy-MM-dd_HH_mm}.zip", DateTime.Now));
                    using (var zip = new ZipArchive(File.Create(zipFileName), ZipArchiveMode.Create))
                    {
                        var entry = zip.CreateEntry("Active.csv");
                        using (var stream = entry.Open())
                        using (var file = File.OpenRead(_logFilePathAndName))
                        {
                            file.CopyTo(stream);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle the exception by logging the error message or taking appropriate action.
                // For example, you could log to a separate error log or silently ignore the error.
            }
        }

        private static void WriteLogSync(LogLevel logLevel, string message, int lineNumber, string callingFilePath, string callingMethod, string threadName, string dateTime)
        {

            if (!File.Exists(_logFilePathAndName))
            {
                CreateNewLogFile();
            }

            using (StreamWriter writer = new StreamWriter(_logFilePathAndName, true))
            {
                var line = string.Format("{0};{1};{2};{3};{4};{5};{6}",
                    dateTime,
                    lineNumber,
                    Path.GetFileName(callingFilePath),
                    callingMethod,
                    threadName,
                    logLevel.ToString(),
                    message);
                writer.WriteLine(line);
            }

            if (new FileInfo(_logFilePathAndName).Length > _megaByte * _sizeLimitInMB)
            {
                try
                {
                    string zipFileName = Path.Combine(_loggingDirectory, string.Format("log_{0:yyyy-MM-dd_HH_mm}.zip", DateTime.Now));
                    using (var zip = new ZipArchive(File.Create(zipFileName), ZipArchiveMode.Create))
                    {
                        var entry = zip.CreateEntry(Path.GetFileName(_logFilePathAndName));
                        using (var stream = entry.Open())
                        using (var file = File.OpenRead(_logFilePathAndName))
                        {
                            file.CopyTo(stream);
                        }
                    }
                    File.Delete(_logFilePathAndName);
                }
                catch (Exception ex)
                {
                    // Handle the exception by logging the error message or taking appropriate action.
                    // For example, you could log to a separate error log or silently ignore the error.
                }
            }
        }

        private static void FlushBuffer()
        {
            lock (_lockObect)
            {
                if (_bufferedLogEntries.Count == 0)
                    return;

                if (!File.Exists(_logFilePathAndName))
                {
                    CreateNewLogFile();
                }

                using (StreamWriter writer = new StreamWriter(_logFilePathAndName, true))
                {
                    foreach (var logEntry in _bufferedLogEntries)
                    {
                        var line = string.Format("{0};{1};{2};{3};{4};{5};{6}",
                            logEntry.DateTime,
                            logEntry.LineNumber,
                            Path.GetFileName(logEntry.CallingFilePath),
                            logEntry.CallingMethod,
                            logEntry.ThreadName,
                            logEntry.LogLevel.ToString(),
                            logEntry.Message);
                        writer.WriteLine(line);
                    }
                }

                _bufferedLogEntries.Clear();

                if (new FileInfo(_logFilePathAndName).Length > _megaByte * _sizeLimitInMB)
                {
                    try
                    {
                        string zipFileName = Path.Combine(_loggingDirectory, string.Format("log_{0:yyyy-MM-dd_HH_mm}.zip", DateTime.Now));
                        using (var zip = new ZipArchive(File.Create(zipFileName), ZipArchiveMode.Create))
                        {
                            var entry = zip.CreateEntry(Path.GetFileName(_logFilePathAndName));
                            using (var stream = entry.Open())
                            using (var file = File.OpenRead(_logFilePathAndName))
                            {
                                file.CopyTo(stream);
                            }
                        }
                        File.Delete(_logFilePathAndName);
                    }
                    catch (Exception ex)
                    {
                        // Handle the exception by logging the error message or taking appropriate action.
                        // For example, you could log to a separate error log or silently ignore the error.
                    }
                }
            }
        }
    }
}
