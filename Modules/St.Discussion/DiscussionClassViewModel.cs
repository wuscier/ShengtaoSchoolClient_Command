using Prism.Commands;
using St.Common;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using Caliburn.Micro;
using Common;
using MenuItem = System.Windows.Controls.MenuItem;
using Serilog;
using MeetingSdk.SdkWrapper;
using MeetingSdk.SdkWrapper.MeetingDataModel;
using Prism.Events;
using St.Common.Commands;
using Action = System.Action;
using LogManager = St.Common.LogManager;
using Timer = System.Threading.Timer;

namespace St.Discussion
{
    public class DiscussionClassViewModel : ViewModelBase, IExitMeeting
    {
        protected override bool HasErrorMsg(string status, string msg)
        {
            IsMenuOpen = true;

            if (status != "0")
            {
                MessageQueueManager.Instance.AddInfo(msg);
            }

            return status != "0";
        }

        public DiscussionClassViewModel(DiscussionClassView meetingView, Action<bool, string> startMeetingCallback,
            Action<bool, string> exitMeetingCallback)
        {
            _discussionClassView = meetingView;

            UpButtonGotFocusCommand = new DelegateCommand<Button>(menuItem =>
            {
                //Console.WriteLine($"up button got focus, {menuItem}");
                _upFocusedUiElement = menuItem;
            });
            DownButtonGotFocusCommand = new DelegateCommand<Button>((menuItem) =>
            {
                //Console.WriteLine($"down button got focus, {menuItem}");
                _downFocusedUiElement = menuItem;
            });

            InitMenuButtonItems();

            _eventAggregator = IoC.Get<IEventAggregator>();
            _eventAggregator.GetEvent<CommandReceivedEvent>()
                .Subscribe(ExecuteCommand, ThreadOption.PublisherThread, false, command => command.IsIntoClassCommand);

            _viewLayoutService = IoC.Get<IViewLayout>();
            _viewLayoutService.ViewFrameList = InitializeViewFrameList(meetingView);

            _sdkService = IoC.Get<IMeeting>();
            _bmsService = IoC.Get<IBms>();

            _localPushLiveService = IoC.Get<IPushLive>(GlobalResources.LocalPushLive);
            _localPushLiveService.ResetStatus();
            _serverPushLiveService = IoC.Get<IPushLive>(GlobalResources.RemotePushLive);
            _serverPushLiveService.ResetStatus();
            _localRecordService = IoC.Get<IRecord>();
            _localRecordService.ResetStatus();

            _startMeetingCallbackEvent = startMeetingCallback;
            _exitMeetingCallbackEvent = exitMeetingCallback;

            MeetingId = _sdkService.MeetingId;

            _lessonDetail = IoC.Get<LessonDetail>();
            _userInfo = IoC.Get<UserInfo>();
            _userInfos = IoC.Get<List<UserInfo>>();

            MeetingOrLesson = _lessonDetail.Id == 0 ? "会议号:" : "课程号:";

            LoadCommand = DelegateCommand.FromAsyncHandler(LoadAsync);
            OpenExitDialogCommand = DelegateCommand.FromAsyncHandler(OpenExitDialogAsync);
            KickoutCommand = new DelegateCommand<string>(KickoutAsync);
            RecordCommand = DelegateCommand.FromAsyncHandler(RecordAsync);
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

            //TouchDownCommand = new DelegateCommand(() =>
            //{
            //    IsMenuOpen = !IsMenuOpen;

            //});
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
                //SpeakItem.Command.Execute(null);
                await SpeakingStatusChangedAsync();
            }
            else if (command.Directive == GlobalCommands.Instance.RecordCommand.Directive)
            {
                await RecordAsync();
            }
            else if (command.Directive == GlobalCommands.Instance.PushLiveCommand.Directive)
            {
                MessageQueueManager.Instance.AddInfo("不支持的命令！");
                // do nothing.
            }
            else if (command.Directive == GlobalCommands.Instance.DocCommand.Directive)
            {
                ShareDocItem.Command.Execute(null);
            }
            else if (command.Directive == GlobalCommands.Instance.AverageCommand.Directive)
            {
                _viewLayoutService.ChangeViewMode(ViewMode.Average);
                await _viewLayoutService.LaunchLayout();
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

                _viewLayoutService.SetSpecialView(openedVfs[bigViewIndex], SpecialViewType.Big);
                await _viewLayoutService.LaunchLayout();

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

                _viewLayoutService.SetSpecialView(openedVfs[fullViewIndex], SpecialViewType.FullScreen);
                await _viewLayoutService.LaunchLayout();
            }
            else if (command.Directive == GlobalCommands.Instance.InteractionCommand.Directive)
            {
                MessageQueueManager.Instance.AddInfo("不支持的命令！");

                // do nothing.
            }
            else if (command.Directive == GlobalCommands.Instance.SpeakerCommand.Directive)
            {
                MessageQueueManager.Instance.AddInfo("不支持的命令！");

                // do nothing.
            }
            else if (command.Directive == GlobalCommands.Instance.ShareCommand.Directive)
            {
                MessageQueueManager.Instance.AddInfo("不支持的命令！");

                // do nothing.
            }
        }

