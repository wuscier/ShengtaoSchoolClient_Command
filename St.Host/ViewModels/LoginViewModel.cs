using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml;
using Caliburn.Micro;
using MeetingSdk.SdkWrapper;
using Newtonsoft.Json;
using Prism.Commands;
using Prism.Regions;
using Serilog;
using St.Common;
using St.Host.Views;
using Action = System.Action;
using Formatting = Newtonsoft.Json.Formatting;
using LogManager = St.Common.LogManager;
using RtClientManager = St.Common.RtClient.RtClientManager;

namespace St.Host.ViewModels
{
    public class LoginViewModel : ViewModelBase
    {
        //private fields
        private readonly Window _loginView;
        private readonly UserInfo _userInfo;
        private readonly IBms _bmsService;
        private readonly IVisualizeShell _visualizeShellSevice;
        private bool _autoLogin;
        private bool _isLoginEnabled = true;
        private InterfaceConfig _environmentConfig;
        private string _password;
        private bool _rememberMe;
        private bool _showProgressBar;
        private readonly string _configResultPath = Path.Combine(Environment.CurrentDirectory, Common.GlobalResources.ConfigPath);
        private readonly string _npsConfigPath = Path.Combine(Environment.CurrentDirectory, Common.GlobalResources.NpsPath);

        //properties
        private string _userName;

        //public fields
        public bool IsLoginSucceeded;

        public LoginViewModel(Window loginView)
        {
            _loginView = loginView;
            _userInfo = IoC.Get<UserInfo>();
            _bmsService = IoC.Get<IBms>();
            _visualizeShellSevice = IoC.Get<IVisualizeShell>();
            _loginView.Closing += _loginView_Closing;

            LoginCommand = DelegateCommand.FromAsyncHandler(LoginAsync);
            LoadCommand = DelegateCommand.FromAsyncHandler(LoadAsync);
            SaveSettingCommand = DelegateCommand.FromAsyncHandler(SaveSettingAsync);
            ShowSettingCommand = new DelegateCommand(ShowSettingAsync);
            TopMostTriggerCommand = new DelegateCommand(TriggerTopMost);
            ShowLogCommand = DelegateCommand.FromAsyncHandler(ShowLogAsync);
        }

        private async Task ShowLogAsync()
        {
            await LogManager.ShowLogAsync();
        }


        private void TriggerTopMost()
        {
            _loginView.Topmost = !_loginView.Topmost;
        }

        private void ShowSettingAsync()
        {
            if (SettingVisibility == Visibility.Collapsed)
            {
                SettingVisibility = Visibility.Visible;
                ReadEnvironmentAsync();
            }
            else if (SettingVisibility == Visibility.Visible)
            {
                SettingVisibility = Visibility.Collapsed;
            }
        }

        public string UserName
        {
            get { return _userName; }
            set { SetProperty(ref _userName, value); }
        }

        public string Password
        {
            get { return _password; }
            set { SetProperty(ref _password, value); }
        }

        public bool IsLoginEnabled
        {
            get { return _isLoginEnabled; }
            set { SetProperty(ref _isLoginEnabled, value); }
        }

        public bool ShowProgressBar
        {
            get { return _showProgressBar; }
            set { SetProperty(ref _showProgressBar, value); }
        }

        public bool RememberMe
        {
            get { return _rememberMe; }
            set
            {
                if (SetProperty(ref _rememberMe, value) && !value)
                    AutoLogin = false;
            }
        }

        public bool AutoLogin
        {
            get { return _autoLogin; }
            set
            {
                if (SetProperty(ref _autoLogin, value) && value)
                    RememberMe = true;
            }
        }

        private string _bmsAddress;

        public string BmsAddress
        {
            get { return _bmsAddress; }
            set { SetProperty(ref _bmsAddress, value); }
        }

        private string _npsAddress;

        public string NpsAddress
        {
            get { return _npsAddress; }
            set { SetProperty(ref _npsAddress, value); }
        }


        private string _rtsServer;
        public string RtsServer
        {
            get { return _rtsServer; }
            set { SetProperty(ref _rtsServer, value); }
        }

        private string _serverVersionInfo;
        public string ServerVersionInfo
        {
            get { return _serverVersionInfo; }
            set { SetProperty(ref _serverVersionInfo, value); }
        }


