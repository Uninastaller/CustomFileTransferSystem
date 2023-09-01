using System;
using System.Collections.Concurrent;
using System.Configuration;
using System.IO;
using System.Threading;

namespace ConfigManager
{
    public static class MyConfigManager
    {

        #region Properties



        #endregion Properties

        #region PublicFields



        #endregion PublicFields

        #region PrivateFields

        private static FileSystemWatcher? _configWatcher;
        private static ConcurrentDictionary<string, string> _configValues = new ConcurrentDictionary<string, string>();
        private static string _configPath = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None).FilePath;
        private static string? _configDirectory = Path.GetDirectoryName(_configPath);

        #endregion PrivateFields

        #region ProtectedFields



        #endregion ProtectedFields

        #region Ctor



        #endregion Ctor

        #region PublicMethods

        public static void StartApplication()
        {
            LoadConfigValues();
            StartConfigWatcher();
        }

        public static void EndApplication()
        {
            StopConfigWatcher();
        }

        public static string GetConfigValue(string key)
        {
            if (_configValues.TryGetValue(key, out string? value))
            {
                return value;
            }
            return string.Empty;
        }

        public static T GetConfigValue<T>(string key) where T : struct
        {
            if (_configValues.TryGetValue(key, out string? value))
            {
                System.Reflection.MethodInfo? parseMethod = typeof(T).GetMethod("Parse", new[] { typeof(string) });
                if (parseMethod != null)
                {
                    object? result = parseMethod.Invoke(null, new object[] { value });
                    if (result != null && result is T Tresult)
                    {
                        return Tresult;
                    }
                }
            }
            return default; // Return the default value of type T
        }

        public static bool TryGetConfigValue<T>(string key, out T Tout) where T : struct
        {
            if (_configValues.TryGetValue(key, out string? value))
            {
                System.Reflection.MethodInfo? tryParseMethod = typeof(T).GetMethod("TryParse", new[] { typeof(string), typeof(T).MakeByRefType() });
                if (tryParseMethod != null)
                {
                    object[] parameters = new object[] { value, null };
                    object? result = tryParseMethod.Invoke(null, parameters);
                    if (result != null && result is bool boolResult && boolResult)
                    {
                        Tout = (T)parameters[1];
                        return true;
                    }
                }
            }
            Tout = default;
            return false;
        }

        public static bool TryGetIntConfigValue(string key, out Int32 outInt)
        {
            if (_configValues.TryGetValue(key, out string? value))
            {
                if (Int32.TryParse(value, out outInt))
                {
                    return true;
                }
            }
            outInt = default;
            return false;
        }

        public static bool TryGetLongConfigValue(string key, out Int64 outLong)
        {
            if (_configValues.TryGetValue(key, out string? value))
            {
                if (Int64.TryParse(value, out outLong))
                {
                    return true;
                }
            }
            outLong = default;
            return false;
        }

        public static bool TryGetBoolConfigValue(string key, out bool outBool)
        {
            if (_configValues.TryGetValue(key, out string? value))
            {
                if (bool.TryParse(value, out outBool))
                {
                    return true;
                }
            }
            outBool = default;
            return false;
        }

        #endregion PublicMethods

        #region PrivateMethods

        private static void LoadConfigValues()
        {

            _configValues.Clear();

            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            KeyValueConfigurationCollection settings = config.AppSettings.Settings;

            foreach (KeyValueConfigurationElement setting in settings)
            {
                _configValues.TryAdd(setting.Key, setting.Value);
            }

        }

        private static void StartConfigWatcher()
        {
            if (!string.IsNullOrEmpty(_configDirectory))
            {
                _configWatcher = new FileSystemWatcher(_configDirectory, Path.GetFileName(_configPath));
                _configWatcher.NotifyFilter = NotifyFilters.LastWrite;
                _configWatcher.Changed += OnConfigFileChange;
                _configWatcher.EnableRaisingEvents = true;
            }
        }

        private static void StopConfigWatcher()
        {
            if (_configWatcher != null)
            {
                _configWatcher.Changed -= OnConfigFileChange;
                _configWatcher.Dispose();
                _configWatcher = null;
            }
        }

        #endregion PrivateMethods

        #region ProtectedMethods



        #endregion ProtectedMethods

        #region Events

