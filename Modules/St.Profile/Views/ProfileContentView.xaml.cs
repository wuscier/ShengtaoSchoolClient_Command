using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Autofac;
using System.Windows.Controls;
using System.Windows.Media;
using Caliburn.Micro;
using St.Common;

namespace St.Profile
{
    /// <summary>
    /// ProfileContentView.xaml 的交互逻辑
    /// </summary>
    public partial class ProfileContentView : UserControl
    {
        private readonly ProfileContentViewModel _viewModel;
        public ProfileContentView(IContainer container)
        {
            InitializeComponent();
            _viewModel = new ProfileContentViewModel(this, container);
            DataContext = _viewModel;
        }

        private void DropBox_DragLeave(object sender, DragEventArgs e)
        {
            var listbox = (ListBox)sender;
            listbox.Background = new SolidColorBrush(Color.FromRgb(226, 226, 226));
        }

        private void DropBox_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
                var listbox = (ListBox)sender;
                //listbox.Background = new SolidColorBrush(Color.FromRgb(155, 155, 155));
                listbox.Background = new SolidColorBrush(Color.FromArgb(0, 255, 255, 255));
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }

        }

        private static readonly HashSet<string> PicExts = new HashSet<string>(StringComparer.OrdinalIgnoreCase){".jpg",".png",".bmp",".gif"};
        private void DropBox_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    var picFile = files[0];
                    var ext = Path.GetExtension(picFile);
                    if (PicExts.Contains(ext))
                    {
                        var bytes = File.ReadAllBytes(picFile);
                        if (bytes.Length < 204800)
                        {
                            Task.Factory.StartNew(async (obj) => await UploadPic((byte[])obj), bytes);
                            _viewModel.SetImageSource(bytes);
                        }
                    }
                }
            }
        }

        private async Task UploadPic(byte[] bytes)
        {
            var bms = IoC.Get<IBms>();
            var str = Convert.ToBase64String(bytes);
            var result = await bms.UpdateUserPic(str);
            if (result.Status == "0")
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    _viewModel.SetImageSource(bytes);
                });
            }
        }
    }
}
