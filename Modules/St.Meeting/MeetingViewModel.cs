using Prism.Commands;
using St.Common;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using Caliburn.Micro;
using Common;
using MeetingSdk.SdkWrapper;
using MeetingSdk.SdkWrapper.MeetingDataModel;
using Prism.Events;
using MenuItem = System.Windows.Controls.MenuItem;
using Serilog;
using St.Common.Commands;
using Action = System.Action;
using LogManager = St.Common.LogManager;

namespace St.Meeting
{
    public class MeetingViewModel : ViewModelBase,IExitMeeting
    {
        protected override bool HasErrorMsg(string status, string msg)
        {
            IsMenuOpen = true;
            return base.HasErrorMsg(status, msg);
        }

        public MeetingViewModel(MeetingView meetingView, Action<bool, string> startMeetingCallback,
            Action<bool, string> exitMeetingCallback)
        {
            _meetingView = meetingView;

            _viewLayoutService = IoC.Get<IViewLayout>();
            _viewLayoutService.ViewFrameList = InitializeViewFrameList(meetingView);

            _sdkService = IoC.Get<IMeeting>();
            _bmsService = IoC.Get<IBms>();

            _eventAggregator = IoC.Get<IEventAggregator>();
            _eventAggregator.GetEvent<CommandReceivedEvent>()
                .Subscribe(ExecuteCommand, ThreadOption.PublisherThread, false, command => command.IsIntoClassCommand);

            _localPushLiveService = IoC.Get<IPushLive>(GlobalResources.LocalPushLive);
            _localPushLiveService.ResetStatus();
            _serverPushLiveService = IoC.Get<IPushLive>(GlobalResources.RemotePushLive);
            _serverPushLiveService.ResetStatus();
            _localRecordService = IoC.Get<IRecord>();
            _localRecordService.ResetStatus();

            _startMeetingCallbackEvent = startMeetingCallback;
            _exitMeetingCallbackEvent = exitMeetingCallback;

            MeetingId = _sdkService.MeetingId;
            SpeakingStatus = IsNotSpeaking;
            //SelfDescription = $"{_sdkService.SelfName}-{_sdkService.SelfPhoneId}";

            _lessonDetail = IoC.Get<LessonDetail>();
            _userInfo = IoC.Get<UserInfo>();
            _userInfos = IoC.Get<List<UserInfo>>();

            MeetingOrLesson = _lessonDetail.Id == 0 ? "会议号:" : "课程号:";
            LessonName = string.IsNullOrEmpty(_lessonDetail.Name)
                ? string.Empty
                : string.Format($"课程名:{_lessonDetail.Name}");

            LoadCommand = DelegateCommand.FromAsyncHandler(LoadAsync);
            ModeChangedCommand = DelegateCommand<string>.FromAsyncHandler(MeetingModeChangedAsync);
            SpeakingStatusChangedCommand = DelegateCommand.FromAsyncHandler(SpeakingStatusChangedAsync);
            ExternalDataChangedCommand = DelegateCommand<string>.FromAsyncHandler(ExternalDataChangedAsync);
            SharingDesktopCommand = DelegateCommand.FromAsyncHandler(SharingDesktopAsync);
            CancelSharingCommand = DelegateCommand.FromAsyncHandler(CancelSharingAsync);
            ExitCommand = DelegateCommand.FromAsyncHandler(ExitAsync);
            OpenExitDialogCommand = DelegateCommand.FromAsyncHandler(OpenExitDialogAsync);
            KickoutCommand = new DelegateCommand<string>(KickoutAsync);
            OpenCloseCameraCommand = DelegateCommand.FromAsyncHandler(OpenCloseCameraAsync);
            GetCameraInfoCommand = DelegateCommand<string>.FromAsyncHandler(GetCameraInfoAsync);
            OpenPropertyPageCommand = DelegateCommand<string>.FromAsyncHandler(OpenPropertyPageAsync);
            SetDefaultDataCameraCommand = DelegateCommand<string>.FromAsyncHandler(SetDefaultDataCameraAsync);
            SetDefaultFigureCameraCommand = DelegateCommand<string>.FromAsyncHandler(SetDefaultFigureCameraAsync);
            SetMicStateCommand = DelegateCommand.FromAsyncHandler(SetMicStateAsync);
            ScreenShareCommand = DelegateCommand.FromAsyncHandler(ScreenShareAsync);
            StartSpeakCommand = new DelegateCommand<string>(StartSpeakAsync);
            StopSpeakCommand = new DelegateCommand<string>(StopSpeakAsync);
            RecordCommand = DelegateCommand.FromAsyncHandler(RecordAsync);
            PushLiveCommand = DelegateCommand.FromAsyncHandler(PushLiveAsync);
            WindowKeyDownCommand = new DelegateCommand<object>(WindowKeyDownHandler);

            TopMostTriggerCommand = new DelegateCommand(() =>
            {
                IsTopMost = !IsTopMost;
                string msg = IsTopMost ? "当前窗口已经置顶！" : "取消当前窗口置顶！";
                MessageQueueManager.Instance.AddInfo(msg);
            });
            ShowLogCommand = new DelegateCommand(async () =>
            {
                IsTopMost = false;
                await LogManager.ShowLogAsync();
            });
            ShowHelpCommand = new DelegateCommand(ShowHelp);

            InitializeMenuItems();
            RegisterMeetingEvents();
        }

