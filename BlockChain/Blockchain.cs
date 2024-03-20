using ConfigManager;
using System.Net;
using System.Text.Json;

namespace BlockChain
{
   public class Blockchain
   {

      private static readonly double _newFileCreditPrice = 10;
      private static readonly double _savedFileReward = 2.5;

      public List<Block> Chain { get; } = new List<Block>();

      public Blockchain()
      {
         // Add genesis block
         Chain.Add(CreateGenesisBlock());
      }

      private Block CreateGenesisBlock()
      {
         return new Block
         {
            Index = 0,
            Timestamp = DateTime.UtcNow,
            PreviousHash = "0"
         };
      }

      public BlockValidationResult Add_AddCredit(double creditValueToAdd)
      {

         double newCreditValue = 0;
         Block? block = FindActualCreditValueOfNode(NodeDiscovery.GetMyNode().Id);
         if (block != null)
         {
            newCreditValue = block.NewCreditVaue;
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
            NewCreditVaue = newCreditValue,
         };

         newBlock.ComputeHash();
         newBlock.SignHash();
         return AddBlock(newBlock, NodeDiscovery.GetMyNode());
      }

      public BlockValidationResult Add_AddFile(Guid fileId, string fileHash, EndPoint myEndPoint)
      {

         Block? block = FindLatestFileUpdate(fileId);
         if (block == null || !block.FileHash.Equals(fileHash))
         {
            return BlockValidationResult.FILE_DOES_NOT_EXIST;
         }

         List<EndPoint>? endPoints = null;
         if (block.Transaction == TransactionType.ADD_FILE_REQUEST)
         {
            endPoints = new List<EndPoint>() { myEndPoint };
         }
         else if (block.Transaction == TransactionType.ADD_FILE || block.Transaction == TransactionType.REMOVE_FILE)
         {
            if (block.FileLocations == null) return BlockValidationResult.UNDEFINED_SITUATION;
            if (block.FileLocations.Exists(ep => ep.Equals(myEndPoint)))
            {
               return BlockValidationResult.YOUR_ENDPOINT_IS_ALREADY_ON_LIST;
            }

            endPoints = block.FileLocations.ToList();
            endPoints.Add(myEndPoint);
         }

         double creditValue = 0;
         Block? creditBlock = FindActualCreditValueOfNode(NodeDiscovery.GetMyNode().Id);
         if (creditBlock != null)
         {
            creditValue = creditBlock.NewCreditVaue;
         }

         creditValue += _savedFileReward;

         Block newBlock = new Block
         {
            Index = Chain.Count,
            Timestamp = DateTime.UtcNow,
            FileHash = fileHash,
            PreviousHash = Chain[Chain.Count - 1].Hash,
            Transaction = TransactionType.ADD_FILE,
            FileID = fileId,
            NodeId = NodeDiscovery.GetMyNode().Id,
            CreditChange = _savedFileReward,
            NewCreditVaue = creditValue,
            FileLocations = endPoints,
         };

         newBlock.ComputeHash();
         newBlock.SignHash();

         return AddBlock(newBlock, NodeDiscovery.GetMyNode());
      }

      public BlockValidationResult Add_RemoveFile(Guid fileId, string fileHash, EndPoint endPoint)
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
         List<EndPoint> endPoints = block.FileLocations.ToList();
         int? removed = endPoints.RemoveAll(ep => ep.Equals(endPoint));

         if (removed.HasValue && removed <= 0)
         {
            return BlockValidationResult.YOUR_ENDPOINT_IS_NOT_ON_LIST;
         }

         double newCreditValue = 0;
         Block? creditBlock = FindActualCreditValueOfNode(NodeDiscovery.GetMyNode().Id);
         if (creditBlock != null)
         {
            newCreditValue = creditBlock.NewCreditVaue;
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
            NewCreditVaue = newCreditValue,
            FileLocations = endPoints,
         };

         newBlock.ComputeHash();
         newBlock.SignHash();
         return AddBlock(newBlock, NodeDiscovery.GetMyNode());
      }

      private Block? FindLatestFileUpdate(Guid fileId)
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

      private bool IsFileFlaggedToBeRemoved(Guid fileId)
      {
         return Chain.Exists(block => block.FileID == fileId && block.Transaction == TransactionType.REMOVE_FILE_REQUEST);
      }

