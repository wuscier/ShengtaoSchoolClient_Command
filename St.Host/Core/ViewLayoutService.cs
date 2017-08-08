using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Caliburn.Micro;
using MeetingSdk.SdkWrapper;
using MeetingSdk.SdkWrapper.MeetingDataModel;
using Serilog;
using St.Common;

namespace St.Host.Core
{
    public class ViewLayoutService : IViewLayout
    {
        private readonly IMeeting _sdkService;
        private readonly IPushLive _localPushLiveService;
        private readonly IPushLive _serverPushLiveService;
        private readonly IRecord _localRecordService;
        private readonly List<UserInfo> _attendees;
        private readonly LessonDetail _lessonDetail;
        private static readonly double Columns = 30;
        private static readonly double Rows = 10;

        private bool _meetingModeChanged = true;


        private MeetingMode meetingMode;

        private ViewMode viewMode;

        public ViewLayoutService()
        {
            _sdkService = IoC.Get<IMeeting>();
            _attendees = IoC.Get<List<UserInfo>>();
            _lessonDetail = IoC.Get<LessonDetail>();
            _localPushLiveService =
                IoC.Get<IPushLive>(GlobalResources.LocalPushLive);
            _serverPushLiveService =
                IoC.Get<IPushLive>(GlobalResources.RemotePushLive);
            _localRecordService = IoC.Get<IRecord>();

            InitializeStatus();
        }

        public List<ViewFrame> ViewFrameList { get; set; }

        public event MeetingModeChanged MeetingModeChangedEvent;
        public event ViewModeChanged ViewModeChangedEvent;

        public MeetingMode MeetingMode
        {
            get { return meetingMode; }
            private set
            {
                if (meetingMode != value)
                {
                    meetingMode = value;
                    _meetingModeChanged = true;
                    MeetingModeChangedEvent?.Invoke(value);
                }
                else
                {
                    _meetingModeChanged = false;
                }
            }
        }

        public ViewMode ViewMode
        {
            get { return viewMode; }
            private set
            {
                viewMode = value;
                ViewModeChangedEvent?.Invoke(value);
            }
        }

        public ViewFrame FullScreenView { get; private set; }

        public async Task ShowViewAsync(ParticipantView view)
        {
            Log.Logger.Debug(
                $"【create view】：hwnd={view.Hwnd}, phoneId={view.Participant.PhoneId}, viewType={view.ViewType}");
            var viewFrameVisible = ViewFrameList.FirstOrDefault(viewFrame => viewFrame.Hwnd == view.Hwnd);

            if (viewFrameVisible != null)
            {
                // LOG return a handle which can not be found in handle list.

                viewFrameVisible.IsOpened = true;
                viewFrameVisible.Visibility = Visibility.Visible;
                viewFrameVisible.PhoneId = view.Participant.PhoneId;

                var attendee = _attendees.FirstOrDefault(userInfo => userInfo.GetNube() == view.Participant.PhoneId);
                string displayName = string.Empty;
                if (!string.IsNullOrEmpty(attendee?.Name))
                {
                    displayName = attendee.Name;
                }

                viewFrameVisible.ViewName = view.ViewType == 1
                    ? displayName
                    : $"(共享){displayName}";

                viewFrameVisible.ViewType = view.ViewType;
                viewFrameVisible.ViewOrder = ViewFrameList.Max(viewFrame => viewFrame.ViewOrder) + 1;
            }

            await LaunchLayout();
        }

        public async Task HideViewAsync(ParticipantView view)
        {
            Log.Logger.Debug(
                $"【close view】：hwnd={view.Hwnd}, phoneId={view.Participant.PhoneId}, viewType={view.ViewType}");

            ResetFullScreenView(view);

            var viewFrameInvisible = ViewFrameList.FirstOrDefault(viewFrame => viewFrame.Hwnd == view.Hwnd);

            if (viewFrameInvisible != null)
            {
                // LOG return a handle which can not be found in handle list.


                viewFrameInvisible.IsOpened = false;
                viewFrameInvisible.Visibility = Visibility.Collapsed;
            }

            await LaunchLayout();
        }

        public void ResetAsAutoLayout()
        {
            ViewFrameList.ForEach(viewFrame => { viewFrame.IsBigView = false; });

            FullScreenView = null;

            ViewMode = ViewMode.Auto;
        }

        public void ResetAsInitialStatus()
        {
            //will call this method when user exits meeting
            MakeAllViewsInvisible();
            InitializeStatus();
        }

        public void ChangeMeetingMode(MeetingMode meetingMode)
        {
            //if (MeetingMode != meetingMode)
            MeetingMode = meetingMode;
        }

        public void ChangeViewMode(ViewMode viewMode)
        {
            if (ViewMode != viewMode)
                ViewMode = viewMode;
        }

