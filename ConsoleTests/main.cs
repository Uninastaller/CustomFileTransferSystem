using Client.Windows;
using Common.Enum;
using Common.Model;
using ConfigManager;
using ConsoleTests;

int _basePort = 34200;

Console.WriteLine("Press any key to start!");
Console.ReadLine();

Console.WriteLine("Starting program!");
MyConfigManager.StartApplication();

// READ PROGRAM PATH
string programPath = MyConfigManager.GetConfigStringValue("programPath");
if (string.IsNullOrEmpty(programPath))
{
   Console.WriteLine($"programPath directory is not in the config!");
   return;
}
if (!Directory.Exists(programPath))
{
   Console.WriteLine($"program path directory: {programPath}, does not exist!");
   return;
}
Console.WriteLine($"programPath: {programPath}");


// READ CLONE DESTINATIONS PATH
string clonedInstancePath = MyConfigManager.GetConfigStringValue("clonedInstancePath");
if (string.IsNullOrEmpty(clonedInstancePath))
{
   Console.WriteLine($"clonedInstancePath directory is not in the config!");
   return;
}
if (!Directory.Exists(clonedInstancePath))
{
   Directory.CreateDirectory(clonedInstancePath);
}
Console.WriteLine($"clonedInstancePath: {clonedInstancePath}");

// DELETING CONTENT OF DIRECTORY
MyUtils.DeleteFolderContents(clonedInstancePath);
Console.WriteLine($"Directory: {clonedInstancePath}, cleaned!");


// READ NUMBER OF INSTANCES
if (!MyConfigManager.TryGetIntConfigValue("numberOfInstances", out int numberOfInstances))
{
   Console.WriteLine($"numberOfInstances is not in the config or is not integer!");
   return;
}
Console.WriteLine($"numberOfInstances: {numberOfInstances}");

// CREATING DIRECTORIES FOR INSTANCES
List<string> instances = new List<string>();
for (int i = 0; i < numberOfInstances; i++)
{
   string instanceDirectory = Path.Combine(clonedInstancePath, "instance" + i);
   Console.WriteLine($"Generating directory: {instanceDirectory}");
   Directory.CreateDirectory(instanceDirectory);
   instances.Add(instanceDirectory);
}

// CLONING PROGRAMS
foreach (string instanceDirectory in instances)
{
   Console.WriteLine($"Cloning program to: {instanceDirectory}");
   MyUtils.CloneDirectory(programPath, instanceDirectory);
}

// READ UPDATE CONFIG
if (!MyConfigManager.TryGetBoolConfigValue("updateConfig", out bool updateConfig))
{
   Console.WriteLine($"updateConfig is not in the config!");
   return;
}
Console.WriteLine($"updateConfig: {updateConfig}");

if (updateConfig)
{
   // READ CONFIG NAME
   string configName = MyConfigManager.GetConfigStringValue("configName");
   if (string.IsNullOrEmpty(configName))
   {
      Console.WriteLine($"configName is not in the config!");
      return;
   }
   Console.WriteLine($"configName: {configName}");

   for (int i = 0; i < instances.Count; i++)
   {
      string instanceDirectory = instances[i];
      string configPath = Path.Combine(instanceDirectory, configName);

      string key = "LoggingDirectory";
      string value = "Logs";
      Console.WriteLine($"XML Updating {key} to {value}");
      MyUtils.ChangeXmlValue(configPath, key, value);

      key = "CertificateDirectory";
      value = "";
      Console.WriteLine($"XML Updating {key} to {value}");
      MyUtils.ChangeXmlValue(configPath, key, value);

      key = "UploadingServerPort";
      value = (_basePort + i).ToString();
      Console.WriteLine($"XML Updating {key} to {value}");
      MyUtils.ChangeXmlValue(configPath, key, value);

      key = "Instance";
      value = i.ToString();
      Console.WriteLine($"XML Updating {key} to {value}");
      MyUtils.ChangeXmlValue(configPath, key, value);
   }
}

// READ UPDATE NODES
if (!MyConfigManager.TryGetBoolConfigValue("updateNodes", out bool updateNodes))
{
   Console.WriteLine($"updateNodes is not in the config!");
   return;
}
Console.WriteLine($"updateNodes: {updateNodes}");

