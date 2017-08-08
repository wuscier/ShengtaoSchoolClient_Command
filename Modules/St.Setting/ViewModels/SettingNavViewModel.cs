using Prism.Commands;
using Prism.Regions;
using St.Common;
using System;
using System.Windows.Input;
using Caliburn.Micro;

namespace St.Setting
{
    public class SettingNavViewModel
    {

        public SettingNavViewModel(IRegionManager regionManager)
        {
            _regionManager = regionManager;
            _groupManager = IoC.Get<IGroupManager>();
            NavToContentCommand = new DelegateCommand(NavToContentHandler);
        }

        //private fields
        private readonly IGroupManager _groupManager;
        private readonly IRegionManager _regionManager;
        private static readonly Uri ContentViewUri = new Uri(Common.GlobalResources.SettingContentView, UriKind.Relative);

        //commands
        public ICommand NavToContentCommand { get; set; }

        //command handlers
        private void NavToContentHandler()
        {
            _regionManager.RequestNavigate(RegionNames.ContentRegion, ContentViewUri,NavigationCallback);

        }
        private void NavigationCallback(NavigationResult navigationResult)
        {
            _groupManager.GotoNewView(navigationResult.Context.Uri.OriginalString);
        }

    }
}
