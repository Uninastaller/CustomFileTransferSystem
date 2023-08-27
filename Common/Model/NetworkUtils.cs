using Logger;
using System;
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
    }
}
