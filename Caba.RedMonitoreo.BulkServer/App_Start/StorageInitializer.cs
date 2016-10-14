using System.Configuration;
using Caba.RedMonitoreo.Azure.Queues;
using Caba.RedMonitoreo.Queues.Messages;
using Microsoft.WindowsAzure.Storage;

namespace Caba.RedMonitoreo.BulkServer
{
	public static class StorageInitializer
	{
		public static void InitializeAll()
		{
			var storageAccount =
				CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings["RedMonitoreo:AzureStorage"].ConnectionString);
			var serviceBusConnection =
				ConfigurationManager.ConnectionStrings["RedMonitoreo:ServiceBus:FilesToProcess"].ConnectionString;
			new ServiceBusInitializer<StationFileToProcess>(serviceBusConnection).Initialize();
		}
	}
}