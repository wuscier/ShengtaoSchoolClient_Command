using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Prism.Commands;
using Prism.Modularity;
using Prism.Regions;
using St.Common;
using System.Windows.Controls;
using System.Windows.Interop;
using Caliburn.Micro;
using Common;
using MahApps.Metro.Controls;
using Prism.Events;
using Serilog;
using St.Common.Commands;
using St.Common.Helper;
using LogManager = St.Common.LogManager;

namespace St.Host.Views
{
    /// <summary>
    ///     MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainView : INotifyPropertyChanged
    {
        private const string FirstModuleName = "CollaborativeInfoModule";

        private static readonly Uri FirstViewUri = new Uri(Common.GlobalResources.CollaborativeInfoContentView,
            UriKind.Relative);

        private readonly IRegionManager _regionManager;
        private readonly IGroupManager _groupManager;
        private readonly IEventAggregator _eventAggregator;
        private IntPtr _windowHandle;
        private HwndSource _source;

        public MainView(IRegionManager regionManager, IModuleManager moduleManager)
        {
            InitializeComponent();

            DataContext = this;

            _regionManager = regionManager;
            _groupManager = IoC.Get<IGroupManager>();
            _eventAggregator = IoC.Get<IEventAggregator>();
            _eventAggregator.GetEvent<CommandReceivedEvent>().Subscribe(ExecuteCommand);

            Application.Current.Deactivated += Current_Deactivated;
            Application.Current.Activated += Current_Activated;

            moduleManager.LoadModuleCompleted +=
                (s, e) =>
                {
                    if (e.ModuleInfo.ModuleName == FirstModuleName)
                    {
                        ExecuteGotoPage(GlobalResources.CollaborativeInfoNavView);
                    }
                };

            TopMostTriggerCommand = new DelegateCommand(TriggerTopMost);
            ShowLogCommand = DelegateCommand.FromAsyncHandler(ShowLogAsync);
            ShowHelpCommand = new DelegateCommand(ShowHelp);
            LoadCommand = new DelegateCommand(() =>
            {
                GlobalData.Instance.CurWindowHwnd = new WindowInteropHelper(this).Handle;

                ListBoxItem listBoxItem2 =
                    ListBoxMenu.ItemContainerGenerator.ContainerFromIndex(0) as ListBoxItem;
                if (listBoxItem2 != null)
                {
                    listBoxItem2.IsSelected = true;
                    listBoxItem2.Focus();
                }

                var commandServer = IoC.Get<ICommandServer>();
                commandServer.RunServer();

                if (GlobalData.Instance.RunMode == RunMode.Development)
                {
                    Topmost = false;
                    Width = 1200;
                    Height = 650;
                    WindowState = WindowState.Normal;
                    IsWindowDraggable = true;
                    ResizeMode = ResizeMode.CanResize;
                    ShowMinButton = true;
                    ShowMaxRestoreButton = true;
                }
            });
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            _windowHandle = new WindowInteropHelper(this).Handle;
            _source = HwndSource.FromHwnd(_windowHandle);
            _source?.AddHook(HwndHook);
            
            HotKeyApi.RegisterHotKey(_windowHandle, HotKeyApi.ActivateHotKeyId, HotKeyApi.ModAlt, HotKeyApi.ZKey);
        }

        protected override void OnClosed(EventArgs e)
        {
            _source.RemoveHook(HwndHook);
            HotKeyApi.UnregisterHotKey(_windowHandle, HotKeyApi.ActivateHotKeyId);

            base.OnClosed(e);
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case HotKeyApi.HotKeyMsgId:
                    switch (wParam.ToInt32())
                    {
                        case HotKeyApi.ActivateHotKeyId:
                            int vKey = (((int) lParam >> 16) & 0xFFFF);
                            if (vKey == HotKeyApi.ZKey)
                            {
                                Window actviveWindow = Application.Current.Windows.OfType<Window>()
                                    .SingleOrDefault(w => new WindowInteropHelper(w).Handle == GlobalData.Instance.CurWindowHwnd);
                                if (actviveWindow != null)
                                {
                                    actviveWindow.WindowState = WindowState.Maximized;
                                    actviveWindow.Activate();
                                }
                            }
                            handled = true;
                            break;
                    }
                    break;
            }

            return IntPtr.Zero;
        }

        private void ExecuteCommand(SscCommand command)
        {
            if (!command.Enabled)
            {
                return;
            }

            if (command.Directive == GlobalCommands.Instance.GotoInteractiveCommand.Directive)
            {
                ExecuteGotoPage(GlobalResources.InteractiveNavView);
            }
            else if (command.Directive == GlobalCommands.Instance.GotoDiscussionCommand.Directive)
            {
                ExecuteGotoPage(GlobalResources.DiscussionNavView);
            }
            else if (command.Directive == GlobalCommands.Instance.GotoInteractiveWithoutLiveCommand.Directive)
            {
                ExecuteGotoPage(GlobalResources.InteractiveWithouLiveNavView);
            }

        }