        private void ShowHelp()
        {
            string helpMsg = GlobalResources.HelpMessage;

            SscDialog helpSscDialog = new SscDialog(helpMsg);
            helpSscDialog.ShowDialog();
        }
        private void InitAutoHideSettings()
        {
            //BindingBase bindingBase = new Binding()
            //{
            //    Source = this,
            //    Mode = BindingMode.OneWayToSource,
            //    Path = new PropertyPath("IsWindowActive")
            //};

            //BindingOperations.SetBinding(_discussionClassView, Window.IsActiveProperty, bindingBase);

            _AutoHideMenuTimer = new Timer((state) =>
            {
                _discussionClassView.Dispatcher.BeginInvoke(new Action(() =>
                {
                    IsWindowActive = _discussionClassView.IsActive;
                }));
                //Console.WriteLine($" is meeting view active, {IsWindowActive}");
                if (DateTime.Now > _autoHideInitialTime.AddSeconds(10) && IsWindowActive)
                {
                    IsMenuOpen = false;
                }
            }, null, 0, 2000);
        }

        private void HandleKeyDown(KeyEventArgs keyEventArgs)
        {
            //Console.WriteLine($"key down, {keyEventArgs.Key}");

            switch (keyEventArgs.Key)
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


                    if (IsMenuOpen)
                    {
                        if (_upFocusedUiElement == null)
                        {
                            _upFocusedUiElement = _discussionClassView.ExitButton;
                        }

                        _upFocusedUiElement.Focus();

                    }

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

                    if (IsMenuOpen)
                    {
                        if (_downFocusedUiElement == null)
                        {
                            ShowUiBasedOnIsSpeaker();
                        }
                        else
                        {
                            _downFocusedUiElement.Focus();
                        }
                    }

                    if (_pressedDownKeyCount == 4)
                    {
                        _pressedDownKeyCount = 0;
                        _sdkService.CloseQosTool();
                    }


                    break;
                case Key.Enter:
                case Key.Apps:
                    if (!IsMenuOpen)
                    {
                        IsMenuOpen = true;
                        _downFocusedUiElement?.Focus();
                        keyEventArgs.Handled = true;
                    }

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

        private void WindowKeyDownHandler(object obj)
        {
            _autoHideInitialTime = DateTime.Now;
            var keyEventArgs = obj as KeyEventArgs;
            HandleKeyDown(keyEventArgs);
        }

        #region private fields

        private readonly DiscussionClassView _discussionClassView;
        private UIElement _downFocusedUiElement;
        private UIElement _upFocusedUiElement;

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
        private Timer _AutoHideMenuTimer;
        private DateTime _autoHideInitialTime = DateTime.Now;
        private bool _exitByDialog = false;

        private readonly Action<bool, string> _startMeetingCallbackEvent;
        private readonly Action<bool, string> _exitMeetingCallbackEvent;
        private const string OpenDoc = "打开课件";
        private const string CloseDoc = "关闭课件";
        private const string IsSpeaking = "取消发言";
        private const string IsNotSpeaking = "发 言";
        private const string ListenMode = "听课模式";
        private const string DiscussionMode = "评课模式";
        private const string StartRecord = "开启录制";
        private const string StopRecord = "关闭录制";

        #endregion

        #region public properties

        public ViewFrame ViewFrame1 { get; private set; }
        public ViewFrame ViewFrame2 { get; private set; }
        public ViewFrame ViewFrame3 { get; private set; }
        public ViewFrame ViewFrame4 { get; private set; }
        public ViewFrame ViewFrame5 { get; private set; }

        public ObservableCollection<MenuItem> LayoutMenuItems { get; set; }
        public ObservableCollection<MenuItem> SharingMenuItems { get; set; }



        public bool IsWindowActive { get; set; }

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


        private string _meetingOrLesson;

        public string MeetingOrLesson
        {
            get { return _meetingOrLesson; }
            set { SetProperty(ref _meetingOrLesson, value); }
        }

        private int _meetingId;

        public int MeetingId
        {
            get { return _meetingId; }
            set { SetProperty(ref _meetingId, value); }
        }


        private string _recordTips;

        public string RecordTips
        {
            get { return _recordTips; }
            set { SetProperty(ref _recordTips, value); }
        }

        private string _phoneId;

        public string PhoneId
        {
            get { return _phoneId; }
            set { SetProperty(ref _phoneId, value); }
        }

        private string _recordMsg = StartRecord;
        public string RecordMsg
        {
            get { return _recordMsg; }
            set
            {
                if (value == StartRecord)
                {
                    RecordTips = null;
                }
                SetProperty(ref _recordMsg, value);
            }
        }

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

        private string _classMode;

        public string ClassMode
        {
            get { return _classMode; }
            set { SetProperty(ref _classMode, value); }
        }


        #endregion

        #region Commands

        public ICommand LoadCommand { get; set; }
        public ICommand OpenExitDialogCommand { get; set; }
        public ICommand KickoutCommand { get; set; }
        public ICommand SetMicStateCommand { get; set; }
        public ICommand StartSpeakCommand { get; set; }
        public ICommand StopSpeakCommand { get; set; }
        public ICommand RecordCommand { get; set; }
        public ICommand TopMostTriggerCommand { get; set; }
        public ICommand ShowLogCommand { get; set; }
        public ICommand TriggerMenuCommand { get; set; }
        public ICommand WindowKeyDownCommand { get; set; }
        public ICommand TouchDownCommand { get; set; }
        public ICommand DownButtonGotFocusCommand { get; set; }
        public ICommand UpButtonGotFocusCommand { get; set; }
        public ICommand ShowHelpCommand { get; set; }


        private void InitMenuButtonItems()
        {
            ClassMode = DiscussionMode;

            ClassModeItem = new MenuButtonItem()
            {
                Visibility = Visibility.Collapsed,
                Content = ListenMode,
                GotFocusCommand = DownButtonGotFocusCommand,
                Command = DelegateCommand.FromAsyncHandler(async () =>
                {
                    _autoHideInitialTime = DateTime.Now;
                    _downFocusedUiElement = _discussionClassView.ClassModeButton;

                    //var participants = _sdkService.GetParticipants();

                    //List<string> listeners = new List<string>();

                    //participants.ForEach(participant =>
                    //{
                    //    if (participant.PhoneId != _sdkService.CreatorPhoneId)
                    //    {
                    //        listeners.Add(participant.PhoneId);
                    //    }
                    //});

                    switch (ClassMode)
                    {
                        case DiscussionMode: //Goto 听课模式,禁言所有听讲教室。

                            ManageListenersItem.Visibility = Visibility.Collapsed;
                            LayoutItem.Visibility = Visibility.Collapsed;

                            ClassMode = ListenMode;
                            ClassModeItem.Content = DiscussionMode;

                            _viewLayoutService.ChangeMeetingMode(MeetingMode.Sharing);


                            await _viewLayoutService.LaunchLayout();

                            MessageQueueManager.Instance.AddInfo(Messages.InfoGotoListenerMode);

                            //foreach (var listener in listeners)
                            //{
                            //    _sdkService.RequireUserStopSpeak(listener);
                            //}
                            int listeningMode = (int) UiMessage.ListeningMode;
                             _sdkService.SendMessage(listeningMode,
                                listeningMode.ToString(),
                                listeningMode.ToString().Length, null);

                            break;
                        case ListenMode: //Goto 评课模式，

                            ManageListenersItem.Visibility = Visibility.Visible;
                            LayoutItem.Visibility = Visibility.Visible;

                            ClassMode = DiscussionMode;
                            ClassModeItem.Content = ListenMode;

                            _viewLayoutService.ChangeMeetingMode(MeetingMode.Interaction);
                            await _viewLayoutService.LaunchLayout();


                            MessageQueueManager.Instance.AddInfo(Messages.InfoGotoDiscussionMode);
                            //foreach (var listener in listeners)
                            //{
                            //    _sdkService.RequireUserSpeak(listener);
                            //}

                            int discussionMode = (int) UiMessage.DiscussionMode;
                             _sdkService.SendMessage(discussionMode,
                                discussionMode.ToString(),
                                discussionMode.ToString().Length, null);


                            break;
                    }

                    _downFocusedUiElement.Focus();
                })
            };

            ShareDocItem = new MenuButtonItem()
            {
                Visibility = Visibility.Collapsed,
                Content = OpenDoc,
                GotFocusCommand = DownButtonGotFocusCommand,
                Command = DelegateCommand.FromAsyncHandler(async () =>
                {
                    _autoHideInitialTime = DateTime.Now;

                    _downFocusedUiElement = _discussionClassView.ShareDocButton;
                    await OpenCloseDocAsync();
                    _downFocusedUiElement.Focus();
                })
            };

            ManageListenersItem = new MenuButtonItem()
            {
                Visibility = Visibility.Collapsed,
                Content = "评课管理",
                GotFocusCommand = DownButtonGotFocusCommand,
                Command = new DelegateCommand(() =>
                {
                    _autoHideInitialTime = DateTime.Now;

                    _downFocusedUiElement = _discussionClassView.ManageListenersButton;
                    ManageAttendeeListView attendeeListView = new ManageAttendeeListView();
                    attendeeListView.ShowDialog();
                    _downFocusedUiElement.Focus();
                    _autoHideInitialTime = DateTime.Now;

                })
            };

            LayoutItem = new MenuButtonItem()
            {
                Visibility = Visibility.Collapsed,
                Content = "画面布局",
                GotFocusCommand = DownButtonGotFocusCommand,
                Command = new DelegateCommand(() =>
                {
                    _autoHideInitialTime = DateTime.Now;

                    _downFocusedUiElement = _discussionClassView.LayoutButton;
                    //if (!CheckIsUserSpeaking(true))
                    //{
                    //    return;
                    //}

                    LayoutView layoutView = new LayoutView();
                    layoutView.ShowDialog();
                    _downFocusedUiElement.Focus();
                    _autoHideInitialTime = DateTime.Now;

                })
            };

            SpeakItem = new MenuButtonItem()
            {
                Visibility = Visibility.Collapsed,
                Content = IsSpeaking,
                GotFocusCommand = DownButtonGotFocusCommand,
                Command = DelegateCommand.FromAsyncHandler(async () =>
                {
                    _downFocusedUiElement = _discussionClassView.SpeakButton;
                    await SpeakingStatusChangedAsync();
                    //_downFocusedUiElement.Focus();
                })
            };
        }


        public MenuButtonItem ClassModeItem { get; set; }
        public MenuButtonItem ShareDocItem { get; set; }
        public MenuButtonItem ManageListenersItem { get; set; }
        public MenuButtonItem LayoutItem { get; set; }
        public MenuButtonItem SpeakItem { get; set; }

        #endregion

        #region Command Handlers

        private void ChangeWindowStyleInDevMode()
        {
            if (GlobalData.Instance.RunMode == RunMode.Development)
            {
                IsTopMost = false;
                _discussionClassView.UseNoneWindowStyle = false;
                _discussionClassView.ResizeMode = ResizeMode.CanResize;
                _discussionClassView.WindowStyle = WindowStyle.SingleBorderWindow;
                _discussionClassView.IsWindowDraggable = true;
                _discussionClassView.ShowMinButton = true;
                _discussionClassView.ShowMaxRestoreButton = true;
                _discussionClassView.ShowCloseButton = false;
                _discussionClassView.IsCloseButtonEnabled = false;
            }

        }

        private async Task JoinMeetingAsync()
        {
            GlobalData.Instance.ViewArea = new ViewArea()
            {
                Width = _discussionClassView.ActualWidth,
                Height = _discussionClassView.ActualHeight
            };

            uint[] uint32SOfNonDataArray =
            {
                (uint) _discussionClassView.PictureBox1.Handle.ToInt32(),
                (uint) _discussionClassView.PictureBox2.Handle.ToInt32(),
                (uint) _discussionClassView.PictureBox3.Handle.ToInt32(),
                (uint) _discussionClassView.PictureBox4.Handle.ToInt32(),
            };

            foreach (var hwnd in uint32SOfNonDataArray)
            {
                Log.Logger.Debug($"【figure hwnd】：{hwnd}");
            }

            uint[] uint32SOfDataArray = { (uint)_discussionClassView.PictureBox5.Handle.ToInt32() };

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
                _discussionClassView.Close();

            }
            else
            {
                IsMenuOpen = true;

                //if join meeting successfully, then make main view invisible
                _startMeetingCallbackEvent(true, "");

                if (_lessonDetail.Id > 0)
                {
                    ResponseResult result = await
                        _bmsService.UpdateMeetingStatus(_lessonDetail.Id, _userInfo.UserId,
                            DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                            string.Empty);

                    HasErrorMsg(result.Status, result.Message);
                }
                ShowUiBasedOnIsSpeaker();
                //if (_sdkService.IsCreator)
                //{
                InitAutoHideSettings();
                //}
            }

        }


