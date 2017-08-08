using Autofac;
using Prism.Modularity;
using St.Common;
using St.Common.Contract;

namespace St.CommandListener
{
    public class CommandListenerModule : IModule
    {
        private readonly IComponentContext _context;

        public CommandListenerModule(IComponentContext context)
        {
            _context = context;
        }


        public void Initialize()
        {
            _context.Register(builder =>
            {
                builder.RegisterType<ServerSocket>().As<ICommandServer>().SingleInstance();
            });
        }
    }
}
