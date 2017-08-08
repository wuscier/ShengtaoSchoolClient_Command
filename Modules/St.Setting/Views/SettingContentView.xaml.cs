using System.Windows.Controls;
using St.Common;

namespace St.Setting
{
    /// <summary>
    /// SettingContentView.xaml 的交互逻辑
    /// </summary>
    public partial class SettingContentView : UserControl
    {
        public SettingContentView()
        {
            InitializeComponent();
            TbSerialNo.Text = string.IsNullOrEmpty(GlobalData.Instance.SerialNo)
                ? string.Empty
                : string.Format($"本机设备号：{GlobalData.Instance.SerialNo}");
            DataContext = new SettingContentViewModel(this);
        }
    }
}
