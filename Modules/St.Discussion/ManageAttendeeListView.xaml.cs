using System.Windows;

namespace St.Discussion
{
    /// <summary>
    /// MeetingView.xaml 的交互逻辑
    /// </summary>
    public partial class ManageAttendeeListView
    {
        public ManageAttendeeListView()
        {
            InitializeComponent();

            DataContext = new ManageAttendeeListViewModel(this);
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
