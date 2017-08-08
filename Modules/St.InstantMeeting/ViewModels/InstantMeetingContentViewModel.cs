using Autofac;
using Prism.Commands;
using System;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Linq;
using St.Common;
using System.Collections.Generic;
using MeetingSdk.SdkWrapper;
using MeetingSdk.SdkWrapper.MeetingDataModel;
using Caliburn.Micro;

namespace St.InstantMeeting
{
    public class InstantMeetingContentViewModel : ViewModelBase
    {
        public InstantMeetingContentViewModel(InstantMeetingContentView meetingContentView)
        {
            _meetingContentView = meetingContentView;
            _sdkService = IoC.Get<IMeeting>();

            MeetingRecords = new ObservableCollection<MeetingRecord>();

            CreateMeetingCommand = DelegateCommand.FromAsyncHandler(CreateMeetingAsync);
            JoinMeetingByNoCommand = DelegateCommand.FromAsyncHandler(JoinMeetingAsync);
            LoadMeetingListCommand = DelegateCommand.FromAsyncHandler(LoadMeetingListAsync);
            JoinMeetingFromListCommand = DelegateCommand<string>.FromAsyncHandler(JoinMeetingFromListAsync);
        }

        //private fields
        private readonly InstantMeetingContentView _meetingContentView;
        private readonly IMeeting _sdkService;

        //properties
        private string _meetingId;
        public string MeetingId
        {
            get { return _meetingId; }
            set { SetProperty(ref _meetingId, value); }
        }

        public ObservableCollection<MeetingRecord> MeetingRecords { get; set; }

        //commands
        public ICommand CreateMeetingCommand { get; set; }
        public ICommand JoinMeetingByNoCommand { get; set; }
        public ICommand LoadMeetingListCommand { get; set; }
        public ICommand JoinMeetingFromListCommand { get; set; }

        //command handlers
        private async Task CreateMeetingAsync()
        {
            AsyncCallbackMsg createMeetingResult = await _sdkService.CreateInstantMeeting(new Participant[0]);

            if (HasErrorMsg(createMeetingResult.Status.ToString(), createMeetingResult.Message))
            {
                return;
            }

            await GotoMeetingViewAsync();
        }

        private async Task JoinMeetingAsync()
        {
            uint mId;
            if (!uint.TryParse(MeetingId, out mId))
            {
                HasErrorMsg("-1", Messages.WarningInvalidMeetingNo);
                return;
            }


            int meetingId = (int) mId;
            if (meetingId == 0)
            {
                HasErrorMsg("-1", Messages.WarningInvalidMeetingNo);
                return;
            }

            AsyncCallbackMsg result = await _sdkService.VerifyMeetingExist(meetingId);
            if (result.Status == 6)
            {
                result.Message = Messages.WarningMeetingNoDoesNotExist;
            }

            if (HasErrorMsg(result.Status.ToString(), result.Message))
            {
                return;
            }

            _sdkService.MeetingId = meetingId;

            await GotoMeetingViewAsync();
        }

        private async Task LoadMeetingListAsync()
        {
            List<Meeting> meetings = await _sdkService.GetAvailableMeetings();

            if (meetings.Count > 0)
            {
                MeetingRecords.Clear();
            }

            meetings.ForEach((meetingInfo) =>
            {
                DateTime baseDateTime = new DateTime(1970, 1, 1);
                baseDateTime = baseDateTime.AddSeconds(double.Parse(meetingInfo.StartTime));
                string formattedStartTime = baseDateTime.ToString("yyyy-MM-dd HH:mm:ss");

                MeetingRecords.Add(new MeetingRecord()
                {
                    CreatorPhoneId = meetingInfo.CreatorId,
                    CreatorName = meetingInfo.CreatorName,
                    MeetingNo = meetingInfo.Id.ToString(),
                    StartTime = formattedStartTime,
                    JoinMeetingByListCommand = JoinMeetingFromListCommand
                });
            });
        }

        private async Task JoinMeetingFromListAsync(string meetingNo)
        {
            //some validation
            _sdkService.MeetingId = int.Parse(meetingNo);

            await GotoMeetingViewAsync();
        }


        //methods
        private async Task GotoMeetingViewAsync()
        {
            var lessonDetail = IoC.Get<LessonDetail>();
            lessonDetail.CloneLessonDetail(new LessonDetail());

            var attendees = IoC.Get<List<UserInfo>>();
            attendees.Clear();

            await _meetingContentView.Dispatcher.BeginInvoke(new System.Action(() =>
            {
                //Window meetingView = _container.ResolveNamed<Window>("MeetingView", new TypedParameter(typeof(int), meetingId));
                IMeetingTrigger meetingService = IoC.Get<IMeetingTrigger>();

                meetingService.StartMeetingCallbackEvent += MeetingService_StartMeetingCallbackEvent;

                meetingService.ExitMeetingCallbackEvent += MeetingService_ExitMeetingCallbackEvent;

                meetingService.StartMeeting();
            }));
        }

        private void MeetingService_ExitMeetingCallbackEvent(bool exitedSuccessful, string arg2)
        {
            if (exitedSuccessful)
            {
                IVisualizeShell visualizeShellService = IoC.Get<IVisualizeShell>();
                visualizeShellService.ShowShell();
            }
        }

        private void MeetingService_StartMeetingCallbackEvent(bool startedSuccessful, string msg)
        {
            if (startedSuccessful)
            {
                IVisualizeShell visualizeShellService = IoC.Get<IVisualizeShell>();
                visualizeShellService.HideShell();
            }
            else
            {
                HasErrorMsg("-1", msg);
            }
        }

        private void InternalMessagePassThroughEventHandler(string internalMessage)
        {
            HasErrorMsg("-1",internalMessage);
        }
    }
}