        public async Task LaunchLayout()
        {
            if (_meetingModeChanged)
            {
                _meetingModeChanged = false;
                switch (MeetingMode)
                {
                    //模式优先级 高于 画面布局，选择一个模式将会重置布局为自动
                    //在某种模式下，用户可以随意更改布局
                    case MeetingMode.Interaction:
                        await LaunchAverageLayout();
                        break;
                    case MeetingMode.Speaker:
                        await GotoSpeakerMode();
                        break;
                    case MeetingMode.Sharing:
                        await GotoSharingMode();
                        break;
                }
            }
            else if (MeetingMode == MeetingMode.Sharing)
            {
                await GotoSharingMode();
            }
            else
            {
                switch (ViewMode)
                {
                    default:
                        switch (MeetingMode)
                        {
                            //模式优先级 高于 画面布局，选择一个模式将会重置布局为自动
                            //在某种模式下，用户可以随意更改布局
                            case MeetingMode.Interaction:
                                await LaunchAverageLayout();
                                break;
                            case MeetingMode.Speaker:
                                await GotoSpeakerMode();
                                break;
                            case MeetingMode.Sharing:
                                await GotoSharingMode();
                                break;
                        }
                        break;
                    case ViewMode.Average:
                        await LaunchAverageLayout();
                        break;
                    case ViewMode.BigSmalls:
                        await LaunchBigSmallLayout();
                        break;
                    case ViewMode.Closeup:
                        await LaunchCloseUpLayout();
                        break;
                }
            }

            await StartOrRefreshLiveAsync();
        }

        private async Task StartOrRefreshLiveAsync()
        {
            if (ViewFrameList.Count(viewFrame => viewFrame.IsOpened && viewFrame.Visibility == Visibility.Visible) > 0)
            {
                if (_sdkService.IsCreator && !_serverPushLiveService.HasPushLiveSuccessfully &&
                    (_lessonDetail.LessonType == LessonType.Interactive ||
                     _lessonDetail.LessonType == LessonType.Discussion))
                {
                    _serverPushLiveService.HasPushLiveSuccessfully = true;
                    await StartPushLiveStreamAutomatically();
                }

                if (_localPushLiveService.LiveId != 0)
                {
                    _localPushLiveService.RefreshLiveStream(GetStreamLayout(
                        _localPushLiveService.LiveParam.Width,
                        _localPushLiveService.LiveParam.Height));
                }

                if (_serverPushLiveService.LiveId != 0)
                {
                    _serverPushLiveService.RefreshLiveStream(
                        GetStreamLayout(_serverPushLiveService.LiveParam.Width,
                            _serverPushLiveService.LiveParam.Height));
                }

                if (_localRecordService.RecordId != 0)
                {
                    _localRecordService.RefreshLiveStream(GetStreamLayout(_localRecordService.RecordParam.Width,
                        _localRecordService.RecordParam.Height));
                }
            }
        }

        public void SetSpecialView(ViewFrame view, SpecialViewType type)
        {
            switch (type)
            {
                case SpecialViewType.Big:
                    ChangeViewMode(ViewMode.BigSmalls);
                    SetBigView(view);
                    break;
                case SpecialViewType.FullScreen:
                    ChangeViewMode(ViewMode.Closeup);
                    SetFullScreenView(view);
                    break;
                default:
                    break;
            }
        }

        private void InitializeStatus()
        {
            ViewFrameList = new List<ViewFrame>();

            MeetingMode = MeetingMode.Interaction;

            ViewMode = ViewMode.Auto;
            FullScreenView = null;

            var count = 0;
            var participants = _sdkService.GetParticipants();
            if (participants != null)
                count = participants.Count(p => p.PhoneId == _sdkService.SelfPhoneId);

            if (count == 0)
                ViewFrameList.Clear();
        }

        public async Task GotoSpeakerMode()
        {
            // 主讲模式下，不会显示听讲者视图
            //1. 有主讲者视图和共享视图，主讲者大，共享小
            //2. 有主讲者，没有共享，主讲者全屏
            //3. 无主讲者，无法设置主讲模式【选择主讲模式时会校验】

            var sharingView =
                ViewFrameList.FirstOrDefault(
                    v => (v.PhoneId == _sdkService.CreatorPhoneId) && v.IsOpened && (v.ViewType == 2));


            var speakerView =
                ViewFrameList.FirstOrDefault(
                    v => (v.PhoneId == _sdkService.CreatorPhoneId) && v.IsOpened && (v.ViewType == 1));

            if (sharingView == null && speakerView == null)
            {
                //await GotoDefaultMode();
                await LaunchAverageLayout();
                return;
            }

            if (sharingView != null && speakerView != null)
            {
                SetBigView(speakerView);
                await LaunchBigSmallLayout();
                return;
            }

            if (sharingView == null)
            {
                FullScreenView = speakerView;
                await LaunchCloseUpLayout();
                return;
            }

            FullScreenView = sharingView;
            await LaunchCloseUpLayout();
        }

