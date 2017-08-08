namespace St.Setting
{
    public enum ConfigItemKey
    {
        MainCamera,
        MainCameraResolution,
        MainCameraCodeRate,
        SecondaryCamera,
        SecondaryCameraResolution,
        SecondaryCameraCodeRate,
        MainMicrophone,
        SecondaryMicrophone,
        Speaker,
        AudioSampleRate,
        AudioCodeRate,
        LiveResolution,
        LiveCodeRate,
        Unknown
    }

    public class ConfigItemTag
    {

        public ConfigItemTag()
        {
            MainCamera = ConfigItemKey.MainCamera;
            MainCameraResolution = ConfigItemKey.MainCameraResolution;
            MainCameraCodeRate = ConfigItemKey.MainCameraCodeRate;
            SecondaryCamera = ConfigItemKey.SecondaryCamera;
            SecondaryCameraResolution = ConfigItemKey.SecondaryCameraResolution;
            SecondaryCameraCodeRate = ConfigItemKey.SecondaryCameraCodeRate;
            MainMicrophone = ConfigItemKey.MainMicrophone;
            SecondaryMicrophone = ConfigItemKey.SecondaryMicrophone;
            Speaker = ConfigItemKey.Speaker;
            AudioSampleRate = ConfigItemKey.AudioSampleRate;
            AudioCodeRate = ConfigItemKey.AudioCodeRate;
            LiveResolution = ConfigItemKey.LiveResolution;
            LiveCodeRate = ConfigItemKey.LiveCodeRate;
        }

        public ConfigItemKey MainCamera { get; set; }
        public ConfigItemKey MainCameraResolution { get; set; }
        public ConfigItemKey MainCameraCodeRate { get; set; }
        public ConfigItemKey SecondaryCamera { get; set; }
        public ConfigItemKey SecondaryCameraResolution { get; set; }
        public ConfigItemKey SecondaryCameraCodeRate { get; set; }
        public ConfigItemKey MainMicrophone { get; set; }
        public ConfigItemKey SecondaryMicrophone { get; set; }
        public ConfigItemKey Speaker { get; set; }
        public ConfigItemKey AudioSampleRate { get; set; }
        public ConfigItemKey AudioCodeRate { get; set; }
        public ConfigItemKey LiveResolution { get; set; }
        public ConfigItemKey LiveCodeRate { get; set; }
    }
}