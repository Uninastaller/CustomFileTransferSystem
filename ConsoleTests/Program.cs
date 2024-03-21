using BlockChain;
using ConfigManager;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using Common.Model;
using System.Runtime.CompilerServices;

MyConfigManager.StartApplication();

IPAddress publicAddress = await NetworkUtils.GetPublicIPAddress();
IPAddress madeAdress = IPAddress.Parse($"192.168.1.241");

NodeDiscovery.SetIpAdresses(NetworkUtils.GetLocalIPAddress(), publicAddress);
NodeDiscovery.StartApplication();

IPEndPoint? myLocalEndpoint = NetworkUtils.GetMyLocalEndpont();
if (!NetworkUtils.TryGetMyLocalCustomEndpoint(out IpAndPortEndPoint? customLocalEndPoint)) return;

Console.WriteLine("Start");




Blockchain MyChain = new Blockchain();

Console.WriteLine(MyChain.Add_AddCredit(50));
Console.WriteLine(MyChain.Add_AddCredit(15.7));
Console.WriteLine(MyChain.Add_AddCredit(2));

string fileHash = "abc";
Console.WriteLine(MyChain.Add_RemoveFileRequest(Guid.NewGuid()));
Console.WriteLine(MyChain.Add_AddFileRequest(fileHash, out Guid fileGuid));
Console.WriteLine(MyChain.Add_RemoveFile(fileGuid, fileHash, customLocalEndPoint));
Console.WriteLine(MyChain.Add_AddFile(fileGuid, fileHash, customLocalEndPoint));
Console.WriteLine(MyChain.Add_AddFile(fileGuid, fileHash, customLocalEndPoint));
Console.WriteLine(MyChain.Add_RemoveFile(fileGuid, fileHash, customLocalEndPoint));
Console.WriteLine(MyChain.Add_RemoveFileRequest(fileGuid));
Console.WriteLine(MyChain.Add_AddFile(fileGuid, fileHash, customLocalEndPoint));


string publicKey = Certificats.ExportPublicKeyToJSON(Certificats.GetCertificate("", Certificats.CertificateType.Node));
bool success = MyChain.Chain[1].VerifyHash(publicKey);

//MyChain.Add_Add(a, fileHash, new IPEndPoint(publicAddress, 8080));

//Console.WriteLine($"Valid: {MyChain.IsBlockChainValid()}");


////NodeDiscovery.AddNode(new Node() { Address = "192.168.1.1", Port = 8080, PublicKey = "MIIBCgKCAQEAvjOiore0nO4+JHhKTMTsj9uv04F2jnVCmsVfDq2c1Fe95+ieLVpiQCKaiSlijFJP/6t1KcOI+4FG0Ami9tdlc/3qs0Oz+IAQ4XrAaVg68LW+D4gMUjPjJhfDONHXWBvRCe8yiuBmMOgQkcrllER6KyW4sZGoiu5d5zp6TQhKSNUCk4VdJbMLRHHocZfZ6BpH0mGTe4AS61aKyO/phOJ0WulA8a1E9cj/psokN59OKLlPv0HdlmlxgdnPQqOKTqMJkGbqoBrtCZ9De9x1Ic5t/PclpDV+/bWTsaQ7DC3ASMJMKa9ouRVj3yhiAsVvAbucsY40oyxs/jdEoLQ+TIIu+QIDAQAB" });
////NodeDiscovery.AddNode(new Node() { Address = "192.168.1.2", Port = 8080, PublicKey = "MIIBCgKCAQEAvjOiore0nO4+JHhKTMTsj9uv04F2jnVCmsVfDq2c1Fe95+ieLVpiQCKaiSlijFJP/6t1KcOI+4FG0Ami9tdlc/3qs0Oz+IAQ4XrAaVg68LW+D4gMUjPjJhfDONHXWBvRCe8yiuBmMOgQkcrllER6KyW4sZGoiu5d5zp6TQhKSNUCk4VdJbMLRHHocZfZ6BpH0mGTe4AS61aKyO/phOJ0WulA8a1E9cj/psokN59OKLlPv0HdlmlxgdnPQqOKTqMJkGbqoBrtCZ9De9x1Ic5t/PclpDV+/bWTsaQ7DC3ASMJMKa9ouRVj3yhiAsVvAbucsY40oyxs/jdEoLQ+TIIu+QIDAQAB" });
////NodeDiscovery.SaveNodes();

//var a = NodeDiscovery.GetAllNodes();


//var b = Certificats.GetCertificate("Node02", Certificats.CertificateType.Node);
//var c = Certificats.ExtractPublicKey(b);

Console.WriteLine(MyChain.ToJson(true));
Console.WriteLine(Blockchain.FromJson(MyChain.ToJson(true), out List<Block>? chain));

Console.ReadLine();
//MyConfigManager.EndApplication();