        private bool _isFormalEnvChecked;

        public bool IsFormalEnvChecked
        {
            get { return _isFormalEnvChecked; }
            set
            {
                if (value && _environmentConfig?.External != null)
                {
                    NpsAddress = _environmentConfig.External.NpsAddress;
                    BmsAddress = _environmentConfig.External.BmsAddress;
                    RtsServer = _environmentConfig.External.RtsAddress;
                    ServerVersionInfo = _environmentConfig.External.ServerUpdatePath;
                }
                SetProperty(ref _isFormalEnvChecked, value);
            }
        }

        private bool _isTestEnvChecked;

        public bool IsTestEnvChecked
        {
            get { return _isTestEnvChecked; }
            set
            {
                if (value && _environmentConfig?.Internal != null)
                {
                    NpsAddress = _environmentConfig.Internal.NpsAddress;
                    BmsAddress = _environmentConfig.Internal.BmsAddress;
                    RtsServer = _environmentConfig.Internal.RtsAddress;
                    ServerVersionInfo = _environmentConfig.Internal.ServerUpdatePath;
                }
                SetProperty(ref _isTestEnvChecked, value);
            }
        }

        private Visibility _settingVisibility = Visibility.Collapsed;
        public Visibility SettingVisibility
        {
            get { return _settingVisibility; }
            set { SetProperty(ref _settingVisibility, value); }
        }

        //commands
        public ICommand LoginCommand { get; set; }
        public ICommand LoadCommand { get; set; }
        public ICommand SaveSettingCommand { get; set; }
        public ICommand ShowSettingCommand { get; set; }

        public ICommand TopMostTriggerCommand { get; set; }
        public ICommand ShowLogCommand { get; set; }

        //command handlers
        private async Task LoginAsync()
        {
            ResetStatus();

            if (!ValidateInputs())
                return;

            await Login();
        }

        private async Task LoadAsync()
        {
            Log.Logger.Debug("LoadAsync => LoginView");
            try
            {
                if (GlobalData.Instance.RunMode == RunMode.Development)
                {
                    _loginView.Topmost = false;
                }

                LoadConfigFromGlobalConfig();


                if (AutoLogin && !_userInfo.IsLogouted)
                    await Login();
            }
            catch (Exception ex)
            {
                string errorMsg = ex.InnerException?.Message.Replace("\r\n", string.Empty) ??
                              ex.Message.Replace("\r\n", string.Empty);
                HasErrorMsg("-1", errorMsg);
            }
        }

        private async Task SaveSettingAsync()
        {
            if (string.IsNullOrEmpty(BmsAddress))
            {
                HasErrorMsg("-1", Messages.WarningBmsAddressEmpty);
                return;
            }
            if (string.IsNullOrEmpty(NpsAddress))
            {
                HasErrorMsg("-1", Messages.WarningNpsAddressEmpty);
                return;
            }

            if (string.IsNullOrEmpty(RtsServer))
            {
                HasErrorMsg("-1", Messages.WarningRtsAddressEmpty);
                return;
            }

            if (string.IsNullOrEmpty(ServerVersionInfo))
            {
                HasErrorMsg("-1", Messages.WarningUpdateAddressEmpty);
                return;
            }

            GlobalData.Instance.AggregatedConfig.InterfaceType = IsTestEnvChecked ? InterfaceTypeStrings.Internal : InterfaceTypeStrings.External;

            await WriteConfigAsync();
            await UpdateNpsAddress();
            await DeleteSdkUserData();
            HasErrorMsg("-1", Messages.InfoSaveConfigSucceeded);
        }


