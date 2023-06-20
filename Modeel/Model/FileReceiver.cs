using Modeel.Log;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Modeel.Model
{
    public class FileReceiver
    {

        #region Properties

        public string FileName => _fileName;
        public long FileSize => _fileSize;
        public int PartSize => _partSize;

        #endregion Properties

        #region PublicFields



        #endregion PublicFields

        #region PrivateFields

        private readonly object _lockObject = new object();
        private readonly FilePartState[] _receivedParts;
        private readonly long _totalParts; // Celkový počet častí suboru
        private readonly string _fileName;
        private readonly long _fileSize;
        private readonly int _partSize;

        #endregion PrivateFields

        #region ProtectedFields



        #endregion ProtectedFields

        #region Ctor

        public FileReceiver(long fileSize, int partSize, string fileName)
        {
            _partSize = partSize;
            _fileSize = fileSize;
            _fileName = fileName;
            _totalParts = fileSize / partSize + ((fileSize % partSize) > 0 ? 1 : 0); ;
            _receivedParts = new FilePartState[_totalParts];
        }

        #endregion Ctor

        #region PublicMethods



        #endregion PublicMethods

        #region PrivateMethods

        private int AssignmentOfFilePart()
        {
            lock (_lockObject)
            {
                for (int i = 0; i < _totalParts; i++)
                {
                    if (_receivedParts[i] == FilePartState.WAITING_FOR_ASSIGNMENT)
                    {
                        _receivedParts[i] = FilePartState.DOWNLOADING;
                        return i;
                    }
                }

                return -1; // Všetky časti sú už prijaté
            }
        }

        private void WriteToFile(int partToProcess, byte[] filePart)
        {
            int position;
            lock (_lockObject)
            {
                position = GetPositionToWrite(partToProcess);

                using (FileStream fileStream = new FileStream(_fileName, FileMode.OpenOrCreate, FileAccess.Write))
                {
                    fileStream.Position = position * _partSize;
                    fileStream.Write(filePart, 0, filePart.Length);
                }

                _receivedParts[partToProcess] = FilePartState.DOWNLOADED;
            }
            Logger.WriteLog($"Part No.{partToProcess} was wrote at position {position}.");
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
