using Prism.Modularity;
using Prism.Regions;
using St.Common;

namespace St.Interactive
{
    public class InteractiveModule : IModule
    {
        private readonly IRegionManager _regionManager;
        
        public InteractiveModule(IRegionManager regionManager)
        {
            _regionManager = regionManager;
        }
        
        public void Initialize()
        {
            //if (GlobalData.Instance.ActiveModules.Contains(LessonType.Interactive.ToString()))
            //{
                _regionManager.RegisterViewWithRegion(RegionNames.NavRegion, typeof(InteractiveNavView));
                _regionManager.RegisterViewWithRegion(RegionNames.ContentRegion, typeof(InteractiveContentView));
            //}
        }
    }
}
