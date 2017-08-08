using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Serilog;

namespace St.Common
{
    public class LogManager
    {
        public static async Task ShowLogAsync()
        {
            await Task.Run(() =>
            {
                string logPath = Path.Combine(Environment.CurrentDirectory, "logs");

                Process.Start("explorer.exe", logPath);
            });
        }

        private static void SortByFileCreationTime(ref FileInfo[] fileInfos)
        {
            Array.Sort(fileInfos, ((a, b) => b.LastWriteTime.CompareTo(a.LastWriteTime)));
        }

        public static void CreateLogFile()
        {
            Log.Logger =
                new LoggerConfiguration()
                    .MinimumLevel.Debug().
                    WriteTo.RollingFile(@"logs\log.txt")
                    .CreateLogger();
            Log.Logger.Debug(
                "【start up app】：#######################################################################################");
        }

        public static void DeleteLogFiles()
        {
            Task.Run(() =>
            {
                try
                {
                    DateTime boundaryDate = DateTime.Now.Date.AddDays(-1);

                    DirectoryInfo curDirectoryInfo =
                        new DirectoryInfo(Path.Combine(Environment.CurrentDirectory, "logs"));

                    FileInfo[] logFileInfos = curDirectoryInfo.GetFiles("*.*");

                    var outdatedLogs = logFileInfos.Where(log => log.LastWriteTime < boundaryDate).ToList();

                    outdatedLogs.ForEach(log =>
                    {
                        Log.Logger.Debug($"【delete file】：{log.FullName}");
                        File.Delete(log.FullName);
                    });

                    string sdkLogFolder = Path.Combine(Environment.CurrentDirectory, "sdk", "LOG");

                    if (Directory.Exists(sdkLogFolder))
                    {
                        DirectoryInfo curSdkLogDirectoryInfo =
                            new DirectoryInfo(sdkLogFolder);

                        FileInfo[] sdkFileInfos = curSdkLogDirectoryInfo.GetFiles("*.*", SearchOption.TopDirectoryOnly);
                        var outdatedSdkLogs =
                            sdkFileInfos.Where(sdkLog => sdkLog.LastWriteTime < boundaryDate).ToList();

                        outdatedSdkLogs.ForEach(sdkLog =>
                        {
                            Log.Logger.Debug($"【delete file】：{sdkLog.FullName}");
                            File.Delete(sdkLog.FullName);
                        });

                    }

                    string innerLogFolder = Path.Combine(Environment.CurrentDirectory,"sdk", "LOG", "LOG");

                    if (Directory.Exists(innerLogFolder))
                    {
                        DirectoryInfo curLogUploadFolder =
                            new DirectoryInfo(innerLogFolder);

                        curLogUploadFolder.Delete(true);
                    }
                }
                catch (Exception ex)
                {
                    Log.Logger.Error($"【delete log files exception】：{ex}");
                }
            });
        }
    }
}