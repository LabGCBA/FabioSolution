using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Caba.RedMonitoreo.Common;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;

namespace Caba.RedMonitoreo.Azure.Queues
{
	public class ServiceBusEnqueuer<TMessage>: IEnqueuer<TMessage> where TMessage: class 
	{
		private readonly string serviceBusConnectionString;
		private readonly ILogger logger;
		private readonly string queueName = typeof(TMessage).Name.ToLowerInvariant();

		public ServiceBusEnqueuer(string serviceBusConnectionString, ILogger logger)
		{
			if (string.IsNullOrWhiteSpace(serviceBusConnectionString)) throw new ArgumentNullException(nameof(serviceBusConnectionString));
			this.serviceBusConnectionString = serviceBusConnectionString;
			this.logger = logger ?? NoOpLogger.DoNothing;
		}

		public async Task Enqueue(TMessage message)
		{
			var client = QueueClient.CreateFromConnectionString(serviceBusConnectionString, queueName);
			var serializedObject = SerializeObjectAsString(message);
			using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(serializedObject), false))
			{
				var bmessage = new BrokeredMessage(stream)
				{
					ContentType = "application/json"
				};
				await client.SendAsync(bmessage);
			}
		}

		protected virtual string SerializeObjectAsString(TMessage message)
		{
			return JsonConvert.SerializeObject(message);
		}
	}
}