        private async void ExecuteCommand(SscCommand command)
        {
            if (!command.Enabled)
            {
                return;
            }

            if (command.Directive == GlobalCommands.Instance.ExitClassCommand.Directive)
            {
                await OpenExitDialogAsync();
            }
            else if (command.Directive == GlobalCommands.Instance.SpeakCommand.Directive)
            {
                await SpeakingStatusChangedAsync();
            }
            else if (command.Directive == GlobalCommands.Instance.RecordCommand.Directive)
            {
                // monitor click operation
                RecordChecked = !RecordChecked;
                await RecordAsync();
            }
            else if (command.Directive == GlobalCommands.Instance.PushLiveCommand.Directive)
            {
                PushLiveChecked = !PushLiveChecked;
                await PushLiveAsync();
            }
            else if (command.Directive == GlobalCommands.Instance.DocCommand.Directive)
            {
                if (SharingVisibility == Visibility.Visible)
                {
                    var docItem = SharingMenuItems.First(menu => menu.Header.ToString() != "桌面");
                    if (docItem != null && docItem.HasItems)
                    {
                        MenuItem docMenuItem = docItem.Items[0] as MenuItem;
                        if (docMenuItem == null) return;
                        await ExternalDataChangedAsync(docMenuItem.Header.ToString());
                    }
                }
                else
                {
                    await CancelSharingAsync();
                }
            }
            else if (command.Directive == GlobalCommands.Instance.AverageCommand.Directive)
            {
                await ViewModeChangedAsync(ViewMode.Average);
            }
            else if (command.Directive == GlobalCommands.Instance.BigSmallsCommand.Directive)
            {
                var openedVfs = _viewLayoutService.ViewFrameList.Where(vf => vf.IsOpened).ToList();

                int viewCount = openedVfs.Count;

                if (viewCount == 0)
                {
                    return;
                }

                int bigViewIndex = -1;
                for (int i = 0; i < viewCount; i++)
                {
                    if (openedVfs[i].IsBigView)
                    {
                        bigViewIndex = i;
                        break;
                    }
                }

                if (bigViewIndex == -1)
                {
                    bigViewIndex = 0;
                }
                else if (bigViewIndex + 1 >= openedVfs.Count)
                {
                    bigViewIndex = 0;
                }
                else
                {
                    bigViewIndex += 1;
                }

                await BigViewChangedAsync(openedVfs[bigViewIndex]);

            }
            else if (command.Directive == GlobalCommands.Instance.CloseupCommand.Directive)
            {
                var openedVfs = _viewLayoutService.ViewFrameList.Where(vf => vf.IsOpened).ToList();

                int viewCount = openedVfs.Count;

                if (viewCount == 0)
                {
                    return;
                }

                int fullViewIndex = -1;
                for (int i = 0; i < viewCount; i++)
                {
                    if (openedVfs[i] == _viewLayoutService.FullScreenView)
                    {
                        fullViewIndex = i;
                        break;
                    }
                }

                if (fullViewIndex == -1)
                {
                    fullViewIndex = 0;
                }
                else if (fullViewIndex + 1 >= openedVfs.Count)
                {
                    fullViewIndex = 0;
                }
                else
                {
                    fullViewIndex += 1;
                }

                await FullScreenViewChangedAsync(openedVfs[fullViewIndex]);
            }
            else if (command.Directive == GlobalCommands.Instance.InteractionCommand.Directive)
            {
                await MeetingModeChangedAsync(MeetingMode.Interaction.ToString());
            }
            else if (command.Directive == GlobalCommands.Instance.SpeakerCommand.Directive)
            {
                await MeetingModeChangedAsync(MeetingMode.Speaker.ToString());
            }
            else if (command.Directive == GlobalCommands.Instance.ShareCommand.Directive)
            {
                await MeetingModeChangedAsync(MeetingMode.Sharing.ToString());
            }
        }

        private void ShowHelp()
        {
            string helpMsg = GlobalResources.HelpMessage;

            SscDialog helpSscDialog = new SscDialog(helpMsg);
            helpSscDialog.ShowDialog();
        }


        private void HandleKeyDown(Key key)
        {
            switch (key)
            {
                case Key.Left:
                    _pressedUpKeyCount = 0;
                    _pressedDownKeyCount = 0;
                    _pressedRightKeyCount = 0;

                    _pressedLeftKeyCount++;

                    break;
                case Key.Right:
                    _pressedUpKeyCount = 0;
                    _pressedDownKeyCount = 0;
                    _pressedLeftKeyCount = 0;

                    _pressedRightKeyCount++;

                    break;
                case Key.Up:
                    _pressedDownKeyCount = 0;
                    _pressedLeftKeyCount = 0;
                    _pressedRightKeyCount = 0;

                    _pressedUpKeyCount++;

                    if (_pressedUpKeyCount == 4)
                    {
                        _pressedUpKeyCount = 0;
                        _sdkService.ShowQosTool();
                    }

                    break;
                case Key.Down:
                    _pressedUpKeyCount = 0;
                    _pressedLeftKeyCount = 0;
                    _pressedRightKeyCount = 0;

                    _pressedDownKeyCount++;

                    if (_pressedDownKeyCount == 4)
                    {
                        _pressedDownKeyCount = 0;
                        _sdkService.CloseQosTool();
                    }

                    break;
                case Key.Enter:
                    IsMenuOpen = true;
                    _pressedUpKeyCount = 0;
                    _pressedDownKeyCount = 0;
                    _pressedLeftKeyCount = 0;
                    _pressedRightKeyCount = 0;
                    break;

                case Key.Escape:
                    IsMenuOpen = false;
                    _pressedUpKeyCount = 0;
                    _pressedDownKeyCount = 0;
                    _pressedLeftKeyCount = 0;
                    _pressedRightKeyCount = 0;
                    break;
                default:
                    _pressedUpKeyCount = 0;
                    _pressedDownKeyCount = 0;
                    _pressedLeftKeyCount = 0;
                    _pressedRightKeyCount = 0;
                    break;
            }
        }

        private  void WindowKeyDownHandler(object obj)
        {
            var keyEventArgs = obj as KeyEventArgs;
            HandleKeyDown(keyEventArgs.Key);
        }

        #region private fields

        private readonly MeetingView _meetingView;

        private delegate Task TaskDelegate();

        private TaskDelegate _cancelSharingAction;

        private readonly IEventAggregator _eventAggregator;
        private readonly IViewLayout _viewLayoutService;
        private readonly IMeeting _sdkService;
        private readonly IBms _bmsService;
        private readonly IPushLive _localPushLiveService;
        private readonly IPushLive _serverPushLiveService;
        private readonly IRecord _localRecordService;
        private readonly LessonDetail _lessonDetail;
        private readonly UserInfo _userInfo;
        private readonly List<UserInfo> _userInfos;
        private int _pressedUpKeyCount = 0;
        private int _pressedDownKeyCount = 0;
        private int _pressedLeftKeyCount = 0;
        private int _pressedRightKeyCount = 0;
        private bool _exitByDialog = false;

        private readonly Action<bool, string> _startMeetingCallbackEvent;
        private readonly Action<bool, string> _exitMeetingCallbackEvent;
        private const string IsSpeaking = "取消发言";
        private const string IsNotSpeaking = "发 言";

        #endregion

        #region public properties

