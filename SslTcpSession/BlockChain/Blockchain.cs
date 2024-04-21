using Common.Model;
using ConfigManager;
using Logger;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Timers;


namespace SslTcpSession.BlockChain
{
    public static class Blockchain
    {

        private static readonly double _savedFileReward = 2.5;
        private static readonly double _sizeToPrizeConversion = .00001;

        private static readonly Timer _filesCheckingTime = new Timer();
        private static readonly Random _random = new Random();

        public static List<Block> Chain { get; private set; } = new List<Block>();

        public delegate void DownloadFileEventHandler(OfferingFileDto offeringFileDto);
        public static event DownloadFileEventHandler? DownloadFile;

        public static void StartApplication()
        {
            Chain.Add(CreateGenesisBlock());

            if (MyConfigManager.TryGetIntConfigValue("BlockchainFilesTimeCheckingInterval", out int intervalInMinutes))
            {
                _filesCheckingTime.Interval = intervalInMinutes * 1000 * 60;
                _filesCheckingTime.Elapsed += _filesCheckingTime_Elapsed;
                _filesCheckingTime.Start();
            }
            else
            {
                Log.WriteLog(LogLevel.WARNING, "Can not find setting, BlockchainFilesTimeCheckingInterval!");
            }
        }

        private static void _filesCheckingTime_Elapsed(object? sender, ElapsedEventArgs e)
        {
            // TO DO checking files
            IEnumerable<Block> blocks = GetAllFilesIShouldHad();

            string folder = MyConfigManager.GetConfigStringValue("BlockchainFileDirectoryDownload");

            foreach (Block block in blocks)
            {
                // Vygenerovanie náhodného čísla od 0 do 2
                int action = _random.Next(3);

                // Rozhodovanie o akcii na základe náhodného čísla
                switch (action)
                {
                    case 0:
                        // Action 0 - only check if file exist
                        if (string.IsNullOrEmpty(DataEncryptor.FindEcryptedFileById(block.FileID, folder)))
                        {
                            Log.WriteLog(LogLevel.INFO, $"File: {block.FileID}, not found, invoking event for download!");
                        }
                        else continue;
                        break;
                    case 1:
                        // Action 1 - also check size of file
                        if (!DataEncryptor.FindEncryptedFileByIdAndCheckHisSize(block.FileID, folder, block.FileSize, out _))
                        {
                            Log.WriteLog(LogLevel.INFO, $"File: {block.FileID}, not found, or have incorect size, invoking event for download!");
                        }
                        else continue;
                        break;
                    case 2:
                        // Action 3 - also check hi hash
                        // Action 1 - also check size of file
                        if (!DataEncryptor.FindEncryptedFileByIdAndCheckHisSizeAndHash(block.FileID, folder, block.FileSize, block.FileHash))
                        {
                            Log.WriteLog(LogLevel.INFO, $"File: {block.FileID}, not found, or have incorect size, or have incorect hash, invoking event for download!");
                        }
                        else continue;
                        break;
                }

                if (NodeDiscovery.TryGetNode(block.NodeId, out Node? node))
                {
                    OnDownloadFile(new OfferingFileDto($"{node.Address}:{node.Port}", Common.Enum.TypeOfServerSocket.TCP_SERVER_SSL)
                    {
                        FileName = block.FileIDAsString,
                        FileSize = block.FileSize,
                    });
                }
            }
        }

        public static void OnDownloadFile(OfferingFileDto offeringFileDto)
        {
            DownloadFile?.Invoke(offeringFileDto);
        }

        private static IEnumerable<Block> GetAllEverRequestedFilesToAdd()
        {
            return Chain.Where(block => block.Transaction == TransactionType.ADD_FILE_REQUEST);
        }

        private static IEnumerable<Block> GetAllActiveRequestedFilesToAdd()
        {
            return GetAllEverRequestedFilesToAdd().Where(block => !IsFileFlaggedToBeRemoved(block.FileID));
        }

        private static IEnumerable<Block> GetAllFilesIShouldHad()
        {
            return GetAllActiveRequestedFilesToAdd().Where(block => ShouldIHaveThatFile(block.FileID));
        }

