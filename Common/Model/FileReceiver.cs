using Common.Enum;
using Common.Interface;
using Logger;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
//using System.Threading.Tasks;

namespace Common.Model
{
    public class FileReceiver
    {

        #region Properties

        public string FileName => _fileName;
        public long FileSize => _fileSize;
        public int PartSize => _partSize;
        public long TotalParts => _totalParts;
        public bool NoPartsForAsignmentLeft
        {
            get
            {
                return _noPartsForAsignmentLeft;
            }
            set
            {
                if (value && AllPartsAreDownloaded)
                {
                    OnDownloadDone();
                }
                _noPartsForAsignmentLeft = value;
            }
        }
        public long NumberOfDownloadedParts { get; private set; } = 0;
        //public float PercentageDownload => (NumberOfDownloadedParts / (float)TotalParts) * 100;
        public float PercentageDownload => _downloadingTime.Elapsed.Seconds*4;
        public int LastPartSize => _lastPartSize;
        public string DownloadingTime
        {
            get
            {
                TimeSpan elapsedTime = _downloadingTime.Elapsed;
                return $"{elapsedTime.Hours:D2}:{elapsedTime.Minutes:D2}:{elapsedTime.Seconds:D2}";
            }
        }

        public bool AllPartsAreDownloaded => NumberOfDownloadedParts == TotalParts;

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
        private readonly int _partSize;
        private readonly int _lastPartSize;
        private bool _noPartsForAsignmentLeft = false;

        private Stopwatch _downloadingTime = new Stopwatch();

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
        public FileReceiver(FilePartState[] receivedParts, long totalParts, long fileSize, int partSize, int lastPartSize, string fileName)
        {
            _receivedParts = receivedParts.Select(part => part == FilePartState.DOWNLOADING ? FilePartState.WAITING_FOR_ASSIGNMENT : part).ToArray();
            _totalParts = totalParts;
            _fileName = fileName;
            _fileSize = fileSize;
            _partSize = partSize;
            _lastPartSize = lastPartSize;
            NumberOfDownloadedParts = _receivedParts.Where(part => part == FilePartState.DOWNLOADED).Count();
            _fileNameDownloading = Path.ChangeExtension(_fileName, ".tmp");
            _fileNameDownloadingStatus = Path.ChangeExtension(_fileName, DownloadingStatusFileController.downloadingStatusFileControllerFileExtension);

            Log.WriteLog(LogLevel.DEBUG, $"Resuming downloading, number of downloaded parts is: {NumberOfDownloadedParts}!");
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
            _fileNameDownloading = _fileName + ".tmp";
            _fileNameDownloadingStatus = _fileName + DownloadingStatusFileController.downloadingStatusFileControllerFileExtension;
            _totalParts = CalculateTotalPartsCount(fileSize, partSize);
            _receivedParts = new FilePartState[_totalParts];
            _lastPartSize = CalculateLastPartSize(fileSize, partSize);

            Log.WriteLog(LogLevel.DEBUG, "Starting downloading from beginning!");

            DownloadingStatusFileController.InitialSaveOfStatusFile(_fileNameDownloadingStatus, _receivedParts, TotalParts, FileSize, PartSize, LastPartSize);
        }

        #endregion Ctor

        #region PublicMethods

        /// <summary>
        /// Look if there is Downloading status file, we need to know if we starting download from beggining or continuing 
        /// </summary>
        /// <param name="downloadingFile"></param>
        /// <param name="fileSize"></param>
        /// <returns></returns>
        public static FileReceiver GetFileReceiver(string downloadingFile, long fileSize)
        {
            string downloadingStatusFile = downloadingFile + DownloadingStatusFileController.downloadingStatusFileControllerFileExtension;

            if (DownloadingStatusFileController.CheckForValidStatusFile(downloadingStatusFile))
            {
                DownloadStatus downloadStatus = DownloadingStatusFileController.LoadStatusFile(downloadingStatusFile);

                if (downloadStatus.FileSize == fileSize)
                {
                    if (FileReceiver.CalculateTotalPartsCount(downloadStatus.FileSize, (int)downloadStatus.PartSize) == downloadStatus.TotalParts)
                    {
                        if (FileReceiver.CalculateLastPartSize(downloadStatus.FileSize, (int)downloadStatus.PartSize) == downloadStatus.LastPartSize)
                        {
                            return new FileReceiver(downloadStatus.ReceivedParts, downloadStatus.TotalParts, downloadStatus.FileSize, downloadStatus.PartSize, downloadStatus.LastPartSize, downloadingFile);
                        }
                        else
                        {
                            Log.WriteLog(LogLevel.WARNING, "Saved Status File has different last part size, starting downloading from beginning");
                        }
                    }
                    else
                    {
                        Log.WriteLog(LogLevel.WARNING, "Saved Status File has different total file part count, starting downloading from beginning");
                    }
                }
                else
                {
                    Log.WriteLog(LogLevel.WARNING, "Saved Status File has different file size, starting downloading from beginning");
                }
            }
            else
            {
                Log.WriteLog(LogLevel.WARNING, "Saved Status File dont exist, or is Invalid, starting downloading from beginning");
            }
            int megabyte = 0x100000;
            int filePartSize = megabyte;
            return new FileReceiver(fileSize, filePartSize, downloadingFile);
        }

