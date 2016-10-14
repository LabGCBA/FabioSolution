using System.Configuration;
using Caba.RedMonitoreo.Azure.Data;
using Caba.RedMonitoreo.Azure.Queues;
using Caba.RedMonitoreo.Azure.Tables;
using Caba.RedMonitoreo.Common;
using Caba.RedMonitoreo.Queues.Messages;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.WindowsAzure.Storage;

namespace Caba.RedMonitoreo.BulkWorker
{
	internal class Program
	{
		private static readonly SimpleServiceContainer container = new SimpleServiceContainer();

		private static void Main()
		{
			container.RegisterAllServices();
			container.RegisterJobs();
			StorageInitialize();
			var config = new JobHostConfiguration
			{
				DashboardConnectionString= ConfigurationManager.ConnectionStrings["RedMonitoreo:AzureLogsStorage"].ConnectionString,
				StorageConnectionString = ConfigurationManager.ConnectionStrings["RedMonitoreo:AzureStorage"].ConnectionString,
				JobActivator = new ContainerBasedJobActivator(container)
			};
			ServiceBusConfiguration serviceBusConfig = new ServiceBusConfiguration
			{
				ConnectionString = ConfigurationManager.ConnectionStrings["RedMonitoreo:ServiceBus:FilesToProcess"].ConnectionString
			};
			config.UseServiceBus(serviceBusConfig);

			var host = new JobHost(config);
			host.RunAndBlock();
		}

		private static void StorageInitialize()
		{
			var account = container.GetInstance<CloudStorageAccount>();
			new QueueStorageInitializer<StationSensorHourlyStateChanged>(account).Initialize();
			new QueueStorageInitializer<StationSensorDayStateChanged>(account).Initialize();
			new TableStorageInitializer(account, StationsSensorsYearActivityPersister.StationsTableName).Initialize();
		}
	}
}