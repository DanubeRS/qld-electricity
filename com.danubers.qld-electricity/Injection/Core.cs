using Autofac;

namespace Danubers.QldElectricity.Injection
{
    public class Core : Module
    {
        public Core()
        {
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterModule<Services>()
                .RegisterModule<Repositories>()
                .RegisterModule<Factories>();
            builder.RegisterType<SQLiteDataProvider>().As<IDataProvider>().SingleInstance();
        }
    }
}