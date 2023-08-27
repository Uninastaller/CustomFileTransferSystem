using Common.Enum;
using Logger;
using System;
using System.IO;
using System.Text;

namespace Modeel.Model
{
    public static class ResourceInformer
    {

        #region Properties



        #endregion Properties

        #region PublicFields

        public const string messageConnector = "||";

        #endregion PublicFields

        #region PrivateFields

        private const int _kilobyte = 0x400;
        private const int _megabyte = 0x100000;

        #endregion PrivateFields

        #region ProtectedFields



        #endregion ProtectedFields

        #region Ctor



        #endregion Ctor

        #region PublicMethods

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

        public static MethodResult GenerateFilePart(string filePath, ISession session, long partNumber, int partSize)
        {
            // FLAG/PART NUMBER/FILE DATA
            byte[] flag = Encoding.UTF8.GetBytes(SocketMessageFlag.FILE_PART.GetStringValue());
            byte[] partNumberBytes = BitConverter.GetBytes(partNumber);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(partNumberBytes);

            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                long offset = partNumber * partSize;
                fileStream.Seek(offset, SeekOrigin.Begin);

                byte[] buffer = new byte[partSize + flag.Length + sizeof(long)];
                int bytesRead = fileStream.Read(buffer, flag.Length + sizeof(long), partSize); // Read the chunk from the file
                System.Buffer.BlockCopy(flag, 0, buffer, 0, flag.Length); // Insert the flag at the start of the buffer
                System.Buffer.BlockCopy(partNumberBytes, 0, buffer, flag.Length, sizeof(long)); // Insert the part number

                bool succes = session.SendAsync(buffer, 0, bytesRead + flag.Length + sizeof(long));
                if (succes)
                {
                    Log.WriteLog(LogLevel.DEBUG, $"Part file: {partNumber}, was sended to client: {session.IpAndPort}!");
                }
                else
                {
                    Log.WriteLog(LogLevel.WARNING, $"Unabled to send part file: {partNumber}, to client: {session.IpAndPort}!");
                }
                return succes ? MethodResult.SUCCES : MethodResult.ERROR;
            }
        }

        public static MethodResult GenerateRequestForFile(string fileName, long fileSize, ISession session)
        {
            byte[] request = GenerateMessage(SocketMessageFlag.FILE_REQUEST, new object[] { fileName, fileSize });
            bool succes = session.SendAsync(request, 0, request.Length);
            if (succes)
            {
                Log.WriteLog(LogLevel.DEBUG, $"Request for file was generated for file: {fileName} with size: {fileSize}, to client: {session.IpAndPort}");
            }
            else
            {
                Log.WriteLog(LogLevel.WARNING, $"Unable to send request for file: {fileName} with size: {fileSize}, to client: {session.IpAndPort}");
            }
            return succes ? MethodResult.SUCCES : MethodResult.ERROR;
        }

        public static MethodResult GenerateReject(ISession session)
        {
            byte[] request = GenerateMessage(SocketMessageFlag.REJECT);
            bool succes = session.SendAsync(request, 0, request.Length);
            if (succes)
            {
                Log.WriteLog(LogLevel.DEBUG, $"Reject was generated to client: {session.IpAndPort}");
            }
            else
            {
                Log.WriteLog(LogLevel.WARNING, $"Unable to send reject to client: {session.IpAndPort}");
            }
            return succes ? MethodResult.SUCCES : MethodResult.ERROR;
        }

        public static MethodResult GenerateAccept(ISession session)
        {
            byte[] request = GenerateMessage(SocketMessageFlag.ACCEPT);
            bool succes = session.SendAsync(request, 0, request.Length);
            if (succes)
            {
                Log.WriteLog(LogLevel.DEBUG, $"Accept was generated to client: {session.IpAndPort}");
            }
            else
            {
                Log.WriteLog(LogLevel.WARNING, $"Unable to send accept to client: {session.IpAndPort}");
            }
            return succes ? MethodResult.SUCCES : MethodResult.ERROR;
        }

        public static MethodResult GenerateRequestForFilePart(long filePart, int partSize, ISession session)
        {
            byte[] request = GenerateMessage(SocketMessageFlag.FILE_PART_REQUEST, new object[] { filePart, partSize });
            bool succes = session.SendAsync(request, 0, request.Length);
            if (succes)
            {
                Log.WriteLog(LogLevel.DEBUG, $"Request for file part was generated for file part No.: {filePart} with size: {partSize}, to client: {session.IpAndPort}");
            }
            else
            {
                Log.WriteLog(LogLevel.WARNING, $"Unable to send request for file part No.: {filePart} with size: {partSize}, to client: {session.IpAndPort}");
            }
            return succes ? MethodResult.SUCCES : MethodResult.ERROR;
        }

        public static byte[] GenerateMessage(SocketMessageFlag flag, object[]? content = null)
        {
            return Encoding.UTF8.GetBytes($"{flag.GetStringValue()}{(content != null ? messageConnector + string.Join(messageConnector, content) : "")}");
        }

        #endregion PublicMethods

        #region PrivateMethods



        #endregion PrivateMethods

        #region ProtectedMethods



        #endregion ProtectedMethods

        #region Events



        #endregion Events

        #region OverridedMethods



        #endregion OverridedMethods

    }
}
