using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;
using Serilog;
using St.Common.Contract;

namespace St.RtClient
{
    public class RtClientService : IRtClientService
    {
        private  HubConnection _connection;
        private readonly IRtClientConfiguration _rfConfig;
        public event EventHandler<RtSignOutEventArgs> SignOutEvent;
        public event EventHandler<RtGroupNotifyEventArgs> GroupNotifyEvent;
        public event EventHandler<RtMessageEventArgs> NotifyUserMessage;
        public event EventHandler<RtMessageEventArgs> NotifyGroupMessage;

        public RtClientService(IRtClientConfiguration rtConfig)
        {
            if(rtConfig == null)
                throw new ArgumentNullException(nameof(rtConfig));

            _rfConfig = rtConfig;
        }

        private IHubProxy _hubProxy;
        private bool _initialized;
        private object _lockObj = new object();
        void EnsureInitialize()
        {
            LazyInitializer.EnsureInitialized(ref _hubProxy, ref _initialized, ref _lockObj, Initialize);
        }

        IHubProxy Initialize()
        {
            if (string.IsNullOrEmpty(_rfConfig.RtServer))
                throw new Exception("RtServer endpoint is null.");

            _connection = new HubConnection(_rfConfig.RtServer);

            _hubProxy = _connection.CreateHubProxy("CommonHub");
            _hubProxy.On("SignOut", OnSignOut);
            _hubProxy.On<string, string[]>("Notify", OnNotify);
            _hubProxy.On<string, string>("NotifyUserMessage", OnNotifyUserMessage);
            _hubProxy.On<string, string>("NotifyGroupMessage", OnNotifyGroupMessage);
            return _hubProxy;
        }

        private void OnNotifyGroupMessage(string openId, string message)
        {
            this.NotifyGroupMessage?.Invoke(this, new RtMessageEventArgs() { OpenId = openId, Message = message });
        }

        private void OnNotifyUserMessage(string openId, string message)
        {
            this.NotifyUserMessage?.Invoke(this, new RtMessageEventArgs() { OpenId = openId, Message = message });
        }

        void OnSignOut()
        {
            this.SignOutEvent?.Invoke(this, new RtSignOutEventArgs());
        }

        void OnNotify(string code, string[] users)
        {
            if (!string.IsNullOrEmpty(code))
            {
                var args = new RtGroupNotifyEventArgs()
                {
                    Code = code
                };
                if (users != null && users.Length > 0)
                {
                    foreach (var u in users)
                    {
                        args.Users.Add(u);
                    }
                }
                this.GroupNotifyEvent?.Invoke(this, args);
            }
        }

        private bool _started;
        public async Task Start(string accessToken)
        {
            EnsureInitialize();

            if(string.IsNullOrEmpty(accessToken))
                throw new ArgumentException("accessToken is null.");

            if (_connection.Headers.ContainsKey("Authorization"))
                _connection.Headers.Remove("Authorization");
            _connection.Headers.Add("Authorization", $"Bearer {accessToken}");
            if (!_started)
            {
                _started = true;
                await _connection.Start();
            }
        }

        public async Task SignIn(string openId)
        {
            await _hubProxy.Invoke("SignIn", openId);
        }

        public async Task SignOut(string openId)
        {
            await _hubProxy.Invoke("SignOut", openId);
        }

        public async Task JoinGroup(string groupName)
        {
            await _hubProxy.Invoke("JoinGroup", groupName);
        }

        public async Task LeaveGroup(string groupName)
        {
            await _hubProxy.Invoke("LeaveGroup", groupName);
        }

        public async Task SendUserMessage(string openId, string message)
        {
            await _hubProxy.Invoke("SendUserMessage", openId, message);
        }

        public async Task SendGroupMessage(string groupName, string message, bool caller)
        {
            await _hubProxy.Invoke("SendGroupMessage", groupName, message, caller);
        }

        public bool IsConnected()
        {
            return _connection != null && _connection.State == ConnectionState.Connected;
        }

        public void Stop()
        {
            _started = false;
            if (_connection != null &&
                _connection.State == ConnectionState.Connected)
            {
                _connection.Stop();
            }
        }

        public void RefreshToken(string newToken)
        {
            Log.Logger.Debug($"【refresh rt server token】：newToken={newToken}");
            if (_connection.Headers.ContainsKey("Authorization"))
            {
                _connection.Headers["Authorization"] = $"Bearer {newToken}";
            }
            else
            {
                _connection.Headers.Add("Authorization", $"Bearer {newToken}");
            }
        }
    }
}
