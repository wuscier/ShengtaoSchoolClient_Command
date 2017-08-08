using System;

namespace St.RtClient
{
    public class RtSignOutEventArgs : EventArgs
    {
        public string OpenId { get; set; }
        public dynamic Data { get; set; }
    }
}
