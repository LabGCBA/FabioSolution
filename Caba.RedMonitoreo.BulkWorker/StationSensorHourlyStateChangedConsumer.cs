using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Caba.RedMonitoreo.Common;
using Caba.RedMonitoreo.Common.Queues;
using Caba.RedMonitoreo.Data;
using Caba.RedMonitoreo.Queues.Messages;
using Microsoft.Azure.WebJobs;

namespace Caba.RedMonitoreo.BulkWorker
{
	public class StationSensorHourlyStateChangedConsumer: IQueueMessageConsumer<StationSensorHourlyStateChanged>
	{
		private const int eachMinutes = 60;
		private readonly IStationSensorStatePersister statePersister;
		private readonly IStationSensorHourlyStatePersister hourlyStatePersister;
		private readonly IEnqueuer<StationSensorDayStateChanged> dailyEnqueuer;
		private readonly ILogger logger;

		public StationSensorHourlyStateChangedConsumer(
			IStationSensorStatePersister statePersister
			, IStationSensorHourlyStatePersister hourlyStatePersister
			, IEnqueuer<StationSensorDayStateChanged> dailyEnqueuer
			, ILogger logger)
		{
			if (statePersister == null) throw new ArgumentNullException(nameof(statePersister));
			if (hourlyStatePersister == null) throw new ArgumentNullException(nameof(hourlyStatePersister));
			if (dailyEnqueuer == null) throw new ArgumentNullException(nameof(dailyEnqueuer));
			this.statePersister = statePersister;
			this.hourlyStatePersister = hourlyStatePersister;
			this.dailyEnqueuer = dailyEnqueuer;
			this.logger = logger ?? NoOpLogger.DoNothing;
		}

		public async Task ProcessMessage([QueueTrigger("stationsensorhourlystatechanged")]StationSensorHourlyStateChanged message)
		{
			if (message == null)
			{
				return;
			}
			var stationId = message.StationId;
			var sensorId = message.SensorId;
			var day = message.Day;
			await PersistAverages(stationId, sensorId, day);
			await dailyEnqueuer.Enqueue(new StationSensorDayStateChanged {StationId = stationId, SensorId = sensorId, Day = day});
		}

		private async Task PersistAverages(string stationId, string sensorId, DateTime day)
		{
			var dayStates = statePersister.DayStates(stationId, sensorId, day);
            var yesterdayStates = statePersister.DayStates(stationId, sensorId, day.AddDays(-1));
            var dayStatesActive = from s in dayStates where s.Active select s;
            var yesterdayStatesActive = from s in yesterdayStates where s.Active select s;
            var minutesAverages = (from s in dayStatesActive
                                   group s by new { Hour = s.At.Hour, Lapse = s.At.Minute / eachMinutes }
                into g
                                   select
                                       new SensorState
                                       {
                                           At =
                                               new DateTimeOffset(day.Year, day.Month, day.Day, g.Key.Hour, g.Key.Lapse * eachMinutes, 0,
                                                   g.Select(x => x.At.Offset).FirstOrDefault()),
                                           State = sensorId.ToLowerInvariant() == "rain" ? g.Sum(x => x.State) : g.Average(x => x.State),
                                           Active = g.Count<SensorState>() >= 0.75 * eachMinutes ? true : false,
                    }).ToArray();
          
            await hourlyStatePersister.Persist(stationId, sensorId, minutesAverages);
			logger.LogTrace(() => $"Actualizados promedios horarios para '{sensorId}' de '{stationId}' del {day}");
		}

    }
}