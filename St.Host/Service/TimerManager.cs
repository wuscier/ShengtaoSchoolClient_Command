using System;
using System.Timers;
using System.Windows;
using Caliburn.Micro;
using Serilog;
using St.Common;
using St.RtClient;
using Action = System.Action;

namespace St.Host
{
    public class TimerManager
    {
        private Timer _timer;

        public static TimerManager Instance { get; } = new TimerManager();


        private TimerManager()
        {

        }

        private async void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Log.Logger.Debug("【refresh token begins】");
            IBms bmsService = IoC.Get<IBms>();

            ResponseResult refreshResult;

            if (GlobalData.Instance.Device.EnableLogin)
            {
                refreshResult =
                    await
                        bmsService.GetImeiToken(GlobalData.Instance.Device.Id,
                            GlobalData.Instance.AggregatedConfig.DeviceKey);
            }
            else
            {
                UserInfo userInfo = IoC.Get<UserInfo>();
                refreshResult =
                    await bmsService.ApplyForToken(userInfo.UserName, userInfo.Pwd, GlobalData.Instance.Device.Id);
            }

            Log.Logger.Debug(
                $"【refresh token result】：status={refreshResult.Status}, msg={refreshResult.Message}, data={refreshResult.Data}");

            if (refreshResult.Status == "0")
            {
                IRtClientService rtClientService = IoC.Get<IRtClientService>();

                rtClientService.RefreshToken(bmsService.AccessToken);
            }
            else
            {
                await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    SscDialog refreshTokenDialog =
                        new SscDialog($"{Messages.WarningRefreshTokenFailed}\r\n{refreshResult.Message}");
                    refreshTokenDialog.ShowDialog();
                }));
            }
        }

        public void StartTimer()
        {
            _timer = new Timer(TimeSpan.FromMinutes(50).TotalMilliseconds) {AutoReset = true};
            _timer.Elapsed += _timer_Elapsed;
            _timer.Start();
            Log.Logger.Debug($"【start refresh token timer】：interval={_timer.Interval}ms");
        }

        public void StopTimer()
        {
            _timer.Stop();
            _timer.Close();
        }
    }
}
