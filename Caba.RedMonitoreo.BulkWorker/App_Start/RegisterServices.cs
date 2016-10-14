using System.Configuration;
using Caba.RedMonitoreo.Azure.Data;
using Caba.RedMonitoreo.Azure.IO;
using Caba.RedMonitoreo.Azure.Queues;
using Caba.RedMonitoreo.Common;
using Caba.RedMonitoreo.Data;
using Caba.RedMonitoreo.IO;
using Caba.RedMonitoreo.Queues.Messages;
using Microsoft.WindowsAzure.Storage;

namespace Caba.RedMonitoreo.BulkWorker
{
	public static class RegisterServices
	{
		public static void RegisterAllServices(this IServiceStore store)
		{
			store.RegisterSingleton<ILogger>(new DiagnosticLogger());
			store.RegisterSingleton(CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings["RedMonitoreo:AzureStorage"].ConnectionString));
			store.RegisterSingleton<IStationFileStore>(c=> new BlobStationFileStore(c.GetInstance<CloudStorageAccount>()));
			store.RegisterSingleton<IStationSensorStatePersister>(c=> new StationSensorStatePersister(c.GetInstance<CloudStorageAccount>()));
            store.RegisterSingleton<IStationSensorStateDeleter>(c => new StationSensorStateDeleter(c.GetInstance<CloudStorageAccount>()));
            store.RegisterSingleton<IStationSensorStateAdder>(c => new StationSensorStateAdder(c.GetInstance<CloudStorageAccount>()));
            store.RegisterSingleton<IStationSensorHourlyStatePersister>(c => new StationSensorHourlyStatePersister(c.GetInstance<CloudStorageAccount>()));
			store.RegisterSingleton<IStationSensorDailyStatePersister>(c => new StationSensorDailyStatePersister(c.GetInstance<CloudStorageAccount>()));
			store.RegisterSingleton<IStationsSensorsYearActivityPersister>(c => new StationsSensorsYearActivityPersister(c.GetInstance<CloudStorageAccount>()));
			store.RegisterSingleton<IEnqueuer<StationSensorHourlyStateChanged>>(c=> new MessageEnqueuer<StationSensorHourlyStateChanged>(c.GetInstance<CloudStorageAccount>()));
			store.RegisterSingleton<IEnqueuer<StationSensorDayStateChanged>>(c => new MessageEnqueuer<StationSensorDayStateChanged>(c.GetInstance<CloudStorageAccount>()));
		}

		public static void RegisterJobs(this IServiceStore store)
		{
			store.RegisterTransient(c=> new StationFileToProcessConsumer(
				c.GetInstance<IStationFileStore>()
				, c.GetInstance<IStationSensorStatePersister>()
				, c.GetInstance<IEnqueuer<StationSensorHourlyStateChanged>>()
				, c.GetInstance<ILogger>()
				));
			store.RegisterTransient(c => new StationSensorHourlyStateChangedConsumer(
				c.GetInstance<IStationSensorStatePersister>()
				, c.GetInstance<IStationSensorHourlyStatePersister>()
				, c.GetInstance<IEnqueuer<StationSensorDayStateChanged>>()
				, c.GetInstance<ILogger>()
				));
			store.RegisterTransient(c => new StationSensorDayStateChangedConsumer(
				c.GetInstance<IStationSensorHourlyStatePersister>()
				, c.GetInstance<IStationSensorDailyStatePersister>()
				, c.GetInstance<IStationsSensorsYearActivityPersister>()
				, c.GetInstance<ILogger>()
				));
		}
	}
}