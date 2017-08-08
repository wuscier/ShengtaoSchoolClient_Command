using System.Windows.Input;

namespace St.InstantMeeting
{
    public class MeetingRecord
    {
        public string MeetingNo { get; set; }
        public string CreatorPhoneId { get; set; }
        public string CreatorName { get; set; }
        public string StartTime { get; set; }
        public ICommand JoinMeetingByListCommand { get; set; }
    }
}
