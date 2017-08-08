using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Autofac;
using Autofac.Builder;

namespace St.Common.Contract
{
    public interface IAutofacRegister
    {
        void Register(Action<ContainerBuilder> action);
    }

    public class AutofacRegister : IAutofacRegister
    {
        private readonly IComponentContext _componentContext;
        private readonly ContainerBuilder _containerBuilder;
        private readonly IList<DeferredCallback> _configurationCallbacks;
        public AutofacRegister(IComponentContext componentContext)
        {
            _componentContext = componentContext;

            var type = typeof(IList<>).MakeGenericType(typeof(DeferredCallback));

            var field = typeof(ContainerBuilder)
                .GetTypeInfo()
                .GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                .SingleOrDefault(m => m.FieldType == type);

            if (field == null)
                throw new NotSupportedException();

            _containerBuilder = new ContainerBuilder();
            _configurationCallbacks = (IList<DeferredCallback>)field.GetValue(_containerBuilder);
            _containerBuilder.Build();
        }

        public void Register(Action<ContainerBuilder> action)
        {
            bool locked = false;
            try
            {
                Monitor.Enter(_containerBuilder, ref locked);

                _configurationCallbacks.Clear();
                action(_containerBuilder);
                foreach (var callback in _configurationCallbacks)
                {
                    callback.Callback(_componentContext.ComponentRegistry);
                }
            }
            finally
            {
                _configurationCallbacks.Clear();
                if (locked)
                {
                    Monitor.Exit(_containerBuilder);
                }
            }
        }
    }

    public static class AutofacExtensions
    {
        public static void Register(this IComponentContext context, Action<ContainerBuilder> action)
        {
            object register;
            if(!context.TryResolve(typeof(IAutofacRegister),out register))
                throw new NotSupportedException();

            ((IAutofacRegister)register).Register(action);
        }
    }
}
