using Common.Enum;
using Common.Model;
using ConfigManager;
using Logger;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace SslTcpSession
{
   class SslCentralServerSession : SslSession
   {

      #region Properties

      public ServerSessionState ServerSessionState
      {
         get => _serverSessionState;

         set
         {
            if (value != _serverSessionState)
            {
               _serverSessionState = value;
               ServerSessionStateChange?.Invoke(this, value);
            }
         }
      }

      #endregion Properties

      #region PublicFields


      #endregion PublicFields

      #region PrivateFields

      private ServerSessionState _serverSessionState = ServerSessionState.NONE;

      #endregion PrivateFields

      #region ProtectedFields



      #endregion ProtectedFields

      #region Ctor

      public SslCentralServerSession(SslServer server) : base(server)
      {
         Log.WriteLog(LogLevel.INFO, $"Guid: {Id}, Starting");

         _flagSwitch.OnNonRegistered(OnNonRegistredMessage);
         _flagSwitch.Register(SocketMessageFlag.OFFERING_FILE, OnOfferingFileHandler);
      }

      #endregion Ctor

      #region PublicMethods



      #endregion PublicMethods

      #region PrivateMethods

      private void OnClientDisconnected()
      {
         ClientDisconnected?.Invoke(this);
      }

      private async Task OnNewOfferingFileReceived(List<OfferingFileDto?> offeringFileDtos)
      {
         string offeringFilesStorePath = MyConfigManager.GetConfigValue("OfferingFilesStorePath");
         if (!Directory.Exists(offeringFilesStorePath))
         {
            Directory.CreateDirectory(offeringFilesStorePath);
         }

         foreach (OfferingFileDto? offeringFileDto in offeringFileDtos)
         {
            if (offeringFileDto != null)
            {

               string fileName = offeringFileDto.FileName + ResourceInformer.offeringFilesJoint + offeringFileDto.FileSize;
               string offeringFileWithStorePath = Path.Combine(offeringFilesStorePath, fileName);

               if (!File.Exists(offeringFileWithStorePath)) // File is new
               {
                  Log.WriteLog(LogLevel.INFO, $"{offeringFileWithStorePath} is new offering file, saving");
                  await File.WriteAllTextAsync(offeringFileWithStorePath, offeringFileDto.GetJson());
               }
               else // File already exist
               {
                  Log.WriteLog(LogLevel.INFO, $"{offeringFileWithStorePath} if offering file that is already stored");
                  string existingFileContent = await File.ReadAllTextAsync(offeringFileWithStorePath);
                  try
                  {
                     // Attempt to parse the JSON string
                     OfferingFileDto? existingOfferingFileDto = OfferingFileDto.ToObjectFromJson(existingFileContent);

                     if (existingOfferingFileDto != null)
                     {
                        // Merging files
                        existingOfferingFileDto.MergeWithAnotherOfferingFileDto(offeringFileDto);

                        // Saving new file as json
                        await File.WriteAllTextAsync(offeringFileWithStorePath, existingOfferingFileDto.GetJson());
                     }
                  }
                  catch (JsonException ex)
                  {
                     // Parsing failed, so the JSON is not valid
                     Log.WriteLog(LogLevel.WARNING, $"Existing offering file dto is corupted, name: {offeringFileWithStorePath}, will be replaced witch new one. {ex.Message}");
                  }
               }
            }
         }
      }

      #endregion PrivateMethods

      #region ProtectedMethods

      protected async override void OnHandshaked()
      {
         Log.WriteLog(LogLevel.INFO, $"Ssl session with Id {Id} handshaked!");

         int maxRepeatCounter = 3;
         await Task.Delay(100);

         // Staf to do after 200ms => waiting without blocking thread
         while (FlagMessagesGenerator.GenerateOfferingFilesRequest(this) == MethodResult.ERROR && maxRepeatCounter-- >= 0)
         {
            await Task.Delay(200);
         }

         if (maxRepeatCounter < -1)
         {
            Disconnect();
         }
      }

      protected override void OnDisconnected()
      {
         OnClientDisconnected();
         Log.WriteLog(LogLevel.INFO, $"Ssl session with Id {Id} disconnected!");
      }

      protected override void OnReceived(byte[] buffer, long offset, long size)
      {
         _flagSwitch.Switch(buffer, offset, size);
      }

      protected override void OnError(SocketError error)
      {
         Log.WriteLog(LogLevel.ERROR, $"Ssl session caught an error with code {error}");
      }

      #endregion ProtectedMethods

      #region Events

      public delegate void ClientDisconnectedHandler(SslSession sender);
      public event ClientDisconnectedHandler? ClientDisconnected;

      public delegate void ServerSessionStateChangeEventHandler(SslSession sender, ServerSessionState serverSessionState);
      public event ServerSessionStateChangeEventHandler? ServerSessionStateChange;

      private void OnNonRegistredMessage(string message)
      {
         ServerSessionState = ServerSessionState.NONE;
         this.Server?.FindSession(this.Id)?.Disconnect();
         Log.WriteLog(LogLevel.WARNING, $"Warning: Non registered message received, disconnecting client!");
      }

      private async void OnOfferingFileHandler(byte[] buffer, long offset, long size)
      {
         if (FlagMessageEvaluator.EvaluateOfferingFile(buffer, offset, size, out List<OfferingFileDto?> offeringFileDto))
         {
            await OnNewOfferingFileReceived(offeringFileDto);
         }
      }


      #endregion Events

      #region OverridedMethods



      #endregion OverridedMethods

   }
}

