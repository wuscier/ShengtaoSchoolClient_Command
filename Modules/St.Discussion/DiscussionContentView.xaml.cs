using System.Windows.Controls;
using System.Windows.Input;

namespace St.Discussion
{
    /// <summary>
    /// DiscussionContentView.xaml 的交互逻辑
    /// </summary>
    public partial class DiscussionContentView : UserControl
    {
        public DiscussionContentView()
        {
            InitializeComponent();
            DataContext = new DiscussionContentViewModel(this);
        }

        private void PreviewLostKeyboardFocusHanlder(object sender, KeyboardFocusChangedEventArgs e)
        {
            DataGridRow dataGridRow = sender as DataGridRow;
            if (dataGridRow != null) dataGridRow.IsSelected = false;
        }


        private void PreviewGotKeyboardFocus_OnHandler(object sender, KeyboardFocusChangedEventArgs e)
        {
            DataGridRow dataGridRow = sender as DataGridRow;
            if (dataGridRow != null) dataGridRow.IsSelected = true;
        }

        private void PreviewMouseLeftButtonDown_OnHandler(object sender, MouseButtonEventArgs e)
        {
            DataGridRow dataGridRow = sender as DataGridRow;
            dataGridRow?.Focus();
        }

        private void SelectedHandler(object sender, System.Windows.RoutedEventArgs e)
        {
            DataGridRow dataGridRow = sender as DataGridRow;
            dataGridRow?.Focus();
        }

    }
}
