using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using IWshRuntimeLibrary;
using St.Common;
using Serilog;
using Squirrel;
using St.Host.Properties;
using File = System.IO.File;
using SscUpdateManager = St.Common.SscUpdateManager;

namespace St.Host
{
    /// <summary>
    ///     App.xaml 的交互逻辑
    /// </summary>
    public partial class App
    {
        private bool _isNewInstance = true;
        public static Mutex Mutex1 { get; private set; }

        protected override async void OnStartup(StartupEventArgs e)
        {
            LogManager.CreateLogFile();

            HandleStartupArgs(e);

            if (!ReadConfig())
            {
                return;
            }

            MakeAppSquirrelAware();

            CheckAppInstance();

            Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            GetLocalVersion();

            SscUpdateManager.InitializePaths();

            await CheckUpdate();

            Current.ShutdownMode = ShutdownMode.OnMainWindowClose;

            var bootstrapper = new Bootstrapper();

            bootstrapper.Run();
        }

        private void HandleStartupArgs(StartupEventArgs e)
        {
            if (e.Args.Length == 0)
            {
                SplashScreen splashScreen = new SplashScreen(@".\starting.png");
                splashScreen.Show(true, true);
            }

            foreach (string t in e.Args)
            {
                if (!t.Contains("squirrel"))
                {
                    SplashScreen splashScreen = new SplashScreen(@".\starting.png");
                    splashScreen.Show(true, true);
                }

                if (t.Contains("uninstall"))
                {
                    RemoveShortcut();
                }

                if (t.ToLower().Contains("deletelog"))
                {
                    LogManager.DeleteLogFiles();
                }

                if (t.ToLower() == "--development" || t.ToLower() == "--dev")
                {
                    GlobalData.Instance.RunMode = RunMode.Development;
                }

                Log.Logger.Debug($"OnStartup => {t}");
            }
        }

