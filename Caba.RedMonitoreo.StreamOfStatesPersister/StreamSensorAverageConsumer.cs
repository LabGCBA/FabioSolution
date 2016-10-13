using System;
using System.Threading.Tasks;
using Caba.RedMonitoreo.Common;
using Caba.RedMonitoreo.Common.Queues;
using Caba.RedMonitoreo.Data;
using Caba.RedMonitoreo.Queues.Messages;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json;

namespace Caba.RedMonitoreo.StreamOfStatesPersister
{
	public class StreamSensorAverageConsumer: IQueueMessageConsumer<StreamSensorAverage>
	{
		private static readonly TimeZoneInfo argentineZoneInfo = TimeZoneInfo.FindSystemTimeZoneById("Argentina Standard Time");
		private readonly IStationSensorStatePersister statePersister;
		private readonly IEnqueuer<StationSensorHourlyStateChanged> hourlyEnqueuer;

		public StreamSensorAverageConsumer(IStationSensorStatePersister statePersister,
			IEnqueuer<StationSensorHourlyStateChanged> hourlyEnqueuer)
		{
			if (statePersister == null) throw new ArgumentNullException(nameof(statePersister));
			if (hourlyEnqueuer == null) throw new ArgumentNullException(nameof(hourlyEnqueuer));
			this.statePersister = statePersister;
			this.hourlyEnqueuer = hourlyEnqueuer;
		}

		public Task ProcessMessage([ServiceBusTrigger("sensoraverage")]string message)
		{
			// NOTE: el siguiente es un workaround a un issue de como stream-analytics genera el mensaje para el servicebus
			// https://social.msdn.microsoft.com/Forums/en-US/bd655fcc-40a0-45ff-806a-da4fa5b53ef7/streamanalytics-to-service-bus-json-file?forum=AzureStreamAnalytics
			dynamic deserializeMessage = JsonConvert.DeserializeObject(message);
			var realMessageObject = new StreamSensorAverage
			{
				StationId = deserializeMessage.stationid,
				SensorId = deserializeMessage.sensorid,
				Time = deserializeMessage.time,
				State = deserializeMessage.state
			};
			return ProcessMessage(realMessageObject);
		}

		public async Task ProcessMessage(StreamSensorAverage message)
		{
			if (string.IsNullOrWhiteSpace(message?.StationId) || string.IsNullOrWhiteSpace(message.SensorId))
			{
				return;
			}
			var stationId = message.StationId;
			var sensorId = message.SensorId;
			/* 
			 * NOTE: el stream-analytics usa el 'time' que eventualmente recibe y lo convierte a UTC para su procesamiento y, en output tambien restituye UTC.
			 * ya que los sensores están en Argentina cambiamos el Timestamp a la hora local.
			 */
			var time = TimeZoneInfo.ConvertTime(message.Time, argentineZoneInfo);
			await statePersister.Persist(stationId, sensorId, time, message.State);
			await hourlyEnqueuer.Enqueue(new StationSensorHourlyStateChanged { StationId = stationId, SensorId = sensorId, Day = time.DateTime });
		}
	}
}