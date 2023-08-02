using Modeel.Model.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Modeel.Model
{
    public static class DownloadingStatusFileController
    {
        #region Properties



        #endregion Properties

        #region PublicFields



        #endregion PublicFields

        #region PrivateFields



        #endregion PrivateFields

        #region ProtectedFields



        #endregion ProtectedFields

        #region Ctor



        #endregion Ctor

        #region PublicMethods

        public static void NewPartDownloaded(string fileNameDownloadingStatus, long partNumber)
        {
            using (FileStream fileStream = new FileStream(fileNameDownloadingStatus, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
            {
            }
        }

        public static void InitialSaveOfStatusFile(string fileNameDownloadingStatus, DownloadStatus downloadingStatus)
            => InitialSaveOfStatusFile(fileNameDownloadingStatus, downloadingStatus.ReceivedParts, downloadingStatus.TotalParts, downloadingStatus.FileSize, downloadingStatus.PartSize, downloadingStatus.LastPartSize);

        public static void InitialSaveOfStatusFile(string fileNameDownloadingStatus, FilePartState[] receivedParts, long totalParts, long fileSize, long partSize, long lastPartSize)
        {
            string serializedData = $"{string.Join(";", receivedParts.Select(e => (int)e))}\n{totalParts}\n{fileSize}\n{partSize}\n{lastPartSize}";
            File.WriteAllText(fileNameDownloadingStatus, serializedData);
        }

        // Helper method to load and deserialize the object from the file
        public static DownloadStatus LoadStatusFile(string fileNameDownloadingStatus)
        {
            if (!File.Exists(fileNameDownloadingStatus))
            {
                Console.WriteLine("File does not exist.");
                return null;
            }

            string[] lines = File.ReadAllLines(fileNameDownloadingStatus);
            if (lines.Length < 5)
            {
                Console.WriteLine("Invalid file format.");
                return null;
            }

            FilePartState[] receivedParts = lines[0].Split(';').Select(value => (FilePartState)int.Parse(value)).ToArray();
            long totalParts = long.Parse(lines[1]);
            long fileSize = long.Parse(lines[2]);
            long partSize = long.Parse(lines[3]);
            long lastPartSize = long.Parse(lines[4]);

            return new DownloadStatus
            {
                ReceivedParts = receivedParts,
                TotalParts = totalParts,
                FileSize = fileSize,
                PartSize = partSize,
                LastPartSize = lastPartSize,
            };
        }

        public static bool CheckForValidStatusFile(string fileNameDownloadingStatus)
        {
            if (File.Exists(fileNameDownloadingStatus) && !(File.ReadAllLines(fileNameDownloadingStatus).Length < 5))
            {
                return true;
            }
            return false;
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

    public class DownloadStatus
    {
        public FilePartState[] ReceivedParts { get; set; }
        public long TotalParts { get; set; }
        public long FileSize { get; set; }
        public long PartSize { get; set; }
        public long LastPartSize { get; set; }
    }
}