        //event handlers
        private void _loginView_Closing(object sender, CancelEventArgs e)
        {
            if (IsLoginSucceeded)
            {
                var mainView = IoC.Get<MainView>();
                if (mainView.IsLoaded)
                {
                    Log.Logger.Debug("【login succeeded, make loaded main view visible】");
                    mainView.Visibility = Visibility.Visible;

                    IRegionManager regionManager = IoC.Get<IRegionManager>();

                    foreach (var activeView in regionManager.Regions[RegionNames.ContentRegion].ActiveViews)
                    {
                        if (activeView.ToString().Contains(GlobalResources.CollaborativeInfoContentView))
                        {
                            Log.Logger.Debug($"【reload default view】：{GlobalResources.CollaborativeInfoContentView}");
                            UserControl regionView = activeView as UserControl;

                            if (regionView != null)
                            {
                                IReloadRegion reloadRegionService = regionView.DataContext as IReloadRegion;

                                reloadRegionService?.ReloadAsync();
                            }
                        }
                        else
                        {
                            Log.Logger.Debug(
                                $"【navigate to default view】：{GlobalResources.CollaborativeInfoContentView}");
                            _visualizeShellSevice.SetSelectedMenu(GlobalResources.CollaborativeInfoNavView);
                            regionManager.RequestNavigate(RegionNames.ContentRegion,
                                new Uri(GlobalResources.CollaborativeInfoContentView, UriKind.Relative));
                        }
                        break;
                    }
                }
            }
            else
            {
                Application.Current.Shutdown();
            }
        }

        //methods
        private bool ValidateInputs()
        {
            if (string.IsNullOrEmpty(UserName))
            {
                HasErrorMsg("-1", Messages.WarningUserNameEmpty);
                return false;
            }

            if (string.IsNullOrEmpty(Password))
            {
                HasErrorMsg("-1", Messages.WarningPasswordEmpty);
                return false;
            }

            return true;
        }

        private void ResetStatus()
        {
            ShowProgressBar = false;
        }

        private void SetBeginLoginStatus()
        {
            IsLoginEnabled = false;
            ShowProgressBar = true;
        }

        private void SetEndLoginStatus()
        {
            IsLoginEnabled = true;
            ShowProgressBar = false;
        }

        private async Task Login()
        {
            SetBeginLoginStatus();

            //logic for login
            try
            {
                bool succeeded = await AuthenticateUserAsync().ConfigureAwait(false);

                if (succeeded)
                {
                    TimerManager.Instance.StartTimer();

                    RtClientManager.Instance.SigninRtServiceBackground();

                    await CacheLessonTypesAsync();
                    IsLoginSucceeded = true;
                    await WriteConfigAsync();
                    await _loginView.Dispatcher.BeginInvoke(new Action(() => { _loginView.Close(); }));
                }
            }
            catch (Exception ex)
            {
                HasErrorMsg("-1", ex.InnerException?.Message.Replace("\r\n", "") ?? ex.Message.Replace("\r\n", ""));
            }
            finally
            {
                SetEndLoginStatus();
            }
        }

        private void ReadEnvironmentAsync()
        {
            Task.Run(() =>
            {
                try
                {
                    _environmentConfig = GlobalData.Instance.AggregatedConfig.InterfaceTypes;

                    IsTestEnvChecked = GlobalData.Instance.AggregatedConfig.InterfaceType == InterfaceTypeStrings.Internal;
                    IsFormalEnvChecked = !IsTestEnvChecked;
                }
                catch (Exception ex)
                {
                    Log.Logger.Error($"【read environment config exception】：{ex}");
                }
            });
        }

        private void LoadConfigFromGlobalConfig()
        {
            UserName = GlobalData.Instance.AggregatedConfig.AccountAutoLogin.UserName;
            Password = GlobalData.Instance.AggregatedConfig.AccountAutoLogin.Password;
            AutoLogin = GlobalData.Instance.AggregatedConfig.AccountAutoLogin.IsAutoLogin;
            RememberMe = !string.IsNullOrEmpty(UserName) && !string.IsNullOrEmpty(Password);
        }

        private async Task WriteConfigAsync()
        {
            await Task.Run(() =>
            {
                GlobalData.Instance.AggregatedConfig.AccountAutoLogin.IsAutoLogin = AutoLogin;
                if (RememberMe && IsLoginSucceeded)
                {
                    GlobalData.Instance.AggregatedConfig.AccountAutoLogin.UserName = UserName;
                    GlobalData.Instance.AggregatedConfig.AccountAutoLogin.Password = Password;
                }

                try
                {
                    string json = JsonConvert.SerializeObject(GlobalData.Instance.AggregatedConfig,
                        Formatting.Indented);

                    File.WriteAllText(_configResultPath, json, Encoding.UTF8);
                    SscUpdateManager.WriteConfigToVersionFolder(json);
                }
                catch (Exception ex)
                {
                    Log.Logger.Error($"【write config exception】：{ex}");
                }
            });
        }