        private static bool ShouldIHaveThatFile(Guid fileID)
        {
            Block? block = FindLatestFileUpdate(fileID);
            if (block == null || block.FileLocations == null)
            {
                return false;
            }
            return block.FileLocations.Contains(NodeDiscovery.GetMyNode().Id);
        }

        public static void EndAplication()
        {
            _filesCheckingTime.Stop();
            _filesCheckingTime.Elapsed -= _filesCheckingTime_Elapsed;
            _filesCheckingTime.Dispose();
        }

        private static Block CreateGenesisBlock()
        {
            Block genesis = new Block
            {
                Index = 0,
                Timestamp = DateTime.UtcNow,
                PreviousHash = "0"
            };

            return genesis;
        }

        public static async Task<BlockValidationResult> Add_AddCredit(double creditValueToAdd)
        {

            if (NodeDiscovery.IsSynchronizationOlderThanMaxOldSynchronizationTime())
            {
                return BlockValidationResult.OLD_SYNCHRONIZATION;
            }

            double newCreditValue = 0;
            Block? block = FindActualCreditValueOfNode(NodeDiscovery.GetMyNode().Id);
            if (block != null)
            {
                newCreditValue = block.NewCreditValue;
            }

            newCreditValue += creditValueToAdd;

            Block newBlock = new Block
            {
                Index = Chain.Count,
                Timestamp = DateTime.UtcNow,
                PreviousHash = Chain[Chain.Count - 1].Hash,
                Transaction = TransactionType.ADD_CREDIT,
                NodeId = NodeDiscovery.GetMyNode().Id,
                CreditChange = creditValueToAdd,
                NewCreditValue = newCreditValue,
            };

            newBlock.ComputeHash();
            newBlock.SignHash();
            return await SendToPrimaryReplica(newBlock, NodeDiscovery.GetMyNode());
        }

        public static async Task<BlockValidationResult> Add_AddFile(Guid fileId)
        {
            if (NodeDiscovery.IsSynchronizationOlderThanMaxOldSynchronizationTime())
            {
                return BlockValidationResult.OLD_SYNCHRONIZATION;
            }

            Block? block = FindLatestFileUpdate(fileId);
            if (block == null || !block.FileHash.Equals(block.FileHash))
            {
                return BlockValidationResult.FILE_DOES_NOT_EXIST;
            }

            List<Guid>? endPoints = null;
            if (block.Transaction == TransactionType.ADD_FILE_REQUEST)
            {
                endPoints = new List<Guid>() { NodeDiscovery.GetMyNode().Id };
            }
            else if (block.Transaction == TransactionType.ADD_FILE || block.Transaction == TransactionType.REMOVE_FILE)
            {
                if (block.FileLocations == null) return BlockValidationResult.UNDEFINED_SITUATION;
                if (block.FileLocations.Exists(ep => ep == NodeDiscovery.GetMyNode().Id))
                {
                    return BlockValidationResult.YOUR_ENDPOINT_IS_ALREADY_ON_LIST;
                }

                endPoints = block.FileLocations.ToList();
                endPoints.Add(NodeDiscovery.GetMyNode().Id);
            }

            double creditValue = 0;
            Block? creditBlock = FindActualCreditValueOfNode(NodeDiscovery.GetMyNode().Id);
            if (creditBlock != null)
            {
                creditValue = creditBlock.NewCreditValue;
            }

            creditValue += _savedFileReward;

            Block newBlock = new Block
            {
                Index = Chain.Count,
                Timestamp = DateTime.UtcNow,
                FileHash = block.FileHash,
                PreviousHash = Chain[Chain.Count - 1].Hash,
                Transaction = TransactionType.ADD_FILE,
                FileID = fileId,
                NodeId = NodeDiscovery.GetMyNode().Id,
                CreditChange = _savedFileReward,
                NewCreditValue = creditValue,
                FileLocations = endPoints,
                FileSize = block.FileSize,
            };

            newBlock.ComputeHash();
            newBlock.SignHash();

            return await SendToPrimaryReplica(newBlock, NodeDiscovery.GetMyNode());
        }

