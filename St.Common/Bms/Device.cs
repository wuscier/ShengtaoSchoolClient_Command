namespace St.Common
{
    public class Device
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public bool IsExpired { get; set; }
        public bool Locked { get; set; }
        public bool EnableLogin { get; set; }
    }
}
