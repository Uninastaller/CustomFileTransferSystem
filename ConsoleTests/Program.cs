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



//MyConfigManager.EndApplication();

List<string> aa = new List<string>();
aa.Add("aabb");
aa.Add("abaa");
aa.Add("a");
aa.Add("x");
aa.Add("xax");
aa.Add("xba");
aa = aa.Order(StringComparer.Ordinal).ToList();
Console.ReadLine();


