namespace St.Common
{
    public interface ITaskCallback
    {
        string Name { get; }
        void SetResult(object obj);
    }
}
