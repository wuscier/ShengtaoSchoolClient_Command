using System.Collections.ObjectModel;

namespace St.Common
{
    public class AudioSetting
    {
        public ObservableCollection<string> SampleRateList { get; set; }
        public ObservableCollection<string> BitRateList { get; set; }

        public void Clear()
        {
            SampleRateList?.Clear();

            BitRateList?.Clear();
        }
    }
}