using System.Windows.Controls;
using System.Windows.Input;
using St.Common.Helper;

namespace St.CollaborativeInfo
{
    /// <summary>
    /// CollaborativeInfoContentView.xaml 的交互逻辑
    /// </summary>
    public partial class CollaborativeInfoContentView : UserControl
    {
        public CollaborativeInfoContentView()
        {
            InitializeComponent();
            DataContext = new CollaborativeInfoContentViewModel(this);
        }

        private void PreviewLostKeyboardFocusHanlder(object sender, KeyboardFocusChangedEventArgs e)
        {
            DataGridRow dataGridRow = sender as DataGridRow;
            if (dataGridRow != null) dataGridRow.IsSelected = false;
        }


        private void PreviewGotKeyboardFocusHanlder(object sender, KeyboardFocusChangedEventArgs e)
        {
            DataGridRow dataGridRow = sender as DataGridRow;
            if (dataGridRow != null) dataGridRow.IsSelected = true;
        }

        private void PreviewMouseLeftButtonDownHandler(object sender, MouseButtonEventArgs e)
        {
            DataGridRow dataGridRow = sender as DataGridRow;
            if (dataGridRow != null)
            {
                DataGridCell dataGridCell = GridInteractiveLessons.GetCell(dataGridRow.GetIndex(), 6);

                ContentPresenter cp = dataGridCell.Content as ContentPresenter;
                if (cp != null)
                {
                    Button button = cp.ContentTemplate.FindName("GotoButton", cp) as Button;
                    button?.Focus();
                }
            }
        }
    }
}