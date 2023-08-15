using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Modeel.Log
{
   public static class Logger
   {
      private static readonly int sizeLimit = 0x100000 * 1; // 1 MB
      private static readonly string _headerLine = "Time;Line;Filename;Thread;Method name;Level;Message";
      private static readonly string _logFilePath = @"C:\Logs";
      private static readonly string _logFilePathAndName = Path.Combine(_logFilePath, "Active.csv");
      private static readonly object _lockObect = new object();

      private static ConcurrentQueue<LogEntry> _concurrentQueue = new ConcurrentQueue<LogEntry>();
      private static AutoResetEvent _autoResetEvent = new AutoResetEvent(false);
      private static Thread _workingThread;
      private static CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

      public static void StartApplication()
      {
         lock (_lockObect)
         {
            ZipExistingLogFile();
            CreateNewLogFile();
         }
         _workingThread = new Thread(() => HandleMessage(_cancellationTokenSource.Token));
         _workingThread.Start();
      }

      public static void EndApplication()
      {
         _cancellationTokenSource.Cancel();
         _workingThread.Join();
      }

      public static void WriteLog(string message, LogLevel logLevel)
      {
         _concurrentQueue.Enqueue(new LogEntry()
         {
            Message = message,
            LogLevel = logLevel
         });
         _autoResetEvent.Set();
      }

      static void HandleMessage(CancellationToken cancellationToken)
      {
         Thread.CurrentThread.Name = $"{nameof(Logger)}_WorkerThread";

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
               if (_concurrentQueue.TryDequeue(out LogEntry? logEntry))
               {

                  lock (_lockObect)
                  {
                     WriteLog_(logEntry.LogLevel, logEntry.Message);
                  }
               }
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

      private static void WriteLog_(LogLevel logLevel, string message = "", [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string callingFilePath = "", [CallerMemberName] string callingMethod = "")
      {

         if (!File.Exists(_logFilePathAndName))
         {
            CreateNewLogFile();
         }

         using (StreamWriter writer = new StreamWriter(_logFilePathAndName, true))
         {
            var line = string.Format("{0:HH:mm:ss:fff};{1};{2};{3};{4};{5};{6}",
                DateTime.Now,
                lineNumber,
                Path.GetFileName(callingFilePath),
                Thread.CurrentThread.Name,
                callingMethod,
                logLevel.ToString(),
                message);
            writer.WriteLine(line);
         }

         if (new FileInfo(_logFilePathAndName).Length > sizeLimit)
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
