using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Danubers.QldElectricity
{
    public class DailyProcessor : IBackgroundProcessor
    {
        private Task _runner;
        public DailyProcessor()
        {
        }

        private CancellationTokenSource _cts;

        public bool Running => _runner.Status == TaskStatus.Running;
        public async Task Start(CancellationToken ct)
        {
            if (_cts != null)
                throw new Exception();
            _cts = new CancellationTokenSource();
            if (_runner != null && !_runner.IsCompleted)
                throw new Exception();  //TODO

            var token = _cts.Token;
            _runner = Task.Run(async () =>
            {
                var checkNext = DateTime.Today.AddDays(1);
                while (!token.IsCancellationRequested)
                {
                    //Check if after midnight, and processing required
                    if (DateTime.Now > checkNext)
                    {
                        try
                        {
                            //TODO Oh god how do I get a time range for SQLite
                        }
                        catch
                        {
                        }
                        finally
                        {
                            checkNext = DateTime.Today.AddDays(1);
                        }
                    }
                    //Check again at next midnight
                    await Task.Delay(checkNext - DateTime.Now, token);
                }
                
            }, _cts.Token);
        }


        public async Task Stop(CancellationToken ct)
        {
            if (_cts == null)
                throw new Exception();

            _cts.Cancel();
            await _runner;
            _cts.Dispose();
            _cts = null;
        }
    }
}
