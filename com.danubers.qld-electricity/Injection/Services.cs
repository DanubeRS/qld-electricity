using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;

namespace Danubers.QldElectricity.Injection
{
    public class Services : Module
    {
        public Services()
        {
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<DefaultBackgroundService>().As<IBackgroundService>().SingleInstance().ExternallyOwned();
        }
    }
}
