using Modeel.Log;
using Modeel.SSL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shapes;
using Path = System.IO.Path;

namespace Modeel.Model
{
    public static class ResourceInformer
    {
        private const int _kilobyte = 1024;
        private const int _megabyte = _kilobyte * 1024;
        public const string messageConnector = "||";

        public static int CalculateBufferSize(long fileSize)
        {
            // Determine the available system memory
            long availableMemory = GC.GetTotalMemory(false);

            // Choose a buffer size based on the file size and available memory
            if (fileSize <= availableMemory)
            {
                // If the file size is smaller than available memory, use a buffer size equal to the file size
                return (int)fileSize;
            }
            else
            {
                // Otherwise, choose a buffer size that is a fraction of available memory
                double bufferFraction = 0.1;
                int bufferSize = (int)(availableMemory * bufferFraction);

                // Ensure the buffer size is at least 4KB and at most 1MB
                return Math.Max(4096, Math.Min(bufferSize, 1048576));
            }
        }

        public static string FormatDataTransferRate(long bytesSent)
        {

            string unit;
            double transferRate;

            if (bytesSent < _kilobyte)
            {
                transferRate = bytesSent;
                unit = "B/s";
            }
            else if (bytesSent < _megabyte)
            {
                transferRate = (double)bytesSent / _kilobyte;
                unit = "KB/s";
            }
            else
            {
                transferRate = (double)bytesSent / _megabyte;
                unit = "MB/s";
            }

            return $"{transferRate:F2} {unit}";
        }

        public static void SendFile(string filePath, ISession session)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            // Open the file for reading
            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                // Choose an appropriate buffer size based on the file size and system resources
                int bufferSize = CalculateBufferSize(fileStream.Length);
                Logger.WriteLog($"File buffer chosen for: {bufferSize}", LoggerInfo.socketMessage);
                bufferSize = 8192;

                byte[] buffer = new byte[bufferSize];
                int bytesRead;
                while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    // Send the bytes read from the file over the network stream
                    session.SendAsync(buffer, 0, bytesRead);
                }
            }
            stopwatch.Stop();
            TimeSpan elapsedTime = stopwatch != null ? stopwatch.Elapsed : TimeSpan.Zero;
            Logger.WriteLog($"File transfer completed in {elapsedTime.TotalSeconds} seconds.", LoggerInfo.socketMessage);
            //MessageBox.Show("File transfer completed");
        }

        public static void SendChunk(string filePath, ISession session, long chunkNumber, int chunkSize)
        {
            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                long offset = chunkNumber * chunkSize;
                fileStream.Seek(offset, SeekOrigin.Begin);

                // Read the chunk from the file
                byte[] buffer = new byte[chunkSize];
                int bytesRead = fileStream.Read(buffer, 0, chunkSize);
                session.SendAsync(buffer, 0, bytesRead);
            }
        }

        public static void StartOfSendingFile(string filePath, ISession session, int chunkSize)
        {
            //Generate header
            string fileName = Path.GetFileName(filePath);
            long fileSize = new System.IO.FileInfo(filePath).Length;
            long totalChunkNumbers = fileSize / chunkSize + ((fileSize % chunkSize) > 0 ? 1 : 0);
        }

        public static bool GenerateRequestForFile(string fileName, long fileSize, ISession session)
        {
            byte[] request = GenerateMessage(SocketMessageFlag.FILE_REQUEST, new object[] { fileName, fileSize });
            bool succes = session.SendAsync(request, 0, request.Length);
            if (succes)
            {
                Logger.WriteLog($"Request for file was generated for file: {fileName} with size: {fileSize}", LoggerInfo.socketMessage);
            }
            return succes;
        }

        public static bool GenerateReject(ISession session)
        {
            byte[] request = GenerateMessage(SocketMessageFlag.REJECT);
            bool succes = session.SendAsync(request, 0, request.Length);
            if (succes)
            {
                Logger.WriteLog($"Reject was generated", LoggerInfo.socketMessage);
            }
            return succes;
        }

        public static bool GenerateAccept(ISession session)
        {
            byte[] request = GenerateMessage(SocketMessageFlag.ACCEPT);
            bool succes = session.SendAsync(request, 0, request.Length);
            if (succes)
            {
                Logger.WriteLog($"Accept was generated", LoggerInfo.socketMessage);
            }
            return succes;
        }

        public static bool GenerateRequestForFilePart(int filePart, int partSize, ISession session)
        {
            byte[] request = GenerateMessage(SocketMessageFlag.FILE_PART_REQUEST, new object[] { filePart, partSize });
            bool succes = session.SendAsync(request, 0, request.Length);
            if (succes)
            {
                Logger.WriteLog($"Request for file part was generated for file part No.: {filePart} with size: {partSize}", LoggerInfo.fileTransfering);
            }
            return succes;
        }

        public static byte[] GenerateMessage(SocketMessageFlag flag, object[]? content = null)
        {
            return Encoding.UTF8.GetBytes($"{flag.GetStringValue()}{(content != null ? messageConnector + string.Join(messageConnector, content) : "")}");
        }
    }
}
