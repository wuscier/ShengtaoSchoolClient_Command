using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using Caliburn.Micro;
using MeetingSdk.SdkWrapper;
using Serilog;
using St.Common;
using St.Common.Commands;
using St.Host.Views;
using St.RtClient;

namespace St.Host
{
    public class VisualizeShellService : IVisualizeShell
    {
        private readonly UserInfo _userInfo;
        private readonly IMeeting _sdkService;
        private readonly MainView _shellView;
        private readonly IRtClientService _rtClientService;

        public VisualizeShellService()
        {
            _userInfo = IoC.Get<UserInfo>();
            _sdkService = IoC.Get<IMeeting>();
            _shellView = IoC.Get<MainView>();
            _rtClientService = IoC.Get<IRtClientService>();
        }


        public void FinishStartingSdk(bool succeeded, string msg)
        {
            _shellView.DialogContent = msg;

            if (succeeded)
            {
                _shellView.IsDialogOpen = false;
                //shell.DialogContent = string.Empty;
            }
        }

        public void HideShell()
        {
            if (_shellView != null)
            {
                _shellView.IsDialogOpen = false;
                _shellView.DialogContent = string.Empty;
                _shellView.Visibility = Visibility.Collapsed;
            }
        }

        public async Task Logout()
        {
            TimerManager.Instance.StopTimer();
            if (GlobalData.Instance.Device.EnableLogin)
            {
                SscDialog dialog = new SscDialog(Messages.WarningYouAreSignedOut);
                dialog.ShowDialog();
                Application.Current.Shutdown();
            }
            else
            {
                Log.Logger.Debug($"【rt server connected】：{_rtClientService.IsConnected()}");
                Log.Logger.Debug($"【stop rt server begins】：");
                _rtClientService.Stop();
                Log.Logger.Debug($"【rt server connected】：{_rtClientService.IsConnected()}");

                _userInfo.IsLogouted = true;

                foreach (Window currentWindow in Application.Current.Windows)
                {
                    if (currentWindow is LoginView)
                    {
                        Log.Logger.Debug("【already in login view, do nothing】");
                        return;
                    }

                    var meeting = currentWindow.DataContext as IExitMeeting;
                    if (meeting != null)
                    {
                        Log.Logger.Debug("【in meeting view, exit meeting】");
                        IExitMeeting exitMeetingService = meeting;
                        await exitMeetingService.ExitAsync();
                    }
                }

                Log.Logger.Debug("【in main view】");
                HideShell();

                LoginView loginView = IoC.Get<LoginView>();
                loginView.Show();

                _sdkService.Stop();
                _sdkService.IsServerStarted = false;
            }
        }

        public void ShowShell()
        {
            GlobalCommands.Instance.SetCommandsBackToMainView();
            _shellView.Visibility = Visibility.Visible;
            GlobalData.Instance.CurWindowHwnd = new WindowInteropHelper(_shellView).Handle;
        }

        public void StartingSdk()
        {
            _shellView.IsDialogOpen = true;
            _shellView.DialogContent = Messages.InfoStartingMeetingSdk;
        }

        public void SetSelectedMenu(string menuName)
        {
            foreach (var item in _shellView.ListBoxMenu.Items)
            {
                if (item.GetType().Name == menuName)
                {
                    _shellView.ListBoxMenu.SelectedItem = item;
                }
            }
        }

        public void SetTopMost(bool isTopMost)
        {
            _shellView.Topmost = isTopMost;
        }
    }
}
