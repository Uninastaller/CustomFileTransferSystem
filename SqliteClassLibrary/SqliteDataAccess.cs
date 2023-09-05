using Common.Enum;
using Common.Model;
using Dapper;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace CentralServer
{
   public class SqliteDataAccess
   {

      #region Properties



      #endregion Properties

      #region PublicFields



      #endregion PublicFields

      #region PrivateFields

      private static string QueryToFetchAllOfferingFilesDto => @"
            SELECT
                o.OfferingFileIdentificator,
                o.FileName,
                o.FileSize,
                CASE WHEN COUNT(e.Id) > 0 THEN json_group_object(e.Endpoint, json_object('Grade', e.Grade, 'TypeOfServerSocket', e.TypeOfServerSocket)) ELSE NULL END AS EndpointsAndPropertiesJson
            FROM OfferingFiles o
            LEFT JOIN EndpointsAndProperties e ON o.OfferingFileIdentificator = e.OfferingFileId
            GROUP BY o.OfferingFileIdentificator, o.FileName, o.FileSize";

      #endregion PrivateFields

      #region ProtectedFields



      #endregion ProtectedFields

      #region Ctor



      #endregion Ctor

      #region PublicMethods

      public static async Task<List<OfferingFileDto>> GetAllOfferingFilesWithEndpointsAsync()
      {
         var offeringFiles = await ExecuteQueryForOfferingFiles(QueryToFetchAllOfferingFilesDto);
         DeserializeEndpointProperties(offeringFiles);
         return offeringFiles;
      }

      private static async Task<List<OfferingFileDto>> ExecuteQueryForOfferingFiles(string query)
      {
         using (IDbConnection cnn = new SQLiteConnection(LoadConnectionString()))
         {
            return (await cnn.QueryAsync<OfferingFileDto>(query).ConfigureAwait(false)).ToList();
         }
      }

      private static void DeserializeEndpointProperties(List<OfferingFileDto> offeringFiles)
      {
         foreach (var offeringFile in offeringFiles)
         {
            if (!string.IsNullOrEmpty(offeringFile.EndpointsAndPropertiesJson))
            {
               offeringFile.EndpointsAndProperties = JsonSerializer.Deserialize<Dictionary<string, EndpointProperties>>(offeringFile.EndpointsAndPropertiesJson);
            }
         }
      }

      public static async Task<List<OfferingFileDto>> GetAllOfferingFilesWithOnlyJsonEndpointsAsync()
      {
         return await ExecuteQueryForOfferingFiles(QueryToFetchAllOfferingFilesDto);
      }

      public static async Task<OfferingFileDto> GetOfferingFileWithEndpointsByIdAsync(string offeringFileId)
      {
         using (IDbConnection cnn = new SQLiteConnection(LoadConnectionString()))
         {
            string query = @"
                  SELECT
                      o.OfferingFileIdentificator,
                      o.FileName,
                      o.FileSize,
                      CASE WHEN COUNT(e.Id) > 0 THEN json_group_object(e.Endpoint, json_object('Grade', e.Grade, 'TypeOfServerSocket', e.TypeOfServerSocket)) ELSE NULL END AS EndpointsAndPropertiesJson
                  FROM OfferingFiles o
                  LEFT JOIN EndpointsAndProperties e ON o.OfferingFileIdentificator = e.OfferingFileId
                  WHERE o.OfferingFileIdentificator = @OfferingFileId
                  GROUP BY o.OfferingFileIdentificator, o.FileName, o.FileSize";

            var offeringFile = await cnn.QuerySingleOrDefaultAsync<OfferingFileDto>(query, new { OfferingFileId = offeringFileId });

            if (offeringFile != null && !string.IsNullOrEmpty(offeringFile.EndpointsAndPropertiesJson))
            {
               offeringFile.EndpointsAndProperties = JsonSerializer.Deserialize<Dictionary<string, EndpointProperties>>(offeringFile.EndpointsAndPropertiesJson);
            }

            return offeringFile;
         }
      }

      public static async Task InsertOrUpdateOfferingFileDtoAsync(OfferingFileDto offeringFileDto)
      {
         using (IDbConnection cnn = new SQLiteConnection(LoadConnectionString()))
         {
            cnn.Open();
            using (IDbTransaction transaction = cnn.BeginTransaction())
            {
               // Insert the offering file or ignore if it already exists
               await cnn.ExecuteAsync("INSERT OR IGNORE INTO OfferingFiles (OfferingFileIdentificator, FileName, FileSize) VALUES (@OfferingFileIdentificator, @FileName, @FileSize)",
                  offeringFileDto, transaction: transaction);

               // Set the grade to 0 for existing endpoints, insert new endpoints
               foreach (KeyValuePair<string, EndpointProperties> endpointAndProperties in offeringFileDto.EndpointsAndProperties)
               {
                  await cnn.ExecuteAsync(@"INSERT OR REPLACE INTO EndpointsAndProperties (OfferingFileId, Endpoint, TypeOfServerSocket)
                     VALUES (@OfferingFileId, @Endpoint, @TypeOfServerSocket)",
                     new { OfferingFileId = offeringFileDto.OfferingFileIdentificator, Endpoint = endpointAndProperties.Key, endpointAndProperties.Value.TypeOfServerSocket }, transaction: transaction);
               }
               transaction.Commit();
            }
         }
      }

      public static async Task InsertOfferingFileDtoAsync(OfferingFileDto offeringFileDto)
      {
         using (IDbConnection cnn = new SQLiteConnection(LoadConnectionString()))
         {
            cnn.Open();
            using (IDbTransaction transaction = cnn.BeginTransaction())
            {
               // Execute the query asynchronously
               await cnn.ExecuteAsync("INSERT INTO OfferingFiles (OfferingFileIdentificator, FileName, FileSize) VALUES (@OfferingFileIdentificator, @FileName, @FileSize)",
                                       offeringFileDto,
                                       transaction: transaction);

               foreach (KeyValuePair<string, EndpointProperties> endpointAndProperties in offeringFileDto.EndpointsAndProperties)
               {
                  // Execute the query asynchronously
                  await cnn.ExecuteAsync("INSERT INTO EndpointsAndProperties (OfferingFileId, Endpoint, Grade, TypeOfServerSocket) VALUES (@OfferingFileId, @Endpoint, @Grade, @TypeOfServerSocket)",
                         new { OfferingFileId = offeringFileDto.OfferingFileIdentificator,
                            Endpoint = endpointAndProperties.Key, Grade = endpointAndProperties.Value,
                            TypeOfServerSocket = endpointAndProperties.Value.TypeOfServerSocket },
                           transaction: transaction);
               }
               transaction.Commit();
            }
         }
      }


      #endregion PublicMethods

      #region PrivateMethods

      private static string LoadConnectionString(string id = "Default")
      {
         return ConfigurationManager.ConnectionStrings[id].ConnectionString;
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