        public ViewFrame ViewFrame1 { get; private set; }
        public ViewFrame ViewFrame2 { get; private set; }
        public ViewFrame ViewFrame3 { get; private set; }
        public ViewFrame ViewFrame4 { get; private set; }
        public ViewFrame ViewFrame5 { get; private set; }

        public ObservableCollection<MenuItem> ModeMenuItems { get; set; }
        public ObservableCollection<MenuItem> LayoutMenuItems { get; set; }
        public ObservableCollection<MenuItem> SharingMenuItems { get; set; }


        private string _meetingOrLesson;

        public string MeetingOrLesson
        {
            get { return _meetingOrLesson; }
            set { SetProperty(ref _meetingOrLesson, value); }
        }

        private string _lessonName;

        public string LessonName
        {
            get { return _lessonName; }
            set { SetProperty(ref _lessonName, value); }
        }


        //private string _selfDescription;

        //public string SelfDescription
        //{
        //    get { return _selfDescription; }
        //    set { SetProperty(ref _selfDescription, value); }
        //}

        private int _meetingId;

        public int MeetingId
        {
            get { return _meetingId; }
            set { SetProperty(ref _meetingId, value); }
        }


        private string _pushLiveStreamTips;

        public string PushLiveStreamTips
        {
            get { return _pushLiveStreamTips; }
            set { SetProperty(ref _pushLiveStreamTips, value); }
        }

        private string _recordTips;

        public string RecordTips
        {
            get { return _recordTips; }
            set { SetProperty(ref _recordTips, value); }
        }

        private string _selectedCamera;

        public string SelectedCamera
        {
            get { return _selectedCamera; }
            set { SetProperty(ref _selectedCamera, value); }
        }

        private string _openCloseCameraOperation = "open camera";

        public string OpenCloseCameraOperation
        {
            get { return _openCloseCameraOperation; }
            set { SetProperty(ref _openCloseCameraOperation, value); }
        }

        private string _openCloseDataOperation = "open data";

        public string OpenCloseDataOperation
        {
            get { return _openCloseDataOperation; }
            set { SetProperty(ref _openCloseDataOperation, value); }
        }

        private string _micState = "静音";

        public string MicState
        {
            get { return _micState; }
            set { SetProperty(ref _micState, value); }
        }

        private string _screenShareState = "共享屏幕";

        public string ScreenShareState
        {
            get { return _screenShareState; }
            set { SetProperty(ref _screenShareState, value); }
        }

        private string _phoneId;

        public string PhoneId
        {
            get { return _phoneId; }
            set { SetProperty(ref _phoneId, value); }
        }

        private string _startStopSpeakOperation = "发言";

        public string StartStopSpeakOperation
        {
            get { return _startStopSpeakOperation; }
            set { SetProperty(ref _startStopSpeakOperation, value); }
        }

        private bool _allowedToSpeak = true;

        public bool AllowedToSpeak
        {
            get { return _allowedToSpeak; }
            set { SetProperty(ref _allowedToSpeak, value); }
        }

        private string _phoneIds;

        public string PhoneIds
        {
            get { return _phoneIds; }
            set { SetProperty(ref _phoneIds, value); }
        }

        private bool _recordChecked;

        public bool RecordChecked
        {
            get { return _recordChecked; }
            set
            {
                if (!value)
                {
                    RecordTips = null;
                }
                SetProperty(ref _recordChecked, value);
            }
        }

        private bool _pushLiveChecked;

        public bool PushLiveChecked
        {
            get { return _pushLiveChecked; }
            set
            {
                if (!value)
                {
                    PushLiveStreamTips = null;
                }
                SetProperty(ref _pushLiveChecked, value);
            }
        }

        private string _speakingStatus;

        public string SpeakingStatus
        {
            get { return _speakingStatus; }
            set { SetProperty(ref _speakingStatus, value); }
        }

        private Visibility _sharingVisibility;

        public Visibility SharingVisibility
        {
            get { return _sharingVisibility; }
            set { SetProperty(ref _sharingVisibility, value); }
        }

        private Visibility _cancelSharingVisibility;

        public Visibility CancelSharingVisibility
        {
            get { return _cancelSharingVisibility; }
            set { SetProperty(ref _cancelSharingVisibility, value); }
        }

        //private object _dialogContent;

        //public object DialogContent
        //{
        //    get { return _dialogContent; }
        //    set { SetProperty(ref _dialogContent, value); }
        //}

        //private bool _isDialogOpen;

        //public bool IsDialogOpen
        //{
        //    get { return _isDialogOpen; }
        //    set { SetProperty(ref _isDialogOpen, value); }
        //}

        //private string _dialogMsg;

        //public string DialogMsg
        //{
        //    get { return _dialogMsg; }
        //    set { SetProperty(ref _dialogMsg, value); }
        //}

        private Visibility _isSpeaker;

        public Visibility IsSpeaker
        {
            get { return _isSpeaker; }
            set { SetProperty(ref _isSpeaker, value); }
        }

        private string _curModeName;

        public string CurModeName
        {
            get { return _curModeName; }
            set { SetProperty(ref _curModeName, value); }
        }

        private string _curLayoutName;

        public string CurLayoutName
        {
            get { return _curLayoutName; }
            set { SetProperty(ref _curLayoutName, value); }
        }

        private bool _isMenuOpen;
        public bool IsMenuOpen
        {
            get { return _isMenuOpen; }
            set { SetProperty(ref _isMenuOpen, value); }
        }

        private bool _isTopMost = true;

        public bool IsTopMost
        {
            get { return _isTopMost; }
            set
            {
                if (!value)
                {
                    IsMenuOpen = false;
                }
                SetProperty(ref _isTopMost, value);
            }
        }


        #endregion

        #region Commands