        private async Task UpdateNpsAddress()
        {
            await Task.Run(() =>
            {
                XmlDocument xmlDocument = new XmlDocument();

                try
                {
                    xmlDocument.Load(_npsConfigPath);

                    var xmlNode = xmlDocument.GetElementsByTagName("nps-url").Item(0);
                    if (xmlNode != null)
                    {
                        xmlNode.FirstChild.InnerText = NpsAddress;
                    }

                    xmlDocument.Save(_npsConfigPath);
                }
                catch (Exception ex)
                {
                    Log.Logger.Error($"【update nps address exception】：{ex}");
                    HasErrorMsg("-1", Messages.ErrorWriteNpsConfigError);
                }
            });
        }

        private async Task DeleteSdkUserData()
        {
            await Task.Run(() =>
            {
                try
                {
                    string sdkSettingPath = Path.Combine(Environment.CurrentDirectory,
                        Common.GlobalResources.SdkSettingPath);
                    if (File.Exists(sdkSettingPath))
                    {
                        File.Delete(sdkSettingPath);

                    }

                    string npsBackupPath = Path.Combine(Environment.CurrentDirectory,
                        Common.GlobalResources.NpsBackupPath);

                    if (File.Exists(npsBackupPath))
                    {
                        File.Delete(npsBackupPath);
                    }
                }
                catch (Exception ex)
                {
                    Log.Logger.Error($"【delete sdk user data exception】：{ex}");
                    HasErrorMsg("-1", Messages.ErrorClearUserDataFailed);
                }
            });
        }

        private async Task<bool> AuthenticateUserAsync()
        {
            if (string.IsNullOrEmpty(GlobalData.Instance.AggregatedConfig.GetInterfaceItem().BmsAddress))
            {
                HasErrorMsg("-1", Messages.WarningUserCenterIpNotConfigured);
                return false;
            }
            Log.Logger.Debug("【**********************************************************************************】");
            Log.Logger.Debug($"【login step1】：apply for token begins");
            ResponseResult bmsTokenResult =
                await _bmsService.ApplyForToken(UserName, Password, GlobalData.Instance.SerialNo);
            Log.Logger.Debug($"【login step1】：apply for token ends");

            if (HasErrorMsg(bmsTokenResult.Status, bmsTokenResult.Message))
            {
                return false;
            }

            Log.Logger.Debug($"【login step2】：get userinfo begins");
            ResponseResult userInfoResult = await _bmsService.GetUserInfo();
            Log.Logger.Debug($"【login step2】：get userinfo ends");

            if (HasErrorMsg(userInfoResult.Status, userInfoResult.Message))
            {
                return false;
            }

            UserInfo userInfo = userInfoResult.Data as UserInfo;

            if (userInfo != null)
            {
                UserInfo globalUserInfo = IoC.Get<UserInfo>();
                userInfo.Pwd = Password;
                globalUserInfo.CloneUserInfo(userInfo);

                IMeeting sdkService = IoC.Get<IMeeting>();
                sdkService.SelfPhoneId = userInfo.GetNube();

                return true;
            }

            Log.Logger.Error($"【get user info via account failed】：user info is null");
            HasErrorMsg("-1", Messages.ErrorLoginFailed);
            return false;
        }

        private async Task CacheLessonTypesAsync()
        {
            Log.Logger.Debug($"【login step4】：get lesson types begins");
            ResponseResult lessonTypeResult = await _bmsService.GetLessonTypes();
            Log.Logger.Debug($"【login step4】：get lesson types ends");

            List<LessonType> lessonTypes = lessonTypeResult.Data as List<LessonType>;
            Log.Logger.Debug($"【lesson types count】：count={lessonTypes?.Count}");
            if (lessonTypes != null)
            {
                GlobalData.Instance.ActiveModules.Clear();
                lessonTypes.ForEach(lessonType =>
                {
                    Log.Logger.Debug($"【iterate lesson type】：{lessonType.ToString()}");
                    GlobalData.Instance.ActiveModules.Add(lessonType.ToString());
                });
            }
        }
    }
}