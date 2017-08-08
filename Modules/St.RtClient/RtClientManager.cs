using System.Threading.Tasks;
using Serilog;
using St.Common;
using St.Common.Contract;

namespace St.RtClient
{
    public class RtClientManager
    {
        public static readonly RtClientManager Instance = new RtClientManager();

        private RtClientManager()
        {

        }

        public void SigninRtServiceBackground()
        {
            Task.Run(() =>
            {
                Log.Logger.Debug($"【login step3】：sign in st server begins");

                var rtService = DependencyResolver.Current.GetService<IRtClientService>();
                var bmsService = DependencyResolver.Current.GetService<IBms>();
                var userInfo = DependencyResolver.Current.GetService<UserInfo>();

                rtService.Start(bmsService.AccessToken);
                var connected = rtService.IsConnected();
                Log.Logger.Debug($"【check rt server connected】：{connected}");
                if (connected)
                {
                    rtService.SignIn(userInfo.OpenId);
                }
                Log.Logger.Debug($"【login step3】：sign in st server ends");
            });

            //TODO,how to catch exception of the action running in the task.
        }
    }
}