using System;
using System.Collections.Generic;
using System.Threading;
using Caliburn.Micro;
using St.RtClient;
using Action = System.Action;

namespace St.Common.RtClient
{
    public class RtServerHandler
    {
        public Action SignOutHandler;
        public Action<string, HashSet<string>> GroupNotifyHandler;

        private Action _signOutAction = () => { };
        private Action<string, HashSet<string>> _groupNotifyAction = (code, users) => { };
        private RtServerHandler()
        {
            SignOutHandler = _signOutAction;
            GroupNotifyHandler = _groupNotifyAction;
        }

        private static RtServerHandler _handler;
        private static bool _initialized;
        private static object _lockObj;

        public static RtServerHandler Instance
        {
            get
            {
                if (_handler == null)
                {
                    Initialize();
                }
                return _handler;
            }
        }

        public static void Initialize()
        {
            var rtService = IoC.Get<IRtClientService>();
            if(rtService == null)
                throw new NullReferenceException("rtService is null.");

            LazyInitializer.EnsureInitialized(ref _handler, ref _initialized, ref _lockObj, () =>
            {
                _handler = new RtServerHandler();
                rtService.SignOutEvent += RtService_SignOutEvent;
                rtService.GroupNotifyEvent += RtService_GroupNotifyEvent;
                return _handler;
            });
        }

        private static void RtService_GroupNotifyEvent(object sender, RtGroupNotifyEventArgs e)
        {
            _handler.GroupNotifyHandler?.Invoke(e.Code, e.Users);
        }

        private static void RtService_SignOutEvent(object sender, RtSignOutEventArgs e)
        {
            _handler.SignOutHandler?.Invoke();
        }

        public void NotifyReset()
        {
            GroupNotifyHandler = _groupNotifyAction;
        }

        public void SignOutReset()
        {
            SignOutHandler = _signOutAction;
        }
    }
}
