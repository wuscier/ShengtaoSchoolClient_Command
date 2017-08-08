using St.Host.ViewModels;

namespace St.Host.Views
{
    /// <summary>
    ///     LoginView.xaml 的交互逻辑
    /// </summary>
    public partial class LoginView
    {
        public LoginView()
        {
            InitializeComponent();
            MouseLeftButtonDown += LoginView_MouseLeftButtonDown;
            DataContext = new LoginViewModel(this);
        }

        private void LoginView_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DragMove();
        }
    }
}