        public static long CalculateTotalPartsCount(long fileSize, int partSize)
        {
            return fileSize / partSize + ((fileSize % partSize) > 0 ? 1 : 0);
        }

        public static int CalculateLastPartSize(long fileSize, int partSize)
        {
            return (int)(fileSize % partSize != 0 ? fileSize % partSize : partSize);
        }

        public void ReAssignFilePart(long filePartNumber)
        {
            // No need to locking object, becouse we are just seting it to waiting for assignment
            if (_receivedParts[filePartNumber] == FilePartState.DOWNLOADING)
                _receivedParts[filePartNumber] = FilePartState.WAITING_FOR_ASSIGNMENT;
        }

        public MethodResult GenerateRequestForFilePart(ISession session, long filePart)
        {
            return FlagMessagesGenerator.GenerateRequestForFilePart(filePart, _partSize, session);
        }

        public MethodResult WriteToFile(long partToProcess, byte[] filePart, int offset, int length)
        {
            if (_receivedParts[partToProcess] != FilePartState.DOWNLOADING)
            {
                Log.WriteLog(LogLevel.WARNING, "We are not waiting this part!");
                return MethodResult.SUCCES;
            }

            long position = partToProcess * _partSize;
            lock (_lockObject)
            {
                //position = GetPositionToWrite(partToProcess);

                int maxRetries = 4;
                int retryDelayMs = 100;

                for (int retryCount = 0; retryCount < maxRetries; retryCount++)
                {
                    try
                    {
                        using (FileStream fileStream = new FileStream(_fileNameDownloading, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
                        {
                            fileStream.Position = position;
                            fileStream.Write(filePart, offset, length);
                        }

                        _receivedParts[partToProcess] = FilePartState.DOWNLOADED;
                        NumberOfDownloadedParts++;


                        DownloadingStatusFileController.NewPartDownloaded(_fileNameDownloadingStatus, partToProcess);

                        if (NoPartsForAsignmentLeft && AllPartsAreDownloaded)
                        {
                            OnDownloadDone();
                        }

                        Log.WriteLog(LogLevel.DEBUG, $"Part No.{partToProcess} was written at position {position}.");
                        return MethodResult.SUCCES;
                    }
                    catch (IOException)
                    {
                        if (retryCount < maxRetries - 1)
                        {
                            Log.WriteLog(LogLevel.WARNING, $"Failed to write Part No.{partToProcess}. Retrying in {retryDelayMs}ms...");
                            Thread.Sleep(retryDelayMs);
                        }
                        else
                        {
                            Log.WriteLog(LogLevel.WARNING, $"Failed to write Part No.{partToProcess}. Max retries exceeded.");
                        }
                    }
                }
            }
            _receivedParts[partToProcess] = FilePartState.WAITING_FOR_ASSIGNMENT;
            return MethodResult.ERROR;
        }

        public long AssignmentOfFilePart()
        {
            lock (_lockObject)
            {
                for (long i = 0; i < _totalParts; i++)
                {
                    if (_receivedParts[i] == FilePartState.WAITING_FOR_ASSIGNMENT)
                    {
                        _receivedParts[i] = FilePartState.DOWNLOADING;
                        return i;
                    }
                }

                NoPartsForAsignmentLeft = true;
                return -1; // There are no part that are waiting for asignment
            }
        }

        public void PauseTimer()
        {
            _downloadingTime.Stop();
        }

        public void StartTimer()
        {
            _downloadingTime.Start();
        }

        #endregion PublicMethods

        #region PrivateMethods

        private void OnDownloadDone()
        {
            Log.WriteLog(LogLevel.DEBUG, "File downloading done!");
            // Change extension of file to default
            if (File.Exists(_fileNameDownloading) && !File.Exists(_fileName))
            {
                Thread.Sleep(100);
                Log.WriteLog(LogLevel.DEBUG, $"Changeing extension of downloading file from .tmp to: {Path.GetExtension(_fileName)}!");
                File.Move(_fileNameDownloading, _fileName);
            }
            DownloadingStatusFileController.DownloadDone(_fileNameDownloadingStatus);

            _downloadingTime.Stop();
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
