using Autofac;
using Prism.Modularity;
using Prism.Regions;
using St.Common.Contract;

namespace St.RtClient
{
    public class RtClientModule : IModule
    {
        private readonly IRegionManager _regionManager;
        private readonly IComponentContext _context;
        public RtClientModule(IRegionManager regionManager, IComponentContext context)
        {
            _regionManager = regionManager;
            _context = context;
        }

        public void Initialize()
        {
            _context.Register(builder =>
            {
                builder.RegisterType<RtClientService>().As<IRtClientService>().SingleInstance();
            });
        }
    }
}