        public async Task GotoSharingMode()
        {
            // 共享模式下，不会显示听讲者视图【设置完共享源，将自动开启共享模式】
            //1. 有主讲者视图和共享视图，主讲者小，共享大
            //2. 无主讲者，有共享，共享全屏
            //3. 没有共享，无法设置共享模式【选择共享模式时会校验】

            var sharingView =
                ViewFrameList.FirstOrDefault(
                    v => (v.PhoneId == _sdkService.CreatorPhoneId) && v.IsOpened && (v.ViewType == 2));



            var speakerView =
                ViewFrameList.FirstOrDefault(
                    v => (v.PhoneId == _sdkService.CreatorPhoneId) && v.IsOpened && (v.ViewType == 1));

            if (sharingView == null && speakerView == null)
            {
                await LaunchAverageLayout();
                //await GotoDefaultMode();
                return;
            }

            if (sharingView != null && speakerView != null)
            {
                SetBigView(sharingView);
                await LaunchBigSmallLayout();
                return;
            }

            if (sharingView == null)
            {
                FullScreenView = speakerView;
                await LaunchCloseUpLayout();
                return;
            }

            FullScreenView = sharingView;
            await LaunchCloseUpLayout();
        }

        public void MakeAllViewsInvisible()
        {
            ViewFrameList.ForEach(viewFrame => { viewFrame.Visibility = Visibility.Collapsed; });
        }

        //private async Task GotoDefaultMode()
        //{
        //    MeetingMode = MeetingMode.Interaction;
        //    ResetAsAutoLayout();

        //    await LaunchLayout();
        //}

        //private void MakeNonCreatorViewsInvisible()
        //{
        //    ViewFrameList.ForEach(viewFrame =>
        //    {
        //        if (viewFrame.PhoneId != _sdkService.TeacherPhoneId)
        //            viewFrame.Visibility = Visibility.Collapsed;
        //    });
        //}

        private void ResetFullScreenView(ParticipantView toBeClosedView)
        {
            if ((FullScreenView != null) && (FullScreenView.PhoneId == toBeClosedView.Participant.PhoneId) &&
                (FullScreenView.ViewType == toBeClosedView.ViewType))
                FullScreenView = null;
        }

        public List<LiveVideoStream> GetStreamLayout(int resolutionWidth, int resolutionHeight)
        {
            var viewFramesVisible =
                ViewFrameList.Where(viewFrame => viewFrame.IsOpened && viewFrame.Visibility == Visibility.Visible);

            var viewFramesByDesending = viewFramesVisible.OrderBy(viewFrame => viewFrame.ViewOrder);

            var orderViewFrames = viewFramesByDesending.ToList();

            List<LiveVideoStream> liveVideoStreamInfos = new List<LiveVideoStream>();


            foreach (var orderViewFrame in orderViewFrames)
            {
                LiveVideoStream newLiveVideoStreamInfo = new LiveVideoStream();
                RefreshLiveLayout(ref newLiveVideoStreamInfo, orderViewFrame, resolutionWidth, resolutionHeight);
                liveVideoStreamInfos.Add(newLiveVideoStreamInfo);
            }

            return liveVideoStreamInfos;
        }

        private void RefreshLiveLayout(ref LiveVideoStream liveVideoStreamInfo, ViewFrame viewFrame,
            int resolutionWidth, int resolutionHeight)
        {
            liveVideoStreamInfo.Handle = (uint) viewFrame.Hwnd.ToInt32();

            liveVideoStreamInfo.X = (int) ((viewFrame.Column / Columns) * resolutionWidth);
            liveVideoStreamInfo.Width = (int) ((viewFrame.ColumnSpan / Columns) * resolutionWidth);

            liveVideoStreamInfo.Y = (int) ((viewFrame.Row / Rows) * resolutionHeight);
            liveVideoStreamInfo.Height = (int) ((viewFrame.RowSpan / Rows) * resolutionHeight);
        }

