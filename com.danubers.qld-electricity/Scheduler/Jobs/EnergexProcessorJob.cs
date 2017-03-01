using Danubers.QldElectricity.Injection;
using FluentScheduler;

namespace Danubers.QldElectricity.Jobs
{
    public class EnergexProcessorJob : IJob
    {
        private readonly EnergexProcessor _processor;

        public EnergexProcessorJob(EnergexProcessor processor)
        {
            _processor = processor;
        }

        public void Execute()
        {
        }
    }
}