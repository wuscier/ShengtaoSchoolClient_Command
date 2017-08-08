using System.Threading.Tasks;
using System.Windows.Input;
using WindowsInput.Native;
using Caliburn.Micro;
using Prism.Commands;
using St.Common;

namespace St.Discussion
{
    public class LayoutViewModel
    {
        private readonly LayoutView _layoutView;
        private readonly IViewLayout _viewLayoutService;

        public LayoutViewModel(LayoutView layoutView)
        {
            _layoutView = layoutView;

            _viewLayoutService = IoC.Get<IViewLayout>();

            WindowKeyDownCommand = new DelegateCommand<object>(WindowKeyDownHandlerAsync);
            SetAverageLayoutCommand = DelegateCommand.FromAsyncHandler(SetAverageLayoutAsync);
            SelectAttendeeAsBigCommand = new DelegateCommand(SelectAttendeeAsBig);
            SelectAttendeeAsFullCommand = new DelegateCommand(SelectAttendeeAsFull);
            LoadedCommand = new DelegateCommand(() =>
            {
                InputSimulatorManager.Instance.Simulator.Keyboard.KeyPress(VirtualKeyCode.TAB);
            });
        }

        private void SelectAttendeeAsFull()
        {
            SelectAttendeeListView selectAttendeeListView = new SelectAttendeeListView(SpecialViewType.FullScreen);
            selectAttendeeListView.ShowDialog();
        }

        private void SelectAttendeeAsBig()
        {
            SelectAttendeeListView selectAttendeeListView = new SelectAttendeeListView(SpecialViewType.Big);
            selectAttendeeListView.ShowDialog();
        }

        private async Task SetAverageLayoutAsync()
        {
            _viewLayoutService.ChangeViewMode(ViewMode.Average);
            await _viewLayoutService.LaunchLayout();
            _layoutView.Close();
        }

        private void WindowKeyDownHandlerAsync(object args)
        {
            KeyEventArgs keyEventArgs = args as KeyEventArgs;
            if (keyEventArgs != null)
            {
                switch (keyEventArgs.Key)
                {
                    case Key.Escape:
                        _layoutView.Close();
                        break;
                    case Key.Up:
                        InputSimulatorManager.Instance.Simulator.Keyboard.KeyPress(VirtualKeyCode.TAB);
                        break;
                    case Key.Down:
                        InputSimulatorManager.Instance.Simulator.Keyboard.KeyPress(VirtualKeyCode.TAB);
                        break;
                }
            }
        }


        public ICommand WindowKeyDownCommand { get; set; }

        public ICommand SetAverageLayoutCommand { get; set; }
        public ICommand SelectAttendeeAsBigCommand { get; set; }
        public ICommand SelectAttendeeAsFullCommand { get; set; }
        public ICommand LoadedCommand { get; set; }
    }
}