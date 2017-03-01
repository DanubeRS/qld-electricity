using Autofac;
using Danubers.QldElectricity.Factories;
using FluentScheduler;

namespace Danubers.QldElectricity.Jobs
{
    public class Scheduler : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<DIFluentSchedulerFactory>().As<IJobFactory>().SingleInstance();
            builder.RegisterType<DefaultRegistry>().As<Registry>();

            builder.RegisterType<BomProcessorJob>();
            builder.RegisterType<EnergexProcessorJob>();
            builder.RegisterType<SummaryGeneratorJob>();
        }

    }
}