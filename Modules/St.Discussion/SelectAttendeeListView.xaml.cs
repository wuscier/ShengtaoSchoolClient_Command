using System.Windows;
using St.Common;

namespace St.Discussion
{
    /// <summary>
    /// MeetingView.xaml 的交互逻辑
    /// </summary>
    public partial class SelectAttendeeListView
    {
        public SelectAttendeeListView(SpecialViewType specialViewType)
        {
            InitializeComponent();

            DataContext = new SelectAttendeeListViewModel(this, specialViewType);
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}