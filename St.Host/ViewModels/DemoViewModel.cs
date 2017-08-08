using System.Collections.Generic;
using System.Windows;
using Caliburn.Micro;

namespace St.Host.ViewModels
{
    public class DemoViewModel : ViewAware
    {
        public DemoViewModel()
        {
            Name = "justlucky";
        }
        public void Save()
        {
            this.Name = "asfasfdsf";
        }

        public void Save2(string type, string message)
        {

        }

        public IEnumerable<IResult> Save3()
        {
            yield return new DelegateResult(() =>
            {
                MessageBox.Show("aaaa");
            });
        }

        private string _name;

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                this.NotifyOfPropertyChange(() => this.Name);
            }
        }

        private IViewAware _contentCtrl;
        public IViewAware ContentCtrl
        {
            get => _contentCtrl;
            set
            {
                _contentCtrl = value;
                this.NotifyOfPropertyChange(() => this.ContentCtrl);
            }
        }

        public void SetContent()
        {
            ContentCtrl = new ChildViewModel();
        }

        protected override void OnViewLoaded(object view)
        {
            // base.OnViewLoaded(view);
        }

        public class ChildViewModel : ViewAware
        {
            
        }
    }
}