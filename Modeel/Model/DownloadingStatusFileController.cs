using Common.Enum;
using Logger;
using System.IO;
using System.Linq;

namespace Modeel.Model
{
    public static class DownloadingStatusFileController
    {
        #region Properties



        #endregion Properties

        #region PublicFields



        #endregion PublicFields

        #region PrivateFields

        private const byte _charZeroPositionInAscci = 48;
        private const byte _valueOfDownloadedPart = ((byte)FilePartState.DOWNLOADED) + _charZeroPositionInAscci;

        #endregion PrivateFields

        #region ProtectedFields



        #endregion ProtectedFields

        #region Ctor



        #endregion Ctor

        #region PublicMethods

        //public static void NewPartDownloaded(string fileNameDownloadingStatus, long partNumber, FilePartState filePartState = FilePartState.DOWNLOADED)
        //{
        //    // Part numbers starting with part 0

        //    byte newValue = (byte)filePartState;
        //    newValue += 48;

        //    using (FileStream fileStream = new FileStream(fileNameDownloadingStatus, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
        //    {
        //        fileStream.Seek((2 * partNumber), SeekOrigin.Begin);
        //        fileStream.WriteByte(newValue);
        //    }
        //}

        public static void NewPartDownloaded(string fileNameDownloadingStatus, long partNumber)
        {
            // Part numbers starting with part 0

            using (FileStream fileStream = new FileStream(fileNameDownloadingStatus, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
            {
                fileStream.Seek((2 * partNumber), SeekOrigin.Begin);
                fileStream.WriteByte(_valueOfDownloadedPart);
            }
        }

        public static void InitialSaveOfStatusFile(string fileNameDownloadingStatus, DownloadStatus downloadingStatus)
            => InitialSaveOfStatusFile(fileNameDownloadingStatus, downloadingStatus.ReceivedParts, downloadingStatus.TotalParts, downloadingStatus.FileSize, downloadingStatus.PartSize, downloadingStatus.LastPartSize);

        public static void InitialSaveOfStatusFile(string fileNameDownloadingStatus, FilePartState[] receivedParts, long totalParts, long fileSize, int partSize, int lastPartSize)
        {
            string serializedData = $"{string.Join(";", receivedParts.Select(e => (int)e))}\n{totalParts}\n{fileSize}\n{partSize}\n{lastPartSize}";
            File.WriteAllText(fileNameDownloadingStatus, serializedData);
        }

        // Helper method to load and deserialize the object from the file
        public static DownloadStatus LoadStatusFile(string fileNameDownloadingStatus)
        {
            if (!File.Exists(fileNameDownloadingStatus))
            {
                Log.WriteLog(LogLevel.ERROR, "File does not exist.");
                return null;
            }

            string[] lines = File.ReadAllLines(fileNameDownloadingStatus);
            if (lines.Length < 5)
            {
                Log.WriteLog(LogLevel.ERROR, "Invalid file format.");
                return null;
            }

            FilePartState[] receivedParts = lines[0].Split(';').Select(value => (FilePartState)int.Parse(value)).ToArray();
            long totalParts = long.Parse(lines[1]);
            long fileSize = long.Parse(lines[2]);
            int partSize = int.Parse(lines[3]);
            int lastPartSize = int.Parse(lines[4]);

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

        public static void DownloadDone(string fileNameDownloadingStatus)
        {
            if (File.Exists(fileNameDownloadingStatus))
            {
                File.Delete(fileNameDownloadingStatus);
            }
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
        public int PartSize { get; set; }
        public int LastPartSize { get; set; }
    }
}
