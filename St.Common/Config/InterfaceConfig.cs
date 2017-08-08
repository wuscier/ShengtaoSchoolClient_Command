namespace St.Common
{
    public class InterfaceConfig
    {
        public InterfaceConfig()
        {
            Internal = new InterfaceItem()
            {
                //InterfaceType = InterfaceType.Internal,
        };
            External = new InterfaceItem()
            {
                //InterfaceType = InterfaceType.External,
            };
        }

        public InterfaceItem Internal { get; set; }
        public InterfaceItem External { get; set; }
    }
}
