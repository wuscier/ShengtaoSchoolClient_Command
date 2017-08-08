using System;

namespace St.Common
{

    public class AggregatedConfig
    {
        /// <summary>
        /// ExternalEnv|InternalEnv
        /// </summary>
        public string InterfaceType { get; set; }

        public InterfaceConfig InterfaceTypes { get; set; }

        public string DeviceNo { get; set; }
        public string DeviceKey { get; set; }
        public int CommandServerPort { get; set; }
        public LoginConfig AccountAutoLogin { get; set; }
        public VideoConfig MainCamera { get; set; }
        public VideoConfig SecondaryCamera { get; set; }
        public AudioConfig AudioConfig { get; set; }
        public LiveConfig LocalLiveConfig { get; set; }
        public LiveConfig RemoteLiveConfig { get; set; }
        public RecordConfig RecordConfig { get; set; }

        public AggregatedConfig()
        {
            InterfaceType = InterfaceTypeStrings.External;

            DeviceNo = string.Empty;
            DeviceKey = string.Empty;

            CommandServerPort = 55555;

            AccountAutoLogin = new LoginConfig();

            MainCamera = new VideoConfig()
            {
                Type = "主摄像头",
            };

            SecondaryCamera = new VideoConfig()
            {
                Type = "辅摄像头"
            };
            AudioConfig = new AudioConfig();
            LocalLiveConfig = new LiveConfig()
            {
                Description = "本地推流",
                IsEnabled = true
            };
            RemoteLiveConfig = new LiveConfig()
            {
                Description = "服务器推流",
                IsEnabled = true
            };
            RecordConfig = new RecordConfig()
            {
                Description = "录制"
            };
        }

        public void CloneConfig(AggregatedConfig newConfig)
        {
            InterfaceType = newConfig.InterfaceType;

            InterfaceTypes = newConfig.InterfaceTypes;

            DeviceNo = newConfig.DeviceNo;
            DeviceKey = newConfig.DeviceKey;

            CommandServerPort = newConfig.CommandServerPort;

            AccountAutoLogin.IsAutoLogin = newConfig.AccountAutoLogin.IsAutoLogin;
            AccountAutoLogin.UserName = newConfig.AccountAutoLogin.UserName;
            AccountAutoLogin.Password = newConfig.AccountAutoLogin.Password;

            MainCamera.CodeRate = newConfig.MainCamera.CodeRate;
            MainCamera.Name = newConfig.MainCamera.Name;
            MainCamera.Resolution = newConfig.MainCamera.Resolution;

            SecondaryCamera.CodeRate = newConfig.SecondaryCamera.CodeRate;
            SecondaryCamera.Name = newConfig.SecondaryCamera.Name;
            SecondaryCamera.Resolution = newConfig.SecondaryCamera.Resolution;

            AudioConfig.CodeRate = newConfig.AudioConfig.CodeRate;
            AudioConfig.MainMicrophone = newConfig.AudioConfig.MainMicrophone;
            AudioConfig.SampleRate = newConfig.AudioConfig.SampleRate;
            AudioConfig.SecondaryMicrophone = newConfig.AudioConfig.SecondaryMicrophone;
            AudioConfig.Speaker = newConfig.AudioConfig.Speaker;

            LocalLiveConfig.CodeRate = newConfig.LocalLiveConfig.CodeRate;
            LocalLiveConfig.Resolution = newConfig.LocalLiveConfig.Resolution;
            LocalLiveConfig.PushLiveStreamUrl = newConfig.LocalLiveConfig.PushLiveStreamUrl;
            LocalLiveConfig.IsEnabled = newConfig.LocalLiveConfig.IsEnabled;
            RemoteLiveConfig.CodeRate = newConfig.RemoteLiveConfig.CodeRate;
            RemoteLiveConfig.Resolution = newConfig.RemoteLiveConfig.Resolution;
            RemoteLiveConfig.IsEnabled = newConfig.RemoteLiveConfig.IsEnabled;
            RecordConfig.CodeRate = newConfig.RecordConfig.CodeRate;
            RecordConfig.Resolution = newConfig.RecordConfig.Resolution;
            RecordConfig.RecordPath = newConfig.RecordConfig.RecordPath;
        }

        public InterfaceItem GetInterfaceItem()
        {
            switch (InterfaceType)
            {
                case InterfaceTypeStrings.External:
                    return InterfaceTypes.External;
                case InterfaceTypeStrings.Internal:
                    return InterfaceTypes.Internal;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
