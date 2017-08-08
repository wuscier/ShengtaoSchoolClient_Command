using Prism.Mvvm;

namespace St.Common
{

    public class RecordConfig : BindableBase
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

        private string recordPath;

        public string RecordPath
        {
            get { return recordPath; }
            set { SetProperty(ref recordPath, value); }
        }

    }

}