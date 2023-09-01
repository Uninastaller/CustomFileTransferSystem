using Common.Interface;
using ConfigManager;
using Logger;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace Common.Model
{
    public static class ResourceInformer
    {

        #region Properties



        #endregion Properties

        #region PublicFields

        public const char offeringFilesJoint = '_';

        #endregion PublicFields

        #region PrivateFields

        private const int _kilobyte = 0x400;
        private const int _megabyte = 0x100000;
        private const string _cftsDirectoryName = "CFTS";
        private const string _cftsFileExtensions = ".cfts";

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

        public static void LoadCftsJsonsAndSendToSession(string uploadingDirectoryPath, ISession session)
        {
            string cftsFilesDirectory = Path.Combine(uploadingDirectoryPath, _cftsDirectoryName);
            if (Directory.Exists(cftsFilesDirectory))
            {
                string[] files = Directory.GetFiles(cftsFilesDirectory);

                foreach (string filePath in files)
                {
                    if (Path.GetExtension(filePath).Equals(_cftsFileExtensions))
                    {
                        // Read content of file
                        string jsonString = File.ReadAllText(filePath);
                        Log.WriteLog(LogLevel.INFO, $"Reading content of file: {filePath}, content: {jsonString}");
                        // Validate if conten is valid json
                        try
                        {
                            // Attempt to parse the JSON string
                            OfferingFileDto? offeringFileDto = OfferingFileDto.ToObjectFromJson(jsonString);

                            // If parsing succeeds, the JSON is valid
                            Log.WriteLog(LogLevel.INFO, "Content is valid");
                            FlagMessagesGenerator.GenerateOfferingFile(jsonString, session);
                        }
                        catch (JsonException ex)
                        {
                            // Parsing failed, so the JSON is not valid
                            Log.WriteLog(LogLevel.WARNING, "Content is invalid! " + ex.Message);
                        }
                    }
                }
            }
        }

        public static void OnUploadFileRequest(IPAddress ipAddress, int port, ISession session) => OnUploadFileRequest(ipAddress.ToString(), port, session);

        public static void OnUploadFileRequest(string ipAddress, int port, ISession session)
        {
            string UploadingDirectoryPath = MyConfigManager.GetConfigValue("UploadingDirectory");
            CreateJsonFiles(ipAddress, port, UploadingDirectoryPath);
            LoadCftsJsonsAndSendToSession(UploadingDirectoryPath, session);
        }

        public static void CreateJsonFiles(IPAddress ipAddress, int port, string uploadingDirectoryPath) => CreateJsonFiles(ipAddress.ToString(), port, uploadingDirectoryPath);

        public static void CreateJsonFiles(string ipAddress, int port, string uploadingDirectoryPath)
        {
            string directoryPathToCftsDirectory = Path.Combine(uploadingDirectoryPath, _cftsDirectoryName);
            if (!Directory.Exists(directoryPathToCftsDirectory))
            {
                Directory.CreateDirectory(directoryPathToCftsDirectory);
            }

            string[] files = Directory.GetFiles(uploadingDirectoryPath);

            foreach (string filePath in files)
            {
                FileInfo fileInfo = new FileInfo(filePath);
                CreateJsonFile(ipAddress, port, fileInfo);
            }
            Log.WriteLog(LogLevel.INFO, "Created .ctfs files in director: " + directoryPathToCftsDirectory);
        }

        public static void CreateJsonFile(string ipAddress, int port, FileInfo fileInfo)
        {
            var offeringFileDto = new OfferingFileDto($"{ipAddress}:{port}")
            {
                FileName = fileInfo.Name,
                FileSize = fileInfo.Length,
            };

            string json = offeringFileDto.GetJson();
            string jsonFileName = $"{Path.GetFileNameWithoutExtension(fileInfo.Name)}{offeringFilesJoint}{fileInfo.Length}{_cftsFileExtensions}";
            string jsonFilePath = Path.Combine(fileInfo.DirectoryName, _cftsDirectoryName, jsonFileName);

            File.WriteAllText(jsonFilePath, json);
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
