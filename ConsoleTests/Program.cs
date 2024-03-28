using Common.Model;
using ConfigManager;
using SslTcpSession.BlockChain;
using System.Net;

MyConfigManager.StartApplication();

IPAddress publicAddress = await NetworkUtils.GetPublicIPAddress();
IPAddress madeAdress = IPAddress.Parse($"192.168.1.241");

NodeDiscovery.SetIpAdresses(NetworkUtils.GetLocalIPAddress(), publicAddress);
NodeDiscovery.StartApplication();
NodeDiscovery.SaveNodes();

IPEndPoint? myLocalEndpoint = NetworkUtils.GetMyLocalEndpont();
if (!NetworkUtils.TryGetMyLocalCustomEndpoint(out IpAndPortEndPoint? customLocalEndPoint)) return;

Console.WriteLine("Start");



// CLIENT 1

Blockchain MyChain = new Blockchain();

Console.WriteLine(MyChain.Add_AddCredit(50));
Console.WriteLine(MyChain.Add_AddCredit(12.7));

// CLIENT 2

Blockchain MyChain2 = new Blockchain();

// CLIENT 3

Blockchain MyChain3 = new Blockchain();

// CLIENT 4

Blockchain MyChain4 = new Blockchain();

// CLIENT 5

Blockchain MyChain5 = new Blockchain();

// CLIENT 6

Blockchain MyChain6 = new Blockchain();


Console.WriteLine(MyChain.ToJson(true));
Console.WriteLine(Blockchain.FromJson(MyChain.ToJson(true), out List<Block>? chain));

Console.ReadLine();
//MyConfigManager.EndApplication();

