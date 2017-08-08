using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using WindowsInput.Native;
using Caliburn.Micro;
using Common;
using MeetingSdk.SdkWrapper;
using MeetingSdk.SdkWrapper.MeetingDataModel;
using Prism.Commands;
using St.Common;

namespace St.Discussion
{
    public class ManageAttendeeListViewModel
    {
        private readonly ManageAttendeeListView _manageAttendeeListView;
        private readonly IMeeting _sdkService;
        private readonly IViewLayout _viewLayoutService;
        public const string SetSpeaking = "指定发言";
        public const string CancelSpeaking = "取消发言";
        private readonly List<UserInfo> _userInfos;

        public ManageAttendeeListViewModel(ManageAttendeeListView manageAttendeeListView)
        {
            _viewLayoutService = IoC.Get<IViewLayout>();
            _manageAttendeeListView = manageAttendeeListView;
            _manageAttendeeListView.Closing += _manageAttendeeListView_Closing;
            _userInfos = IoC.Get<List<UserInfo>>();

            _sdkService = IoC.Get<IMeeting>();

            RegisterEvents();

            AttendeeItems = new ObservableCollection<AttendeeItem>();

            LoadedCommand = new DelegateCommand(LoadedAsync);
            WindowKeyDownCommand = new DelegateCommand<object>(WindowKeyDownHandlerAsync);
        }

        private void _manageAttendeeListView_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            UnregisterEvents();
        }

        private void UnregisterEvents()
        {
            _sdkService.ViewCreatedEvent -= _sdkService_ViewCreateEvent;
            _sdkService.ViewClosedEvent -= _sdkService_ViewCloseEvent;
            _sdkService.OtherStartSpeakEvent -= OtherStartSpeakEventHandler;
            _sdkService.OtherStopSpeakEvent -= OtherStopSpeakEventHandler;

        }

        private void RegisterEvents()
        {
            //_sdkService.RequireUserSpeakEvent += _sdkService_RequireUserSpeakEvent;
            //_sdkService.RequireUserStopSpeakEvent += _sdkService_RequireUserStopSpeakEvent;
            _sdkService.ViewCreatedEvent += _sdkService_ViewCreateEvent;
            _sdkService.ViewClosedEvent += _sdkService_ViewCloseEvent;
            _sdkService.OtherStartSpeakEvent += OtherStartSpeakEventHandler;
            _sdkService.OtherStopSpeakEvent += OtherStopSpeakEventHandler;

        }

        private void OtherStopSpeakEventHandler(Participant participant)
        {
            var attendeeItem = AttendeeItems.FirstOrDefault(attendee => attendee.Id == participant.PhoneId);
            if (attendeeItem != null) attendeeItem.Content = SetSpeaking;
        }

        private void OtherStartSpeakEventHandler(Participant participant)
        {
            var attendeeItem = AttendeeItems.FirstOrDefault(attendee => attendee.Id == participant.PhoneId);
            if (attendeeItem != null) attendeeItem.Content = CancelSpeaking;
        }

        private void _sdkService_ViewCloseEvent(ParticipantView speakerView)
        {
            var targetAttendee = AttendeeItems.FirstOrDefault(
                attendee => attendee.Id == speakerView.Participant.PhoneId && attendee.Hwnd == speakerView.Hwnd);

            if (targetAttendee != null) targetAttendee.Content = SetSpeaking;
        }

        private void _sdkService_ViewCreateEvent(ParticipantView speakerView)
        {
            var targetAttendee = AttendeeItems.FirstOrDefault(
                attendee => attendee.Id == speakerView.Participant.PhoneId && attendee.Hwnd == speakerView.Hwnd);

            if (targetAttendee != null) targetAttendee.Content = CancelSpeaking;
        }

