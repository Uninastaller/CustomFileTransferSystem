using System;
using System.IO;
using System.Threading;
using System.IO.Compression;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Modeel
{
   public static class Logger
   {
      private static readonly int sizeLimit = 1048576; // 1 MB
      private static readonly string headerLine = "Time;Line;Filename;Thread;Method name;Message info;Message";
      private static readonly string logDirectory = @"C:\Logs";
      private static readonly object lockObject = new object();
      private static bool newFile = false;

      public static void WriteLog(string message = "", string loggerInfo = "", string? msgName = "", [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string callingFilePath = "", [CallerMemberName] string callingMethod = "")
      {
         if (!Directory.Exists(logDirectory))
         {
            Directory.CreateDirectory(logDirectory);
         }

         string fileName = Path.Combine(logDirectory, string.Format("log_{0:yyyy-MM-dd}.csv", DateTime.Now));

         if (!File.Exists(fileName))
         {
            newFile = true;
         }

         lock (lockObject)
         {
            using (StreamWriter writer = new StreamWriter(fileName, true))
            {

               if (newFile)
               {
                  writer.WriteLine(headerLine);
                  newFile = false;
               }

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

         if (new FileInfo(fileName).Length > sizeLimit)
         {
            string zipFileName = Path.Combine(logDirectory, string.Format("log_{0:yyyy-MM-dd}.zip", DateTime.Now));
            ZipFile.CreateFromDirectory(logDirectory, zipFileName);
            File.Delete(fileName);
         }
      }
   }
}
