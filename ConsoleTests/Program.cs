using BlockChain;
using ConfigManager;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;

//Blockchain MyChain = new Blockchain();

//string fileHash = "abc";

//Guid a = MyChain.Add_AddRequest(fileHash);

//MyChain.Add_Add(a, fileHash);

//Console.WriteLine($"Valid: {MyChain.IsValid()}");

MyConfigManager.StartApplication();

//NodeDiscovery.AddNode(new Node() { Address = "192.168.1.1", Port = 8080, PublicKey = "MIIBCgKCAQEAvjOiore0nO4+JHhKTMTsj9uv04F2jnVCmsVfDq2c1Fe95+ieLVpiQCKaiSlijFJP/6t1KcOI+4FG0Ami9tdlc/3qs0Oz+IAQ4XrAaVg68LW+D4gMUjPjJhfDONHXWBvRCe8yiuBmMOgQkcrllER6KyW4sZGoiu5d5zp6TQhKSNUCk4VdJbMLRHHocZfZ6BpH0mGTe4AS61aKyO/phOJ0WulA8a1E9cj/psokN59OKLlPv0HdlmlxgdnPQqOKTqMJkGbqoBrtCZ9De9x1Ic5t/PclpDV+/bWTsaQ7DC3ASMJMKa9ouRVj3yhiAsVvAbucsY40oyxs/jdEoLQ+TIIu+QIDAQAB" });
//NodeDiscovery.AddNode(new Node() { Address = "192.168.1.2", Port = 8080, PublicKey = "MIIBCgKCAQEAvjOiore0nO4+JHhKTMTsj9uv04F2jnVCmsVfDq2c1Fe95+ieLVpiQCKaiSlijFJP/6t1KcOI+4FG0Ami9tdlc/3qs0Oz+IAQ4XrAaVg68LW+D4gMUjPjJhfDONHXWBvRCe8yiuBmMOgQkcrllER6KyW4sZGoiu5d5zp6TQhKSNUCk4VdJbMLRHHocZfZ6BpH0mGTe4AS61aKyO/phOJ0WulA8a1E9cj/psokN59OKLlPv0HdlmlxgdnPQqOKTqMJkGbqoBrtCZ9De9x1Ic5t/PclpDV+/bWTsaQ7DC3ASMJMKa9ouRVj3yhiAsVvAbucsY40oyxs/jdEoLQ+TIIu+QIDAQAB" });
//NodeDiscovery.SaveNodes();

var a = NodeDiscovery.GetAllNodes();


var b = Certificats.GetCertificate("Node02", Certificats.CertificateType.Node);
var c = Certificats.ExtractPublicKey(b);

Console.ReadLine();

