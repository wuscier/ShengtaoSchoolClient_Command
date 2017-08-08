using System;
using System.Threading.Tasks;

namespace St.RtClient
{
    public interface IRtClientService
    {
        event EventHandler<RtSignOutEventArgs> SignOutEvent;
        event EventHandler<RtGroupNotifyEventArgs> GroupNotifyEvent;
        event EventHandler<RtMessageEventArgs> NotifyUserMessage;
        event EventHandler<RtMessageEventArgs> NotifyGroupMessage;

        Task SignIn(string openId);
        Task SignOut(string openId);
        
        Task JoinGroup(string groupName);
        Task LeaveGroup(string groupName);

        Task SendUserMessage(string openId, string message);
        Task SendGroupMessage(string groupName, string message, bool caller);

        Task Start(string accessToken);
        void Stop();
        bool IsConnected();
        void RefreshToken(string newToken);
    }
}
