namespace St.Host
{
    /// <summary>
    /// DeviceLoginView.xaml 的交互逻辑
    /// </summary>
    public partial class DeviceLoginView
    {
        public DeviceLoginView()
        {
            InitializeComponent();
            DataContext = new DeviceLoginViewModel(this);
        }
    }
}
