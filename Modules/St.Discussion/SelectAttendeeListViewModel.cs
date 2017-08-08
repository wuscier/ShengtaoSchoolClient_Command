using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using WindowsInput.Native;
using Caliburn.Micro;
using Common;
using MeetingSdk.SdkWrapper;
using Prism.Commands;
using St.Common;

namespace St.Discussion
{
    public class SelectAttendeeListViewModel
    {
        private readonly SelectAttendeeListView _selectAttendeeListView;
        private readonly IMeeting _sdkService;
        private readonly List<UserInfo> _userInfos;
        private readonly SpecialViewType _targetSpecialViewType;
        private readonly IViewLayout _viewLayoutService;

        public SelectAttendeeListViewModel(SelectAttendeeListView selectAttendeeListView, SpecialViewType specialViewType)
        {
            _selectAttendeeListView = selectAttendeeListView;
            _targetSpecialViewType = specialViewType;

            _sdkService = IoC.Get<IMeeting>();
            _userInfos = IoC.Get<List<UserInfo>>();
            _viewLayoutService = IoC.Get<IViewLayout>();

            AttendeeItems = new ObservableCollection<AttendeeItem>();

            LoadedCommand = new DelegateCommand(LoadedAsync);
            WindowKeyDownCommand = new DelegateCommand<object>(WindowKeyDownHandlerAsync);
        }

        private void WindowKeyDownHandlerAsync(object args)
        {
            KeyEventArgs keyEventArgs = args as KeyEventArgs;
            if (keyEventArgs != null)
            {
                switch (keyEventArgs.Key)
                {
                    case Key.Escape:
                        _selectAttendeeListView.Close();
                        break;
                    //case Key.Up:
                    //    InputSimulatorManager.Instance.Simulator.Keyboard.KeyPress(VirtualKeyCode.TAB);
                    //    break;
                    //case Key.Down:
                    //    InputSimulatorManager.Instance.Simulator.Keyboard.KeyPress(VirtualKeyCode.TAB);
                    //    break;
                }
            }
        }

        private void LoadedAsync()
        {
            var openedViews = _viewLayoutService.ViewFrameList.Where(v => v.IsOpened);

            var attendees = from openedView in openedViews
                select new AttendeeItem()
                {
                    Text = openedView.ViewName,
                    Id = openedView.PhoneId,
                    Hwnd = openedView.Hwnd,
                    ButtonCommand = DelegateCommand<AttendeeItem>.FromAsyncHandler(async (attendeeItem) =>
                    {
                        var specialView =
                            _viewLayoutService.ViewFrameList.FirstOrDefault(
                                v => v.PhoneId == attendeeItem.Id && v.Hwnd == attendeeItem.Hwnd);

                        if (!CheckIsUserSpeaking(specialView, true))
                        {
                            return;
                        }

                        _viewLayoutService.SetSpecialView(specialView, _targetSpecialViewType);
                        await _viewLayoutService.LaunchLayout();
                        _selectAttendeeListView.Close();
                    })
                };

            attendees.ToList().ForEach(attendee =>
            {
                AttendeeItems.Add(attendee);
            });

            InputSimulatorManager.Instance.Simulator.Keyboard.KeyPress(VirtualKeyCode.TAB);
            //InputSimulatorManager.Instance.Simulator.Keyboard.KeyPress(VirtualKeyCode.TAB);

        }


        private bool CheckIsUserSpeaking(ViewFrame speakerView, bool showMsgBar = false)
        {
            //return true;


            var speaker =
                _viewLayoutService.ViewFrameList.FirstOrDefault(
                    p => p.PhoneId == speakerView.PhoneId && p.Hwnd == speakerView.Hwnd && p.IsOpened);

            bool isUserNotSpeaking = speaker == null;

            if (isUserNotSpeaking && showMsgBar)
            {
                MessageQueueManager.Instance.AddInfo(Messages.WarningUserNotSpeaking);
            }

            return !isUserNotSpeaking;
        }


        public ObservableCollection<AttendeeItem> AttendeeItems { get; set; }
        public ICommand LoadedCommand { get; set; }
        public ICommand WindowKeyDownCommand { get; set; }
    }
}