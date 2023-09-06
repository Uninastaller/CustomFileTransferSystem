using Logger;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Common.Model
{
    public class NetworkUtils
    {
        public static IPAddress? GetLocalIPAddress()
        {
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip;
                }
            }
            Log.WriteLog(LogLevel.ERROR, "No network adapters with an IPv4 address in the system!");
            return null;
        }

        public static async Task<IPAddress> GetPublicIPAddress()
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    string apiUrl = "https://api.ipify.org?format=text";
                    var response = await httpClient.GetAsync(apiUrl);
                    response.EnsureSuccessStatusCode();
                    string ipString = await response.Content.ReadAsStringAsync();
                    return IPAddress.Parse(ipString.Trim()); // Remove any leading/trailing whitespaces
                }
            }
            catch (Exception ex)
            {
                // Handle any exceptions that might occur during the request
                Log.WriteLog(LogLevel.ERROR, $"Error while fetching public IP address: {ex.Message}");
                return null;
            }
        }

        public static bool TryGetIPEndPointFromString(string ipAddressAndPort, [MaybeNullWhen(false)] out IPEndPoint iPEndPoint)
        {
            iPEndPoint = null;

            // Split the string by the ':' character to separate the IP address and port
            string[] parts = ipAddressAndPort.Split(':');
            if (parts.Length != 2)
            {
                Log.WriteLog(LogLevel.WARNING, $"Invalid format of ipArressAndPort: {ipAddressAndPort}, Expected format is IPAddress:Port");
                return false;
            }

            // Parse the IP address part
            IPAddress ipAddress;
            if (!IPAddress.TryParse(parts[0], out ipAddress))
            {
                Log.WriteLog(LogLevel.WARNING, $"Invalid IP address: {ipAddress}");
                return false;
            }

            // Parse the port part
            int port;
            if (!int.TryParse(parts[1], out port))
            {
                Log.WriteLog(LogLevel.WARNING, $"Invalid port number: {port}");
                return false;
            }

            // Create and return the IPEndPoint object
            iPEndPoint = new IPEndPoint(ipAddress, port);
            return true;
        }

        public static bool IsPortFree(int port, IPAddress ipAddress)
        {
            bool isFree = true;
            TcpListener? listener = null;

            try
            {
                listener = new TcpListener(ipAddress, port);
                listener.Start();
            }
            catch (SocketException)
            {
                isFree = false;
            }
            finally
            {
                listener?.Stop();
            }

            return isFree;
        }

        public static int GetRandomFreePort(IPAddress ipAddress)
        {
            int port = 0;
            TcpListener? listener = null;

            try
            {
                listener = new TcpListener(ipAddress, 0);
                listener.Start();
                port = ((IPEndPoint)listener.LocalEndpoint).Port;
            }
            catch (SocketException ex)
            {
                // Log the exception or handle it appropriately
                Console.WriteLine($"SocketException: {ex}");
            }
            finally
            {
                listener?.Stop();
            }

            // Log the random free port generated
            Console.WriteLine($"Random free port generated: {port}");

            return port;
        }
    }
}
