using Common.Model;
using Dapper;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Net;
using System.Text;
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



      #endregion PrivateFields

      #region ProtectedFields



      #endregion ProtectedFields

      #region Ctor



      #endregion Ctor

      #region PublicMethods

      public static List<OfferingFileDto> GetAllOfferingFilesWithGrades()
      {
         using (IDbConnection cnn = new SQLiteConnection(LoadConnectionString()))
         {
            string query = @"
                    SELECT
                        o.OfferingFileIdentificator,
                        o.FileName,
                        o.FileSize,
                        CASE WHEN COUNT(e.Id) > 0 THEN json_group_object(e.Endpoint, e.Grade) ELSE NULL END AS EndpointsAndGradesJson
                    FROM OfferingFiles o
                    LEFT JOIN EndpointsAndGrades e ON o.OfferingFileIdentificator = e.OfferingFileId
                    GROUP BY o.OfferingFileIdentificator, o.FileName, o.FileSize";

            var offeringFiles = cnn.Query<OfferingFileDto>(query).ToList();

            foreach (var offeringFile in offeringFiles)
            {
               if (!string.IsNullOrEmpty(offeringFile.EndpointsAndGradesJson))
               {
                  offeringFile.EndpointsAndGrades = JsonSerializer.Deserialize<Dictionary<string, int>>(offeringFile.EndpointsAndGradesJson);
               }
            }

            return offeringFiles;
         }
      }

      public static OfferingFileDto GetOfferingFileWithGradesById(string offeringFileId)
      {
         using (IDbConnection cnn = new SQLiteConnection(LoadConnectionString()))
         {
            string query = @"
                    SELECT
                        o.OfferingFileIdentificator,
                        o.FileName,
                        o.FileSize,
                        json_group_object(e.Endpoint, e.Grade) AS EndpointsAndGradesJson
                    FROM OfferingFiles o
                    LEFT JOIN EndpointsAndGrades e ON o.OfferingFileIdentificator = e.OfferingFileId
                    WHERE o.OfferingFileIdentificator = @OfferingFileId
                    GROUP BY o.OfferingFileIdentificator, o.FileName, o.FileSize";

            var offeringFile = cnn.QuerySingleOrDefault<OfferingFileDto>(query, new { OfferingFileId = offeringFileId });

            if (offeringFile != null && !string.IsNullOrEmpty(offeringFile.EndpointsAndGradesJson))
            {
               offeringFile.EndpointsAndGrades = JsonSerializer.Deserialize<Dictionary<string, int>>(offeringFile.EndpointsAndGradesJson);
            }

            return offeringFile;
         }
      }

      public static void InsertOfferingFileDto(OfferingFileDto offeringFileDto)
      {
         using (IDbConnection cnn = new SQLiteConnection(LoadConnectionString()))
         {
            cnn.Execute("INSERT INTO OfferingFiles (OfferingFileIdentificator, FileName, FileSize) VALUES (@OfferingFileIdentificator, @FileName, @FileSize)", offeringFileDto);
            foreach (KeyValuePair<string, int> endpontAndGrade in offeringFileDto.EndpointsAndGrades)
            {
               cnn.Execute("INSERT INTO EndpointsAndGrades (OfferingFileId, Endpoint, Grade) VALUES (@OfferingFileId, @Endpoint, @Grade)",
                  new { OfferingFileId = offeringFileDto.OfferingFileIdentificator, Endpoint = endpontAndGrade.Key, Grade = endpontAndGrade.Value });
            }
         }
      }

      public static void InsertOrUpdateOfferingFileDto(OfferingFileDto offeringFileDto)
      {
         using (IDbConnection cnn = new SQLiteConnection(LoadConnectionString()))
         {
            // Insert the offering file or ignore if it already exists
            cnn.Execute("INSERT OR IGNORE INTO OfferingFiles (OfferingFileIdentificator, FileName, FileSize) VALUES (@OfferingFileIdentificator, @FileName, @FileSize)", offeringFileDto);

            // Set the grade to 0 for existing endpoints, insert new endpoints
            foreach (KeyValuePair<string, int> endpointAndGrade in offeringFileDto.EndpointsAndGrades)
            {
               cnn.Execute(@"INSERT OR REPLACE INTO EndpointsAndGrades (OfferingFileId, Endpoint, Grade) VALUES (@OfferingFileId, @Endpoint, 0)", new { OfferingFileId = offeringFileDto.OfferingFileIdentificator, Endpoint = endpointAndGrade.Key});
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
