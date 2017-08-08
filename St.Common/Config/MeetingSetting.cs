using System.Collections.ObjectModel;

namespace St.Common
{
    public class MeetingSetting
    {
        public VideoSetting UserCameraSetting { get; set; }
        public VideoSetting DataCameraSetting { get; set; }
        public VideoSetting Live { get; set; }
        public AudioSetting Audio { get; set; }

        public MeetingSetting()
        {
            UserCameraSetting = new VideoSetting() { BitRateList = new ObservableCollection<string>(), ResolutionList = new ObservableCollection<string>(), VideoType = "人像" };
            DataCameraSetting = new VideoSetting() { BitRateList = new ObservableCollection<string>(), ResolutionList = new ObservableCollection<string>(), VideoType = "数据" };
            Live = new VideoSetting() { BitRateList = new ObservableCollection<string>(), ResolutionList = new ObservableCollection<string>(), VideoType = "直播录制" };
            Audio = new AudioSetting() { BitRateList = new ObservableCollection<string>(), SampleRateList = new ObservableCollection<string>() };
        }
    }
}