        public ICommand LoadCommand { get; set; }
        public ICommand ModeChangedCommand { get; set; }
        public ICommand SpeakingStatusChangedCommand { get; set; }
        public ICommand ExternalDataChangedCommand { get; set; }
        public ICommand SharingDesktopCommand { get; set; }
        public ICommand CancelSharingCommand { get; set; }
        public ICommand ExitCommand { get; set; }
        public ICommand OpenExitDialogCommand { get; set; }
        public ICommand KickoutCommand { get; set; }
        public ICommand OpenCloseCameraCommand { get; set; }
        public ICommand GetCameraInfoCommand { get; set; }
        public ICommand OpenPropertyPageCommand { get; set; }
        public ICommand SetDefaultFigureCameraCommand { get; set; }
        public ICommand SetDefaultDataCameraCommand { get; set; }
        public ICommand SetMicStateCommand { get; set; }
        public ICommand ScreenShareCommand { get; set; }
        public ICommand StartSpeakCommand { get; set; }
        public ICommand StopSpeakCommand { get; set; }
        public ICommand StartDoubleScreenCommand { get; set; }
        public ICommand StopDoubleScreenCommand { get; set; }
        public ICommand StartMonitorStreamCommand { get; set; }
        public ICommand StopMonitorStreamCommand { get; set; }
        public ICommand RecordCommand { get; set; }
        public ICommand PushLiveCommand { get; set; }
        public ICommand TopMostTriggerCommand { get; set; }
        public ICommand ShowLogCommand { get; set; }
        public ICommand TriggerMenuCommand { get; set; }
        public ICommand WindowKeyDownCommand { get; set; }
        public ICommand ShowHelpCommand { get; set; }

        #endregion

        #region Command Handlers

        private void ChangeWindowStyleInDevMode()
        {
            if (GlobalData.Instance.RunMode == RunMode.Development)
            {
                IsTopMost = false;
                _meetingView.UseNoneWindowStyle = false;
                _meetingView.ResizeMode = ResizeMode.CanResize;
                _meetingView.WindowStyle = WindowStyle.SingleBorderWindow;
                _meetingView.IsWindowDraggable = true;
                _meetingView.ShowMinButton = true;
                _meetingView.ShowMaxRestoreButton = true;
                _meetingView.ShowCloseButton = false;
                _meetingView.IsCloseButtonEnabled = false;
            }
        }

        private async Task JoinMeetingAsync()
        {
            GlobalData.Instance.ViewArea = new ViewArea()
            {
                Width = _meetingView.ActualWidth,
                Height = _meetingView.ActualHeight
            };

            uint[] uint32SOfNonDataArray =
            {
                (uint) _meetingView.PictureBox1.Handle.ToInt32(),
                (uint) _meetingView.PictureBox2.Handle.ToInt32(),
                (uint) _meetingView.PictureBox3.Handle.ToInt32(),
                (uint) _meetingView.PictureBox4.Handle.ToInt32(),
            };

            foreach (var hwnd in uint32SOfNonDataArray)
            {
                Log.Logger.Debug($"【figure hwnd】：{hwnd}");
            }

            uint[] uint32SOfDataArray = { (uint)_meetingView.PictureBox5.Handle.ToInt32() };

            foreach (var hwnd in uint32SOfDataArray)
            {
                Log.Logger.Debug($"【data hwnd】：{hwnd}");
            }

            AsyncCallbackMsg joinMeetingResult =
                await
                    _sdkService.JoinMeeting(MeetingId, uint32SOfNonDataArray, uint32SOfNonDataArray.Length,
                        uint32SOfDataArray,
                        uint32SOfDataArray.Length);

            //if failed to join meeting, needs to roll back
            if (joinMeetingResult.Status != 0)
            {
                Log.Logger.Error(
                    $"【join meeting result】：result={joinMeetingResult.Status}, msg={joinMeetingResult.Message}");


                _startMeetingCallbackEvent(false, joinMeetingResult.Message);
                _exitByDialog = true;

                _meetingView.Close();

            }
            else
            {
                //if join meeting successfully, then make main view invisible
                _startMeetingCallbackEvent(true, "");


                //if not speaker, then clear mode menu items
                if (!_sdkService.IsCreator)
                {
                    ModeMenuItems.Clear();
                    IsSpeaker = Visibility.Collapsed;
                }
                else
                {
                    IsSpeaker = Visibility.Visible;
                }

                if (_lessonDetail.Id > 0)
                {
                    ResponseResult result = await
                        _bmsService.UpdateMeetingStatus(_lessonDetail.Id, _userInfo.UserId,
                            DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                            string.Empty);

                    HasErrorMsg(result.Status, result.Message);
                }
            }

        }

        //command handlers
        private async Task LoadAsync()
        {
            GlobalData.Instance.CurWindowHwnd = new WindowInteropHelper(_meetingView).Handle;

            ChangeWindowStyleInDevMode();
            await JoinMeetingAsync();
            GlobalCommands.Instance.SetCommandsStateInNonDiscussionClass();
        }

        private async Task MeetingModeChangedAsync(string meetingMode)
        {
            if (!CheckIsUserSpeaking(true))
            {
                return;
            }

            if (meetingMode == MeetingMode.Speaker.ToString() &&
                !_viewLayoutService.ViewFrameList.Any(
                    v => v.PhoneId == _sdkService.CreatorPhoneId && v.ViewType == 1))
            {
                //如果选中的模式条件不满足，则回滚到之前的模式，
                //没有主讲者视图无法设置主讲模式，没有共享无法共享模式，没有发言无法设置任何模式

                HasErrorMsg("-1", Messages.WarningNoSpeaderView);
                return;
            }

            if (meetingMode == MeetingMode.Sharing.ToString() &&
                !_viewLayoutService.ViewFrameList.Any(
                    v => v.PhoneId == _sdkService.CreatorPhoneId && v.ViewType == 2))
            {
                //如果选中的模式条件不满足，则回滚到之前的模式，
                //没有主讲者视图无法设置主讲模式，没有共享无法共享模式，没有发言无法设置任何模式

                HasErrorMsg("-1", Messages.WarningNoSharingView);
                return;
            }

            var newMeetingMode = (MeetingMode) Enum.Parse(typeof(MeetingMode), meetingMode);

            _viewLayoutService.ChangeMeetingMode(newMeetingMode);

            _viewLayoutService.ResetAsAutoLayout();

            await _viewLayoutService.LaunchLayout();
        }

        private async Task SpeakingStatusChangedAsync()
        {
            if (SpeakingStatus == IsSpeaking)
            {
                AsyncCallbackMsg stopSucceeded = await _sdkService.StopSpeak();
                return;
                //will change SpeakStatus in StopSpeakCallbackEventHandler.
            }

            if (SpeakingStatus == IsNotSpeaking)
            {
                AsyncCallbackMsg result = await _sdkService.ApplyToSpeak();
                if (!HasErrorMsg(result.Status.ToString(), result.Message))
                {
                    // will change SpeakStatus in callback???
                    SpeakingStatus = IsSpeaking;
                }
            }
        }

