namespace St.Common
{
    /// <summary>
    /// ConfirmDialogContent.xaml 的交互逻辑
    /// </summary>
    public partial class ConfirmDialogContent
    {
        public ConfirmDialogContent()
        {
            InitializeComponent();
            Loaded += ConfirmDialogContent_Loaded;
        }

        private void ConfirmDialogContent_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            PositiveButton.Focus();
        }
    }
}
