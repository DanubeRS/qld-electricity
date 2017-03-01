using Autofac;
using FluentScheduler;

namespace Danubers.QldElectricity.Factories
{
    internal class DIFluentSchedulerFactory : IJobFactory
    {
        private readonly IComponentContext _resolver;

        public DIFluentSchedulerFactory(IComponentContext resolver)
        {
            _resolver = resolver;
        }

        public IJob GetJobInstance<T>() where T : IJob
        {
            return _resolver.Resolve<T>();
        }
    }
}