        private async Task ExternalDataChangedAsync(string sourceName)
        {
            if (!CheckIsUserSpeaking(true))
            {
                return;
            }

            AsyncCallbackMsg openDataResult = await _sdkService.OpenSharedCamera(sourceName);
            if (!HasErrorMsg(openDataResult.Status.ToString(), openDataResult.Message))
            {
                _cancelSharingAction = async () =>
                {
                    AsyncCallbackMsg result = await _sdkService.CloseSharedCamera();
                    if (!HasErrorMsg(result.Status.ToString(), result.Message))
                    {
                        SharingVisibility = Visibility.Visible;
                        CancelSharingVisibility = Visibility.Collapsed;
                    }
                };

                SharingVisibility = Visibility.Collapsed;
                CancelSharingVisibility = Visibility.Visible;
            }
        }

        private async Task SharingDesktopAsync()
        {
            if (!CheckIsUserSpeaking(true))
            {
                return;
            }

            AsyncCallbackMsg startResult = await _sdkService.StartScreenSharing();
            if (!HasErrorMsg(startResult.Status.ToString(), startResult.Message))
            {
                _cancelSharingAction = async () =>
                {
                    AsyncCallbackMsg result = await _sdkService.StopScreenSharing();
                    if (!HasErrorMsg(result.Status.ToString(), result.Message))
                    {
                        SharingVisibility = Visibility.Visible;
                        CancelSharingVisibility = Visibility.Collapsed;
                    }
                };
                SharingVisibility = Visibility.Collapsed;
                CancelSharingVisibility = Visibility.Visible;
            }
        }

        private async Task CancelSharingAsync()
        {
            await _cancelSharingAction();
        }

        public async Task ExitAsync()
        {
            try
            {
                await StopAllLives();

                AsyncCallbackMsg exitResult = await _sdkService.ExitMeeting();
                _viewLayoutService.ResetAsInitialStatus();

                Log.Logger.Debug($"【exit meeting】：result={exitResult.Status}, msg={exitResult.Message}");
                HasErrorMsg(exitResult.Status.ToString(), exitResult.Message);

                UnRegisterMeetingEvents();

                await UpdateExitTime();

                await _meetingView.Dispatcher.BeginInvoke(new Action(() =>
                {
                    _exitByDialog = true;
                    _meetingView.Close();

                    _exitMeetingCallbackEvent(true, "");

                }));

            }
            catch (Exception ex)
            {
                Log.Logger.Error($"ExitAsync => {ex}");
            }
        }

        private async Task StopAllLives()
        {
            await _localPushLiveService.StopPushLiveStream();
            await _serverPushLiveService.StopPushLiveStream();
            await _localRecordService.StopRecord();
        }

        private async Task UpdateExitTime()
        {
            if (_lessonDetail.Id > 0)
            {
                ResponseResult updateResult = await
                    _bmsService.UpdateMeetingStatus(_lessonDetail.Id, _userInfo.UserId,
                        string.Empty, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    );

                HasErrorMsg(updateResult.Status, updateResult.Message);
            }
        }

        private async Task OpenExitDialogAsync()
        {
            await _meetingView.Dispatcher.BeginInvoke(new Action(async () =>
            {
                IsMenuOpen = false;

                YesNoDialog yesNoDialog = new YesNoDialog("确定退出？");
                bool? result = yesNoDialog.ShowDialog();


                if (result.HasValue && result.Value)
                {
                    await ExitAsync();
                }
                else
                {
                    IsMenuOpen = true;
                }
            }));
        }

        private void KickoutAsync(string userPhoneId)
        {
             _sdkService.HostKickoutUser(userPhoneId);
        }

        private async Task OpenCloseCameraAsync()
        {
            if (OpenCloseCameraOperation == "open camera")
            {
                AsyncCallbackMsg result = await _sdkService.OpenCamera(SelectedCamera);
                if (!HasErrorMsg(result.Status.ToString(), result.Message))
                {
                    OpenCloseCameraOperation = "close camera";
                }

            }
            else
            {
                AsyncCallbackMsg result = await _sdkService.CloseCamera();
                if (!HasErrorMsg(result.Status.ToString(), result.Message))
                {
                    OpenCloseCameraOperation = "open camera";
                }

            }
        }

        private async Task OpenPropertyPageAsync(string cameraName)
        {
            await _meetingView.Dispatcher.BeginInvoke(new Action(() =>
            {
                AsyncCallbackMsg result = _sdkService.ShowCameraPropertyPage(cameraName);
            }));
        }

        private async Task GetCameraInfoAsync(string cameraName)
        {
            await Task.Run(() =>
            {
                Camera videoDeviceInfo = _sdkService.GetCameraInfo(cameraName);
            });
        }

        private async Task SetDefaultFigureCameraAsync(string cameraName)
        {
            AsyncCallbackMsg result = await _sdkService.SetDefaultCamera(1, cameraName);
            HasErrorMsg(result.Status.ToString(), result.Message);
        }

        private async Task SetDefaultDataCameraAsync(string cameraName)
        {
            AsyncCallbackMsg result = await _sdkService.SetDefaultCamera(2, cameraName);
            HasErrorMsg(result.Status.ToString(), result.Message);
        }

        private async Task ScreenShareAsync()
        {

            if (ScreenShareState == "共享屏幕")
            {
                AsyncCallbackMsg result = await _sdkService.StartScreenSharing();
                if (!HasErrorMsg(result.Status.ToString(), result.Message))
                {
                    ScreenShareState = "取消屏幕共享";
                }
            }
            else
            {
                AsyncCallbackMsg result = await _sdkService.StopScreenSharing();
                if (!HasErrorMsg(result.Status.ToString(), result.Message))
                {
                    ScreenShareState = "共享屏幕";
                }
            }

        }

        private async Task SetMicStateAsync()
        {
            if (MicState == "静音")
            {
                AsyncCallbackMsg result = await _sdkService.SetMicMuteState(1);
                if (!HasErrorMsg(result.Status.ToString(), result.Message))
                {
                    MicState = "取消静音";
                }
            }
            else
            {
                AsyncCallbackMsg result = await _sdkService.SetMicMuteState(0);
                if (!HasErrorMsg(result.Status.ToString(), result.Message))
                {
                    MicState = "静音";
                }
            }
        }

        private void StartSpeakAsync(string userPhoneId)
        {
             _sdkService.RequireUserSpeak(userPhoneId);
        }

