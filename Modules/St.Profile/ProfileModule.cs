using Prism.Modularity;
using Prism.Regions;
using St.Common;

namespace St.Profile
{
    public class ProfileModule : IModule
    {
        private readonly IRegionManager _regionManager;
        
        public ProfileModule(IRegionManager regionManager)
        {
            _regionManager = regionManager;
        }
        
        public void Initialize()
        {
            _regionManager.RegisterViewWithRegion(RegionNames.NavRegion, typeof(ProfileNavView));
            _regionManager.RegisterViewWithRegion(RegionNames.ContentRegion, typeof(ProfileContentView));
        }
    }
}