        public static async Task<BlockValidationResult> Add_RemoveFile(Guid fileId, string fileHash, Guid endPointToRemove)
        {

            Block? block = FindLatestFileUpdate(fileId);
            if (block == null || block.Transaction == TransactionType.ADD_FILE_REQUEST)
            {
                return BlockValidationResult.FILE_DOES_NOT_EXIST;
            }

            if (block.FileLocations == null)
            {
                return BlockValidationResult.YOUR_ENDPOINT_IS_NOT_ON_LIST;
            }
            List<Guid> endPoints = block.FileLocations.ToList();
            int? removed = endPoints.RemoveAll(ep => ep == endPointToRemove);

            if (removed.HasValue && removed <= 0)
            {
                return BlockValidationResult.YOUR_ENDPOINT_IS_NOT_ON_LIST;
            }

            double newCreditValue = 0;
            Block? creditBlock = FindActualCreditValueOfNode(NodeDiscovery.GetMyNode().Id);
            if (creditBlock != null)
            {
                newCreditValue = creditBlock.NewCreditValue;
            }

            Block newBlock = new Block
            {
                Index = Chain.Count,
                Timestamp = DateTime.UtcNow,
                FileHash = fileHash,
                PreviousHash = Chain[Chain.Count - 1].Hash,
                Transaction = TransactionType.REMOVE_FILE,
                FileID = fileId,
                NodeId = NodeDiscovery.GetMyNode().Id,
                CreditChange = 0,
                NewCreditValue = newCreditValue,
                FileLocations = endPoints,
            };

            newBlock.ComputeHash();
            newBlock.SignHash();
            return await SendToPrimaryReplica(newBlock, NodeDiscovery.GetMyNode());
        }

        private static Block? FindLatestFileUpdate(Guid fileId)
        {
            for (int i = Chain.Count() - 1; i >= 0; i--)
            {
                if (Chain[i].FileID == fileId)
                {
                    return Chain[i];
                }
            }
            return null;
        }

        private static bool IsFileFlaggedToBeRemoved(Guid fileId)
        {
            return Chain.Exists(block => block.FileID == fileId && block.Transaction == TransactionType.REMOVE_FILE_REQUEST);
        }

        private static Block? FindActualCreditValueOfNode(Guid nodeId)
        {
            for (int i = Chain.Count() - 1; i >= 0; i--)
            {
                if (Chain[i].NodeId == nodeId)
                {
                    return Chain[i];
                }
            }
            return null;
        }

        public static async Task<BlockValidationResult> Add_AddFileRequest(string pathToFileForRequest)
        {
            if (NodeDiscovery.IsSynchronizationOlderThanMaxOldSynchronizationTime())
            {
                return BlockValidationResult.OLD_SYNCHRONIZATION;
            }

            Guid fileId = Guid.NewGuid();

            string newFilePath = await DataEncryptor.EncryptFileAsync(pathToFileForRequest, fileId.ToString(),
               MyConfigManager.GetConfigStringValue("BlockchainFileDirectoryUpload"),
               Certificats.GetCertificate("ReplicaXY", Certificats.CertificateType.Node));

            Logger.Log.WriteLog(Logger.LogLevel.INFO, $"Encryption completed and encrypted file: {newFilePath}" +
               $" was created from file: {pathToFileForRequest}");

            if (string.IsNullOrEmpty(newFilePath))
            {
                return BlockValidationResult.UNABLE_TO_ENCRYPT_FILE;
            }

            double priceOfRequest = CalculatePriceOfFile(newFilePath, out Int64 fileSize);
            if (priceOfRequest <= 0)
            {
                return BlockValidationResult.UNABLE_TO_CALCULATE_PRIZE_OF_REQUEST;
            }

            double creditValue = 0;
            Block? creditBlock = FindActualCreditValueOfNode(NodeDiscovery.GetMyNode().Id);
            if (creditBlock != null)
            {
                creditValue = creditBlock.NewCreditValue;
            }

            if (creditValue < priceOfRequest)
            {
                return BlockValidationResult.NOT_ENOUGHT_CREDIT;
            }
            else
            {
                creditValue -= priceOfRequest;
            }

            Block newBlock = new Block
            {
                Index = Chain.Count,
                Timestamp = DateTime.UtcNow,
                PreviousHash = Chain[Chain.Count - 1].Hash,
                Transaction = TransactionType.ADD_FILE_REQUEST,
                FileID = fileId,
                NodeId = NodeDiscovery.GetMyNode().Id,
                CreditChange = -priceOfRequest,
                NewCreditValue = creditValue,
                FileSize = fileSize,
                FileHash = CalculateHashOfFile(newFilePath)
            };

            newBlock.ComputeHash();
            newBlock.SignHash();

            var result = await SendToPrimaryReplica(newBlock, NodeDiscovery.GetMyNode());
            return result;
        }