        private void ExecuteGotoPage(string navViewName)
        {
            try
            {
                var targetItem =
                    ListBoxMenu.FindChildren<UserControl>().FirstOrDefault(uc => uc.GetType().Name == navViewName);

                if (targetItem != null)
                {
                    var index = ListBoxMenu.Items.IndexOf(targetItem);

                    ListBoxItem listBoxItem =
                        ListBoxMenu.ItemContainerGenerator.ContainerFromIndex(index) as ListBoxItem;

                    if (listBoxItem != null)
                    {
                        listBoxItem.IsSelected = true;
                        listBoxItem.Focus();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Error($"ExecuteGotoPage => {ex}");

            }
        }

        private void Current_Activated(object sender, EventArgs e)
        {
            GlobalCommands.Instance.SetAppActivatedState(true);
            _eventAggregator.GetEvent<CommandUpdatedEvent>().Publish();
        }

        private void Current_Deactivated(object sender, EventArgs e)
        {
            GlobalCommands.Instance.SetAppActivatedState(false);
            _eventAggregator.GetEvent<CommandUpdatedEvent>().Publish();
        }

        private void ShowHelp()
        {
            string helpMsg = GlobalResources.HelpMessage;

            SscDialog helpSscDialog = new SscDialog(helpMsg);
            helpSscDialog.ShowDialog();
        }

        private void TriggerTopMost()
        {
            Topmost = !Topmost;
            string msg = Topmost ? "当前窗口已经置顶！" : "取消当前窗口置顶！";
            MessageQueueManager.Instance.AddInfo(msg);
        }

        private bool _isDialogOpen;

        public bool IsDialogOpen
        {
            get { return _isDialogOpen; }
            set
            {
                Topmost = !value;
                SetProperty(ref _isDialogOpen, value);
            }
        }

        private string _dialogContent;

        public string DialogContent
        {
            get { return _dialogContent; }
            set { SetProperty(ref _dialogContent, value); }
        }

        public ICommand LoadCommand { get; set; }
        public ICommand TopMostTriggerCommand { get; set; }
        public ICommand ShowLogCommand { get; set; }
        public ICommand ShowHelpCommand { get; set; }

        private async Task ShowLogAsync()
        {
            Topmost = !Topmost;
            await LogManager.ShowLogAsync();
        }

        #region Infrastructure Code

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value))
            {
                return false;
            }

            storage = value;
            OnPropertyChanged(propertyName);

            return true;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }



        #endregion

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            object selectedObj = ListBoxMenu.SelectedItem;

            if (selectedObj == null)
            {
                return;
            }
            string navViewName = selectedObj.GetType().Name;

            switch (navViewName)
            {
                case GlobalResources.SettingNavView:
                    navViewName = GlobalResources.SettingContentView;
                    break;
                case GlobalResources.ProfileNavView:
                    navViewName = GlobalResources.ProfileContentView;
                    break;
                case GlobalResources.InstantMeetingNavView:
                    navViewName = GlobalResources.InstantMeetingContentView;
                    break;
                case GlobalResources.InteractiveNavView:
                    navViewName = GlobalResources.InteractiveContentView;
                    break;
                case GlobalResources.InteractiveWithouLiveNavView:
                    navViewName = GlobalResources.InteractiveWithouLiveContentView;
                    break;
                case GlobalResources.CollaborativeInfoNavView:
                    navViewName = GlobalResources.CollaborativeInfoContentView;
                    break;
                case GlobalResources.DiscussionNavView:
                    navViewName = GlobalResources.DiscussionContentView;
                    break;
            }

            GlobalCommands.Instance.GotoClassCommand.Enabled = false;

            _regionManager.RequestNavigate(RegionNames.ContentRegion, new Uri(navViewName, UriKind.Relative),
                NavigationCallback);
        }

        private void NavigationCallback(NavigationResult navigationResult)
        {
            _groupManager.GotoNewView(navigationResult.Context.Uri.OriginalString);
        }

        //private void SelectedHandler(object sender, RoutedEventArgs e)
        //{
        //    ListBoxItem listBoxItem = sender as ListBoxItem;
        //    listBoxItem?.Focus();
        //}

        private void PreviewLostKeyboardFocusHandler(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (sender is ListBoxItem)
            {
                ListBoxMenu.SelectedItem = null;
            }
        }
    }
}