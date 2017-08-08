using Prism.Mvvm;

namespace St.Common
{
    public class AudioConfig:BindableBase
    {
        private string mainMicrophone;
        public string MainMicrophone
        {
            get { return mainMicrophone; }
            set { SetProperty(ref mainMicrophone, value); }
        }

        private string secondaryMicrophone;
        public string SecondaryMicrophone
        {
            get { return secondaryMicrophone; }
            set { SetProperty(ref secondaryMicrophone, value); }
        }


        private string speaker;
        public string Speaker
        {
            get { return speaker; }
            set { SetProperty(ref speaker, value); }
        }
        private string sampleRate;
        public string SampleRate
        {
            get { return sampleRate; }
            set { SetProperty(ref sampleRate, value); }
        }
        private string codeRate;
        public string CodeRate
        {
            get { return codeRate; }
            set { SetProperty(ref codeRate, value); }
        }
    }
}