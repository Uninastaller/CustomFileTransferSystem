﻿using Common.Enum;
using Common.Interface;
using Logger;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Model
{
    public class FlagMessagesGenerator
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

        #region PublicMethod

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
            // Message has 3 parts: FLAG, FILE_NAME, FILE_SIZE
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
            // Message has 3 parts: FLAG, FILE_PART_NUMBER, PART_SIZE
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

        public static MethodResult GenerateOfferingFilesRequest(ISession session)
        {
            byte[] request = GenerateMessage(SocketMessageFlag.OFFERING_FILES_REQUEST);
            bool succes = session.SendAsync(request, 0, request.Length);
            if (succes)
            {
                Log.WriteLog(LogLevel.DEBUG, $"Offering file request was generated to client: {session.IpAndPort}");
            }
            else
            {
                Log.WriteLog(LogLevel.WARNING, $"Unable to send offering file request to client: {session.IpAndPort}");
            }
            return succes ? MethodResult.SUCCES : MethodResult.ERROR;
        }

        public static MethodResult GenerateOfferingFile(string offeringFileDtoJson, ISession session)
        {
            // Message has 2 parts: FLAG, OFFERING_FILE_ON_JSON_FORMAT
            byte[] request = GenerateMessage(SocketMessageFlag.OFFERING_FILE, new object[] { offeringFileDtoJson });
            bool succes = session.SendAsync(request, 0, request.Length);
            if (succes)
            {
                Log.WriteLog(LogLevel.DEBUG, $"Offering file was generated, to central server");
            }
            else
            {
                Log.WriteLog(LogLevel.WARNING, $"Unable to send offering file, to central server");
            }
            return succes ? MethodResult.SUCCES : MethodResult.ERROR;
        }

        #endregion PublicMethods

        #region PrivateMethods

        public static byte[] GenerateMessage(SocketMessageFlag flag, object[]? content = null)
        {
            return Encoding.UTF8.GetBytes($"{flag.GetStringValue()}{(content != null ? messageConnector + string.Join(messageConnector, content) : "")}");
        }

        #endregion PrivateMethods

        #region ProtectedMethods



        #endregion ProtectedMethods

        #region Events



        #endregion Events

        #region OverridedMethods



        #endregion OverridedMethods
    }
}
