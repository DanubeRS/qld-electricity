using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentScheduler;

namespace Danubers.QldElectricity.Scheduler
{
    /// <summary>
    /// Simple shim that is used in hopes of native TPL support
    /// </summary>
    public abstract class AsyncJobShim : IJob
    {
        public void Execute()
        {
            ExecuteAsync(CancellationToken.None).Wait();
        }

        protected abstract Task ExecuteAsync(CancellationToken ct);
    }
}