        private void StopSpeakAsync(string userPhoneId)
        {
            _sdkService.RequireUserStopSpeak(userPhoneId);
        }

        private async Task PushLiveAsync()
        {
            if (PushLiveChecked)
            {
                _localPushLiveService.GetLiveParam();

                AsyncCallbackMsg result =
                    await
                        _localPushLiveService.StartPushLiveStream(
                            _viewLayoutService.GetStreamLayout(_localPushLiveService.LiveParam.Width,
                                _localPushLiveService.LiveParam.Height));

                if (HasErrorMsg(result.Status.ToString(), result.Message))
                {
                    PushLiveChecked = false;
                }
                else
                {
                    PushLiveStreamTips =
                        string.Format(
                            $"分辨率：{_localPushLiveService.LiveParam.Width}*{_localPushLiveService.LiveParam.Height}\r\n" +
                            $"码率：{_localPushLiveService.LiveParam.VideoBitrate}\r\n" +
                            $"推流地址：{_localPushLiveService.LiveParam.Url1}");
                }
            }
            else
            {
                AsyncCallbackMsg result = await _localPushLiveService.StopPushLiveStream();
                if (HasErrorMsg(result.Status.ToString(), result.Message))
                {
                    PushLiveChecked = true;
                }
            }
        }

        private async Task RecordAsync()
        {
            if (RecordChecked)
            {
                _localRecordService.GetRecordParam();

                AsyncCallbackMsg result =
                    await
                        _localRecordService.StartRecord(
                            _viewLayoutService.GetStreamLayout(_localRecordService.RecordParam.Width,
                                _localRecordService.RecordParam.Height));

                if (HasErrorMsg(result.Status.ToString(), result.Message))
                {
                    RecordChecked = false;
                }
                else
                {
                    RecordTips =
                        string.Format(
                            $"分辨率：{_localRecordService.RecordParam.Width}*{_localRecordService.RecordParam.Height}\r\n" +
                            $"码率：{_localRecordService.RecordParam.VideoBitrate}\r\n" +
                            $"录制路径：{_localRecordService.RecordDirectory}");
                }
            }
            else
            {
                AsyncCallbackMsg result = await _localRecordService.StopRecord();
                if (HasErrorMsg(result.Status.ToString(), result.Message))
                {
                    RecordChecked = true;
                }
            }
        }


        //dynamic commands
        private async Task ViewModeChangedAsync(ViewMode viewMode)
        {
            if (!CheckIsUserSpeaking(true))
            {
                return;
            }

            _viewLayoutService.ChangeViewMode(viewMode);
            await _viewLayoutService.LaunchLayout();
        }

        private async Task FullScreenViewChangedAsync(ViewFrame fullScreenView)
        {
            if (!CheckIsUserSpeaking(true))
            {
                return;
            }


            if (!CheckIsUserSpeaking(fullScreenView, true))
            {
                return;
            }

            _viewLayoutService.SetSpecialView(fullScreenView, SpecialViewType.FullScreen);

            await _viewLayoutService.LaunchLayout();
        }

        private async Task BigViewChangedAsync(ViewFrame bigView)
        {
            if (!CheckIsUserSpeaking(true))
            {
                return;
            }

            if (_viewLayoutService.ViewFrameList.Count(viewFrame => viewFrame.IsOpened) < 2)
            {
                //一大多小至少有两个视图，否则不予设置

                HasErrorMsg("-1", Messages.WarningBigSmallLayoutNeedsTwoAboveViews);
                return;
            }

            if (!CheckIsUserSpeaking(bigView, true))
            {
                return;
            }

            var bigSpeakerView =
                _viewLayoutService.ViewFrameList.FirstOrDefault(
                    v => v.PhoneId == bigView.PhoneId && v.Hwnd == bigView.Hwnd);

            if (bigSpeakerView == null)
            {
                //LOG ViewFrameList may change during this period.
            }

            _viewLayoutService.SetSpecialView(bigSpeakerView, SpecialViewType.Big);

            await _viewLayoutService.LaunchLayout();
        }

        #endregion

        #region Methods

        private List<ViewFrame> InitializeViewFrameList(MeetingView meetingView)
        {
            List<ViewFrame> viewFrames = new List<ViewFrame>();

            ViewFrame1 = new ViewFrame(meetingView.PictureBox1.Handle, meetingView.PictureBox1, meetingView.Label1);
            ViewFrame2 = new ViewFrame(meetingView.PictureBox2.Handle, meetingView.PictureBox2, meetingView.Label2);
            ViewFrame3 = new ViewFrame(meetingView.PictureBox3.Handle, meetingView.PictureBox3, meetingView.Label3);
            ViewFrame4 = new ViewFrame(meetingView.PictureBox4.Handle, meetingView.PictureBox4, meetingView.Label4);
            ViewFrame5 = new ViewFrame(meetingView.PictureBox5.Handle, meetingView.PictureBox5, meetingView.Label5);

            viewFrames.Add(ViewFrame1);
            viewFrames.Add(ViewFrame2);
            viewFrames.Add(ViewFrame3);
            viewFrames.Add(ViewFrame4);
            viewFrames.Add(ViewFrame5);

            return viewFrames;
        }

        public void RefreshLayoutMenu(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = e.OriginalSource as MenuItem;
            if (menuItem.Header is StackPanel)
            {
                RefreshLayoutMenuItems();
            }
            else
            {
                e.Handled = true;
            }
        }

        private void InitializeMenuItems()
        {
            LoadModeMenuItems();
            RefreshLayoutMenuItems();
            RefreshExternalData();
        }

        private void RegisterMeetingEvents()
        {
            _meetingView.LocationChanged += _meetingView_LocationChanged;
            _meetingView.Deactivated += _meetingView_Deactivated;
            _meetingView.Closing += _meetingView_Closing;
            _sdkService.ViewCreatedEvent += ViewCreateEventHandler;
            _sdkService.ViewClosedEvent += ViewCloseEventHandler;
            _sdkService.StartSpeakEvent += StartSpeakEventHandler;
            _sdkService.StopSpeakEvent += StopSpeakEventHandler;
            _viewLayoutService.MeetingModeChangedEvent += MeetingModeChangedEventHandler;
            _viewLayoutService.ViewModeChangedEvent += ViewModeChangedEventHandler;
            _sdkService.OtherJoinMeetingEvent += OtherJoinMeetingEventHandler;
            _sdkService.OtherExitMeetingEvent += OtherExitMeetingEventHandler;
            _sdkService.TransparentMessageReceivedEvent += UIMessageReceivedEventHandler;
            _sdkService.ErrorMsgReceivedEvent += ErrorMsgReceivedEventHandler;
            _sdkService.KickedByHostEvent += KickedByHostEventHandler;
            _sdkService.DiskSpaceNotEnoughEvent += DiskSpaceNotEnoughEventHandler;
        }