      private Block? FindActualCreditValueOfNode(Guid nodeId)
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

      public BlockValidationResult Add_AddFileRequest(string fileHash, out Guid fileId)
      {
         fileId = Guid.NewGuid();

         double creditValue = 0;
         Block? creditBlock = FindActualCreditValueOfNode(NodeDiscovery.GetMyNode().Id);
         if (creditBlock != null)
         {
            creditValue = creditBlock.NewCreditVaue;
         }

         if (creditValue < _newFileCreditPrice)
         {
            return BlockValidationResult.NOT_ENOUGHT_CREDIT;
         }
         else
         {
            creditValue -= _newFileCreditPrice;
         }

         Block newBlock = new Block
         {
            Index = Chain.Count,
            Timestamp = DateTime.UtcNow,
            FileHash = fileHash,
            PreviousHash = Chain[Chain.Count - 1].Hash,
            Transaction = TransactionType.ADD_FILE_REQUEST,
            FileID = fileId,
            NodeId = NodeDiscovery.GetMyNode().Id,
            CreditChange = -_newFileCreditPrice,
            NewCreditVaue = creditValue
         };

         newBlock.ComputeHash();
         newBlock.SignHash();

         return AddBlock(newBlock, NodeDiscovery.GetMyNode());
      }

      public BlockValidationResult Add_RemoveFileRequest(Guid fileId)
      {
         Block? fileBlock = Chain.FirstOrDefault(b => b.FileID == fileId);
         if (fileBlock == null)
         {
            return BlockValidationResult.FILE_DOES_NOT_EXIST;
         }

         double newCreditValue = 0;
         Block? creditBlock = FindActualCreditValueOfNode(NodeDiscovery.GetMyNode().Id);
         if (creditBlock != null)
         {
            newCreditValue = creditBlock.NewCreditVaue;
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
            NewCreditVaue = newCreditValue
         };

         fileBlock.ComputeHash();
         newBlock.SignHash();
         return AddBlock(newBlock, NodeDiscovery.GetMyNode());

      }

      public BlockValidationResult AddBlock(Block newBlock, Node fromNode)
      {
         BlockValidationResult result = IsNewBlockValid(newBlock, fromNode);
         if (result == BlockValidationResult.VALID)
         {
            Chain.Add(newBlock);
         }
         return result;
      }

      public bool IsBlockChainValid()
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

