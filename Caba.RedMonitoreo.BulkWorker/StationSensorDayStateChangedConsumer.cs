using System;
using System.Linq;
using System.Threading.Tasks;
using Caba.RedMonitoreo.Common;
using Caba.RedMonitoreo.Common.Queues;
using Caba.RedMonitoreo.Data;
using Caba.RedMonitoreo.Queues.Messages;
using Microsoft.Azure.WebJobs;

namespace Caba.RedMonitoreo.BulkWorker
{
	public class StationSensorDayStateChangedConsumer: IQueueMessageConsumer<StationSensorDayStateChanged>
	{
		private readonly IStationSensorHourlyStatePersister hourlyStatePersister;
		private readonly IStationSensorDailyStatePersister dailyStatePersister;
		private readonly IStationsSensorsYearActivityPersister sensorActivityPersister;
		private readonly ILogger logger;

		public StationSensorDayStateChangedConsumer(
			IStationSensorHourlyStatePersister hourlyStatePersister
			, IStationSensorDailyStatePersister dailyStatePersister
			, IStationsSensorsYearActivityPersister sensorActivityPersister
			, ILogger logger)
		{
			if (hourlyStatePersister == null) throw new ArgumentNullException(nameof(hourlyStatePersister));
			if (dailyStatePersister == null) throw new ArgumentNullException(nameof(dailyStatePersister));
			if (sensorActivityPersister == null) throw new ArgumentNullException(nameof(sensorActivityPersister));
			this.hourlyStatePersister = hourlyStatePersister;
			this.dailyStatePersister = dailyStatePersister;
			this.sensorActivityPersister = sensorActivityPersister;
			this.logger = logger ?? NoOpLogger.DoNothing;
		}

		public async Task ProcessMessage([QueueTrigger("stationsensordaystatechanged")]StationSensorDayStateChanged message)
		{
			if (message == null)
			{
				return;
			}
			var stationId = message.StationId;
			var sensorId = message.SensorId;
			var day = message.Day;
			var dayStates = hourlyStatePersister.DayStates(stationId, sensorId, day);
            var dayStatesActive = from s in dayStates where s.Active select s;
            var dayAverage = new SensorDayAverageState
			{
				Day = day,
				Average = sensorId.ToLowerInvariant() == "rain" ? dayStatesActive.Sum(x => x.State) : dayStatesActive.Average(x=> x.State)
			};
			await dailyStatePersister.Persist(stationId, sensorId, dayAverage);
			logger.LogTrace(() => $"Actualizado promedio diario para '{sensorId}' de '{stationId}' del {day}");

			await sensorActivityPersister.Persist(stationId, sensorId, day.Year);
			logger.LogTrace(() => $"Registrado '{sensorId}' de '{stationId}' en {day.Year}");
		}
	}
}