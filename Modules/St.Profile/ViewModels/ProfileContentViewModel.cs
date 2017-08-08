using Prism.Commands;
using System.Windows.Input;
using System.Threading.Tasks;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Autofac;
using Caliburn.Micro;
using Serilog;
using St.Common;
using IContainer = Autofac.IContainer;

namespace St.Profile
{
    public class ProfileContentViewModel : ViewModelBase
    {
        public ProfileContentViewModel(ProfileContentView profileContentView, IContainer container)
        {
            _profileContentView = profileContentView;
            _userInfo = container.Resolve<UserInfo>();

            LoadCommand = DelegateCommand.FromAsyncHandler(LoadAsync);
            LogoutCommand = new DelegateCommand(LogoutAsync);
        }

        //private fields
        private readonly ProfileContentView _profileContentView;
        private readonly UserInfo _userInfo;

        //properties
        private string nickName;

        public string NickName
        {
            get { return nickName; }
            set { SetProperty(ref nickName, value); }
        }

        private string mobile;

        public string Mobile
        {
            get { return mobile; }
            set { SetProperty(ref mobile, value); }
        }

        private string email;

        public string Email
        {
            get { return email; }
            set { SetProperty(ref email, value); }
        }

        private string phoneId;

        public string PhoneId
        {
            get { return phoneId; }
            set { SetProperty(ref phoneId, value); }
        }

        private Visibility _logoutVisibility = Visibility.Visible;
        public Visibility LogoutVisibility
        {
            get { return _logoutVisibility; }
            set { SetProperty(ref _logoutVisibility, value); }
        }

        //commands
        public ICommand LoadCommand { get; set; }
        public ICommand LogoutCommand { get; set; }

        //command handlers
        private async Task LoadAsync()
        {
            if (GlobalData.Instance.Device.EnableLogin)
            {
                LogoutVisibility = Visibility.Collapsed;
            }

            NickName = _userInfo.Name;
            Mobile = _userInfo.Mobile;
            Email = _userInfo.Email;
            PhoneId = _userInfo.GetNube();

            await LoadPic();
        }

        private async Task LoadPic()
        {
            try
            {
                var bms = IoC.Get<IBms>();
                var result = await bms.GetUserPic();
                if (result.Status == "0")
                {
                    var bytes = Convert.FromBase64String(result.Data.ToString());
                    SetImageSource(bytes);
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Error($"【load pic exception】：{ex}");
            }
        }

        private void LogoutAsync()
        {
            Log.Logger.Debug("【log out by logout command】");   
            IVisualizeShell visualizeShellService = IoC.Get<IVisualizeShell>();
            visualizeShellService.Logout();
        }

        private ImageSource _imageSource;

        public ImageSource ImageSource
        {
            get { return _imageSource; }
            set
            {
                _imageSource = value;
                SetProperty(ref _imageSource, value);
            }
        }

        public ObservableCollection<BitmapSource> Pictures { get; } = new ObservableCollection<BitmapSource>();

        public void SetImageSource(byte[] imageData)
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.StreamSource = new MemoryStream(imageData);
            image.EndInit();
            ImageSource = image;

            Pictures.Clear();
            Pictures.Add(image);
        }
    }
}