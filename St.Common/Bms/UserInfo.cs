using Prism.Mvvm;
using Serilog;

namespace St.Common
{
    public class UserInfo : BindableBase
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Mobile { get; set; }
        public string SchoolId { get; set; }
        public string SchoolName { get; set; }

        /// <summary>
        /// 公网系统Nube号
        /// </summary>
        public string Nube { get; set; }

        /// <summary>
        /// 本地系统Nube号
        /// </summary>
        public string Nube2 { get; set; }

        /// <summary>
        /// 设备号
        /// </summary>
        public string Imei { get; set; }

        public string AppKey { get; set; }
        public string OpenId { get; set; }

        public string PushStreamUrl { get; set; }

        public bool IsTeacher { get; set; }

        public bool IsLogouted { get; set; }

        private bool _isOnline;

        public bool IsOnline
        {
            get { return _isOnline; }
            set { SetProperty(ref _isOnline, value); }
        }

        public string Pwd { get; set; }

        public void CloneUserInfo(UserInfo newUserInfo)
        {
            UserName = newUserInfo.UserName;
            UserId = newUserInfo.UserId;
            Name = newUserInfo.Name;
            Mobile = newUserInfo.Mobile;
            Email = newUserInfo.Email;
            OpenId = newUserInfo.OpenId;
            Nube = newUserInfo.Nube;
            Nube2 = newUserInfo.Nube2;
            Imei = newUserInfo.Imei;
            AppKey = newUserInfo.AppKey;
            PushStreamUrl = newUserInfo.PushStreamUrl;
            SchoolId = newUserInfo.SchoolId;
            SchoolName = newUserInfo.SchoolName;

            IsTeacher = newUserInfo.IsTeacher;
            IsLogouted = newUserInfo.IsLogouted;
            IsOnline = newUserInfo.IsOnline;
            Pwd = newUserInfo.Pwd;

            Log.Logger.Debug($"【user info】：username={UserName}, name={Name}, nube={GetNube()}");
        }

        public string GetNube()
        {
            return GlobalData.Instance.AggregatedConfig.InterfaceType == InterfaceTypeStrings.External ? Nube : Nube2;
        }
    }
}