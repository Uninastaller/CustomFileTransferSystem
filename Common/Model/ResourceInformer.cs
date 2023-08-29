using Common.Enum;
using Common.Interface;
using Logger;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Xml;

namespace Common.Model
{
    public static class ResourceInformer
    {

        #region Properties



        #endregion Properties

        #region PublicFields

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

        public static void CreateJsonFiles(IPAddress ipAddress, int port, string directoryPath) => CreateJsonFiles(ipAddress.ToString(), port, directoryPath);

        public static void CreateJsonFiles(string ipAddress, int port, string directoryPath)
        {

            string directoryPathToCftsDirectory = Path.Combine(directoryPath, _cftsDirectoryName);
            if (!Directory.Exists(directoryPathToCftsDirectory))
            {
                Directory.CreateDirectory(directoryPathToCftsDirectory);
            }

            string[] files = Directory.GetFiles(directoryPath);

            foreach (string filePath in files)
            {
                FileInfo fileInfo = new FileInfo(filePath);

                // Skip directories
                if ((fileInfo.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
                    continue;

                CreateJsonFile(ipAddress, port, fileInfo);
            }
        }


        #endregion PublicMethods

        #region PrivateMethods

        private static void CreateJsonFile(string ipAddress, int port, FileInfo fileInfo)
        {
            var fileInfoDto = new FileInfoDto
            {
                FileName = fileInfo.Name,
                FileSize = fileInfo.Length,
                IpAddressesAndPorts = new List<string> { $"{ipAddress}:{port}" }
            };

            string json = JsonConvert.SerializeObject(fileInfoDto, Newtonsoft.Json.Formatting.Indented);
            string jsonFileName = $"{Path.GetFileNameWithoutExtension(fileInfo.Name)}_{fileInfo.Length}{_cftsFileExtensions}";
            string jsonFilePath = Path.Combine(fileInfo.DirectoryName, _cftsDirectoryName, jsonFileName);

            File.WriteAllText(jsonFilePath, json);
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
