using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Autofac.Core.Activators;
using Dapper;

namespace Danubers.QldElectricity.Tools
{
    public class DataMigrator
    {
        private readonly IDataProvider _provider;

        public DataMigrator(IDataProvider provider)
        {
            _provider = provider;
        }

        public Task Migrate(CancellationTokenSource cts)
        {
            //todo DI?
            var migrationSteps =
                typeof(DataMigrator).GetTypeInfo().Assembly.GetTypes().Where(t => t.IsAssignableTo<DataMigratorStep>()).Select(Activator.CreateInstance).Cast<DataMigratorStep>().OrderBy(t => t.Version);

            using (var conn = _provider.GetConnection())
            {
                using (var transaction = conn.BeginTransaction())
                {
                    var schema = conn.ExecuteScalarAsync<int>("PRAGMA user_version");
                }
            }

            throw new NotImplementedException();
        }
    }

    public abstract class DataMigratorStep
    {
        public MigrationStepVersion Version { get; }
    }

    public class MigrationStepVersion
    {
        private int _major;
        private int _minor;
        private int _revision;

        public int Major
        {
            get { return _major; }
            internal set { if (value > 999)
                    throw new ArgumentOutOfRangeException("Value more than 3 digits");
                    _major = value; }
        }

        public int Minor
        {
            get { return _minor; }
            internal set { if (value > 999)
                    throw new ArgumentOutOfRangeException("Vlaue more than 3 digits");_minor = value; }
        }

        public int Revision
        {
            get { return _revision; }
            internal set { _revision = value; }
        }

        internal int ToInteger()
        {
            return Major * ((int) Math.Pow(10, 6)) + Minor * ((int) Math.Pow(10, 3)) + Revision;
        }
    }
}
