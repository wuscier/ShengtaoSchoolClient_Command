using Autofac;
using Prism.Modularity;
using Prism.Regions;
using St.Common;

namespace St.InstantMeeting
{
    public class InstantMeetingModule : IModule
    {
        private readonly IRegionManager _regionManager;
        private readonly IComponentContext _context;
        public InstantMeetingModule(IRegionManager regionManager, IComponentContext context)
        {
            _regionManager = regionManager;
            _context = context;
        }

        public void Initialize()
        {
            _regionManager.RegisterViewWithRegion(RegionNames.NavRegion, typeof(InstantMeetingNavView));
            _regionManager.RegisterViewWithRegion(RegionNames.ContentRegion, typeof(InstantMeetingContentView));
        }
    }
}