        public static string CalculateHashOfFile(string pathToFile)
        {
            if (File.Exists(pathToFile))
            {
                using (SHA256 sha256 = SHA256.Create())
                {
                    using (FileStream fileStream = File.OpenRead(pathToFile))
                    {
                        byte[] hashValue = sha256.ComputeHash(fileStream);
                        return BitConverter.ToString(hashValue).Replace("-", String.Empty).ToLowerInvariant();
                    }
                }
            }
            return string.Empty;
        }

        public static async Task<BlockValidationResult> Add_RemoveFileRequest(Guid fileId)
        {
            if (NodeDiscovery.IsSynchronizationOlderThanMaxOldSynchronizationTime())
            {
                return BlockValidationResult.OLD_SYNCHRONIZATION;
            }

            Block? fileBlock = Chain.FirstOrDefault(b => b.FileID == fileId);
            if (fileBlock == null)
            {
                return BlockValidationResult.FILE_DOES_NOT_EXIST;
            }

            double newCreditValue = 0;
            Block? creditBlock = FindActualCreditValueOfNode(NodeDiscovery.GetMyNode().Id);
            if (creditBlock != null)
            {
                newCreditValue = creditBlock.NewCreditValue;
            }

            Block newBlock = new Block
            {
                Index = Chain.Count,
                Timestamp = DateTime.UtcNow,
                FileHash = fileBlock.FileHash,
                PreviousHash = Chain[Chain.Count - 1].Hash,
                Transaction = TransactionType.REMOVE_FILE_REQUEST,
                FileLocations = fileBlock.FileLocations,
                FileID = fileId,
                NodeId = NodeDiscovery.GetMyNode().Id,
                CreditChange = 0,
                NewCreditValue = newCreditValue
            };

            newBlock.ComputeHash();
            newBlock.SignHash();
            return await SendToPrimaryReplica(newBlock, NodeDiscovery.GetMyNode());
        }

        private static async Task<BlockValidationResult> SendToPrimaryReplica(Block newBlock, Node fromNode)
        {

            // Local test for validation
            BlockValidationResult result = IsNewBlockValid(newBlock, fromNode);
            if (result == BlockValidationResult.VALID)
            {
                // Choosing primary replica and check his ip
                if (!TryToChooseViewPrimaryReplica(newBlock.Timestamp, out Node? primaryReplica) ||
                    !IPAddress.TryParse(primaryReplica.Address, out IPAddress? iPAddress))
                {
                    return BlockValidationResult.UNABLE_TO_CHOOSE_PRIMARY_REPLICA;
                }

                // send to primary replica
                bool success = await SslPbftTmpClientBusinessLogic.SendPbftRequestAndDispose(iPAddress, primaryReplica.Port,
                   newBlock, NodeDiscovery.SynchronizationHash, primaryReplica.Id);

                if (!success)
                {
                    result = BlockValidationResult.BLOCK_IS_VALID_BUT_PRIMARY_REPLICA_CAN_NOT_BE_REACHED;
                }
            }
            return result;
        }

        private static bool TryToChooseViewPrimaryReplica(DateTime requestedBlockTimestamp, [MaybeNullWhen(false)] out Node primaryReplica)
        {

            IEnumerable<Node> nodes = NodeDiscovery.GetAllCurrentlyVerifiedActiveNodes();

            primaryReplica = null;
            string? smallestHash = null;
            string lastBlockHash = Chain[^1].Hash;

            foreach (var node in nodes)
            {
                string nodeHash = node.GenerateNodeSpecificHash(lastBlockHash, requestedBlockTimestamp);
                if (smallestHash == null || string.CompareOrdinal(nodeHash, smallestHash) < 0)
                {
                    smallestHash = nodeHash;
                    primaryReplica = node;
                }
            }

            return primaryReplica != null;
        }

