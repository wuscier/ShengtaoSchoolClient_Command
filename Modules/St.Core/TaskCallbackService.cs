using System.Threading.Tasks;
using St.Common;

namespace St.Core
{
    public class TaskCallback<TResult> : ITaskCallback
        where TResult : class
    {
        private readonly TaskCompletionSource<TResult> _tcs;

        public TaskCallback(string name)
        {
            Name = name;
            _tcs = new TaskCompletionSource<TResult>();
        }

        public Task<TResult> Task => _tcs.Task;

        public string Name { get; }

        public void SetResult(object obj)
        {
            SetResult(obj as TResult);
        }

        public void SetResult(TResult obj)
        {
            _tcs.SetResult(obj);
        }
    }
}