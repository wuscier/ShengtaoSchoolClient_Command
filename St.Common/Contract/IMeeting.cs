using System;

namespace St.Common
{
    public interface IMeetingTrigger
    {
        void StartMeeting();
        event Action<bool, string> StartMeetingCallbackEvent;
        event Action<bool, string> ExitMeetingCallbackEvent;
    }
}
