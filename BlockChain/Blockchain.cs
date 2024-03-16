using ConfigManager;
using System.Net;

namespace BlockChain
{
   public class Blockchain
   {
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

      public AddBlockResponses Add_AddCredit(double creditValueToAdd)
      {
         if (creditValueToAdd <= 0)
         {
            return AddBlockResponses.INVALID_CREDIT_VALUE_TO_ADD;
         }

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
            Transaction = TransactionType.AddCredit,
            NodeId = NodeDiscovery.GetMyNode().Id,
            CreditChange = creditValueToAdd,
            NewCreditVaue = newCreditValue,
         };

         newBlock.ComputeHash();
         newBlock.SignHash();
         return AddBlock(newBlock);
      }

      public AddBlockResponses Add_AddFile(Guid fileId, string fileHash, EndPoint myEndPoint)
      {

         Block? block = FindLatestFileUpdate(fileId);
         if (block == null || !block.FileHash.Equals(fileHash))
         {
            return AddBlockResponses.FILE_DOES_NOT_EXIST;
         }

         if (IsFileFlaggedToBeRemoved(fileId))
         {
            return AddBlockResponses.FILE_IS_FLAGED_TO_BE_REMOVED;
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
            Transaction = TransactionType.AddFile,
            FileID = fileId,
            NodeId = NodeDiscovery.GetMyNode().Id,
            CreditChange = 0,
            NewCreditVaue = newCreditValue
         };

         if (block.Transaction == TransactionType.AddFileRequest)
         {
            newBlock.FileLocations = new List<EndPoint>() { myEndPoint };
         }
         else if (block.Transaction == TransactionType.AddFile || block.Transaction == TransactionType.RemoveFile)
         {

            if (block.FileLocations.Exists(ep => ep.ToString().Equals(myEndPoint.ToString())))
            {
               return AddBlockResponses.YOUR_ENDPOINT_IS_ALREADY_ON_LIST;
            }

            newBlock.FileLocations = block.FileLocations.ToList();
            newBlock.FileLocations.Add(myEndPoint);
         }

         newBlock.ComputeHash();
         newBlock.SignHash();

         return AddBlock(newBlock);
      }

      public AddBlockResponses Add_RemoveFile(Guid fileId, string fileHash, EndPoint endPoint)
      {

         Block? block = FindLatestFileUpdate(fileId);
         if (block == null || block.Transaction == TransactionType.AddFileRequest)
         {
            return AddBlockResponses.FILE_DOES_NOT_EXIST;
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
            Transaction = TransactionType.RemoveFile,
            FileID = fileId,
            NodeId = NodeDiscovery.GetMyNode().Id,
            CreditChange = 0,
            NewCreditVaue = newCreditValue
         };


         newBlock.FileLocations = block.FileLocations.ToList();
         int? removed = newBlock.FileLocations.RemoveAll(ep => ep.ToString().Equals(endPoint));

         if (removed.HasValue && removed > 0)
         {
            newBlock.ComputeHash();
            newBlock.SignHash();
            return AddBlock(newBlock);
         }

         return AddBlockResponses.YOUR_ENDPOINT_IS_NOT_ON_LIST;
      }

      private Block? FindLatestFileUpdate(Guid fileId)
      {
         for (int i = Chain.Count() - 1; i >= 0; i++)
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
         return Chain.Exists(block => block.FileID == fileId && block.Transaction == TransactionType.RemoveFileRequest);
      }

      private Block? FindActualCreditValueOfNode(Guid nodeId)
      {
         for (int i = Chain.Count() - 1; i >= 0; i++)
         {
            if (Chain[i].NodeId == nodeId)
            {
               return Chain[i];
            }
         }
         return null;
      }

      public AddBlockResponses Add_AddFileRequest(string fileHash, out Guid fileId)
      {
         fileId = Guid.NewGuid();

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
            Transaction = TransactionType.AddFileRequest,
            FileID = fileId,
            NodeId = NodeDiscovery.GetMyNode().Id,
            CreditChange = 0,
            NewCreditVaue = newCreditValue
         };

         newBlock.ComputeHash();
         newBlock.SignHash();

         return AddBlock(newBlock);
      }

      public AddBlockResponses Add_RemoveFileRequest(Guid fileId)
      {
         Block? fileBlock = Chain.FirstOrDefault(b => b.FileID == fileId);
         if (fileBlock == null)
         {
            return AddBlockResponses.FILE_DOES_NOT_EXIST;
         }

         if (IsFileFlaggedToBeRemoved(fileId))
         {
            return AddBlockResponses.FILE_IS_FLAGED_TO_BE_REMOVED;
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
            Transaction = TransactionType.RemoveFileRequest,
            FileLocations = fileBlock.FileLocations,
            FileID = fileId,
            NodeId = NodeDiscovery.GetMyNode().Id,
            CreditChange = 0,
            NewCreditVaue = newCreditValue
         };

         fileBlock.ComputeHash();
         newBlock.SignHash();
         return AddBlock(newBlock);

      }

      public AddBlockResponses AddBlock(Block newBlock)
      {
         if (IsNewBlockValid(newBlock) == BlockFromBlockChainValidationResult.VALID)
         {
            Chain.Add(newBlock);
            return AddBlockResponses.SUCCES;
         }
         return AddBlockResponses.VALIDATION_TEST_FAILD;
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

      public BlockFromBlockChainValidationResult IsNewBlockValid(Block newBlock)
      {
         // Check pevious hash
         if (newBlock.PreviousHash != Chain[Chain.Count - 1].Hash)
         {
            return BlockFromBlockChainValidationResult.INVALID_PREVIOUS_HASH;
         }

         // Check signed hash
         if (NodeDiscovery.GetNode(newBlock.NodeId, out Node? node))
         {
            if (!newBlock.VerifyHash(node.PublicKey))
            {
               return BlockFromBlockChainValidationResult.INVALID_SIGN;
            }
         }
         else
         {
            return BlockFromBlockChainValidationResult.UNABLE_TO_DECIDE;
         }

         // Check transaction
         switch (newBlock.Transaction)
         {
            case TransactionType.AddFile:
               break;
            case TransactionType.AddFileRequest:
               return BlockFromBlockChainValidationResult.VALID;
            case TransactionType.RemoveFile:
               break;
            case TransactionType.RemoveFileRequest:
               return BlockFromBlockChainValidationResult.VALID;
            default:
               break;
         }

         return BlockFromBlockChainValidationResult.UNABLE_TO_DECIDE;
      }
   }
}
