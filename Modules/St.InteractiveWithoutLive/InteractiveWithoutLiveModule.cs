using Prism.Modularity;
using Prism.Regions;
using St.Common;
using St.InteractiveWithouLive;


namespace St.InteractiveWithoutLive
{
    public class InteractiveWithoutLiveModule : IModule
    {
        private readonly IRegionManager _regionManager;
        public InteractiveWithoutLiveModule(IRegionManager regionManager)
        {
            _regionManager = regionManager;

        }

        public void Initialize()
        {
            //if (GlobalData.Instance.ActiveModules.Contains(LessonType.InteractiveWithoutLive.ToString()))
            //{
                _regionManager.RegisterViewWithRegion(RegionNames.ContentRegion,
                    typeof(InteractiveWithouLiveContentView));
                _regionManager.RegisterViewWithRegion(RegionNames.NavRegion, typeof(InteractiveWithouLiveNavView));
            //}
        }
    }
}