        //command handlers
        private async Task LoadAsync()
        {
            GlobalData.Instance.CurWindowHwnd = new WindowInteropHelper(_discussionClassView).Handle;
            ChangeWindowStyleInDevMode();
            await JoinMeetingAsync();
            GlobalCommands.Instance.SetCommandsStateInDiscussionClass();
        }


        private void ShowUiBasedOnIsSpeaker()
        {
            //if not speaker, then clear mode menu items
            if (_sdkService.IsCreator)
            {
                IsSpeaker = Visibility.Visible;


                ClassModeItem.Visibility = Visibility.Visible;
                ShareDocItem.Visibility = Visibility.Visible;
                ManageListenersItem.Visibility = Visibility.Visible;
                LayoutItem.Visibility = Visibility.Visible;

                SpeakItem.Visibility = Visibility.Collapsed;

                _downFocusedUiElement = _discussionClassView.ClassModeButton;

            }
            else
            {
                IsSpeaker = Visibility.Collapsed;

                ClassModeItem.Visibility = Visibility.Collapsed;
                ShareDocItem.Visibility = Visibility.Collapsed;
                ManageListenersItem.Visibility = Visibility.Collapsed;

                LayoutItem.Visibility = Visibility.Visible;

                if (ClassMode == ListenMode)
                {
                    SpeakItem.Visibility = Visibility.Collapsed;

                }
                if (ClassMode == DiscussionMode)
                {
                    SpeakItem.Visibility = Visibility.Visible;

                }

                _downFocusedUiElement = _discussionClassView.LayoutButton;
            }

            _downFocusedUiElement.Focus();
            
        }