        private async Task LaunchAverageLayout()
        {
            await Task.Run(() =>
            {
                var viewFramesVisible = ViewFrameList.Where(viewFrame => viewFrame.IsOpened);

                var viewFramesByDesending = viewFramesVisible.OrderBy(viewFrame => viewFrame.ViewOrder);

                var orderViewFrames = viewFramesByDesending.ToList();
                switch (orderViewFrames.Count)
                {
                    case 0:
                        //displays a picture
                        break;
                    case 1:
                        var viewFrameFull = orderViewFrames[0];
                        viewFrameFull.Visibility = Visibility.Visible;
                        viewFrameFull.Row = 0;
                        viewFrameFull.RowSpan = 10;
                        viewFrameFull.Column = 0;
                        viewFrameFull.ColumnSpan = 30;

                        viewFrameFull.Width = GlobalData.Instance.ViewArea.Width;
                        viewFrameFull.Height = GlobalData.Instance.ViewArea.Height;
                        viewFrameFull.VerticalAlignment = VerticalAlignment.Center;
                        Log.Logger.Debug(
                            $"LaunchAverageLayout 1 => phoneId={viewFrameFull.PhoneId}, name={viewFrameFull.ViewName}, hwnd={viewFrameFull.Hwnd}, width={viewFrameFull.Width}, height={viewFrameFull.Height}, row={viewFrameFull.Row}, rowspan={viewFrameFull.RowSpan}, column={viewFrameFull.Column}, columnspan={viewFrameFull.ColumnSpan}");
                        break;


                    case 2:
                        var viewFrameLeft2 = orderViewFrames[0];
                        var viewFrameRight2 = orderViewFrames[1];

                        viewFrameLeft2.Visibility = Visibility.Visible;
                        viewFrameLeft2.Row = 0;
                        viewFrameLeft2.RowSpan = 10;
                        viewFrameLeft2.Column = 0;
                        viewFrameLeft2.ColumnSpan = 15;
                        viewFrameLeft2.Width = GlobalData.Instance.ViewArea.Width / 2;
                        viewFrameLeft2.Height = GlobalData.Instance.ViewArea.Height / 2;
                        viewFrameLeft2.VerticalAlignment = VerticalAlignment.Center;

                        viewFrameRight2.Visibility = Visibility.Visible;
                        viewFrameRight2.Row = 0;
                        viewFrameRight2.RowSpan = 10;
                        viewFrameRight2.Column = 15;
                        viewFrameRight2.ColumnSpan = 15;
                        viewFrameRight2.Width = GlobalData.Instance.ViewArea.Width / 2;
                        viewFrameRight2.Height = GlobalData.Instance.ViewArea.Height / 2;
                        viewFrameRight2.VerticalAlignment = VerticalAlignment.Center;
                        Log.Logger.Debug(
                            $"LaunchAverageLayout 2 => left phoneId={viewFrameLeft2.PhoneId}, name={viewFrameLeft2.ViewName}, hwnd={viewFrameLeft2.Hwnd}, width={viewFrameLeft2.Width}, height={viewFrameLeft2.Height}, row={viewFrameLeft2.Row}, rowspan={viewFrameLeft2.RowSpan}, column={viewFrameLeft2.Column}, columnspan={viewFrameLeft2.ColumnSpan}");
                        Log.Logger.Debug(
                            $"LaunchAverageLayout 2 => right phoneId={viewFrameRight2.PhoneId}, name={viewFrameRight2.ViewName}, hwnd={viewFrameRight2.Hwnd}, width={viewFrameRight2.Width}, height={viewFrameRight2.Height}, row={viewFrameRight2.Row}, rowspan={viewFrameRight2.RowSpan}, column={viewFrameRight2.Column}, columnspan={viewFrameRight2.ColumnSpan}");

                        break;
                    case 3:

                        var viewFrameLeft3 = orderViewFrames[0];
                        var viewFrameRight3 = orderViewFrames[1];
                        var viewFrameBottom3 = orderViewFrames[2];


                        viewFrameLeft3.Visibility = Visibility.Visible;
                        viewFrameLeft3.Row = 0;
                        viewFrameLeft3.RowSpan = 5;
                        viewFrameLeft3.Column = 0;
                        viewFrameLeft3.ColumnSpan = 15;
                        viewFrameLeft3.Width = GlobalData.Instance.ViewArea.Width / 2;
                        viewFrameLeft3.Height = GlobalData.Instance.ViewArea.Height / 2;
                        viewFrameLeft3.VerticalAlignment = VerticalAlignment.Center;

                        viewFrameRight3.Visibility = Visibility.Visible;
                        viewFrameRight3.Row = 0;
                        viewFrameRight3.RowSpan = 5;
                        viewFrameRight3.Column = 15;
                        viewFrameRight3.ColumnSpan = 15;
                        viewFrameRight3.Width = GlobalData.Instance.ViewArea.Width / 2;
                        viewFrameRight3.Height = GlobalData.Instance.ViewArea.Height / 2;
                        viewFrameRight3.VerticalAlignment = VerticalAlignment.Center;

                        viewFrameBottom3.Visibility = Visibility.Visible;
                        viewFrameBottom3.Row = 5;
                        viewFrameBottom3.RowSpan = 5;
                        viewFrameBottom3.Column = 0;
                        viewFrameBottom3.ColumnSpan = 15;
                        viewFrameBottom3.Width = GlobalData.Instance.ViewArea.Width / 2;
                        viewFrameBottom3.Height = GlobalData.Instance.ViewArea.Height / 2;
                        viewFrameBottom3.VerticalAlignment = VerticalAlignment.Center;

                        Log.Logger.Debug(
                            $"LaunchAverageLayout 3 => left phoneId={viewFrameLeft3.PhoneId}, name={viewFrameLeft3.ViewName}, hwnd={viewFrameLeft3.Hwnd}, width={viewFrameLeft3.Width}, height={viewFrameLeft3.Height}, row={viewFrameLeft3.Row}, rowspan={viewFrameLeft3.RowSpan}, column={viewFrameLeft3.Column}, columnspan={viewFrameLeft3.ColumnSpan}");
                        Log.Logger.Debug(
                            $"LaunchAverageLayout 3 => right phoneId={viewFrameRight3.PhoneId}, name={viewFrameRight3.ViewName}, hwnd={viewFrameRight3.Hwnd}, width={viewFrameRight3.Width}, height={viewFrameRight3.Height}, row={viewFrameRight3.Row}, rowspan={viewFrameRight3.RowSpan}, column={viewFrameRight3.Column}, columnspan={viewFrameRight3.ColumnSpan}");
                        Log.Logger.Debug(
                            $"LaunchAverageLayout 3 => bottom phoneId={viewFrameBottom3.PhoneId}, name={viewFrameBottom3.ViewName}, hwnd={viewFrameBottom3.Hwnd}, width={viewFrameBottom3.Width}, height={viewFrameBottom3.Height}, row={viewFrameBottom3.Row}, rowspan={viewFrameBottom3.RowSpan}, column={viewFrameBottom3.Column}, columnspan={viewFrameBottom3.ColumnSpan}");


                        break;
                    case 4:
                        var viewFrameLeftTop4 = orderViewFrames[0];
                        var viewFrameRightTop4 = orderViewFrames[1];
                        var viewFrameLeftBottom4 = orderViewFrames[2];
                        var viewFrameRightBottom4 = orderViewFrames[3];

                        viewFrameLeftTop4.Visibility = Visibility.Visible;
                        viewFrameLeftTop4.Row = 0;
                        viewFrameLeftTop4.RowSpan = 5;
                        viewFrameLeftTop4.Column = 0;
                        viewFrameLeftTop4.ColumnSpan = 15;
                        viewFrameLeftTop4.Width = GlobalData.Instance.ViewArea.Width / 2;
                        viewFrameLeftTop4.Height = GlobalData.Instance.ViewArea.Height / 2;
                        viewFrameLeftTop4.VerticalAlignment = VerticalAlignment.Center;

                        viewFrameRightTop4.Visibility = Visibility.Visible;
                        viewFrameRightTop4.Row = 0;
                        viewFrameRightTop4.RowSpan = 5;
                        viewFrameRightTop4.Column = 15;
                        viewFrameRightTop4.ColumnSpan = 15;
                        viewFrameRightTop4.Width = GlobalData.Instance.ViewArea.Width / 2;
                        viewFrameRightTop4.Height = GlobalData.Instance.ViewArea.Height / 2;
                        viewFrameRightTop4.VerticalAlignment = VerticalAlignment.Center;

                        viewFrameLeftBottom4.Visibility = Visibility.Visible;
                        viewFrameLeftBottom4.Row = 5;
                        viewFrameLeftBottom4.RowSpan = 5;
                        viewFrameLeftBottom4.Column = 0;
                        viewFrameLeftBottom4.ColumnSpan = 15;
                        viewFrameLeftBottom4.Width = GlobalData.Instance.ViewArea.Width / 2;
                        viewFrameLeftBottom4.Height = GlobalData.Instance.ViewArea.Height / 2;
                        viewFrameLeftBottom4.VerticalAlignment = VerticalAlignment.Center;

                        viewFrameRightBottom4.Visibility = Visibility.Visible;
                        viewFrameRightBottom4.Row = 5;
                        viewFrameRightBottom4.RowSpan = 5;
                        viewFrameRightBottom4.Column = 15;
                        viewFrameRightBottom4.ColumnSpan = 15;
                        viewFrameRightBottom4.Width = GlobalData.Instance.ViewArea.Width / 2;
                        viewFrameRightBottom4.Height = GlobalData.Instance.ViewArea.Height / 2;
                        viewFrameRightBottom4.VerticalAlignment = VerticalAlignment.Center;
                        Log.Logger.Debug(
                            $"LaunchAverageLayout 4 => left_top phoneId={viewFrameLeftTop4.PhoneId}, name={viewFrameLeftTop4.ViewName}, hwnd={viewFrameLeftTop4.Hwnd}, width={viewFrameLeftTop4.Width}, height={viewFrameLeftTop4.Height}, row={viewFrameLeftTop4.Row}, rowspan={viewFrameLeftTop4.RowSpan}, column={viewFrameLeftTop4.Column}, columnspan={viewFrameLeftTop4.ColumnSpan}");
                        Log.Logger.Debug(
                            $"LaunchAverageLayout right_top => bottom phoneId={viewFrameRightTop4.PhoneId}, name={viewFrameRightTop4.ViewName}, hwnd={viewFrameRightTop4.Hwnd}, width={viewFrameRightTop4.Width}, height={viewFrameRightTop4.Height}, row={viewFrameRightTop4.Row}, rowspan={viewFrameRightTop4.RowSpan}, column={viewFrameRightTop4.Column}, columnspan={viewFrameRightTop4.ColumnSpan}");
                        Log.Logger.Debug(
                            $"LaunchAverageLayout 4 => left_bottom phoneId={viewFrameLeftBottom4.PhoneId}, name={viewFrameLeftBottom4.ViewName}, hwnd={viewFrameLeftBottom4.Hwnd}, width={viewFrameLeftBottom4.Width}, height={viewFrameLeftBottom4.Height}, row={viewFrameLeftBottom4.Row}, rowspan={viewFrameLeftBottom4.RowSpan}, column={viewFrameLeftBottom4.Column}, columnspan={viewFrameLeftBottom4.ColumnSpan}");
                        Log.Logger.Debug(
                            $"LaunchAverageLayout 4 => right_bottom phoneId={viewFrameRightBottom4.PhoneId}, name={viewFrameRightBottom4.ViewName}, hwnd={viewFrameRightBottom4.Hwnd}, width={viewFrameRightBottom4.Width}, height={viewFrameRightBottom4.Height}, row={viewFrameRightBottom4.Row}, rowspan={viewFrameRightBottom4.RowSpan}, column={viewFrameRightBottom4.Column}, columnspan={viewFrameRightBottom4.ColumnSpan}");

                        break;
                    case 5:

                        #region 三托二

                        //var viewFrameLeftTop5 = orderViewFrames[0];
                        //var viewFrameMiddleTop5 = orderViewFrames[1];
                        //var viewFrameRightTop5 = orderViewFrames[2];
                        //var viewFrameLeftBottom5 = orderViewFrames[3];
                        //var viewFrameRightBottom5 = orderViewFrames[4];

                        //viewFrameLeftTop5.Visibility = Visibility.Visible;
                        //viewFrameLeftTop5.Row = 0;
                        //viewFrameLeftTop5.RowSpan = 5;
                        //viewFrameLeftTop5.Column = 5;
                        //viewFrameLeftTop5.ColumnSpan = 10;
                        //viewFrameLeftTop5.Width = GlobalData.Instance.ViewArea.Width * 0.3333;
                        //viewFrameLeftTop5.Height = GlobalData.Instance.ViewArea.Width*0.1875;
                        //viewFrameLeftTop5.VerticalAlignment = VerticalAlignment.Bottom;

                        //viewFrameMiddleTop5.Visibility = Visibility.Visible;
                        //viewFrameMiddleTop5.Row = 0;
                        //viewFrameMiddleTop5.RowSpan = 5;
                        //viewFrameMiddleTop5.Column = 15;
                        //viewFrameMiddleTop5.ColumnSpan = 10;
                        //viewFrameMiddleTop5.Width = GlobalData.Instance.ViewArea.Width * 0.3333;
                        //viewFrameMiddleTop5.Height = GlobalData.Instance.ViewArea.Width * 0.1875;
                        //viewFrameMiddleTop5.VerticalAlignment = VerticalAlignment.Bottom;


                        //viewFrameRightTop5.Visibility = Visibility.Visible;
                        //viewFrameRightTop5.Row = 5;
                        //viewFrameRightTop5.RowSpan = 5;
                        //viewFrameRightTop5.Column = 0;
                        //viewFrameRightTop5.ColumnSpan = 10;
                        //viewFrameRightTop5.Width = GlobalData.Instance.ViewArea.Width * 0.3333;
                        //viewFrameRightTop5.Height = GlobalData.Instance.ViewArea.Width * 0.1875;
                        //viewFrameRightTop5.VerticalAlignment = VerticalAlignment.Top;

                        //viewFrameLeftBottom5.Visibility = Visibility.Visible;
                        //viewFrameLeftBottom5.Row = 5;
                        //viewFrameLeftBottom5.RowSpan = 5;
                        //viewFrameLeftBottom5.Column = 10;
                        //viewFrameLeftBottom5.ColumnSpan = 10;
                        //viewFrameLeftBottom5.Width = GlobalData.Instance.ViewArea.Width*0.3333;
                        //viewFrameLeftBottom5.Height = GlobalData.Instance.ViewArea.Width * 0.1875;
                        //viewFrameLeftBottom5.VerticalAlignment = VerticalAlignment.Top;

                        //viewFrameRightBottom5.Visibility = Visibility.Visible;
                        //viewFrameRightBottom5.Row = 5;
                        //viewFrameRightBottom5.RowSpan = 5;
                        //viewFrameRightBottom5.Column = 20;
                        //viewFrameRightBottom5.ColumnSpan = 10;
                        //viewFrameRightBottom5.Width = GlobalData.Instance.ViewArea.Width * 0.3333;
                        //viewFrameRightBottom5.Height = GlobalData.Instance.ViewArea.Width * 0.1875;
                        //viewFrameRightBottom5.VerticalAlignment = VerticalAlignment.Top;

                        #endregion

                        #region 平均排列，两行三列

                        var viewFrameLeftTop5 = orderViewFrames[0];
                        var viewFrameMiddleTop5 = orderViewFrames[1];
                        var viewFrameRightTop5 = orderViewFrames[2];
                        var viewFrameLeftBottom5 = orderViewFrames[3];
                        var viewFrameMiddleBottom5 = orderViewFrames[4];

                        viewFrameLeftTop5.Visibility = Visibility.Visible;
                        viewFrameLeftTop5.Row = 0;
                        viewFrameLeftTop5.RowSpan = 5;
                        viewFrameLeftTop5.Column = 0;
                        viewFrameLeftTop5.ColumnSpan = 10;
                        viewFrameLeftTop5.Width = GlobalData.Instance.ViewArea.Width * 0.3333;
                        viewFrameLeftTop5.Height = GlobalData.Instance.ViewArea.Width * 0.1875;
                        viewFrameLeftTop5.VerticalAlignment = VerticalAlignment.Bottom;

                        viewFrameMiddleTop5.Visibility = Visibility.Visible;
                        viewFrameMiddleTop5.Row = 0;
                        viewFrameMiddleTop5.RowSpan = 5;
                        viewFrameMiddleTop5.Column = 10;
                        viewFrameMiddleTop5.ColumnSpan = 10;
                        viewFrameMiddleTop5.Width = GlobalData.Instance.ViewArea.Width * 0.3333;
                        viewFrameMiddleTop5.Height = GlobalData.Instance.ViewArea.Width * 0.1875;
                        viewFrameMiddleTop5.VerticalAlignment = VerticalAlignment.Bottom;


                        viewFrameRightTop5.Visibility = Visibility.Visible;
                        viewFrameRightTop5.Row = 0;
                        viewFrameRightTop5.RowSpan = 5;
                        viewFrameRightTop5.Column = 20;
                        viewFrameRightTop5.ColumnSpan = 10;
                        viewFrameRightTop5.Width = GlobalData.Instance.ViewArea.Width * 0.3333;
                        viewFrameRightTop5.Height = GlobalData.Instance.ViewArea.Width * 0.1875;
                        viewFrameRightTop5.VerticalAlignment = VerticalAlignment.Bottom;

                        viewFrameLeftBottom5.Visibility = Visibility.Visible;
                        viewFrameLeftBottom5.Row = 5;
                        viewFrameLeftBottom5.RowSpan = 5;
                        viewFrameLeftBottom5.Column = 0;
                        viewFrameLeftBottom5.ColumnSpan = 10;
                        viewFrameLeftBottom5.Width = GlobalData.Instance.ViewArea.Width * 0.3333;
                        viewFrameLeftBottom5.Height = GlobalData.Instance.ViewArea.Width * 0.1875;
                        viewFrameLeftBottom5.VerticalAlignment = VerticalAlignment.Top;

                        viewFrameMiddleBottom5.Visibility = Visibility.Visible;
                        viewFrameMiddleBottom5.Row = 5;
                        viewFrameMiddleBottom5.RowSpan = 5;
                        viewFrameMiddleBottom5.Column = 10;
                        viewFrameMiddleBottom5.ColumnSpan = 10;
                        viewFrameMiddleBottom5.Width = GlobalData.Instance.ViewArea.Width * 0.3333;
                        viewFrameMiddleBottom5.Height = GlobalData.Instance.ViewArea.Width * 0.1875;
                        viewFrameMiddleBottom5.VerticalAlignment = VerticalAlignment.Top;

                        Log.Logger.Debug(
                            $"LaunchAverageLayout 5 => left_top phoneId={viewFrameLeftTop5.PhoneId}, name={viewFrameLeftTop5.ViewName}, hwnd={viewFrameLeftTop5.Hwnd}, width={viewFrameLeftTop5.Width}, height={viewFrameLeftTop5.Height}, row={viewFrameLeftTop5.Row}, rowspan={viewFrameLeftTop5.RowSpan}, column={viewFrameLeftTop5.Column}, columnspan={viewFrameLeftTop5.ColumnSpan}");
                        Log.Logger.Debug(
                            $"LaunchAverageLayout 5 => middle_top phoneId={viewFrameMiddleTop5.PhoneId}, name={viewFrameMiddleTop5.ViewName}, hwnd={viewFrameMiddleTop5.Hwnd}, width={viewFrameMiddleTop5.Width}, height={viewFrameMiddleTop5.Height}, row={viewFrameMiddleTop5.Row}, rowspan={viewFrameMiddleTop5.RowSpan}, column={viewFrameMiddleTop5.Column}, columnspan={viewFrameMiddleTop5.ColumnSpan}");
                        Log.Logger.Debug(
                            $"LaunchAverageLayout 5 => right_top phoneId={viewFrameRightTop5.PhoneId}, name={viewFrameRightTop5.ViewName}, hwnd={viewFrameRightTop5.Hwnd}, width={viewFrameRightTop5.Width}, height={viewFrameRightTop5.Height}, row={viewFrameRightTop5.Row}, rowspan={viewFrameRightTop5.RowSpan}, column={viewFrameRightTop5.Column}, columnspan={viewFrameRightTop5.ColumnSpan}");
                        Log.Logger.Debug(
                            $"LaunchAverageLayout 5 => left_bottom phoneId={viewFrameLeftBottom5.PhoneId}, name={viewFrameLeftBottom5.ViewName}, hwnd={viewFrameLeftBottom5.Hwnd}, width={viewFrameLeftBottom5.Width}, height={viewFrameLeftBottom5.Height}, row={viewFrameLeftBottom5.Row}, rowspan={viewFrameLeftBottom5.RowSpan}, column={viewFrameLeftBottom5.Column}, columnspan={viewFrameLeftBottom5.ColumnSpan}");
                        Log.Logger.Debug(
                            $"LaunchAverageLayout 5 => middle_bottom phoneId={viewFrameMiddleBottom5.PhoneId}, name={viewFrameMiddleBottom5.ViewName}, hwnd={viewFrameMiddleBottom5.Hwnd}, width={viewFrameMiddleBottom5.Width}, height={viewFrameMiddleBottom5.Height}, row={viewFrameMiddleBottom5.Row}, rowspan={viewFrameMiddleBottom5.RowSpan}, column={viewFrameMiddleBottom5.Column}, columnspan={viewFrameMiddleBottom5.ColumnSpan}");

                        #endregion

                        break;
                    default:

                        // LOG count of view frames is not between 0 and 5 
                        break;
                }
            });
        }

