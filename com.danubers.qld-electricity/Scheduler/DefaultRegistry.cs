using FluentScheduler;

namespace Danubers.QldElectricity.Jobs
{
    public class DefaultRegistry : Registry
    {
        public DefaultRegistry()
        {
            Schedule<BomProcessorJob>().WithName(typeof(BomProcessorJob).FullName).ToRunNow().AndEvery(10).Minutes();
            Schedule<EnergexProcessorJob>().WithName(typeof(EnergexProcessorJob).FullName).ToRunNow().AndEvery(5).Minutes();


            //Run summary generator
            Schedule<SummaryGeneratorJob>().WithName("summary").ToRunOnceAt(0, 0).AndEvery(1).Days().At(0, 0);
        }
    }
}