using Prism.Commands;

using St.Common;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Collections.ObjectModel;
using System.IO;
using Caliburn.Micro;
using MeetingSdk.SdkWrapper;
using MeetingSdk.SdkWrapper.MeetingDataModel;
using Prism.Regions;

namespace St.CollaborativeInfo
{
    public class CollaborativeInfoContentViewModel : ViewModelBase, IReloadRegion
    {
        public CollaborativeInfoContentViewModel(CollaborativeInfoContentView collaborativeInfoContentView)
        {
            _bmsService = IoC.Get<IBms>();
            _sdkService = IoC.Get<IMeeting>();
            _lessonInfo = IoC.Get<LessonInfo>();
            _regionManager = IoC.Get<IRegionManager>();

            LoadCommand = DelegateCommand.FromAsyncHandler(LoadAsync);
            RefreshCommand = DelegateCommand.FromAsyncHandler(LoadAsync);
            GotoLessonTypeCommand = new DelegateCommand(GotoLessonTypeAsync);

            Lessons = new ObservableCollection<LessonInfo>();
        }

        //private fields
        private readonly IBms _bmsService;
        private readonly IMeeting _sdkService;
        private readonly LessonInfo _lessonInfo;
        private readonly IRegionManager _regionManager;

        //properties
        private LessonInfo _selectedLesson;

        public LessonInfo SelectedLesson
        {
            get { return _selectedLesson; }
            set { SetProperty(ref _selectedLesson, value); }
        }


        public ObservableCollection<LessonInfo> Lessons { get; set; }

        //commands
        public ICommand LoadCommand { get; set; }
        public ICommand RefreshCommand { get; set; }
        public ICommand GotoLessonTypeCommand { get; set; }

        //command hanlders
        private async Task LoadAsync()
        {
            await StartSdkAsync();
            await GetLessonsAsync();
        }

        private void GotoLessonTypeAsync()
        {
            if (SelectedLesson == null)
            {
                return;
            }

            string view = string.Empty;
            switch (SelectedLesson.LessonType)
            {
                case LessonType.Discussion:
                    view = GlobalResources.DiscussionContentView;
                    break;
                case LessonType.Interactive:
                    view = GlobalResources.InteractiveContentView;
                    break;
                case LessonType.InteractiveWithoutLive:
                    view = GlobalResources.InteractiveWithouLiveContentView;
                    break;
            }

            _lessonInfo.CloneLessonInfo(SelectedLesson);
            _regionManager.RequestNavigate(RegionNames.ContentRegion, new Uri(view, UriKind.Relative));
        }

        private async Task GetLessonsAsync()
        {
            ResponseResult allLessonsResult = await _bmsService.GetLessons(false, null);

            if (!HasErrorMsg(allLessonsResult.Status, allLessonsResult.Message))
            {
                List<LessonInfo> lessonList = allLessonsResult.Data as List<LessonInfo>;

                Lessons.Clear();

                lessonList?.ForEach((lesson) =>
                {
                    lesson.GotoLessonTypeCommand = GotoLessonTypeCommand;
                    Lessons.Add(lesson);
                });
            }
        }

        private async Task StartSdkAsync()
        {
            if (!_sdkService.IsServerStarted)
            {
                _sdkService.SetSdkServerPath(Path.Combine(Environment.CurrentDirectory, "sdk"));

                IVisualizeShell visualizeShellService =
                    IoC.Get<IVisualizeShell>();

                visualizeShellService.StartingSdk();

                UserInfo userInfo = IoC.Get<UserInfo>();

                AsyncCallbackMsg startResult = await _sdkService.StartServerViaAppKey(userInfo.AppKey, userInfo.OpenId,
                    userInfo.GetNube(), "http://xmeeting.butel.com/nps_x1");

                if (startResult.Status != 0)
                {
                    visualizeShellService.FinishStartingSdk(false, startResult.Message);
                }
                else
                {
                    visualizeShellService.FinishStartingSdk(true, Messages.InfoMeetingSdkStarted);
                    AsyncCallbackMsg setFillModeResult = _sdkService.SetFillMode(0);

                    HasErrorMsg(setFillModeResult.Status.ToString(), Messages.WarningSetFillModeFailed);
                }

                //Thread.Sleep(1000);
            }
        }

        public async Task ReloadAsync()
        {
            await LoadAsync();
        }
    }
}
