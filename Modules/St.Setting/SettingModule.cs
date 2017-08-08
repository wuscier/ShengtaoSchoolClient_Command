using Prism.Modularity;
using Prism.Regions;
using St.Common;

namespace St.Setting
{
    public class SettingModule :IModule
    {
        private readonly IRegionManager _regionManager;
        public SettingModule(IRegionManager regionManager)
        {
            _regionManager = regionManager;
        }


        public void Initialize()
        {
            _regionManager.RegisterViewWithRegion(RegionNames.NavRegion, typeof(SettingNavView));
            _regionManager.RegisterViewWithRegion(RegionNames.ContentRegion, typeof(SettingContentView));
        }
    }
}
