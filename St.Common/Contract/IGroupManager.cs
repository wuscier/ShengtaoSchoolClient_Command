using System.Threading.Tasks;

namespace St.Common
{
    public interface IGroupManager
    {
        string CurGroupCode { get;}

        string CurViewName { get;}

        Task JoinGroup(string groupCode, string viewName);

        Task LeaveGroup();

        Task GotoNewView(string newViewName);
    }
}
