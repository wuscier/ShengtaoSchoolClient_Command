using Prism.Modularity;
using Prism.Regions;
using St.Common;

namespace St.CollaborativeInfo
{
    public class CollaborativeInfoModule : IModule
    {
        private readonly IRegionManager _regionManager;

        public CollaborativeInfoModule()
        {
            
        }

        public CollaborativeInfoModule(IRegionManager regionManager)
        {
            _regionManager = regionManager;
        }

        public void Initialize()
        {
            _regionManager.RegisterViewWithRegion(RegionNames.NavRegion, typeof(CollaborativeInfoNavView));
            _regionManager.RegisterViewWithRegion(RegionNames.ContentRegion, typeof(CollaborativeInfoContentView));
        }
    }
}
