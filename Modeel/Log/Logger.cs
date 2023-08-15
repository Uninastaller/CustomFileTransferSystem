using Modeel.Frq;
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
      private static readonly int sizeLimit = 1048576 * 1; // 1 MB
      private static readonly string headerLine = "Time;Line;Filename;Thread;Method name;Message info;Message";
      private static readonly string logDirectory = @"C:\Logs";
      private static readonly object lockObject = new object();

      public static void StartApplication()
      {
         ZipExistingLogFile();
         CreateNewLogFile();
      }

      private static void CreateNewLogFile()
      {
         try
         {
            if (!Directory.Exists(logDirectory))
            {
               Directory.CreateDirectory(logDirectory);
            }

            string fileName = Path.Combine(logDirectory, "Active.csv");

            lock (lockObject)
            {
               using (StreamWriter writer = new StreamWriter(fileName, false)) // Pass 'false' to create a new file and overwrite if it already exists
               {
                  writer.WriteLine(headerLine);
               }
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
            string existingFileName = Path.Combine(logDirectory, "Active.csv");

            if (File.Exists(existingFileName))
            {
               lock (lockObject)
               {
                  string zipFileName = Path.Combine(logDirectory, string.Format("log_{0:yyyy-MM-dd_HH_mm}.zip", DateTime.Now));
                  using (var zip = new ZipArchive(File.Create(zipFileName), ZipArchiveMode.Create))
                  {
                     var entry = zip.CreateEntry("Active.csv");
                     using (var stream = entry.Open())
                     using (var file = File.OpenRead(existingFileName))
                     {
                        file.CopyTo(stream);
                     }
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

      public static void WriteLog(string message = "", string loggerInfo = "", string? msgName = "", [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string callingFilePath = "", [CallerMemberName] string callingMethod = "")
      {
         string fileName = Path.Combine(logDirectory, "Active.csv");

         if (!File.Exists(fileName))
         {
            CreateNewLogFile();
         }

         lock (lockObject)
         {
            using (StreamWriter writer = new StreamWriter(fileName, true))
            {
               var line = string.Format("{0:HH:mm:ss:fff};{1};{2};{3};{4};{5};{6}",
                   DateTime.Now,
                   lineNumber,
                   Path.GetFileName(callingFilePath),
                   Thread.CurrentThread.Name,
                   callingMethod,
                   loggerInfo + msgName,
                   message);
               writer.WriteLine(line);
            }
         }

         if (File.Exists(fileName) && new FileInfo(fileName).Length > sizeLimit)
         {
            try
            {
               string zipFileName = Path.Combine(logDirectory, string.Format("log_{0:yyyy-MM-dd_HH_mm}.zip", DateTime.Now));
               using (var zip = new ZipArchive(File.Create(zipFileName), ZipArchiveMode.Create))
               {
                  var entry = zip.CreateEntry(Path.GetFileName(fileName));
                  using (var stream = entry.Open())
                  using (var file = File.OpenRead(fileName))
                  {
                     file.CopyTo(stream);
                  }
               }
               File.Delete(fileName);
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
