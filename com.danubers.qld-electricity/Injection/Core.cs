using Autofac;
using Danubers.QldElectricity.Injection;

namespace Danubers.QldElectricity.Jobs
{
    public class Core : Module
    {
        public Core()
        {
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterModule<Injection.Services>()
                .RegisterModule<Repositories>()
                .RegisterModule<Injection.Factories>()
                .RegisterModule<Scheduler>();
            builder.RegisterType<SQLiteDataProvider>().As<IDataProvider>().SingleInstance();

        }
    }
}