using System.Windows;
using System.Windows.Input;
using Prism.Mvvm;

namespace St.Discussion
{
    public class MenuButtonItem : BindableBase
    {
        private string _content;
        public string Content
        {
            get { return _content; }
            set { SetProperty(ref _content, value); }
        }


        private Visibility _visibility;
        public Visibility Visibility
        {
            get { return _visibility; }
            set { SetProperty(ref _visibility, value); }
        }


        private ICommand _command;
        public ICommand Command
        {
            get { return _command; }
            set { SetProperty(ref _command, value); }
        }

        public ICommand GotFocusCommand { get; set; }
    }
}
