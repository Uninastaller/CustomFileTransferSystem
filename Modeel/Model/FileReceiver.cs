using Microsoft.Win32;
using Modeel.Log;
using Modeel.Model.Enums;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Modeel.Model
{
    public class FileReceiver
    {

        #region Properties

        public string FileName => _fileName;
        public long FileSize => _fileSize;
        public long PartSize => _partSize;
        public long TotalParts => _totalParts;
        public bool DownloadDone
        {
            get
            {
                return _downloadDone;
            }
            set
            {
                if (value)
                {
                    OnDownloadDone();
                }
                _downloadDone = value;
            }
        }
        public long NumberOfDownloadedParts { get; private set; } = 0;
        public float PercentageDownload => (NumberOfDownloadedParts / (float)TotalParts) * 100;
        public long LastPartSize => _lastPartSize;

        #endregion Properties

        #region PublicFields



        #endregion PublicFields

        #region PrivateFields

        private readonly object _lockObject = new object();
        private readonly FilePartState[] _receivedParts;
        private readonly long _totalParts; // Celkový počet častí suboru
        private readonly string _fileName;
        private readonly string _fileNameDownloading;
        private readonly string _fileNameDownloadingStatus;
        private readonly long _fileSize;
        private readonly long _partSize;
        private readonly long _lastPartSize;
        private bool _downloadDone = false;

        #endregion PrivateFields

        #region ProtectedFields



        #endregion ProtectedFields

        #region Ctor

        /// <summary>
        /// For Resuming Download
        /// </summary>
        /// <param name="receivedParts"></param>
        /// <param name="totalParts"></param>
        /// <param name="fileSize"></param>
        /// <param name="partSize"></param>
        /// <param name="lastPartSize"></param>
        /// <param name="fileName"></param>
        public FileReceiver(FilePartState[] receivedParts, long totalParts, long fileSize, int partSize, long lastPartSize, string fileName)
        {
            _receivedParts = receivedParts.Select(part => part == FilePartState.DOWNLOADING ? FilePartState.WAITING_FOR_ASSIGNMENT : part).ToArray();
            _totalParts = totalParts;
            _fileName = fileName;
            _fileSize = fileSize;
            _partSize = partSize;
            _lastPartSize = lastPartSize;
            NumberOfDownloadedParts = _receivedParts.Where(part => part == FilePartState.DOWNLOADED).Count();
            _fileNameDownloading = Path.ChangeExtension(_fileName, ".tmp");
            _fileNameDownloadingStatus = Path.ChangeExtension(_fileName, ".cfts");

            Logger.WriteLog($"Resuming downloading, number of downloaded parts is: {NumberOfDownloadedParts}!", LoggerInfo.downloadingStatusFile);
        }

        /// <summary>
        /// For Starting Download From Beggining
        /// </summary>
        /// <param name="fileSize"></param>
        /// <param name="partSize"></param>
        /// <param name="fileName"></param>
        public FileReceiver(long fileSize, int partSize, string fileName)
        {
            _partSize = partSize;
            _fileSize = fileSize;
            _fileName = fileName;
            _fileNameDownloading = Path.ChangeExtension(_fileName, ".tmp");
            _fileNameDownloadingStatus = Path.ChangeExtension(_fileName, ".cfts");
            _totalParts = CalculateTotalPartsCount(fileSize, partSize);
            _receivedParts = new FilePartState[_totalParts];
            _lastPartSize = CalculateLastPartSize(fileSize, partSize);

            Logger.WriteLog("Starting downloading from beginning!", LoggerInfo.downloadingStatusFile);

            DownloadingStatusFileController.InitialSaveOfStatusFile(_fileNameDownloadingStatus, _receivedParts, TotalParts, FileSize, PartSize, LastPartSize);
        }

        #endregion Ctor

        #region PublicMethods

        public static long CalculateTotalPartsCount(long fileSize, int partSize)
        {
            return fileSize / partSize + ((fileSize % partSize) > 0 ? 1 : 0);
        }

        public static long CalculateLastPartSize(long fileSize, int partSize)
        {
            return fileSize % partSize != 0 ? fileSize % partSize : partSize;
        }

        public void ReAssignFilePart(long filePartNumber)
        {
            // No need to locking object, becouse we are just seting it to waiting for assignment
            if (_receivedParts[filePartNumber] == FilePartState.DOWNLOADING)
                _receivedParts[filePartNumber] = FilePartState.WAITING_FOR_ASSIGNMENT;
        }

        public MethodResult GenerateRequestForFilePart(ISession session, long filePart)
        {
            return ResourceInformer.GenerateRequestForFilePart(filePart, _partSize, session);
        }

        public MethodResult WriteToFile(int partToProcess, byte[] filePart, int offset, int length)
        {
            if (_receivedParts[partToProcess] != FilePartState.DOWNLOADING)
            {
                Logger.WriteLog("We are not waiting this part!", LoggerInfo.warning);
                return MethodResult.SUCCES;
            }

            int position;
            lock (_lockObject)
            {
                position = GetPositionToWrite(partToProcess);

                int maxRetries = 4;
                int retryDelayMs = 100;

                for (int retryCount = 0; retryCount < maxRetries; retryCount++)
                {
                    try
                    {
                        using (FileStream fileStream = new FileStream(_fileNameDownloading, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
                        {
                            fileStream.Position = position * _partSize;
                            fileStream.Write(filePart, offset, length);
                        }

                        _receivedParts[partToProcess] = FilePartState.DOWNLOADED;
                        NumberOfDownloadedParts++;

                        //DownloadingStatusFileController.Save(_fileNameDownloadingStatus);

                        Logger.WriteLog($"Part No.{partToProcess} was written at position {position}.", LoggerInfo.fileTransfering);
                        return MethodResult.SUCCES;
                    }
                    catch (IOException)
                    {
                        if (retryCount < maxRetries - 1)
                        {
                            Logger.WriteLog($"Failed to write Part No.{partToProcess}. Retrying in {retryDelayMs}ms...", LoggerInfo.warning);
                            Thread.Sleep(retryDelayMs);
                        }
                        else
                        {
                            Logger.WriteLog($"Failed to write Part No.{partToProcess}. Max retries exceeded.", LoggerInfo.warning);
                        }
                    }
                }
            }
            _receivedParts[partToProcess] = FilePartState.WAITING_FOR_ASSIGNMENT;
            return MethodResult.ERROR;
        }

        public int AssignmentOfFilePart()
        {
            lock (_lockObject)
            {
                for (int i = 0; i < _totalParts; i++)
                {
                    if (_receivedParts[i] == FilePartState.WAITING_FOR_ASSIGNMENT)
                    {
                        _receivedParts[i] = FilePartState.DOWNLOADING;
                        _receivedParts.FirstOrDefault(x => x == FilePartState.DOWNLOADED);
                        return i;
                    }
                }

                DownloadDone = true;
                return -1; // Všetky časti sú už prijaté
            }
        }

        #endregion PublicMethods

        #region PrivateMethods

        private void OnDownloadDone()
        {
            Logger.WriteLog("File downloading done!", LoggerInfo.fileTransfering);
            // Change extension of file to default
            if (File.Exists(_fileNameDownloading) && !File.Exists(_fileName))
            {
                Logger.WriteLog($"Changeing extension of downloading file from .tmp to: {Path.GetExtension(_fileName)}!", LoggerInfo.fileTransfering);
                File.Move(_fileNameDownloading, _fileName);
            }
        }

        private int GetPositionToWrite(int partToProcess)
        {
            int position = 0;
            for (int i = 0; i < partToProcess; i++)
            {
                if (_receivedParts[i] == FilePartState.DOWNLOADED)
                {
                    position += 1; // Predchádzajúce časti sú už prijaté
                }
            }

            return position;
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
