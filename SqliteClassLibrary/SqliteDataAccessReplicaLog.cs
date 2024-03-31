using Common.Enum;
using Common.Model;
using Dapper;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Threading.Tasks;

namespace CentralServer
{
    public class SqliteDataAccessReplicaLog
    {

        #region Properties



        #endregion Properties

        #region PublicFields



        #endregion PublicFields

        #region PrivateFields

        private static string QueryToFetchAllReplicaLogFilesDto => @"
            SELECT * FROM ReplicaLog";

        #endregion PrivateFields

        #region ProtectedFields



        #endregion ProtectedFields

        #region Ctor



        #endregion Ctor

        #region PublicMethods

        public static async Task InsertLogAsync(PbftReplicaLogDto log)
        {
            using (IDbConnection cnn = new SQLiteConnection(LoadConnectionString()))
            {
                cnn.Open();
                using (IDbTransaction transaction = cnn.BeginTransaction())
                {
                    // Execute the query asynchronously with parameters
                    await cnn.ExecuteAsync("INSERT INTO ReplicaLog (MessageType, Time, MessageDirection, SynchronizationHash,HashOfRequest, ReceiverId, SenderId, Message)" +
                        " VALUES (@MessageType, @Time, @MessageDirection, @SynchronizationHash, @HashOfRequest, @ReceiverId, @SenderId, @Message)",
                        log, transaction: transaction);

                    transaction.Commit();
                }
            }
        }

        public static async Task<List<PbftReplicaLogDto>> GetAllLogsAsync()
        {
            return await ExecuteQueryForReplicaLogs(QueryToFetchAllReplicaLogFilesDto);
        }

        private static async Task<List<PbftReplicaLogDto>> ExecuteQueryForReplicaLogs(string query)
        {
            using (IDbConnection cnn = new SQLiteConnection(LoadConnectionString()))
            {
                return (await cnn.QueryAsync<PbftReplicaLogDto>(query).ConfigureAwait(false)).ToList();
            }
        }

        #endregion PublicMethods

        #region PrivateMethods

        private static string LoadConnectionString(string id = "ReplicaLog")
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
