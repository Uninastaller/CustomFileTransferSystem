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

      public bool Add_Add(Guid fileId, string fileHash, EndPoint myEndPoint)
      {

         Block? block = FindLatestFileUpdate(fileId);
         if (block == null || !block.FileHash.Equals(fileHash))
         {
            return false;
         }

         Block newBlock = new Block
         {
            Index = Chain.Count,
            Timestamp = DateTime.UtcNow,
            FileHash = fileHash,
            PreviousHash = Chain[Chain.Count - 1].Hash,
            Transaction = TransactionType.Add,
            FileID = fileId,
         };

         if (block.Transaction == TransactionType.AddRequest)
         {
            newBlock.FileLocations = new List<EndPoint>() { myEndPoint };
         }
         else if (block.Transaction == TransactionType.Add || block.Transaction == TransactionType.Remove)
         {
            newBlock.FileLocations = block.FileLocations.ToList();
            newBlock.FileLocations.Add(myEndPoint);
         }

         newBlock.ComputeHash();
         
         return AddBlock(newBlock);
      }

      public bool Add_Remove(Guid fileId, string fileHash)
      {

         EndPoint myEndpoint = new IPEndPoint(IPAddress.Parse($"192.168.1.241"), 8080);

         Block? block = FindLatestFileUpdate(fileId);
         if (block == null || block.Transaction == TransactionType.AddRequest)
         {
            return false;
         }

         Block newBlock = new Block
         {
            Index = Chain.Count,
            Timestamp = DateTime.UtcNow,
            FileHash = fileHash,
            PreviousHash = Chain[Chain.Count - 1].Hash,
            Transaction = TransactionType.Remove,
            FileID = fileId,
         };


         newBlock.FileLocations = block.FileLocations.ToList();
         int? removed = newBlock.FileLocations.RemoveAll(endpoint => endpoint.ToString().Equals(myEndpoint));

         if (removed.HasValue && removed > 0)
         {
            newBlock.ComputeHash();            
            return AddBlock(newBlock);
         }

         return false;
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

      public bool Add_AddRequest(string fileHash, out Guid fileId)
      {
         fileId = Guid.NewGuid();

         Block newBlock = new Block
         {
            Index = Chain.Count,
            Timestamp = DateTime.UtcNow,
            FileHash = fileHash,
            PreviousHash = Chain[Chain.Count - 1].Hash,
            Transaction = TransactionType.AddRequest,
            FileID = fileId,
         };

         newBlock.ComputeHash();
         newBlock.SignHash();
         
         return AddBlock(newBlock);
      }

      public bool Add_RemoveRequest(Guid fileId)
      {
         Block? fileBlock = Chain.FirstOrDefault(b => b.FileID == fileId);

         if (fileBlock != null)
         {
            Block newBlock = new Block
            {
               Index = Chain.Count,
               Timestamp = DateTime.UtcNow,
               FileHash = fileBlock.FileHash,
               PreviousHash = Chain[Chain.Count - 1].Hash,
               Transaction = TransactionType.RemoveRequest,
               FileLocations = fileBlock.FileLocations,
               FileID = fileId,
            };

            fileBlock.ComputeHash();
            return AddBlock(newBlock);
         }
         return false;
      }

      public bool AddBlock(Block newBlock)
      {
         if (IsNewBlockValid(newBlock))
         {
            Chain.Add(newBlock);
            return true;
         }
         return false;
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

      public bool IsNewBlockValid(Block newBlock)
      {

         if (newBlock.PreviousHash != Chain[Chain.Count-1].Hash)
         {
            return false;
         }

         return true;
      }
   }
}
