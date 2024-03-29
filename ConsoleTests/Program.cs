using Common.Model;
using ConfigManager;
using SslTcpSession.BlockChain;
using System.Net;

MyConfigManager.StartApplication();

IPAddress publicAddress = await NetworkUtils.GetPublicIPAddress();
IPAddress madeAdress = IPAddress.Parse($"192.168.1.241");

NodeDiscovery.SetIpAddresses(NetworkUtils.GetLocalIPAddress(), publicAddress);
NodeDiscovery.StartApplication();
NodeDiscovery.SaveNodes();

IPEndPoint? myLocalEndpoint = NetworkUtils.GetMyLocalEndpont();
if (!NetworkUtils.TryGetMyLocalCustomEndpoint(out IpAndPortEndPoint? customLocalEndPoint)) return;

Console.WriteLine("Start");




Console.ReadLine();
//MyConfigManager.EndApplication();

