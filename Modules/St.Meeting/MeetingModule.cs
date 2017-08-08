using Autofac;
using St.Common;
using Prism.Modularity;
using St.Common.Contract;


namespace St.Meeting
{
    public class MeetingModule : IModule
    {
        private readonly IComponentContext _context;
        public MeetingModule(IComponentContext context)
        {
            _context = context;
        }

        public void Initialize()
        {
            _context.Register(builder =>
            {
                builder.RegisterType<MeetingService>().As<IMeetingTrigger>();
                builder.RegisterType<LocalPushLiveService>().Named<IPushLive>(GlobalResources.LocalPushLive).SingleInstance();
                builder.RegisterType<ServerPushLiveService>().Named<IPushLive>(GlobalResources.RemotePushLive).SingleInstance();
                builder.RegisterType<LocalRecordService>().As<IRecord>().SingleInstance();
            });
        }
    }
}
