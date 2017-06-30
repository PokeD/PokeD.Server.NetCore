using System;
using System.Threading;
using System.Threading.Tasks;

namespace PokeD.Server.NetCore.Extensions
{
    public static class TaskExtension
    {
        public static TResult Wait<TResult>(this Task<TResult> task, CancellationTokenSource cancellationTokenSource)
        {
            try
            {
                task.Wait(cancellationTokenSource.Token);
                return task.Result;
            }
            catch(Exception ex) { throw task?.Exception ?? ex; }
        }
    }
}