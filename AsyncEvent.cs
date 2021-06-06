using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncNetworking {
    public static class DelegateExtensions {
        public static Task InvokeAsync<TArgs>(this Func<object, TArgs, CancellationToken, Task> func, object sender, TArgs e, CancellationToken t) =>
            func == null ? Task.CompletedTask : Task.WhenAll(func.GetInvocationList().Cast<Func<object, TArgs, CancellationToken, Task>>().Select(f => f(sender, e, t)));
    }
}
