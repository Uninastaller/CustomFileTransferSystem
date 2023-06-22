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

        public static void SendFilePart(string filePath, ISession session, long partNumber, int partSize)
        {
            byte[] flag = Encoding.UTF8.GetBytes(SocketMessageFlag.FILE_PART.GetStringValue());
            byte[] partNumberBytes = BitConverter.GetBytes(partNumber);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(partNumberBytes);

            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                long offset = partNumber * partSize;
                fileStream.Seek(offset, SeekOrigin.Begin);

                byte[] buffer = new byte[partSize + flag.Length + sizeof(int)];
                int bytesRead = fileStream.Read(buffer, flag.Length + sizeof(int), partSize); // Read the chunk from the file
                System.Buffer.BlockCopy(flag, 0, buffer, 0, flag.Length); // Insert the flag at the start of the buffer
                System.Buffer.BlockCopy(partNumberBytes, 0, buffer, flag.Length, sizeof(int)); // Insert the part number
                if(session.SendAsync(buffer, 0, bytesRead + flag.Length + sizeof(int)))
                {
                    Logger.WriteLog($"Part file: {partNumber}, was sended to client: {session.IpAndPort}!", LoggerInfo.socketMessage);
                }
                else
                {
                    Logger.WriteLog($"Unabled to send part file: {partNumber}, to client: {session.IpAndPort}!", LoggerInfo.warning);
                }
            }
        }

        public static bool GenerateRequestForFile(string fileName, long fileSize, ISession session)
        {
            byte[] request = GenerateMessage(SocketMessageFlag.FILE_REQUEST, new object[] { fileName, fileSize });
            bool succes = session.SendAsync(request, 0, request.Length);
            if (succes)
            {
                Logger.WriteLog($"Request for file was generated for file: {fileName} with size: {fileSize}, to client: {session.IpAndPort}", LoggerInfo.socketMessage);
            }
            else
            {
                Logger.WriteLog($"Unable to send request for file: {fileName} with size: {fileSize}, to client: {session.IpAndPort}", LoggerInfo.warning);
            }
            return succes;
        }

        public static bool GenerateReject(ISession session)
        {
            byte[] request = GenerateMessage(SocketMessageFlag.REJECT);
            bool succes = session.SendAsync(request, 0, request.Length);
            if (succes)
            {
                Logger.WriteLog($"Reject was generated to client: {session.IpAndPort}", LoggerInfo.socketMessage);
            }
            else
            {
                Logger.WriteLog($"Unable to send reject to client: {session.IpAndPort}", LoggerInfo.warning);
            }
            return succes;
        }

        public static bool GenerateAccept(ISession session)
        {
            byte[] request = GenerateMessage(SocketMessageFlag.ACCEPT);
            bool succes = session.SendAsync(request, 0, request.Length);
            if (succes)
            {
                Logger.WriteLog($"Accept was generated to client: {session.IpAndPort}", LoggerInfo.socketMessage);
            }
            else
            {
                Logger.WriteLog($"Unable to send accept to client: {session.IpAndPort}", LoggerInfo.warning);
            }
            return succes;
        }

        public static MethodResults GenerateRequestForFilePart(int filePart, long partSize, ISession session)
        {
            byte[] request = GenerateMessage(SocketMessageFlag.FILE_PART_REQUEST, new object[] { filePart, partSize });
            bool succes = session.SendAsync(request, 0, request.Length);
            if (succes)
            {
                Logger.WriteLog($"Request for file part was generated for file part No.: {filePart} with size: {partSize}, to client: {session.IpAndPort}", LoggerInfo.fileTransfering);
            }
            else
            {
                Logger.WriteLog($"Unable to send request for file part No.: {filePart} with size: {partSize}, to client: {session.IpAndPort}", LoggerInfo.warning);
            }
            return succes ? MethodResults.SUCCES : MethodResults.ERROR;
        }

        public static byte[] GenerateMessage(SocketMessageFlag flag, object[]? content = null)
        {
            return Encoding.UTF8.GetBytes($"{flag.GetStringValue()}{(content != null ? messageConnector + string.Join(messageConnector, content) : "")}");
        }
    }
}
