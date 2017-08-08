using Autofac;
using MeetingSdk.SdkWrapper;
using Prism.Modularity;
using St.Common;
using Prism.Regions;
using St.Common.Contract;

namespace St.Core
{
    public class CoreModule : IModule
    {
        private readonly IRegionManager _regionManager;
        private readonly IComponentContext _context;
        public CoreModule(IRegionManager regionManager, IComponentContext context)
        {
            _regionManager = regionManager;
            _context = context;
        }

        public CoreModule()
        {
            
        }

        public void Initialize()
        {
            _context.Register(builder =>
            {
                builder.RegisterType<BmsService>().As<IBms>().SingleInstance();
                builder.RegisterType<MeetingService>().As<IMeeting>().SingleInstance();
                builder.RegisterType<ViewLayoutService>().As<IViewLayout>().SingleInstance();
                builder.RegisterType<GroupManager>().As<IGroupManager>().SingleInstance();
            });
        }
    }
}