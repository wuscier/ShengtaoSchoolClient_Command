using System.Windows.Controls;
using System.Windows.Input;

namespace St.InstantMeeting
{
    /// <summary>
    /// MeetingContentView.xaml 的交互逻辑
    /// </summary>
    public partial class InstantMeetingContentView : UserControl
    {
        public InstantMeetingContentView()
        {
            InitializeComponent();
            FocusManager.SetFocusedElement(this, meetingId);
            DataContext = new InstantMeetingContentViewModel(this);
        }
    }
}
