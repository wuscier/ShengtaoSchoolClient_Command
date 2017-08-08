using System.Threading.Tasks;
using Caliburn.Micro;
using Serilog;
using St.RtClient;

namespace St.Common.RtClient
{
    public class RtClientManager
    {
        public static readonly RtClientManager Instance = new RtClientManager();

        private RtClientManager()
        {

        }

        public void SigninRtServiceBackground()
        {
            Task.Run(async () =>
            {
                Log.Logger.Debug($"【login step3】：sign in st server begins");

                var rtService = IoC.Get<IRtClientService>();
                var bmsService = IoC.Get<IBms>();
                var userInfo = IoC.Get<UserInfo>();

                await rtService.Start(bmsService.AccessToken);
                var connected = rtService.IsConnected();
                Log.Logger.Debug($"【check rt server connected】：{connected}");
                if (connected)
                {
                    await rtService.SignIn(userInfo.OpenId);
                }
                Log.Logger.Debug($"【login step3】：sign in st server ends");
            });

            //TODO,how to catch exception of the action running in the task.
        }
    }
}