        private async Task CheckUpdate()
        {
            bool hasUpdate = false;

            try
            {
                string updateUrl = GlobalData.Instance.AggregatedConfig.GetInterfaceItem().ServerUpdatePath;
                Log.Logger.Debug($"CheckUpdate => {updateUrl}");

                using (var updateMgr = new UpdateManager(updateUrl))
                {
                    var updateInfo = await updateMgr.CheckForUpdate();
                    if (updateInfo != null && updateInfo.ReleasesToApply?.Any() == true)
                    {
                        // 包含更新
                        hasUpdate = true;
                    }
                }

                if (hasUpdate)
                {
                    string updateMsg = "检测到有新版本，是否升级？";

                    UpdateConfirmView updateConfirmView = new UpdateConfirmView(updateMsg);
                    bool? update = updateConfirmView.ShowDialog();

                    if (update.HasValue && update.Value)
                    {
                        Log.Logger.Debug("【agree to update】");

                        UpdatingView updatingView = new UpdatingView();
                        updatingView.ShowDialog();
                    }
                    else
                    {
                        Log.Logger.Debug("【refuse to update】");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Error($"CheckUpdate => {ex}");
            }
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            Log.Logger.Error("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
            Log.Logger.Error($"【unhandled exception】：{exception}");
        }

        private void Current_DispatcherUnhandledException(object sender,
            System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Log.Logger.Error("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
            Log.Logger.Error($"【unhandled exception】：{e.Exception}");
            MessageBox.Show("当前应用程序遇到一些问题，将要退出！", "意外的操作", MessageBoxButton.OK, MessageBoxImage.Information);
            e.Handled = true;
        }

        private void CheckAppInstance()
        {
            Mutex1 = new Mutex(true, "ShengtaoSchoolClient", out _isNewInstance);

            if (!_isNewInstance)
            {
                SscDialog dialog = new SscDialog("程序已经在运行中！");
                dialog.ShowDialog();
                Current.Shutdown();
            }
        }

        private void GetLocalVersion()
        {
            GlobalData.Instance.Version = Assembly.GetExecutingAssembly().GetName().Version;
            GlobalResources.VersionInfo =
                $"互联网校际协作客户端 V{GlobalData.Instance.Version.Major}.{GlobalData.Instance.Version.Minor}.{GlobalData.Instance.Version.Build}";
            Log.Logger.Debug($"【current version info】：{GlobalData.Instance.Version}");
        }

        private async Task CopyConfigFile()
        {
            await Task.Run(() =>
            {
                try
                {
                    string sharedConfigFile = Path.Combine(SscUpdateManager.VersionFolder, GlobalResources.ConfigPath);
                    string curConfigFile = Path.Combine(Environment.CurrentDirectory, GlobalResources.ConfigPath);

                    bool hasSharedConfig = File.Exists(sharedConfigFile);
                    bool useSharedConfig = Settings.Default.UseUserConfig;

                    Log.Logger.Debug($"has shared config?{hasSharedConfig}, useSharedConfig?{useSharedConfig}");

                    if (hasSharedConfig && useSharedConfig)
                    {
                        Log.Logger.Debug($"【copy config file】：copy {sharedConfigFile} to {curConfigFile}");

                        File.Copy(sharedConfigFile, curConfigFile, true);
                    }
                }
                catch (Exception ex)
                {
                    Log.Logger.Error($"【copy config file exception】：{ex}");
                    Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        SscDialog dialog = new SscDialog(Messages.ErrorCopyConfigFile);
                        dialog.ShowDialog();
                        Current.Shutdown();
                    }));
                }
            });
        }

        private bool ReadConfig()
        {
            BaseResult result = ConfigManager.ReadConfig();

            if (result.Status == "-1")
            {
                SscDialog dialog = new SscDialog(result.Message);
                dialog.ShowDialog();
                Current.Shutdown();
                return false;
            }

            return true;
        }

        private void MakeAppSquirrelAware()
        {
            try
            {
                string updateUrl = GlobalData.Instance.AggregatedConfig.GetInterfaceItem().ServerUpdatePath;
                Log.Logger.Debug($"MakeAppSquirrelAware => {updateUrl}");
                using (var mgr = new UpdateManager(updateUrl))
                {
                    SquirrelAwareApp.HandleEvents(
                        onInitialInstall: v =>
                        {
                            Log.Logger.Information($"onInitialInstall => 版本号：{v.ToString(3)}");
                            CreateShrotcut();
                            Current.Shutdown();
                        },

                        onAppUpdate: async v =>
                        {
                            await CopyConfigFile();
                            Log.Logger.Information($"onAppUpdate => 版本号：{v.ToString(3)}");
                            CreateShrotcut();
                            Current.Shutdown();
                        },

                        onAppUninstall: v =>
                        {
                            Log.Logger.Information($"onAppUninstall => 版本号：{v.ToString(3)}");
                            RemoveShortcut();
                            Current.Shutdown();
                        },

                        onAppObsoleted: v =>
                        {
                            Current.Shutdown();
                        },

                        onFirstRun: () =>
                        {
                            Log.Logger.Information("onFirstRun =>");
                            CreateShrotcut();
                        }
                    );
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Error($"MakeAppSquirrelAware => {ex}");
            }
        }

        private void CreateShrotcut()
        {
            try
            {
                string location = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                    "校际协作.lnk");

                Log.Logger.Debug($"create shortcut => {location}");

                WshShell shell = new WshShell();
                IWshShortcut shortcut = (IWshShortcut) shell.CreateShortcut(location);

                string currentVersionExeFullName = Process.GetCurrentProcess().MainModule.FileName;

                DirectoryInfo curDirectoryInfo = new DirectoryInfo(currentVersionExeFullName);

                string lastLevelDirName = curDirectoryInfo.Parent.ToString();
                string exeName = Path.GetFileName(currentVersionExeFullName);

                string lastLevelPath = Path.Combine(lastLevelDirName, exeName);

                string targetDir = currentVersionExeFullName.Replace(lastLevelPath, string.Empty);

                string targetExeFullName = Path.Combine(targetDir, exeName);

                shortcut.TargetPath = targetExeFullName;

                Log.Logger.Debug($"target path => {shortcut.TargetPath}");
                shortcut.Save();

            }
            catch (Exception ex)
            {
                Log.Logger.Error($"CreateShrotcut => {ex}");
            }
        }

        private void RemoveShortcut()
        {
            try
            {
                DirectoryInfo desktopDirectoryInfo =
                    new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory));

                if (desktopDirectoryInfo.GetFiles("*.lnk")
                    .Any(file => file.Name == "校际协作.lnk"))
                {
                    Log.Logger.Debug($"RemoveShortcut => has shortcut on desktop");
                    File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                        "校际协作.lnk"));
                }
                else
                {
                    Log.Logger.Debug($"RemoveShortcut => no shortcut no desktop");
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Error($"RemoveShortcut => {ex}");
            }
        }

        //protected override void OnExit(ExitEventArgs e)
        //{
        //    base.OnExit(e);

        //    if (isNewInstance)
        //    {
        //        Log.Logger.Debug(
        //            "【exit app】：#######################################################################################");
        //        IGroupManager groupManager = IoC.Get<IGroupManager>();
        //        groupManager.LeaveGroup();

        //        IRtClientService rtClientService = IoC.Get<IRtClientService>();
        //        Log.Logger.Debug($"【rt server connected】：{rtClientService.IsConnected()}");
        //        Log.Logger.Debug($"【stop rt server begins】：");
        //        rtClientService.Stop();
        //        Log.Logger.Debug($"【rt server connected】：{rtClientService.IsConnected()}");
        //    }
        //}
    }
}