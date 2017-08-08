using System.Threading.Tasks;

namespace St.Common
{
    public interface IVisualizeShell
    {
        void ShowShell();
        void HideShell();
        Task Logout();
        void StartingSdk();
        void FinishStartingSdk(bool succeeded, string msg);
        void SetSelectedMenu(string menuName);
        void SetTopMost(bool isTopMost);
    }
}
