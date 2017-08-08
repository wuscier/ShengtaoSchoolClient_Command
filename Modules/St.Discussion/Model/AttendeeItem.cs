using System;
using System.Windows.Input;
using Prism.Mvvm;

namespace St.Discussion
{
    public class AttendeeItem : BindableBase
    {
        public string Text { get; set; }

        private string _content;

        public string Content
        {
            get { return _content; }
            set { SetProperty(ref _content, value); }
        }

        public IntPtr Hwnd { get; set; }

        public string Id { get; set; }

        public ICommand ButtonCommand { get; set; }
    }
}
