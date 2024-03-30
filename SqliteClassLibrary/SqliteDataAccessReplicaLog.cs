using Common.Enum;
using Common.Model;
using Dapper;
using System.Configuration;
using System.Data;
using System.Data.SQLite;
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
                    await cnn.ExecuteAsync("INSERT INTO ReplicaLog (MessageType, Time, MessageDirection, SynchronizationHash,HashOfRequest, ReceiverId, SenderId)" +
                        " VALUES (@MessageType, @TimeAsString, @MessageDirection, @SynchronizationHash, @HashOfRequest, @ReceiverId, @SenderId)",
                        log, transaction: transaction);

                    transaction.Commit();
                }
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