if (updateNodes)
{
   // READ NODES FILE NAME
   string nodesFileName = MyConfigManager.GetConfigStringValue("nodesFileName");
   if (string.IsNullOrEmpty(nodesFileName))
   {
      Console.WriteLine($"nodesFileName is not in the config!");
      return;
   }
   Console.WriteLine($"nodesFileName: {nodesFileName}");

   string ipAddress = MyUtils.GetLocalIPAddress().ToString();
   for (int i = 0; i < instances.Count; i++)
   {
      string instanceDirectory = instances[i];
      string filePath = Path.Combine(instanceDirectory, nodesFileName);

      string key = "Address";
      Console.WriteLine($"Json Updating {key} to {ipAddress}");
      MyUtils.ChangeJsonStringValue(filePath, key, ipAddress);

      key = "Port";
      int value = _basePort + 1 + i;
      Console.WriteLine($"Json Updating {key} to {value}");
      MyUtils.ChangeJsonIntValue(filePath, key, value);
   }
}

// READ UPDATE OFFERING FILES
if (!MyConfigManager.TryGetBoolConfigValue("updateOfferingFiles", out bool updateOfferingFiles))
{
   Console.WriteLine($"updateOfferingFiles is not in the config!");
   return;
}
Console.WriteLine($"updateOfferingFiles: {updateOfferingFiles}");

if (updateOfferingFiles)
{
   // READ OFFERING FILES DIRECTORY NAME
   string offeringFilesDirectoryName = MyConfigManager.GetConfigStringValue("offeringFiles");
   if (string.IsNullOrEmpty(offeringFilesDirectoryName))
   {
      Console.WriteLine($"offeringFiles is not in the config!");
      return;
   }
   Console.WriteLine($"offeringFiles: {offeringFilesDirectoryName}");

   string ipAddress = MyUtils.GetLocalIPAddress().ToString();

   if (!MyConfigManager.TryGetLongConfigValue("offeringFilesSize", out Int64 size))
   {
      Console.WriteLine($"offeringFilesSize is not in the config!");
      return;
   }
   Console.WriteLine($"offeringFilesSize: {size}");

   string name = MyConfigManager.GetConfigStringValue("offeringFilesName");
   Console.WriteLine($"offeringFilesName: {name}");
   if (string.IsNullOrEmpty(name))
   {
      return;
   }

   if (!MyConfigManager.TryGetIntConfigValue("offeringFileSocketType", out int socketType))
   {
      Console.WriteLine($"offeringFileSocketType is not in the config!");
      return;
   }
   TypeOfServerSocket typeOfServerSocet = (TypeOfServerSocket)socketType;
   Console.WriteLine($"offeringFileSocketType: {typeOfServerSocet}");

   OfferingFileDto ofd = new OfferingFileDto();
   ofd.FileName = name;
   ofd.FileSize = size;

   string ipAddres = MyUtils.GetLocalIPAddress().ToString();

   for (int i = 0; i < instances.Count; i++)
   {
      ofd.EndpointsAndProperties.TryAdd($"{ipAddres}:{_basePort + i}", new EndpointProperties() { TypeOfServerSocket = typeOfServerSocet });
   }
   // SAVE
   string fileName = ofd.OfferingFileIdentificator + MainWindow.cftsFileExtensions;

   if (!Directory.Exists(offeringFilesDirectoryName))
   {
      Directory.CreateDirectory(offeringFilesDirectoryName);
   }
   File.WriteAllText(Path.Combine(offeringFilesDirectoryName, fileName), ofd.GetJson());
}

Console.WriteLine("\nDONE!");

Console.WriteLine("\nStart all instances? Y/N");
string? output = Console.ReadLine();
if (output != null)
{
   if (output == "Y")
   {
      for (int i = 0; i < instances.Count; i++)
      {
         Console.WriteLine($"Starting: {i + 1}");
         string instance = instances[i];
         MyUtils.LaunchFirstExeInDirectory(instance);
         Thread.Sleep(1000);
      }
   }
}


MyConfigManager.EndApplication();
