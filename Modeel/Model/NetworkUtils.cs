using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using Modeel.Log;

namespace Modeel.Model
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
            Logger.WriteLog("No network adapters with an IPv4 address in the system!");
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
                Console.WriteLine($"Error while fetching public IP address: {ex.Message}");
                return null;
            }
        }
    }
}
