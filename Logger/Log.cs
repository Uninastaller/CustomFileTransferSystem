using System.Collections.Concurrent;
using System.Configuration;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Logger
{
    public static class Log
    {
        private static readonly string _headerLine = "Time;Line;Filename;Method name;Thread name;Level;Message";
        private static readonly string _logFilePath = @"C:\Logs";
        private static readonly string _logFilePathAndName = Path.Combine(_logFilePath, "Active.csv");
        private static readonly object _lockObect = new object();
        private static readonly int _megaByte = 0x100000;

        private static ConcurrentQueue<LogEntry> _concurrentQueue = new ConcurrentQueue<LogEntry>();
        private static AutoResetEvent _autoResetEvent = new AutoResetEvent(false);
        private static Thread? _workingThread;
        private static CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private static FileSystemWatcher? _configWatcher;

        private static readonly string _configPath = Assembly.GetExecutingAssembly().Location;

        #region Config

        private static bool _useAsynchronousLogging = true;
        private static bool _enableLogging = true;
        private static int _sizeLimitInMB = 10;

        #endregion Config

        public static void StartApplication()
        {

            LoadSettingsFromConfig();

            lock (_lockObect)
            {
                ZipExistingLogFile();
                CreateNewLogFile();
            }
            _workingThread = new Thread(() => HandleMessage(_cancellationTokenSource.Token));
            _workingThread.Start();


            string? configDirectory = Path.GetDirectoryName(_configPath);
            if (!string.IsNullOrEmpty(configDirectory))
            {
                _configWatcher = new FileSystemWatcher(configDirectory, Path.GetFileName(_configPath + ".config"));
                _configWatcher.NotifyFilter = NotifyFilters.LastWrite;
                _configWatcher.Changed += OnConfigFileChange;
                _configWatcher.EnableRaisingEvents = true;
            }
        }

        private static void OnConfigFileChange(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType == WatcherChangeTypes.Changed)
            {
                Log.WriteLog(LogLevel.INFO, "Config Changed");

                Thread.Sleep(100);

                LoadSettingsFromConfig();
            }
        }

        private static void LoadSettingsFromConfig()
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(_configPath);
            bool.TryParse(config.AppSettings.Settings["UseAsynchronousLogging"].Value, out _useAsynchronousLogging);
            bool.TryParse(config.AppSettings.Settings["EnableLogging"].Value, out _enableLogging);
            int.TryParse(config.AppSettings.Settings["SizeLimitInMB"].Value, out _sizeLimitInMB);
        }

        public static void EndApplication()
        {
            if (_configWatcher != null)
            {
                _configWatcher.Changed -= OnConfigFileChange;
            }
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
                        WriteLog_(logLevel, message, lineNumber, callingFilePath, callingMethod, Thread.CurrentThread.Name ?? string.Empty, DateTime.Now.ToString("HH:mm:ss:fff"));
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
                        lock (_lockObect)
                        {
                            while (_concurrentQueue.TryDequeue(out LogEntry? logEntry))
                            {

                                WriteLog_(logEntry.LogLevel, logEntry.Message, logEntry.LineNumber, logEntry.CallingFilePath, logEntry.CallingMethod, logEntry.ThreadName, logEntry.DateTime);
                            }
                        }
                    }

                    //if (_concurrentQueue.TryDequeue(out LogEntry? logEntry))
                    //{

                    //    lock (_lockObect)
                    //    {
                    //        WriteLog_(logEntry.LogLevel, logEntry.Message, logEntry.LineNumber, logEntry.CallingFilePath, logEntry.CallingMethod, logEntry.ThreadName, logEntry.DateTime);
                    //    }
                    //}
                }
            }
        }

        private static void CreateNewLogFile()
        {
            try
            {
                if (!Directory.Exists(_logFilePath))
                {
                    Directory.CreateDirectory(_logFilePath);
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
                    string zipFileName = Path.Combine(_logFilePath, string.Format("log_{0:yyyy-MM-dd_HH_mm}.zip", DateTime.Now));
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

        private static void WriteLog_(LogLevel logLevel, string message, int lineNumber, string callingFilePath, string callingMethod, string threadName, string dateTime)
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
                    string zipFileName = Path.Combine(_logFilePath, string.Format("log_{0:yyyy-MM-dd_HH_mm}.zip", DateTime.Now));
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
