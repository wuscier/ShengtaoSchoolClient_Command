namespace St.RtClient
{
    public class RtClientConfiguration : IRtClientConfiguration
    {
        public RtClientConfiguration(string rtServer)
        {
            this.RtServer = rtServer;
        }

        public string RtServer { get; }
    }
}