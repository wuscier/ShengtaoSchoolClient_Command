using System;
using Prism.Mvvm;
using MaterialDesignThemes.Wpf;

namespace St.Common
{
    public class ViewModelBase : BindableBase
    {
        protected ViewModelBase()
        {
            MessageQueue = new SnackbarMessageQueue(TimeSpan.FromSeconds(2));
        }


        private SnackbarMessageQueue _messageQueue;
        public SnackbarMessageQueue MessageQueue
        {
            get { return _messageQueue; }
            set { SetProperty(ref _messageQueue, value); }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="status"></param>
        /// <param name="msg"></param>
        /// <returns>返回true表示有错误，返回falseb表示成功</returns>
        protected virtual bool HasErrorMsg(string status, string msg)
        {
            if (status != "0")
            {
                MessageQueue.Enqueue(msg, true);
                return true;
            }

            return false;
        }
    }
}
