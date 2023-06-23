using Modeel.Log;
using System.IO;
using System.Threading;

namespace Modeel.Model
{
   public class FileReceiver
   {

      #region Properties

      public string FileName => _fileName;
      public long FileSize => _fileSize;
      public long PartSize => _partSize;

      #endregion Properties

      #region PublicFields



      #endregion PublicFields

      #region PrivateFields

      private readonly object _lockObject = new object();
      private readonly FilePartState[] _receivedParts;
      private readonly long _totalParts; // Celkový počet častí suboru
      private readonly string _fileName;
      private readonly long _fileSize;
      private readonly long _partSize;

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

      public MethodResult GenerateRequestForFilePart(ISession session)
      {
         int filePart = AssignmentOfFilePart();
         if (filePart == -1) return MethodResult.DONE;
         return ResourceInformer.GenerateRequestForFilePart(filePart, _partSize, session);
      }

      public MethodResult WriteToFile(int partToProcess, byte[] filePart, int offset, int length)
      {
         if (_receivedParts[partToProcess] != FilePartState.DOWNLOADING)
         {
            Logger.WriteLog("We are not waiting this part!", LoggerInfo.warning);
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
                  using (FileStream fileStream = new FileStream(_fileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
                  {
                     fileStream.Position = position * _partSize;
                     fileStream.Write(filePart, offset, length);
                  }

                  _receivedParts[partToProcess] = FilePartState.DOWNLOADED;
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
