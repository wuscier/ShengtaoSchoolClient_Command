using System;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace JsonConfig
{
    public static class Config
    {
        public static dynamic Default = new ConfigObject();
        public static dynamic User;

        public static dynamic MergedConfig
        {
            get
            {
                return Merger.Merge(User, Default);
            }
        }

        public static string DefaultEnding = ".conf";

        private static dynamic _globalConfig;
        public static dynamic Global
        {
            get
            {
                if (_globalConfig == null)
                {
                    _globalConfig = MergedConfig;
                }
                return _globalConfig;
            }
            set
            {
                _globalConfig = Merger.Merge(value, MergedConfig);
            }
        }

        /// <summary>
        /// Gets a ConfigObject that represents the current configuration. Since it is 
        /// a cloned copy, changes to the underlying configuration files that are done
        /// after GetCurrentScope() is called, are not applied in the returned instance.
        /// </summary>
        public static ConfigObject GetCurrentScope()
        {
            if (Global is NullExceptionPreventer)
                return new ConfigObject();
            else
                return Global.Clone();
        }

        public delegate void UserConfigFileChangedHandler();
        public static event UserConfigFileChangedHandler OnUserConfigFileChanged;

        static Config()
        {
            var entryAssembly = Assembly.GetEntryAssembly();
            Default = Merger.Merge(GetDefaultConfig(entryAssembly), Default);

            var executionPath = Path.GetDirectoryName( entryAssembly.Location);
            

            // User config (provided through a settings.conf file)
            var userConfigFilename = "settings";

            // TODO this is ugly but makes life easier
            // we are run from the IDE, so the settings.conf needs
            // to be searched two levels up
            var settingsPath = GetUserSettingsFolder(executionPath);
            var settingsFolder = new DirectoryInfo(settingsPath);
            var userConfig = (from FileInfo fi in settingsFolder.GetFiles()
                              where (
                                  fi.FullName.EndsWith(userConfigFilename + ".conf") ||
                                  fi.FullName.EndsWith(userConfigFilename + ".json") ||
                                  fi.FullName.EndsWith(userConfigFilename + ".conf.json") ||
                                  fi.FullName.EndsWith(userConfigFilename + ".json.conf")
                              )
                              select fi).FirstOrDefault();

            if (userConfig != null)
            {
                User = ParseJson(File.ReadAllText(userConfig.FullName));
                WatchUserConfig(userConfig);
            }
            else
            {
                User = new NullExceptionPreventer();
            }
        }

        private static string GetUserSettingsFolder(string executionPath)
        {
            //For .Net Core
            if (Regex.IsMatch(executionPath, @"//bin//Debug//netcoreapp.*$"))
                return Regex.Replace(executionPath, @"//bin//Debug//netcoreapp.*$", ""); // for Unix-like
            if (Regex.IsMatch(executionPath, @"\\bin\\Debug\\netcoreapp.*$"))
                return Regex.Replace(executionPath, @"\\bin\\Debug\\netcoreapp.*$", ""); // for Win

            //For .Net 4.0
            if (Regex.IsMatch(executionPath, @"//bin//Debug//"))
                return Regex.Replace(executionPath, @"//bin//Debug//", ""); // for Unix-like
            if (Regex.IsMatch(executionPath, @"\\bin\\Debug\\"))
                return Regex.Replace(executionPath, @"\\bin\\Debug\\", ""); // for Win

            return executionPath;
        }

        private static FileSystemWatcher _userConfigWatcher;
        public static void WatchUserConfig(FileInfo info)
        {
            var lastRead = File.GetLastWriteTime(info.FullName);
            if (info.Directory != null)
                _userConfigWatcher = new FileSystemWatcher(info.Directory.FullName, info.Name)
                {
                    NotifyFilter = NotifyFilters.LastWrite
                };
            _userConfigWatcher.Changed += delegate {
                DateTime lastWriteTime = File.GetLastWriteTime(info.FullName);
                if (lastWriteTime.Subtract(lastRead).TotalMilliseconds > 100)
                {
                    Console.WriteLine("user configuration has changed, updating config information");
                    try
                    {
                        User = ParseJson(File.ReadAllText(info.FullName));
                    }
                    catch (IOException)
                    {
                        System.Threading.Thread.Sleep(100); //Sleep shortly, and try again.
                        try
                        {
                            User = ParseJson(File.ReadAllText(info.FullName));
                        }
                        catch (Exception)
                        {
                            Console.WriteLine("updating user config failed.");
                            throw;
                        }
                    }

                    // invalidate the Global config, forcing a re-merge next time its accessed
                    _globalConfig = null;

                    // trigger our event
                    if (OnUserConfigFileChanged != null)
                        OnUserConfigFileChanged();
                }
                lastRead = lastWriteTime;
            };
            _userConfigWatcher.EnableRaisingEvents = true;
        }
        public static ConfigObject ApplyJsonFromFileInfo(FileInfo file, ConfigObject config = null)
        {
            var overlayJson = File.ReadAllText(file.FullName);
            dynamic overlayConfig = ParseJson(overlayJson);
            return Merger.Merge(overlayConfig, config);
        }
        public static ConfigObject ApplyJsonFromPath(string path, ConfigObject config = null)
        {
            return ApplyJsonFromFileInfo(new FileInfo(path), config);
        }
        public static ConfigObject ApplyJson(string json, ConfigObject config = null)
        {
            if (config == null)
                config = new ConfigObject();

            dynamic parsed = ParseJson(json);
            return Merger.Merge(parsed, config);
        }
        // seeks a folder for .conf files
        public static ConfigObject ApplyFromDirectory(string path, ConfigObject config = null, bool recursive = false)
        {
            if (!Directory.Exists(path))
                throw new Exception("no folder found in the given path");

            if (config == null)
                config = new ConfigObject();

            DirectoryInfo info = new DirectoryInfo(path);
            if (recursive)
            {
                foreach (var dir in info.GetDirectories())
                {
                    Console.WriteLine("reading in folder {0}", dir);
                    config = ApplyFromDirectoryInfo(dir, config, true);
                }
            }

            // find all files
            var files = info.GetFiles();
            foreach (var file in files)
            {
                Console.WriteLine("reading in file {0}", file);
                config = ApplyJsonFromFileInfo(file, config);
            }
            return config;
        }
        public static ConfigObject ApplyFromDirectoryInfo(DirectoryInfo info, ConfigObject config = null, bool recursive = false)
        {
            return ApplyFromDirectory(info.FullName, config, recursive);
        }

        public static ConfigObject ParseJson(string json)
        {
            var lines = json.Split(new char[] { '\n' });
            // remove lines that start with a dash # character 
            var filtered = from l in lines
                           where !(Regex.IsMatch(l, @"^\s*#(.*)"))
                           select l;

            var filteredJson = string.Join("\n", filtered);

            //var json_reader = new JsonReader ();
            //dynamic parsed = JObject.Parse(filtered_json);
            dynamic parsed = JsonConvert.DeserializeObject<ExpandoObject>(filteredJson);
            // convert the ExpandoObject to ConfigObject before returning
            var result = ConfigObject.FromExpando(parsed);
            return result;
        }
        // overrides any default config specified in default.conf
        public static void SetDefaultConfig(dynamic config)
        {
            Default = config;

            // invalidate the Global config, forcing a re-merge next time its accessed
            _globalConfig = null;
        }
        public static void SetUserConfig(ConfigObject config)
        {
            User = config;
            // disable the watcher
            if (_userConfigWatcher != null)
            {
                _userConfigWatcher.EnableRaisingEvents = false;
                _userConfigWatcher.Dispose();
                _userConfigWatcher = null;
            }

            // invalidate the Global config, forcing a re-merge next time its accessed
            _globalConfig = null;
        }
        private static dynamic GetDefaultConfig(Assembly assembly)
        {
            var dconfJson = ScanForDefaultConfig(assembly);
            if (dconfJson == null)
                return null;
            return ParseJson(dconfJson);
        }
        private static string ScanForDefaultConfig(Assembly assembly)
        {
            if (assembly == null)
                assembly = Assembly.GetEntryAssembly();

            string[] res;
            try
            {
                // this might fail for the 'Anonymously Hosted DynamicMethods Assembly' created by an Reflect.Emit()
                res = assembly.GetManifestResourceNames();
            }
            catch
            {
                // for those assemblies, we don't provide a config
                return null;
            }
            var dconfResource = res
                .FirstOrDefault(r => r.EndsWith("default.conf", StringComparison.OrdinalIgnoreCase) ||
                   r.EndsWith("default.json", StringComparison.OrdinalIgnoreCase) ||
                   r.EndsWith("default.conf.json", StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrEmpty(dconfResource))
                return null;

            var stream = assembly.GetManifestResourceStream(dconfResource);
            if (stream != null)
            {
                var defaultJson = new StreamReader(stream).ReadToEnd();
                return defaultJson;
            }
            return null;
        }
    }
}