        private void _meetingView_Deactivated(object sender, EventArgs e)
        {
            IsMenuOpen = false;
        }

        private void _meetingView_LocationChanged(object sender, EventArgs e)
        {
            try
            {
                MethodInfo methodInfo = typeof(Popup).GetMethod("UpdatePosition",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (IsMenuOpen)
                {
                    methodInfo.Invoke(_meetingView.TopMenu, null);
                    methodInfo.Invoke(_meetingView.BottomMenu, null);
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Error($"_meetingView_LocationChanged => {ex}");
            }
        }

        private void DiskSpaceNotEnoughEventHandler(AsyncCallbackMsg msg)
        {
            HasErrorMsg(msg.Status.ToString(), msg.Message);
        }

        private async void _meetingView_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (GlobalData.Instance.RunMode == RunMode.Development && !_exitByDialog)
            {
                e.Cancel = true;
                await OpenExitDialogAsync();
            }
        }

        private void UnRegisterMeetingEvents()
        {
            _eventAggregator.GetEvent<CommandReceivedEvent>().Unsubscribe(ExecuteCommand);

            _sdkService.ViewCreatedEvent -= ViewCreateEventHandler;
            _sdkService.ViewClosedEvent -= ViewCloseEventHandler;
            _sdkService.StartSpeakEvent -= StartSpeakEventHandler;
            _sdkService.StopSpeakEvent -= StopSpeakEventHandler;
            _viewLayoutService.MeetingModeChangedEvent -= MeetingModeChangedEventHandler;
            _viewLayoutService.ViewModeChangedEvent -= ViewModeChangedEventHandler;
            _sdkService.OtherJoinMeetingEvent -= OtherJoinMeetingEventHandler;
            _sdkService.OtherExitMeetingEvent -= OtherExitMeetingEventHandler;
            _sdkService.TransparentMessageReceivedEvent -= UIMessageReceivedEventHandler;
            _sdkService.ErrorMsgReceivedEvent -= ErrorMsgReceivedEventHandler;
            _sdkService.KickedByHostEvent -= KickedByHostEventHandler;
            _sdkService.DiskSpaceNotEnoughEvent -= DiskSpaceNotEnoughEventHandler;
        }

        private void KickedByHostEventHandler()
        {
            _meetingView.Dispatcher.BeginInvoke(new Action(() =>
            {
                _exitByDialog = true;

                _meetingView.Close();


                _exitMeetingCallbackEvent(true, "");

                //MetroWindow mainView = App.SSCBootstrapper.Container.ResolveKeyed<MetroWindow>("MainView");
                //mainView.GlowBrush = new SolidColorBrush(Colors.Purple);
                //mainView.NonActiveGlowBrush = new SolidColorBrush((Color) ColorConverter.ConvertFromString("#FF999999"));
                //mainView.Visibility = Visibility.Visible;
            }));
        }

        private void ViewModeChangedEventHandler(ViewMode viewMode)
        {
            CurLayoutName = EnumHelper.GetDescription(typeof(ViewMode), viewMode);
        }

        private void MeetingModeChangedEventHandler(MeetingMode meetingMode)
        {
            CurModeName = EnumHelper.GetDescription(typeof(MeetingMode), meetingMode);

            if (_sdkService.IsCreator)
            {
                AsyncCallbackMsg result =
                        _sdkService.SendMessage((int) _viewLayoutService.MeetingMode,
                            _viewLayoutService.MeetingMode.ToString(), _viewLayoutService.MeetingMode.ToString().Length,
                            null);
                HasErrorMsg(result.Status.ToString(), result.Message);
            }
        }

        private void ErrorMsgReceivedEventHandler(AsyncCallbackMsg error)
        {
            HasErrorMsg("-1", error.Message);
        }

        private void UIMessageReceivedEventHandler(TransparentMessage message)
        {
            if (message.MessageId < 3)
            {
                _sdkService.CreatorPhoneId = message.Sender.PhoneId;

                MeetingMode meetingMode = (MeetingMode) message.MessageId;
                _viewLayoutService.ChangeMeetingMode(meetingMode);

                _viewLayoutService.LaunchLayout();
            }
            else
            {
                if (message.MessageId == (int) UiMessage.BannedToSpeak)
                {
                    AllowedToSpeak = false;
                }
                if (message.MessageId == (int) UiMessage.AllowToSpeak)
                {
                    AllowedToSpeak = true;
                }
            }
        }

        private void OtherExitMeetingEventHandler(Participant contactInfo)
        {
            //var attendee = _userInfos.FirstOrDefault(userInfo => userInfo.GetNube() == contactInfo.m_szPhoneId);

            //string displayName = string.Empty;
            //if (!string.IsNullOrEmpty(attendee?.Name))
            //{
            //    displayName = attendee.Name + " - ";
            //}

            //string exitMsg = $"{displayName}{contactInfo.m_szPhoneId}退出会议！";
            //HasErrorMsg("-1", exitMsg);

            if (contactInfo.PhoneId == _sdkService.CreatorPhoneId)
            {
                //
            }
        }

        private void OtherJoinMeetingEventHandler(Participant contactInfo)
        {
            var attendee = _userInfos.FirstOrDefault(userInfo => userInfo.GetNube() == contactInfo.PhoneId);

            //string displayName = string.Empty;
            //if (!string.IsNullOrEmpty(attendee?.Name))
            //{
            //    displayName = attendee.Name + " - ";
            //}

            //string joinMsg = $"{displayName}{contactInfo.m_szPhoneId}加入会议！";
            //HasErrorMsg("-1", joinMsg);

            //speaker automatically sends a message(with creatorPhoneId) to nonspeakers
            //!!!CAREFUL!!! ONLY speaker will call this
            if (_sdkService.IsCreator)
            {
                _sdkService.SendMessage((int) _viewLayoutService.MeetingMode,
                    _viewLayoutService.MeetingMode.ToString(), _viewLayoutService.MeetingMode.ToString().Length, null);
            }
        }

        private async void ViewCloseEventHandler(ParticipantView speakerView)
        {
            await _viewLayoutService.HideViewAsync(speakerView);
        }

        private void StopSpeakEventHandler()
        {
            _viewLayoutService.ChangeViewMode(ViewMode.Auto);

            if (_sdkService.IsCreator)
            {
                _viewLayoutService.ChangeMeetingMode(MeetingMode.Interaction);
            }

            SpeakingStatus = IsNotSpeaking;
            SharingVisibility = Visibility.Visible;
            CancelSharingVisibility = Visibility.Collapsed;

            _meetingView.Dispatcher.BeginInvoke(new Action(RefreshExternalData));
            //reload menus
        }

        private void StartSpeakEventHandler()
        {
            SpeakingStatus = IsSpeaking;
        }

        private async void ViewCreateEventHandler(ParticipantView speakerView)
        {
            await _viewLayoutService.ShowViewAsync(speakerView);
        }


        private bool CheckIsUserSpeaking(bool showMsgBar = false)
        {
            //return true;

            List<Participant> participants = _sdkService.GetParticipants();

            var self = participants.FirstOrDefault(p => p.PhoneId == _sdkService.SelfPhoneId);

            if (self != null && (showMsgBar && !self.IsSpeaking))
            {
                HasErrorMsg("-1", Messages.WarningYouAreNotSpeaking);
            }

            return self != null && self.IsSpeaking;
        }

        private bool CheckIsUserSpeaking(ViewFrame speakerView, bool showMsgBar = false)
        {
            //return true;

            List<Participant> participants = _sdkService.GetParticipants();

            var speaker = participants.FirstOrDefault(p => p.PhoneId == speakerView.PhoneId);

            bool isUserNotSpeaking = string.IsNullOrEmpty(speaker.PhoneId) || !speaker.IsSpeaking;

            if (isUserNotSpeaking && showMsgBar)
            {
                HasErrorMsg("-1", Messages.WarningUserNotSpeaking);
            }

            return !isUserNotSpeaking;
        }

        private void RefreshExternalData()
        {
            if (SharingMenuItems == null)
            {
                SharingMenuItems = new ObservableCollection<MenuItem>();

            }
            else
            {
                SharingMenuItems.Clear();
            }

            var sharings = Enum.GetNames(typeof(Sharing));
            foreach (var sharing in sharings)
            {
                var newSharingMenu = new MenuItem();
                newSharingMenu.Header = EnumHelper.GetDescription(typeof(Sharing), Enum.Parse(typeof(Sharing), sharing));

                if (sharing == Sharing.Desktop.ToString())
                {
                    newSharingMenu.Command = SharingDesktopCommand;
                }

                if (sharing == Sharing.ExternalData.ToString())
                {
                    MeetingSdk.SdkWrapper.MeetingDataModel.Device[] cameras = _sdkService.GetDevices(1);
                    foreach (var camera in cameras)
                    {
                        if (!string.IsNullOrEmpty(camera.Name) && !camera.IsDefault)
                        {
                            newSharingMenu.Items.Add(
                                new MenuItem()
                                {
                                    Header = camera.Name,
                                    Command = ExternalDataChangedCommand,
                                    CommandParameter = camera.Name
                                });
                        }
                    }
                }

                SharingMenuItems.Add(newSharingMenu);
            }
        }

        private void LoadModeMenuItems()
        {
            if (ModeMenuItems == null)
            {
                ModeMenuItems = new ObservableCollection<MenuItem>();
            }
            else
            {
                ModeMenuItems.Clear();
            }

            var modes = Enum.GetNames(typeof(MeetingMode));
            foreach (var mode in modes)
            {
                var newModeMenu = new MenuItem();
                newModeMenu.Header = EnumHelper.GetDescription(typeof(MeetingMode),
                    Enum.Parse(typeof(MeetingMode), mode));
                newModeMenu.Command = ModeChangedCommand;
                newModeMenu.CommandParameter = mode;

                ModeMenuItems.Add(newModeMenu);
            }
            CurModeName = EnumHelper.GetDescription(typeof(MeetingMode), _viewLayoutService.MeetingMode);

        }

        private void RefreshLayoutMenuItems()
        {
            if (LayoutMenuItems == null)
            {
                LayoutMenuItems = new ObservableCollection<MenuItem>();
            }
            else
            {
                LayoutMenuItems.Clear();
            }

            var layouts = Enum.GetNames(typeof(ViewMode));
            foreach (var layout in layouts)
            {
                var newLayoutMenu = new MenuItem();
                newLayoutMenu.Header = EnumHelper.GetDescription(typeof(ViewMode), Enum.Parse(typeof(ViewMode), layout));
                newLayoutMenu.Tag = layout;

                if (layout == ViewMode.BigSmalls.ToString() || layout == ViewMode.Closeup.ToString())
                {
                    foreach (var speakerView in _viewLayoutService.ViewFrameList)
                    {
                        if (speakerView.IsOpened)
                        {
                            newLayoutMenu.Items.Add(new MenuItem()
                            {
                                Header =
                                    string.IsNullOrEmpty(speakerView.ViewName)
                                        ? speakerView.PhoneId
                                        : (speakerView.ViewName + " - " + speakerView.PhoneId),
                                Tag = speakerView
                            });
                        }
                    }
                }

                newLayoutMenu.Click += LayoutChangedEventHandler;

                LayoutMenuItems.Add(newLayoutMenu);
            }

            CurLayoutName = EnumHelper.GetDescription(typeof(ViewMode), _viewLayoutService.ViewMode);
        }

        private async void LayoutChangedEventHandler(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            MenuItem sourceMenuItem = e.OriginalSource as MenuItem;

            string header = menuItem.Tag.ToString();

            ViewMode viewMode = (ViewMode) Enum.Parse(typeof(ViewMode), header);

            switch (viewMode)
            {
                case ViewMode.Auto:
                case ViewMode.Average:
                    await ViewModeChangedAsync(viewMode);
                    break;
                case ViewMode.BigSmalls:
                    ViewFrame bigView = sourceMenuItem.Tag as ViewFrame;
                    await BigViewChangedAsync(bigView);
                    break;
                case ViewMode.Closeup:
                    ViewFrame fullView = sourceMenuItem.Tag as ViewFrame;
                    await FullScreenViewChangedAsync(fullView);
                    break;
                default:
                    break;
            }
        }

        #endregion
    }
}