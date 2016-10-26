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
            var yesterdayStates = hourlyStatePersister.DayStates(stationId, sensorId, day.AddDays(-1));
            var dayStatesActive = from s in dayStates where s.Active select s;
            var yesterdayStatesActive = from s in yesterdayStates where s.Active select s;
            var minutesAverages = (from s in dayStatesActive
                                   group s by new { Hour = s.At.Hour, Lapse = s.At.Minute / eachMinutes }
                into g
                                   select
                                       new SensorStateHourly
                                       {
                                           At =
                                               new DateTimeOffset(day.Year, day.Month, day.Day, g.Key.Hour, g.Key.Lapse * eachMinutes, 0,
                                                   g.Select(x => x.At.Offset).FirstOrDefault()),
                                           State = sensorId.ToLowerInvariant() == "rain" ? g.Sum(x => x.State) : g.Average(x => x.State),
                                           Active = g.Count() >= 0.75 * eachMinutes ? true : false,
                                           Max = g.Max(x => x.State),
                                           Min = g.Min(x => x.State),
                    });
            var hourAverages = generateList(minutesAverages, yesterdayStatesActive);
            await hourlyStatePersister.Persist(stationId, sensorId, hourAverages.ToArray());
			logger.LogTrace(() => $"Actualizados promedios horarios para '{sensorId}' de '{stationId}' del {day}");
		}

        private IEnumerable<SensorStateHourly> generateList(IEnumerable<SensorStateHourly> todayList, IEnumerable<SensorStateHourly> yesterdayList)
        {
            foreach (SensorStateHourly ssHora in todayList)
            {
                DateTime variableDate = ssHora.At.DateTime.AddHours(-23);
                DateTime fixedDate = ssHora.At.DateTime;
                var prom24 = 0.0;
                var i = 0;
                if (variableDate.Day != fixedDate.Day)
                {
                    var yes = (from s in yesterdayList where (s.At.DateTime.Hour >= variableDate.Hour && s.Active) select s);
                    var averageYes = yes.Sum(x => x.State);
                    prom24 += averageYes;
                    i = yes.Count();
                }
                var tod =  (from s in todayList where (s.At.DateTime.Hour <= fixedDate.Hour && s.Active) select s);
                var averageTod = tod.Sum(x=> x.State);
                i += tod.Count();
                prom24 += averageTod;
                prom24 = i != 0 ? prom24/i : 0;
                i = 0;
                var prom8 = 0.0;
                variableDate = fixedDate.AddHours(-7);
                if (variableDate.Day != fixedDate.Day)
                {
                    var yes = (from s in yesterdayList where (s.At.DateTime.Hour >= variableDate.Hour && s.Active) select s);
                    var averageYes = yes.Sum(x => x.State);
                    prom8 += averageYes;
                    i = yes.Count();
                }
                tod = i != 0 ? (from s in todayList where (s.At.DateTime.Hour <= fixedDate.Hour && s.Active) select s) :
                    (from s in todayList where (s.At.DateTime.Hour <= fixedDate.Hour && s.At.DateTime.Hour > variableDate.Hour && s.Active) select s);
                averageTod = tod.Sum(x => x.State);
                prom8 += averageTod;
                i += tod.Count();
                prom8 = i != 0 ? prom8/i : 0;
                yield return new SensorStateHourly
                {
                    At = ssHora.At,
                    State = ssHora.State,
                    Active = ssHora.Active,
                    Max = ssHora.Max,
                    Min = ssHora.Min,
                    EightHour = prom8,
                    FullDay = prom24,
                };
            }
        }
    }
}