using Autofac;
using Prism.Modularity;
using Prism.Regions;
using St.Common;

namespace St.Discussion
{
    public class DiscussionModule: IModule
    {
        private readonly IRegionManager _regionManager;
        private readonly IComponentContext _context;
        public DiscussionModule(IRegionManager regionManager, IComponentContext context)
        {
            _regionManager = regionManager;
            _context = context;
        }

        public void Initialize()
        {
            //if (GlobalData.Instance.ActiveModules.Contains(Common.LessonType.Discussion.ToString()))
            //{
                _regionManager.RegisterViewWithRegion(RegionNames.NavRegion, typeof(DiscussionNavView));
                _regionManager.RegisterViewWithRegion(RegionNames.ContentRegion, typeof(DiscussionContentView));
            //}
        }

    }
}