using System.Configuration;
using System.Web.Http;
using Caba.RedMonitoreo.Azure.Data;
using Caba.RedMonitoreo.Azure.IO;
using Caba.RedMonitoreo.Azure.Queues;
using Caba.RedMonitoreo.IO;
using Caba.RedMonitoreo.BulkServer.Controllers;
using Caba.RedMonitoreo.Common;
using Caba.RedMonitoreo.Data;
using Caba.RedMonitoreo.Queues.Messages;
using Microsoft.WindowsAzure.Storage;

namespace Caba.RedMonitoreo.BulkServer
{
	public static class RegisterServices
	{
		public static void RegisterAllServices(this IServiceStore store)
		{
			store.RegisterSingleton<ILogger>(container => new DiagnosticLogger());
			store.RegisterSingleton(CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings["RedMonitoreo:AzureStorage"].ConnectionString));
			store.RegisterSingleton<IStationFileStore>(c=> new BlobStationFileStore(c.GetInstance<CloudStorageAccount>()));
			store.RegisterSingleton<IEnqueuer<StationFileToProcess>>(c => 
			new ServiceBusEnqueuer<StationFileToProcess>(
				ConfigurationManager.ConnectionStrings["RedMonitoreo:ServiceBus:FilesToProcess"].ConnectionString
				, c.GetInstance<ILogger>()));
            store.RegisterSingleton<IEnqueuer<StationSensorHourlyStateChanged>>(c =>
            new MessageEnqueuer<StationSensorHourlyStateChanged>(c.GetInstance<CloudStorageAccount>()));
            store.RegisterSingleton<IStationsSensorsYearActivityPersister>(c => new StationsSensorsYearActivityPersister(c.GetInstance<CloudStorageAccount>()));
			store.RegisterSingleton<IStationSensorDailyStatePersister>(c => new StationSensorDailyStatePersister(c.GetInstance<CloudStorageAccount>()));
			store.RegisterSingleton<IStationSensorHourlyStatePersister>(c => new StationSensorHourlyStatePersister(c.GetInstance<CloudStorageAccount>()));
            store.RegisterSingleton<IStationSensorStatePersister>(c => new StationSensorStatePersister(c.GetInstance<CloudStorageAccount>()));
            store.RegisterSingleton<IStationSensorStateDeleter>(c => new StationSensorStateDeleter(c.GetInstance<CloudStorageAccount>()));
            store.RegisterSingleton<IStationSensorStateAdder>(c => new StationSensorStateAdder(c.GetInstance<CloudStorageAccount>()));
            RegisterAllControllers(store);
		}

		private static void RegisterAllControllers(IServiceStore store)
		{
			store.RegisterTransient(
				c => new OtfsController(c.GetInstance<IStationFileStore>(), c.GetInstance<IEnqueuer<StationFileToProcess>>()));
			store.RegisterTransient(c => new StationsController(c.GetInstance<IStationsSensorsYearActivityPersister>()));
			store.RegisterTransient(c => new SensorsController(
				c.GetInstance<IStationsSensorsYearActivityPersister>()
			, c.GetInstance<IStationSensorDailyStatePersister>()
			, c.GetInstance<IStationSensorHourlyStatePersister>()
            , c.GetInstance<IStationSensorStatePersister>()
            , c.GetInstance<IStationSensorStateDeleter>()
            , c.GetInstance<IEnqueuer<StationSensorHourlyStateChanged>>(),
                c.GetInstance<IStationSensorStateAdder>()
            ));
		}

		public static void UseSimpleDependencyResolver(this HttpConfiguration config, IServiceContainer container)
		{
			config.DependencyResolver = new DependencyResolver(container);
		}
	}
}