        private void WindowKeyDownHandlerAsync(object args)
        {
            KeyEventArgs keyEventArgs = args as KeyEventArgs;
            if (keyEventArgs != null)
            {
                switch (keyEventArgs.Key)
                {
                    case Key.Escape:
                        _manageAttendeeListView.Close();
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
            var participants = _sdkService.GetParticipants();

            participants.ForEach(view =>
            {
                AttendeeItems.Add(new AttendeeItem()
                {
                    Text =
                        view.PhoneId == _sdkService.SelfPhoneId
                            ? "我"
                            : _userInfos.FirstOrDefault(user => user.GetNube() == view.PhoneId)?.Name,
                    Content = view.IsSpeaking ? CancelSpeaking : SetSpeaking,
                    Id = view.PhoneId,
                    ButtonCommand = view.PhoneId == _sdkService.SelfPhoneId
                        ? new DelegateCommand<AttendeeItem>(async (self) =>
                        {
                            switch (self.Content)
                            {
                                case CancelSpeaking:
                                    AsyncCallbackMsg stopSpeakMsg = await _sdkService.StopSpeak();
                                    if (stopSpeakMsg.HasError)
                                    {
                                        self.Content = CancelSpeaking;
                                        MessageQueueManager.Instance.AddInfo(stopSpeakMsg.Message);
                                    }
                                    else
                                    {
                                        self.Content = SetSpeaking;
                                    }
                                    break;

                                case SetSpeaking:
                                    AsyncCallbackMsg startSpeakMsg = await _sdkService.ApplyToSpeak();
                                    if (startSpeakMsg.HasError)
                                    {
                                        self.Content = SetSpeaking;
                                        MessageQueueManager.Instance.AddInfo(startSpeakMsg.Message);
                                    }
                                    else
                                    {
                                        self.Content = CancelSpeaking;
                                    }
                                    break;
                            }
                        })
                        : new DelegateCommand<AttendeeItem>((attendeeItem) =>
                        {

                            switch (attendeeItem.Content)
                            {
                                case CancelSpeaking:

                                    //AsyncCallbackMsg stopCallbackMsg = _sdkService.RequireUserStopSpeak(attendeeItem.Id);
                                    int stopSpeakCmd = (int) UiMessage.BannedToSpeak;
                                    AsyncCallbackMsg sendStopSpeakMsg = _sdkService.SendMessage(stopSpeakCmd,
                                        stopSpeakCmd.ToString(), stopSpeakCmd.ToString().Length, attendeeItem.Id);

                                    if (sendStopSpeakMsg.HasError)
                                    {
                                        attendeeItem.Content = CancelSpeaking;
                                        MessageQueueManager.Instance.AddInfo(sendStopSpeakMsg.Message);
                                    }
                                    else
                                    {
                                        attendeeItem.Content = SetSpeaking;
                                    }

                                    break;
                                case SetSpeaking:
                                    //AsyncCallbackMsg startCallbackMsg = _sdkService.RequireUserSpeak(attendeeItem.Id);

                                    int startSpeakCmd = (int) UiMessage.AllowToSpeak;
                                    AsyncCallbackMsg sendStartSpeakMsg = _sdkService.SendMessage(startSpeakCmd,
                                        startSpeakCmd.ToString(), startSpeakCmd.ToString().Length, attendeeItem.Id);


                                    if (sendStartSpeakMsg.HasError)
                                    {
                                        attendeeItem.Content = SetSpeaking;
                                        MessageQueueManager.Instance.AddInfo(sendStartSpeakMsg.Message);
                                    }
                                    else
                                    {
                                        attendeeItem.Content = CancelSpeaking;
                                    }

                                    break;
                            }
                        })
                });
            });

            //InputSimulatorManager.Instance.Simulator.Keyboard.KeyPress(VirtualKeyCode.TAB);
            InputSimulatorManager.Instance.Simulator.Keyboard.KeyPress(VirtualKeyCode.TAB);
        }

        public ObservableCollection<AttendeeItem> AttendeeItems { get; set; }
        public ICommand LoadedCommand { get; set; }
        public ICommand WindowKeyDownCommand { get; set; }
    }
}