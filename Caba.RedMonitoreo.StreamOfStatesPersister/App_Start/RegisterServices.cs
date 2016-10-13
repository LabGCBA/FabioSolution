using System.Configuration;
using Caba.RedMonitoreo.Azure.Data;
using Caba.RedMonitoreo.Azure.Queues;
using Caba.RedMonitoreo.Common;
using Caba.RedMonitoreo.Data;
using Caba.RedMonitoreo.Queues.Messages;
using Microsoft.WindowsAzure.Storage;

namespace Caba.RedMonitoreo.StreamOfStatesPersister
{
	public static class RegisterServices
	{
		public static void RegisterAllServices(this IServiceStore store)
		{
			store.RegisterSingleton<ILogger>(new DiagnosticLogger());
			store.RegisterSingleton(CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings["RedMonitoreo:AzureStorage"].ConnectionString));
			store.RegisterSingleton<IStationSensorStatePersister>(c=> new StationSensorStatePersister(c.GetInstance<CloudStorageAccount>()));
			store.RegisterSingleton<IEnqueuer<StationSensorHourlyStateChanged>>(c=> new MessageEnqueuer<StationSensorHourlyStateChanged>(c.GetInstance<CloudStorageAccount>()));
		}

		public static void RegisterJobs(this IServiceStore store)
		{
			store.RegisterTransient(c=> new StreamSensorAverageConsumer(
				c.GetInstance<IStationSensorStatePersister>()
				, c.GetInstance<IEnqueuer<StationSensorHourlyStateChanged>>()));
		}
	}
}