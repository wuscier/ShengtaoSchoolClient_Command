using Prism.Mvvm;
using St.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using Caliburn.Micro;
using Prism.Commands;
using Serilog;
using MeetingSdk.SdkWrapper;
using Action = System.Action;
using RtClientManager = St.Common.RtClient.RtClientManager;

namespace St.Host
{
    public class DeviceLoginViewModel : BindableBase
    {
        public DeviceLoginViewModel(DeviceLoginView deviceLoginView)
        {
            _deviceLoginView = deviceLoginView;
            _userInfo = IoC.Get<UserInfo>();
            _bmsService = IoC.Get<IBms>();

            LoadCommand = DelegateCommand.FromAsyncHandler(LoadAsync);
            TopMostTriggerCommand = new DelegateCommand(TriggerTopMost);
            ShowLogCommand = DelegateCommand.FromAsyncHandler(ShowLogAsync);
        }

        private readonly DeviceLoginView _deviceLoginView;
        private readonly UserInfo _userInfo;
        private readonly IBms _bmsService;

        public bool IsLoginSucceeded { get; set; }

        private bool _isDialogOpen;
        public bool IsDialogOpen
        {
            get { return _isDialogOpen; }
            set
            {
                _deviceLoginView.Topmost = !value;
                SetProperty(ref _isDialogOpen, value);
            }
        }

        private string _dialogContent;
        public string DialogContent
        {
            get { return _dialogContent; }
            set { SetProperty(ref _dialogContent, value); }
        }

        public ICommand LoadCommand { get; set; }
        public ICommand TopMostTriggerCommand { get; set; }
        public ICommand ShowLogCommand { get; set; }

        private void TriggerTopMost()
        {
            _deviceLoginView.Topmost = !_deviceLoginView.Topmost;
        }

        private async Task ShowLogAsync()
        {
            await Task.Run(() =>
            {
                string logPath = Path.Combine(Environment.CurrentDirectory, $"log-{DateTime.Now:yyyyMMdd}.txt");
                if (File.Exists(logPath))
                {
                    Process.Start(logPath);
                }
            });
        }

        private async Task LoadAsync()
        {
            Log.Logger.Debug("LoadAsync => DeviceLoginView");

            DialogContent = Messages.InfoLogging;
            IsDialogOpen = true;

            bool succeeded = await AuthenticateUserAsync().ConfigureAwait(false);
            if (succeeded)
            {
                TimerManager.Instance.StartTimer();

                RtClientManager.Instance.SigninRtServiceBackground();

                await CacheLessonTypesAsync();

                IsLoginSucceeded = true;
                await _deviceLoginView.Dispatcher.BeginInvoke(new Action(() => { _deviceLoginView.Close(); }));
            }
        }


        private async Task<bool> AuthenticateUserAsync()
        {
            if (string.IsNullOrEmpty(GlobalData.Instance.AggregatedConfig.GetInterfaceItem().BmsAddress))
            {
                DialogContent = Messages.WarningUserCenterIpNotConfigured;

                return false;
            }
            Log.Logger.Debug("【**********************************************************************************】");

            Log.Logger.Debug($"【login step1】：get imei token begins");
            ResponseResult getImeiToekenResult =
                await
                    _bmsService.GetImeiToken(GlobalData.Instance.SerialNo,
                        GlobalData.Instance.AggregatedConfig.DeviceKey);
            Log.Logger.Debug($"【login step1】：get imei token ends");

            if (getImeiToekenResult.Status != "0")
            {
                DialogContent = getImeiToekenResult.Message;
                return false;
            }

            Log.Logger.Debug($"【login step2】：get userinfo begins");
            ResponseResult getUserInfoResult = await _bmsService.GetUserInfo();
            Log.Logger.Debug($"【login step2】：get userinfo ends");

            if (getUserInfoResult.Status != "0")
            {
                DialogContent = getUserInfoResult.Message;
                return false;
            }

            UserInfo userInfo = getUserInfoResult.Data as UserInfo;
            if (userInfo != null)
            {
                UserInfo globalUserInfo = IoC.Get<UserInfo>();
                globalUserInfo.CloneUserInfo(userInfo);

                IMeeting sdkService = IoC.Get<IMeeting>();
                sdkService.SelfPhoneId = userInfo.GetNube();

                return true;
            }

            Log.Logger.Error($"【get user info via device no. failed】：user info is null");
            DialogContent = Messages.ErrorLoginFailed;
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