        private async Task OpenCloseDocAsync()
        {
            if (ShareDocItem.Content == OpenDoc)
            {
                if (_sdkService.IsSharedDocOpened())
                {
                    MessageQueueManager.Instance.AddInfo(Messages.DocumentAlreadyOpened);
                    ShareDocItem.Content = CloseDoc;
                    return;
                }

                AsyncCallbackMsg openDocResult = await _sdkService.StartShareDoc();

                if (openDocResult.Status != 0)
                {
                    ShareDocItem.Content = OpenDoc;
                    MessageQueueManager.Instance.AddInfo(openDocResult.Message);
                }
                else
                {
                    ShareDocItem.Content = CloseDoc;
                }

            }
            else if (ShareDocItem.Content == CloseDoc)
            {
                if (_sdkService.IsSharedDocOpened())
                {
                    AsyncCallbackMsg closeDocResult = await _sdkService.StopShareDoc();

                    if (closeDocResult.Status != 0)
                    {
                        ShareDocItem.Content = CloseDoc;
                        MessageQueueManager.Instance.AddInfo(closeDocResult.Message);
                    }
                    else
                    {
                        ShareDocItem.Content = OpenDoc;
                    }
                }
                else
                {
                    MessageQueueManager.Instance.AddInfo(Messages.DocumentAlreadyClosed);
                    ShareDocItem.Content = OpenDoc;
                }
            }
        }