        // request
        public static bool VerifyPrimaryReplica(Guid replicaId, DateTime requestedBlockTimestamp)
        {
            if (!TryToChooseViewPrimaryReplica(requestedBlockTimestamp, out Node? primaryReplica))
            {
                return false;
            }
            return replicaId == primaryReplica.Id;
        }

        // pre-prepare
        public static bool VerifyPrimaryReplica(string hashOfRequestedBlock, string signOfPrimaryReplica, DateTime requestedBlockTimestamp)
        {
            if (!TryToChooseViewPrimaryReplica(requestedBlockTimestamp, out Node? primaryReplica))
            {
                return false;
            }
            return Certificats.VerifyString(hashOfRequestedBlock, signOfPrimaryReplica, primaryReplica.PublicKey);
        }

        // Prepare
        public static bool VerifyBackupReplica(Node backupReplica, string signOfBackupReplica, string hashToVerify)
        {
            return Certificats.VerifyString(hashToVerify, signOfBackupReplica, backupReplica.PublicKey);
        }

        public static bool IsBlockChainValid()
        {
            for (int i = 1; i < Chain.Count; i++)
            {
                Block currentBlock = Chain[i];
                Block previousBlock = Chain[i - 1];

                if (currentBlock.PreviousHash != previousBlock.Hash)
                {
                    return false;
                }
            }
            return true;
        }

        public static double CalculatePriceOfFile(string pathWithFile, out Int64 fileSize)
        {
            fileSize = -1;
            if (File.Exists(pathWithFile))
            {
                FileInfo fileInfo = new FileInfo(pathWithFile);
                fileSize = fileInfo.Length;

                return CalculatePriceOfFile(fileSize);
            }
            return -1;
        }

        public static double CalculatePriceOfFile(double sizeOfFile)
        {
            double price = sizeOfFile * _sizeToPrizeConversion;
            return price > 0 ? price : _sizeToPrizeConversion;
        }

        public static BlockValidationResult IsNewBlockValid(Block newBlock, Node fromNode)
        {

            BlockValidationResult result;

            // Check for index and previous hash
            result = CheckPreviousHash(newBlock);
            if (result != BlockValidationResult.VALID)
            {
                return result;
            }

            // Check for valid sign
            result = CheckSignedHash(newBlock);
            if (result != BlockValidationResult.VALID)
            {
                return result;
            }

            // Check for right new credit value calculation
            result = CheckCreditChange(newBlock);
            if (result != BlockValidationResult.VALID)
            {
                return result;
            }

            // Check transaction
            switch (newBlock.Transaction)
            {
                case TransactionType.ADD_FILE:
                    return ValidateAddFile(newBlock, fromNode);
                case TransactionType.ADD_FILE_REQUEST:
                    return ValidateAddFileRequest(newBlock);
                case TransactionType.REMOVE_FILE:
                    return ValidateRemoveFile(newBlock, fromNode);
                case TransactionType.REMOVE_FILE_REQUEST:
                    return ValidateRemoveFileRequest(newBlock);
                case TransactionType.ADD_CREDIT:
                    return ValidateAddCredit(newBlock);
                default:
                    break;
            }

            return BlockValidationResult.UNABLE_TO_DECIDE;
        }

        private static BlockValidationResult CheckPreviousHash(Block blockToCheck)
        {
            // Check pevious hash
            if (Chain.Count < blockToCheck.Index - 1)
            {
                return BlockValidationResult.INVALID_BLOCK_INDEX;
            }

            if (blockToCheck.PreviousHash != Chain[blockToCheck.Index - 1].Hash)
            {
                return BlockValidationResult.INVALID_PREVIOUS_HASH;
            }
            return BlockValidationResult.VALID;
        }

