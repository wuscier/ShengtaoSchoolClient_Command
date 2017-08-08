using System.Windows;

namespace St.Discussion
{
    /// <summary>
    /// MeetingView.xaml 的交互逻辑
    /// </summary>
    public partial class LayoutView
    {
        public LayoutView()
        {
            InitializeComponent();

            DataContext = new LayoutViewModel(this);
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