        private async Task SpeakingStatusChangedAsync()
        {
            if (SpeakItem.Content == IsSpeaking)
            {
                AsyncCallbackMsg stopSucceeded = await _sdkService.StopSpeak();
                return;
                //will change SpeakStatus in StopSpeakCallbackEventHandler.
            }

            if (SpeakItem.Content == IsNotSpeaking)
            {
                AsyncCallbackMsg result = await _sdkService.ApplyToSpeak();
                if (!HasErrorMsg(result.Status.ToString(), result.Message))
                {
                    // will change SpeakStatus in callback???
                    SpeakItem.Content = IsSpeaking;
                }
            }
        }

        public async Task ExitAsync()
        {
            try
            {
                _autoHideInitialTime = DateTime.Now;

                await StopAllLives();

                AsyncCallbackMsg exitResult = await _sdkService.ExitMeeting();
                _viewLayoutService.ResetAsInitialStatus();

                Log.Logger.Debug($"【exit meeting】：result={exitResult.Status}, msg={exitResult.Message}");
                HasErrorMsg(exitResult.Status.ToString(), exitResult.Message);

                _AutoHideMenuTimer?.Dispose();

                UnRegisterMeetingEvents();

                await UpdateExitTime();


                await _discussionClassView.Dispatcher.BeginInvoke(new Action(() =>
                {
                    _exitByDialog = true;
                    _discussionClassView.Close();

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
            await _discussionClassView.Dispatcher.BeginInvoke(new Action(async () =>
            {
                IsMenuOpen = false;
                _upFocusedUiElement = _discussionClassView.ExitButton;


                YesNoDialog yesNoDialog = new YesNoDialog("确定退出？");
                bool? result = yesNoDialog.ShowDialog();
                _autoHideInitialTime = DateTime.Now;

                if (result.HasValue && result.Value)
                {
                    await ExitAsync();
                }
                else
                {
                    IsMenuOpen = true;
                    _upFocusedUiElement.Focus();
                }

            }));
        }

        private void KickoutAsync(string userPhoneId)
        {
            _sdkService.HostKickoutUser(userPhoneId);
        }

        private async Task RecordAsync()
        {
            _autoHideInitialTime = DateTime.Now;

            _upFocusedUiElement = _discussionClassView.RecordButton;


            switch (RecordMsg)
            {
                case StartRecord:

                    _localRecordService.GetRecordParam();

                    AsyncCallbackMsg startRecordResult =
                        await
                            _localRecordService.StartRecord(
                                _viewLayoutService.GetStreamLayout(_localRecordService.RecordParam.Width,
                                    _localRecordService.RecordParam.Height));

                    if (HasErrorMsg(startRecordResult.Status.ToString(), startRecordResult.Message))
                    {
                        RecordMsg = StartRecord;
                    }
                    else
                    {
                        RecordMsg = StopRecord;
                        RecordTips =
                            string.Format(
                                $"分辨率：{_localRecordService.RecordParam.Width}*{_localRecordService.RecordParam.Height}\r\n" +
                                $"码率：{_localRecordService.RecordParam.VideoBitrate}\r\n" +
                                $"录制路径：{_localRecordService.RecordDirectory}");

                    }

                    break;
                case StopRecord:

                    AsyncCallbackMsg stopRecrodResult = await _localRecordService.StopRecord();
                    if (HasErrorMsg(stopRecrodResult.Status.ToString(), stopRecrodResult.Message))
                    {
                        RecordMsg = StopRecord;
                    }

                    RecordMsg = StartRecord;

                    break;
            }

            _upFocusedUiElement.Focus();
        }

        #endregion

        #region Methods

        private List<ViewFrame> InitializeViewFrameList(DiscussionClassView meetingView)
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

        private void RegisterMeetingEvents()
        {
            _discussionClassView.LocationChanged += _discussionClassView_LocationChanged;
            _discussionClassView.Deactivated += _discussionClassView_Deactivated; 
            _discussionClassView.Closing += _meetingView_Closing;
            _sdkService.ViewCreatedEvent += ViewCreateEventHandler;
            _sdkService.ViewClosedEvent += ViewCloseEventHandler;
            _sdkService.CloseSharedDocEvent += _sdkService_CloseSharedDocEvent;
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

        private void _discussionClassView_LocationChanged(object sender, EventArgs e)
        {
            try
            {
                MethodInfo methodInfo = typeof(Popup).GetMethod("UpdatePosition",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (IsMenuOpen)
                {
                    methodInfo.Invoke(_discussionClassView.TopMenu, null);
                    methodInfo.Invoke(_discussionClassView.BottomMenu, null);
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Error($"_discussionClassView_LocationChanged => {ex}");
            }
        }

        private void _discussionClassView_Deactivated(object sender, EventArgs e)
        {
            IsMenuOpen = false;
        }

        private void _sdkService_CloseSharedDocEvent(AsyncCallbackMsg asyncCallbackMsg)
        {
            ShareDocItem.Content = OpenDoc;
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
            _sdkService.CloseSharedDocEvent -= _sdkService_CloseSharedDocEvent;
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
            _discussionClassView.Dispatcher.BeginInvoke(new Action(() =>
            {
                _exitByDialog = true;

                _discussionClassView.Close();


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

        private async void UIMessageReceivedEventHandler(TransparentMessage message)
        {
            Log.Logger.Debug(
                $"UIMessageReceivedEventHandler => msgId={message.MessageId}, senderPhoneId={message.Sender?.PhoneId}");

            if (message.MessageId < 3)
            {
                _sdkService.CreatorPhoneId = message.Sender.PhoneId;

                MeetingMode meetingMode = (MeetingMode) message.MessageId;
                _viewLayoutService.ChangeMeetingMode(meetingMode);

                await _viewLayoutService.LaunchLayout();
            }
            else
            {
                if (message.MessageId == (int) UiMessage.BannedToSpeak)
                {

                    AsyncCallbackMsg stopSucceeded = await _sdkService.StopSpeak();
                    return;
                }

                if (message.MessageId == (int) UiMessage.AllowToSpeak)
                {
                    AsyncCallbackMsg result = await _sdkService.ApplyToSpeak();
                    if (!HasErrorMsg(result.Status.ToString(), result.Message))
                    {
                        // will change SpeakStatus in callback???
                        SpeakItem.Content = IsSpeaking;
                    }

                    return;
                }

                if (message.MessageId == (int) UiMessage.ListeningMode)
                {
                    ClassMode = ListenMode;
                    SpeakItem.Visibility = Visibility.Collapsed;
                    MessageQueueManager.Instance.AddInfo(Messages.InfoGotoListenerMode);

                    await _sdkService.StopSpeak();
                    return;

                }
                if (message.MessageId == (int) UiMessage.DiscussionMode)
                {
                   await GotoDiscussionMode();
                }
            }
        }

        private async Task GotoDiscussionMode()
        {
            _viewLayoutService.ChangeMeetingMode(MeetingMode.Interaction);
            await _viewLayoutService.LaunchLayout();

            ClassMode = DiscussionMode;
            SpeakItem.Visibility = Visibility.Visible;
            MessageQueueManager.Instance.AddInfo(Messages.InfoGotoDiscussionMode);
        }

        private async void OtherExitMeetingEventHandler(Participant contactInfo)
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
                await GotoDiscussionMode();
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

            Log.Logger.Debug($"OtherJoinMeetingEventHandler => phoneId={contactInfo.PhoneId}, name={contactInfo.Name}");

            if (_sdkService.IsCreator)
            {
                //var newView =
                //    _viewLayoutService.ViewFrameList.FirstOrDefault(
                //        v => v.PhoneId == contactInfo.PhoneId && v.ViewType == 1);

                //if (newView != null) newView.IsIntoMeeting = true;

                //_sdkService.SendUIMessage((int) _viewLayoutService.MeetingMode,
                //    _viewLayoutService.MeetingMode.ToString(), _viewLayoutService.MeetingMode.ToString().Length, null);


                int meetingModeCmd = (int) _viewLayoutService.MeetingMode;
                AsyncCallbackMsg sendMeetingModeMsg = _sdkService.SendMessage(meetingModeCmd,
                    meetingModeCmd.ToString(), meetingModeCmd.ToString().Length,
                    contactInfo.PhoneId);

                Log.Logger.Debug(
                    $"sendMeetingModeMsg => msgId={meetingModeCmd}, targetPhoneId={contactInfo.PhoneId}, result={sendMeetingModeMsg.Status}");

                // send a message to sync new attendee's class mode
                int messageId = (int) UiMessage.DiscussionMode;
                if (ClassMode == ListenMode)
                {
                    messageId = (int) UiMessage.ListeningMode;
                    //_sdkService.RequireUserStopSpeak(contactInfo.PhoneId);

                }
                else if (ClassMode == DiscussionMode)
                {
                    messageId = (int) UiMessage.DiscussionMode;
                }

                AsyncCallbackMsg sendClassModeMsg = _sdkService.SendMessage(messageId, messageId.ToString(),
                    messageId.ToString().Length,
                    contactInfo.PhoneId);

                Log.Logger.Debug(
                    $"sendClassModeMsg => msgId={messageId}, targetPhoneId={contactInfo.PhoneId}, result={sendClassModeMsg.Status}");

            }
        }

        private async void ViewCloseEventHandler(ParticipantView speakerView)
        {
            await _viewLayoutService.HideViewAsync(speakerView);
        }

        private void StopSpeakEventHandler()
        {
            //_viewLayoutService.ChangeViewMode(ViewMode.Auto);

            if (_sdkService.IsCreator)
            {
                _viewLayoutService.ChangeMeetingMode(MeetingMode.Interaction);
            }

            SpeakItem.Content = IsNotSpeaking;

            //reload menus
        }

        private void StartSpeakEventHandler()
        {
            SpeakItem.Content = IsSpeaking;
        }

        private async void ViewCreateEventHandler(ParticipantView speakerView)
        {
            await _viewLayoutService.ShowViewAsync(speakerView);
        }

        private bool CheckIsUserSpeaking(bool showMsgBar = false)
        {
            var self =
                _viewLayoutService.ViewFrameList.FirstOrDefault(p => p.PhoneId == _sdkService.SelfPhoneId && p.IsOpened);

            if (showMsgBar && self == null)
            {
                MessageQueueManager.Instance.AddInfo(Messages.WarningYouAreNotSpeaking);
            }

            return self != null;
        }

        

        #endregion
    }
}