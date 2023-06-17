using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;

namespace Modeel.Log
{
    public static class Logger
    {
        private static readonly int sizeLimit = 1048576 * 10; // 1 MB
        private static readonly string headerLine = "Time;Line;Filename;Thread;Method name;Message info;Message";
        private static readonly string logDirectory = @"C:\Logs";
        private static readonly object lockObject = new object();
        private static bool newFile = false;
        private static object zipLock = new object(); // Define a lock object

        public static void WriteLog(string message = "", string loggerInfo = "", string? msgName = "", [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string callingFilePath = "", [CallerMemberName] string callingMethod = "")
        {
            //if (loggerInfo.Equals(LoggerInfo.socketMessage)) return;

            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            string fileName = Path.Combine(logDirectory, "Active.csv");

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

            if (File.Exists(fileName) && new FileInfo(fileName).Length > sizeLimit)
            {
                lock (zipLock) // Use a lock to ensure only one thread at a time can access this code block
                {

                    if (!File.Exists(fileName) || new FileInfo(fileName).Length < sizeLimit) return; // another thread may already created zip file

                    string zipFileName = Path.Combine(logDirectory, string.Format("log_{0:yyyy-MM-dd_HH_mm}.zip", DateTime.Now));

                    DirectoryInfo directoryInfo = new DirectoryInfo(logDirectory);

                    // Check if the user has read access to the directory
                    bool hasReadAccess = (directoryInfo.Attributes & FileAttributes.ReadOnly) != FileAttributes.ReadOnly;

                    // Check if the user has write access to the directory
                    bool hasWriteAccess = (directoryInfo.Attributes & FileAttributes.ReadOnly) == 0;

                    if (!hasReadAccess || !hasWriteAccess)
                    {
                        MessageBox.Show("Can not zip log files becouse of lack of permissions! Logs will be deleted!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        File.Delete(fileName);
                        return;
                    }

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
            }
        }
    }
}
