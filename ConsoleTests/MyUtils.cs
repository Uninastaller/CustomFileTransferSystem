using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Diagnostics;

namespace ConsoleTests
{
    public class MyUtils
    {
        public static void DeleteFolderContents(string folderPath)
        {
            if (Directory.Exists(folderPath))
            {
                foreach (string file in Directory.GetFiles(folderPath))
                {
                    File.Delete(file);
                }

                foreach (string directory in Directory.GetDirectories(folderPath))
                {
                    Directory.Delete(directory, true);
                }
            }
        }

        public static void CloneDirectory(string sourceDir, string destinationDir)
        {
            Directory.CreateDirectory(destinationDir);

            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string destFile = Path.Combine(destinationDir, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }

            foreach (string subDir in Directory.GetDirectories(sourceDir))
            {
                string newDestinationDir = Path.Combine(destinationDir, Path.GetFileName(subDir));
                CloneDirectory(subDir, newDestinationDir);
            }
        }

        public static void ChangeXmlValue(string pathToXml, string key, string value)
        {
            XDocument doc = XDocument.Load(pathToXml);

            XElement element = doc.Descendants("add").FirstOrDefault(el => (string)el.Attribute("key") == key);

            if (element != null)
            {
                element.SetAttributeValue("value", value);

                doc.Save(pathToXml);
            }
            else
            {
                Console.WriteLine($"key: {key}, was not found!");
            }
        }

        public static void ChangeJsonStringValue(string filePath, string keyToChange, string newValue)
        {

            string jsonString = File.ReadAllText(filePath);

            var data = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, object>>>(jsonString);

            if (data != null)
            {
                foreach (var item in data)
                {
                    string key = item.Key;
                    Dictionary<string, object> objectData = item.Value;

                    objectData[keyToChange] = newValue; 

                    string modifiedJsonString = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(filePath, modifiedJsonString);
                    break;
                }
            }
        }

        public static void ChangeJsonIntValue(string filePath, string keyToChange, int newValue)
        {

            string jsonString = File.ReadAllText(filePath);

            var data = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, object>>>(jsonString);

            if (data != null)
            {
                foreach (var item in data)
                {
                    string key = item.Key;
                    Dictionary<string, object> objectData = item.Value;

                    objectData[keyToChange] = newValue;

                    string modifiedJsonString = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(filePath, modifiedJsonString);
                    break;
                }
            }
        }

        public static IPAddress GetLocalIPAddress()
        {
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip;
                }
            }
            return IPAddress.None;
        }

        public static void LaunchFirstExeInDirectory(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                Console.WriteLine("Invalid directory: " + directoryPath);
                return;
            }

            string[] exeFiles = Directory.GetFiles(directoryPath, "*.exe");

            if (exeFiles.Length == 0)
            {
                Console.WriteLine("No .exe file found in: " + directoryPath);
                return;
            }

            string exeToLaunch = exeFiles[0];

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = exeToLaunch;
            startInfo.WorkingDirectory = directoryPath;

            try
            {
                Process process = new Process();
                process.StartInfo = startInfo;
                process.Start();
                Console.WriteLine($"Starting: {exeToLaunch}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not start {exeToLaunch}: {ex.Message}");
            }
        }
    }
}