        private async Task LaunchBigSmallLayout()
        {
            var viewFramesVisible = ViewFrameList.Where(viewFrame => viewFrame.IsOpened);
            var framesVisible = viewFramesVisible as ViewFrame[] ?? viewFramesVisible.ToArray();
            if (framesVisible.Length <= 1)
            {
                await LaunchAverageLayout();
                return;
            }

            var bigViewFrame = framesVisible.FirstOrDefault(viewFrame => viewFrame.IsBigView);
            if (bigViewFrame == null)
            {
                await LaunchAverageLayout();
                return;
            }

            bigViewFrame.Visibility = Visibility.Visible;
            bigViewFrame.Row = 1;
            bigViewFrame.RowSpan = 8;
            bigViewFrame.Column = 0;
            bigViewFrame.ColumnSpan = 24;
            bigViewFrame.Width = GlobalData.Instance.ViewArea.Width * 0.8;
            bigViewFrame.Height = GlobalData.Instance.ViewArea.Width * 0.45;
            bigViewFrame.VerticalAlignment = VerticalAlignment.Center;

            Log.Logger.Debug(
                $"LaunchBigSmallLayout => big_view phoneId={bigViewFrame.PhoneId}, name={bigViewFrame.ViewName}, hwnd={bigViewFrame.Hwnd}, width={bigViewFrame.Width}, height={bigViewFrame.Height}, row={bigViewFrame.Row}, rowspan={bigViewFrame.RowSpan}, column={bigViewFrame.Column}, columnspan={bigViewFrame.ColumnSpan}");

            var smallViewFrames = framesVisible.Where(viewFrame => !viewFrame.IsBigView);
            var row = 1;
            foreach (var frame in smallViewFrames.OrderBy(viewFrame => viewFrame.ViewOrder))
            {
                if (row > 7)
                    break;

                frame.Visibility = Visibility.Visible;
                frame.Row = row;
                frame.RowSpan = 2;
                frame.Column = 24;
                frame.ColumnSpan = 6;
                frame.Width = GlobalData.Instance.ViewArea.Width * 0.2;
                frame.Height = GlobalData.Instance.ViewArea.Width * 0.1125;
                frame.VerticalAlignment = VerticalAlignment.Center;

                row += 2;

                Log.Logger.Debug(
                    $"LaunchBigSmallLayout => small_view phoneId={frame.PhoneId}, name={frame.ViewName}, hwnd={frame.Hwnd}, width={frame.Width}, height={frame.Height}, row={frame.Row}, rowspan={frame.RowSpan}, column={frame.Column}, columnspan={frame.ColumnSpan}");

            }
        }

