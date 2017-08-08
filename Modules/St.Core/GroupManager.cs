using System;
using System.Threading.Tasks;
using Serilog;
using St.Common;
using St.Common.Contract;

namespace St.Core
{
    public class GroupManager : IGroupManager
    {
        private readonly IRtClientService _rtClientService;

        public GroupManager()
        {
            _rtClientService = DependencyResolver.Current.GetService<IRtClientService>();
        }

        public string CurGroupCode { get; private set; }

        public string CurViewName { get; private set; }

        public async Task GotoNewView(string newViewName)
        {
            if (_rtClientService == null)
            {
                Log.Logger.Error("【rt client service is null】");
                return;
                //throw new Exception("_rtClientService is null");
            }

            if (newViewName != CurViewName && !string.IsNullOrEmpty(CurGroupCode))
            {
                try
                {
                    // go to a different view, need to leave group of CurGroupCode
                    await _rtClientService.LeaveGroup(CurGroupCode);

                    CurGroupCode = string.Empty;
                    CurViewName = string.Empty;
                }
                catch (Exception ex)
                {
                    Log.Logger.Error($"【leave group exception】：{ex}");
                }
            }
        }

        public async Task JoinGroup(string newGroupCode, string newViewName)
        {
            if (_rtClientService == null)
            {
                Log.Logger.Error("【rt client service is null】");
                return;
                //throw new Exception("_rtClientService is null");
            }

            try
            {
                if (newGroupCode != CurGroupCode && !string.IsNullOrEmpty(CurGroupCode))
                {
                    // join a new group for the first time 
                    // or join a different group(in this case, need to leave group of CurGroupCode)
                    await _rtClientService.LeaveGroup(CurGroupCode);
                }

                CurGroupCode = newGroupCode;
                CurViewName = newViewName;

                await _rtClientService.JoinGroup(newGroupCode);

            }
            catch (Exception ex)
            {
                Log.Logger.Error($"【join group exception】：{ex}");
            }
        }

        public async Task LeaveGroup()
        {
            if (_rtClientService == null)
            {
                Log.Logger.Error("【rt client service is null】");
                return;
            }

            try
            {
                if (!string.IsNullOrEmpty(CurGroupCode))
                {
                    Log.Logger.Debug($"【leave gorup】：groupcode={CurGroupCode}");
                    await _rtClientService.LeaveGroup(CurGroupCode);
                    CurGroupCode = string.Empty;
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Error($"【leave group exception】：{ex}");
            }
        }
    }
}
