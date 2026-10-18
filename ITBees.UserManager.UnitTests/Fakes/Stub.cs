using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ITBees.UserManager.UnitTests.Fakes
{
    /// <summary>An interface stand-in that answers the named methods and returns defaults for the rest.</summary>
    public class Stub : DispatchProxy
    {
        private Dictionary<string, Func<object[], object>> _handlers = new();

        public static T Of<T>(params (string Method, Func<object[], object> Handler)[] handlers) where T : class
        {
            var proxy = Create<T, Stub>();
            ((Stub)(object)proxy)._handlers = handlers.ToDictionary(x => x.Method, x => x.Handler);
            return proxy;
        }

        protected override object Invoke(MethodInfo targetMethod, object[] args)
        {
            if (_handlers.TryGetValue(targetMethod.Name, out var handler))
                return handler(args);
            return targetMethod.ReturnType.IsValueType && targetMethod.ReturnType != typeof(void)
                ? Activator.CreateInstance(targetMethod.ReturnType)
                : null;
        }
    }
}
