using Autofac;
using FluentScheduler;

namespace Danubers.QldElectricity.Jobs
{
    public class Scheduler : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<DefaultRegistry>().As<Registry>();
        }

    }
}