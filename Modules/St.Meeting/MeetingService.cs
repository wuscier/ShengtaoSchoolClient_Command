using System;
using St.Common;

namespace St.Meeting
{
    public class MeetingService : IMeetingTrigger
    {

        public MeetingService()
        {
            
        }

        public event Action<bool, string> StartMeetingCallbackEvent;
        public event Action<bool, string> ExitMeetingCallbackEvent;


        public void StartMeeting()
        {
            MeetingView mv = new MeetingView(StartMeetingCallbackEvent, ExitMeetingCallbackEvent);
            //MeetingView mv =
            //    IoC.Get<MeetingView>(
            //        new TypedParameter(typeof(Action<bool, string>), StartMeetingCallbackEvent),
            //        new TypedParameter(typeof(Action<bool, string>), ExitMeetingCallbackEvent));
            mv.Show();
        }
    }
}