        private async Task LaunchCloseUpLayout()
        {
            if (FullScreenView == null)
            {
                await LaunchAverageLayout();
                return;
            }

            ViewFrameList.ForEach(viewFrame =>
            {
                if (viewFrame.Hwnd != FullScreenView.Hwnd)
                    viewFrame.Visibility = Visibility.Collapsed;
            });

            FullScreenView.Visibility = Visibility.Visible;
            FullScreenView.Row = 0;
            FullScreenView.RowSpan = 10;
            FullScreenView.Column = 0;
            FullScreenView.ColumnSpan = 30;
            FullScreenView.Width = GlobalData.Instance.ViewArea.Width;
            FullScreenView.Height = GlobalData.Instance.ViewArea.Height;
            FullScreenView.VerticalAlignment = VerticalAlignment.Center;
        }

        private void SetBigView(ViewFrame view)
        {
            ViewFrameList.ForEach(viewFrame => { viewFrame.IsBigView = viewFrame.Hwnd == view.Hwnd ? true : false; });
        }

        private void SetFullScreenView(ViewFrame view)
        {
            FullScreenView = view;
        }

        private async Task StartPushLiveStreamAutomatically()
        {
            _serverPushLiveService.GetLiveParam();

            UserInfo userInfo = IoC.Get<UserInfo>();

            AsyncCallbackMsg result =
                await
                    _serverPushLiveService.StartPushLiveStream(
                        GetStreamLayout(_serverPushLiveService.LiveParam.Width,
                            _serverPushLiveService.LiveParam.Height), userInfo.PushStreamUrl);
        }
    }
}