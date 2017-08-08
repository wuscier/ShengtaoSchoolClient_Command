using Prism.Mvvm;

namespace St.Common
{

    public class LiveConfig : BindableBase
    {
        public string Description { get; set; }

        private string resolution;

        public string Resolution
        {
            get { return resolution; }
            set { SetProperty(ref resolution, value); }
        }

        private string codeRate;

        public string CodeRate
        {
            get { return codeRate; }
            set { SetProperty(ref codeRate, value); }
        }

        private string pushLiveStreamUrl;

        public string PushLiveStreamUrl
        {
            get { return pushLiveStreamUrl; }
            set { SetProperty(ref pushLiveStreamUrl, value); }
        }

        public bool IsEnabled { get; set; }
    }
}