        private static BlockValidationResult CheckSignedHash(Block blockToCheck)
        {
            // Check signed hash
            if (NodeDiscovery.TryGetNode(blockToCheck.NodeId, out Node? node))
            {
                if (!blockToCheck.VerifyHash(node.PublicKey))
                {
                    return BlockValidationResult.INVALID_SIGN;
                }
            }
            else
            {
                return BlockValidationResult.UNABLE_TO_DECIDE;
            }
            return BlockValidationResult.VALID;
        }

        private static BlockValidationResult CheckCreditChange(Block blockToCheck)
        {
            // Find latest block with this nodeId and chcek if latest credit + change credit value = new credit value
            double oldCreditValue = 0;
            Block? block = FindActualCreditValueOfNode(blockToCheck.NodeId);
            if (block != null)
            {
                oldCreditValue = block.NewCreditValue;
            }
            if (blockToCheck.NewCreditValue < 0)
            {
                return BlockValidationResult.NEGATIVE_VALUE_OF_CREDIT;
            }
            if (blockToCheck.NewCreditValue != oldCreditValue + blockToCheck.CreditChange)
            {
                return BlockValidationResult.INVALID_CREDIT_CALCULATION;
            }

            return BlockValidationResult.VALID;
        }

        /////////////////////////// VALIDATION

        private static BlockValidationResult ValidateAddCredit(Block newBlock)
        {
            if (newBlock.CreditChange <= 0)
            {
                return BlockValidationResult.INVALID_PRICE_CALCULATION;
            }

            return BlockValidationResult.VALID;
        }

        private static BlockValidationResult ValidateAddFileRequest(Block newBlock)
        {
            if (newBlock.FileSize <= 0)
            {
                return BlockValidationResult.INVALID_SIZE_OF_FILE;
            }

            // Check for right price for operation
            if (newBlock.CreditChange != -CalculatePriceOfFile(newBlock.FileSize))
            {
                return BlockValidationResult.INVALID_PRICE_CALCULATION;
            }

            return BlockValidationResult.VALID;
        }

        private static BlockValidationResult ValidateAddFile(Block newBlock, Node fromNode)
        {

            // Check if block with that fileId exist
            Block? block = FindLatestFileUpdate(newBlock.FileID);
            if (block == null || !block.FileHash.Equals(newBlock.FileHash))
            {
                return BlockValidationResult.FILE_DOES_NOT_EXIST;
            }

            if (newBlock.FileSize != block.FileSize)
            {
                return BlockValidationResult.WRONG_FILE_SIZE;
            }

            // Check if this pathWithFile is not flaged to remove
            if (IsFileFlaggedToBeRemoved(newBlock.FileID))
            {
                return BlockValidationResult.FILE_IS_FLAGED_TO_BE_REMOVED;
            }

            // Create virtual new end point list
            List<Guid>? endPoints = null;
            // If its Add pathWithFile request - create new list
            if (block.Transaction == TransactionType.ADD_FILE_REQUEST)
            {
                endPoints = new List<Guid>();
            }
            // If anyting else, take old list
            else
            {
                endPoints = block.FileLocations?.ToList();
                // Check if its not null
                if (endPoints == null)
                {
                    return BlockValidationResult.INVALID_FILE_LOCATIONS;
                }
            }

            // Check if endpoint is not already there
            if (block.FileLocations != null && block.FileLocations.Exists(ep => ep == fromNode.Id))
            {
                return BlockValidationResult.YOUR_ENDPOINT_IS_ALREADY_ON_LIST;
            }
            // Add end point to list
            endPoints.Add(fromNode.Id);
            // Check if virtual new end point list is the same as provided
            if (!CompareGuidLists(endPoints, newBlock.FileLocations))
            {
                return BlockValidationResult.INVALID_FILE_LOCATIONS;
            }

            // Check for right price for operation
            if (newBlock.CreditChange != _savedFileReward)
            {
                return BlockValidationResult.INVALID_PRICE_CALCULATION;
            }

            return BlockValidationResult.VALID;
        }

