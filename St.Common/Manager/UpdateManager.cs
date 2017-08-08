using Serilog;
using System;
using System.IO;
using System.Text;

namespace St.Common
{
    public static class SscUpdateManager
    {
        public static string VersionFolder
            => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                VersionFolderName);

        private const string VersionFolderName = "ShengtaoSchoolClient_Version";

        public static void InitializePaths()
        {
            if (!Directory.Exists(VersionFolder))
            {
                Log.Logger.Debug($"【create folder begins】：{VersionFolder}");
                Directory.CreateDirectory(VersionFolder);
            }
        }


        public static void WriteConfigToVersionFolder(string configJson)
        {
            string sharedConfig = Path.Combine(VersionFolder, GlobalResources.ConfigPath);
            File.WriteAllText(sharedConfig, configJson, Encoding.UTF8);
        }
    }
}
