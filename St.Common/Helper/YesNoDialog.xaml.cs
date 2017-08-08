using System.Windows;

namespace St.Common
{
    /// <summary>
    /// UpdateConfirmView.xaml 的交互逻辑
    /// </summary>
    public partial class YesNoDialog
    {
        public YesNoDialog(string updateMsg) : this()
        {
            TbUpdateMsg.Text = updateMsg;
        }

        public YesNoDialog()
        {
            InitializeComponent();
            BtnUpdate.Focus();
        }

        private void BtnUpdate_OnClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void BtnNextTime_OnClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