      public BlockValidationResult IsNewBlockValid(Block newBlock, Node fromNode)
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
               return ValidateRemoveFile(newBlock);
            case TransactionType.REMOVE_FILE_REQUEST:
               return ValidateRemoveFileRequest(newBlock);
            case TransactionType.ADD_CREDIT:
               return ValidateAddCredit(newBlock);
            default:
               break;
         }

         return BlockValidationResult.UNABLE_TO_DECIDE;
      }

      private BlockValidationResult CheckPreviousHash(Block blockToCheck)
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

      private BlockValidationResult CheckSignedHash(Block blockToCheck)
      {
         // Check signed hash
         if (NodeDiscovery.GetNode(blockToCheck.NodeId, out Node? node))
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

      private BlockValidationResult CheckCreditChange(Block blockToCheck)
      {
         // Find latest block with this nodeId and chcek if latest credit + change credit value = new credit value
         double oldCreditValue = 0;
         Block? block = FindActualCreditValueOfNode(blockToCheck.NodeId);
         if (block != null)
         {
            oldCreditValue = block.NewCreditVaue;
         }
         if (blockToCheck.NewCreditVaue < 0)
         {
            return BlockValidationResult.NEGATIVE_VALUE_OF_CREDIT;
         }
         if (blockToCheck.NewCreditVaue != oldCreditValue + blockToCheck.CreditChange)
         {
            return BlockValidationResult.INVALID_CREDIT_CALCULATION;
         }

         return BlockValidationResult.VALID;
      }

      /////////////////////////// VALIDATION

      private BlockValidationResult ValidateAddCredit(Block newBlock)
      {
         if (newBlock.CreditChange <= 0)
         {
            return BlockValidationResult.INVALID_PRICE_CALCULATION;
         }

         return BlockValidationResult.VALID;
      }

      private BlockValidationResult ValidateAddFileRequest(Block newBlock)
      {
         // Check for right price for operation
         if (newBlock.CreditChange != -_newFileCreditPrice)
         {
            return BlockValidationResult.INVALID_PRICE_CALCULATION;
         }

         return BlockValidationResult.VALID;
      }

      private BlockValidationResult ValidateAddFile(Block newBlock, Node fromNode)
      {

         // Check if block with that fileId exist
         Block? block = FindLatestFileUpdate(newBlock.FileID);
         if (block == null || !block.FileHash.Equals(newBlock.FileHash))
         {
            return BlockValidationResult.FILE_DOES_NOT_EXIST;
         }

         // Check if this file is not flaged to remove
         if (IsFileFlaggedToBeRemoved(newBlock.FileID))
         {
            return BlockValidationResult.FILE_IS_FLAGED_TO_BE_REMOVED;
         }

         // Create virtual new end point list
         List<EndPoint>? endPoints = null;
         // If its Add file request - create new list
         if (block.Transaction == TransactionType.ADD_FILE_REQUEST)
         {
            endPoints = new List<EndPoint>();
         }
         // If anyting else, take old list
         else
         {
            endPoints = block.FileLocations;
            // Check if its not null
            if (endPoints == null)
            {
               return BlockValidationResult.INVALID_FILE_LOCATIONS;
            }
         }
         // Try to extract endpoint from node
         EndPoint? newEndPoint = fromNode.GetNodeEndpoint();
         if (newEndPoint == null)
         {
            return BlockValidationResult.INVALID_NODE_ENDPOINT;
         }
         // Check if endpoint is not already there
         if (block.FileLocations != null && block.FileLocations.Exists(ep => ep.Equals(newEndPoint)))
         {
            return BlockValidationResult.YOUR_ENDPOINT_IS_ALREADY_ON_LIST;
         }
         // Add end point to list
         endPoints.Add(newEndPoint);
         // Check if virtual new end point list is the same as provided
         if (!CompareAddressAndPortOfEndPointLists(endPoints, newBlock.FileLocations))
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

      private BlockValidationResult ValidateRemoveFile(Block newBlock)
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

         if (newBlock.CreditChange != 0)
         {
            return BlockValidationResult.INVALID_PRICE_CALCULATION;
         }

         return BlockValidationResult.VALID;
      }

      private BlockValidationResult ValidateRemoveFileRequest(Block newBlock)
      {

         Block? fileBlock = Chain.FirstOrDefault(b => b.FileID == newBlock.FileID);
         if (fileBlock == null)
         {
            return BlockValidationResult.FILE_DOES_NOT_EXIST;
         }

         if (IsFileFlaggedToBeRemoved(newBlock.FileID))
         {
            return BlockValidationResult.FILE_IS_FLAGED_TO_BE_REMOVED;
         }

         if (newBlock.CreditChange != 0)
         {
            return BlockValidationResult.INVALID_PRICE_CALCULATION;
         }

         return BlockValidationResult.VALID;
      }

      public string ToJson(bool prettyPrinted = false)
      {
         JsonSerializerOptions options = new JsonSerializerOptions
         {
            WriteIndented = prettyPrinted
         };

         return JsonSerializer.Serialize(Chain, options);
      }

      private bool CompareAddressAndPortOfEndPointLists(List<EndPoint>? list1, List<EndPoint>? list2)
      {
         if(list1 == null || list2 == null) return false;

         // Rýchle zlyhanie, ak sú zoznamy rôznych veľkostí
         if (list1.Count != list2.Count) return false;

         // Konvertujeme list1 na HashSet pre efektívne vyhľadávanie
         HashSet<string> hashSet = new HashSet<string>(list1.Select(ep => EndPointToString(ep)));

         // Porovnávame každý EndPoint v list2, či existuje v HashSet
         foreach (var endPoint in list2)
         {
            var epString = EndPointToString(endPoint);
            if (!hashSet.Contains(epString)) return false;
         }

         return true; // Všetky EndPointy boli nájdené
      }

      // Pomocná metóda na konverziu EndPoint na reťazec
      private string EndPointToString(EndPoint endPoint)
      {
         if (endPoint is IPEndPoint ipEndPoint)
         {
            return $"{ipEndPoint.Address}:{ipEndPoint.Port}";
         }
         // Tu by ste mohli pridať ďalšie prípady pre iné typy EndPoint, ak je to potrebné
         throw new ArgumentException("Unsupported EndPoint type", nameof(endPoint));
      }
   }
}
