using System.Threading.Tasks;

namespace St.Common
{
    public interface ICommandServer
    {
        Task RunServer();
        void StopServer();
    }
}