        public static event EventHandler? ConfigChanged;

        private static void OnConfigFileChange(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType == WatcherChangeTypes.Changed)
            {
                Thread.Sleep(100); // Give the file some time to be completely written

                LoadConfigValues();
                ConfigChanged?.Invoke(null, EventArgs.Empty);
            }
        }

        #endregion Events

        #region OverridedMethods



        #endregion OverridedMethods
    }
}

//using System;
//using System.Collections.Concurrent;
//using System.Configuration;
//using System.IO;
//using System.Reflection;
//using System.Threading;

//namespace ConfigManager
//{
//    public static class MyConfigManager
//    {

//        #region Properties



//        #endregion Properties

//        #region PublicFields



//        #endregion PublicFields

//        #region PrivateFields

//        private static FileSystemWatcher? _configWatcher;
//        private static ConcurrentDictionary<string, string> _configValues = new ConcurrentDictionary<string, string>();
//        //private static string _configPath = Assembly.GetExecutingAssembly().Location;
//        private static string _configPath = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None).FilePath;
//        private static string? _configDirectory = Path.GetDirectoryName(_configPath);
//        private static Thread? _configThread;

//        #endregion PrivateFields

//        #region ProtectedFields



//        #endregion ProtectedFields

//        #region Ctor



//        #endregion Ctor

//        #region PublicMethods

//        public static void StartApplication()
//        {
//            _configThread = new Thread(ConfigThreadMain);
//            _configThread.IsBackground = true;
//            _configThread.Start();
//        }

//        public static void EndApplication()
//        {
//            StopConfigWatcher();
//            if (_configThread != null)
//            {
//                _configThread.Join();
//            }
//        }

//        public static string GetConfigValue(string key)
//        {
//            if (_configValues.TryGetValue(key, out string? value))
//            {
//                return value;
//            }
//            return string.Empty;
//        }

//        #endregion PublicMethods

//        #region PrivateMethods

//        private static void ConfigThreadMain()
//        {
//            Thread.CurrentThread.Name = $"{nameof(MyConfigManager)}_WorkerThread";
//            try
//            {
//                LoadConfigValues();
//                StartConfigWatcher();
//            }
//            catch (Exception ex)
//            {
//                // Handle any exceptions that might occur during initialization.
//                // You might want to log the exception or take other appropriate action.
//            }

//            // Keep the thread alive while watching for changes.
//            while (_configWatcher != null)
//            {
//                Thread.Sleep(1000); // You might adjust the sleep duration as needed.
//            }
//        }

//        private static void LoadConfigValues()
//        {

//            _configValues.Clear();

//            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
//            KeyValueConfigurationCollection settings = config.AppSettings.Settings;

//            foreach (KeyValueConfigurationElement setting in settings)
//            {
//                _configValues.TryAdd(setting.Key, setting.Value);
//            }

//        }

//        private static void StartConfigWatcher()
//        {
//            if (!string.IsNullOrEmpty(_configDirectory))
//            {
//                //_configWatcher = new FileSystemWatcher(_configDirectory, Path.GetFileName(_configPath + ".config"));
//                _configWatcher = new FileSystemWatcher(_configDirectory, Path.GetFileName(_configPath));
//                _configWatcher.NotifyFilter = NotifyFilters.LastWrite;
//                _configWatcher.Changed += OnConfigFileChange;
//                _configWatcher.EnableRaisingEvents = true;
//            }
//        }

//        private static void StopConfigWatcher()
//        {
//            if (_configWatcher != null)
//            {
//                _configWatcher.Changed -= OnConfigFileChange;
//                _configWatcher.Dispose();
//                _configWatcher = null;
//            }
//        }

//        #endregion PrivateMethods

//        #region ProtectedMethods



//        #endregion ProtectedMethods

//        #region Events

//        public static event EventHandler? ConfigChanged;

//        private static void OnConfigFileChange(object sender, FileSystemEventArgs e)
//        {
//            if (e.ChangeType == WatcherChangeTypes.Changed)
//            {
//                Thread.Sleep(100); // Give the file some time to be completely written

//                LoadConfigValues();
//                ConfigChanged?.Invoke(null, EventArgs.Empty);
//            }
//        }

//        #endregion Events

//        #region OverridedMethods



//        #endregion OverridedMethods
//    }
//}

