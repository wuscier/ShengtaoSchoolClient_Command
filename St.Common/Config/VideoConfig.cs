using Prism.Mvvm;

namespace St.Common
{

    public class VideoConfig : BindableBase
    {
        public string Type { get; set; }


        private string name;
        public string Name
        {
            get { return name; }
            set { SetProperty(ref name, value); }
        }

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
    }
}