        private static BlockValidationResult ValidateRemoveFile(Block newBlock, Node fromNode)
        {
            Block? block = FindLatestFileUpdate(newBlock.FileID);
            if (block == null || block.Transaction == TransactionType.ADD_FILE_REQUEST)
            {
                return BlockValidationResult.FILE_DOES_NOT_EXIST;
            }

            if (block.FileLocations == null)
            {
                return BlockValidationResult.YOUR_ENDPOINT_IS_NOT_ON_LIST;
            }

            List<Guid> endPoints = block.FileLocations.ToList();
            int? removed = endPoints.RemoveAll(ep => ep == fromNode.Id);

            if (removed.HasValue && removed <= 0)
            {
                return BlockValidationResult.YOUR_ENDPOINT_IS_NOT_ON_LIST;
            }

            // Check if virtual new end point list is the same as provided
            if (!CompareGuidLists(endPoints, newBlock.FileLocations))
            {
                return BlockValidationResult.INVALID_FILE_LOCATIONS;
            }

            if (newBlock.CreditChange != 0)
            {
                return BlockValidationResult.INVALID_PRICE_CALCULATION;
            }

            return BlockValidationResult.VALID;
        }

        private static BlockValidationResult ValidateRemoveFileRequest(Block newBlock)
        {

            Block? fileBlock = Chain.FirstOrDefault(b => b.FileID == newBlock.FileID && b.Transaction == TransactionType.ADD_FILE_REQUEST);
            if (fileBlock == null)
            {
                return BlockValidationResult.FILE_DOES_NOT_EXIST;
            }

            if (IsFileFlaggedToBeRemoved(newBlock.FileID))
            {
                return BlockValidationResult.FILE_IS_FLAGED_TO_BE_REMOVED;
            }

            if (fileBlock.NodeId != newBlock.NodeId)
            {
                return BlockValidationResult.YOU_ARE_NOT_FILE_OWNER;
            }

            if (newBlock.CreditChange != 0)
            {
                return BlockValidationResult.INVALID_PRICE_CALCULATION;
            }

            return BlockValidationResult.VALID;
        }

        public static string ToJson(bool prettyPrinted = false)
        {
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                WriteIndented = prettyPrinted
            };

            return JsonSerializer.Serialize(Chain, options);
        }

        public static bool FromJson(string json, [MaybeNullWhen(false)] out List<Block> chain)
        {
            try
            {
                chain = JsonSerializer.Deserialize<List<Block>>(json);
                if (chain != null)
                {
                    return true;
                }
                else return false;
            }
            catch
            {
                chain = null;
                return false;
            }
        }

        private static bool CompareGuidLists(List<Guid>? list1, List<Guid>? list2)
        {
            if (list1 == null || list2 == null) return false;

            // Rýchle zlyhanie, ak sú zoznamy rôznych veľkostí
            if (list1.Count != list2.Count) return false;

            return list1.OrderBy(x => x).SequenceEqual(list2.OrderBy(x => x));

        }

        public static void AddBlockAfterConsensus(Block block)
        {
            Chain.Add(block);
            Logger.Log.WriteLog(Logger.LogLevel.INFO, "BlockChain: " + PrintChain());
        }

        public static string PrintChain()
        {
            StringBuilder sb = new StringBuilder();

            foreach (Block block in Chain)
            {
                sb.Append(block.ToJson().Replace(';', '|')).Append('\n');
            }
            return sb.ToString();
        }

        public static IEnumerable<Guid> GetPossibleGuidsForRemoveRequest(Guid replicaId)
        {
            return Chain.Where(block => block.Transaction == TransactionType.ADD_FILE_REQUEST &&
                                  block.NodeId == replicaId &&
                                  !IsFileFlaggedToBeRemoved(block.FileID)).Select(block => block.FileID);
        }

        public static bool DoISharingThisFile(Guid fileId)
        {
            return Chain.Exists(block => block.Transaction == TransactionType.ADD_FILE_REQUEST &&
                        block.NodeId == NodeDiscovery.GetMyNode().Id && !IsFileFlaggedToBeRemoved(fileId));
        }

        public static IEnumerable<Guid> GetPossibleGuidsForAddRequest()
        {
            return Chain.Where(block => block.Transaction == TransactionType.ADD_FILE_REQUEST &&
                                  !IsFileFlaggedToBeRemoved(block.FileID)).Select(block => block.FileID);
        }

        public static void LoadedChainFromDb(List<Block> chain)
        {
            Chain = chain;
        }
    }
}
