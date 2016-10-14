using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Caba.RedMonitoreo.Common;
using Caba.RedMonitoreo.Common.Queues;
using Caba.RedMonitoreo.Data;
using Caba.RedMonitoreo.IO;
using Caba.RedMonitoreo.Azure.Data;
using Caba.RedMonitoreo.Queues.Messages;
using Microsoft.Azure.WebJobs;

namespace Caba.RedMonitoreo.BulkWorker
{
	public class StationFileToProcessConsumer: IQueueMessageConsumer<StationFileToProcess>
	{
		private readonly IStationFileStore fileStore;
		private readonly IStationSensorStatePersister statePersister;
		private readonly IEnqueuer<StationSensorHourlyStateChanged> hourlyEnqueuer;
		private readonly ILogger logger;
		public StationFileToProcessConsumer(IStationFileStore fileStore, IStationSensorStatePersister statePersister, IEnqueuer<StationSensorHourlyStateChanged> hourlyEnqueuer, ILogger logger)
		{
			if (fileStore == null) throw new ArgumentNullException(nameof(fileStore));
			if (statePersister == null) throw new ArgumentNullException(nameof(statePersister));
			if (hourlyEnqueuer == null) throw new ArgumentNullException(nameof(hourlyEnqueuer));
			this.fileStore = fileStore;
			this.statePersister = statePersister;
			this.hourlyEnqueuer = hourlyEnqueuer;
			this.logger = logger ?? NoOpLogger.DoNothing;
		}

		public async Task ProcessMessage([ServiceBusTrigger("stationfiletoprocess")]StationFileToProcess message)
		{
			if (string.IsNullOrWhiteSpace(message?.FilePath))
			{
				return;
			}
			int registrosProcesados = 0;
			var stationId = message.StationId;
			using (var stream = new MemoryStream())
			{
				if (!await fileStore.TryGet(message.FilePath, stream))
				{
					logger.LogWarning(()=> $"File '{message.FilePath}' NO ENCONTRADO para '{stationId}' (subido {message.RecivedAt})");
					return;
				}
				stream.Seek(0, SeekOrigin.Begin);
				var parser = new OtsFileParser();
				foreach (var statesPage in parser.Parse(stream).PagedBy(200))
				{
					int registros;
					var perSensorState = GetPerSensorStates(statesPage, out registros);

					registrosProcesados += registros;
					PersistAll(stationId, perSensorState);
					NotifyChanges(stationId, perSensorState);
				}
			}
			if (registrosProcesados==0)
			{
				logger.LogWarning(() => $"File '{message.FilePath}' NO VALIDO para '{stationId}' (subido {message.RecivedAt})");
			}
			else
			{
				logger.LogTrace(()=> $"'{stationId}' procesado file '{message.FilePath}' con {registrosProcesados} registros (subido {message.RecivedAt})");
			}
		}

		private static Dictionary<string, List<SensorState>> GetPerSensorStates(IEnumerable<OtsFileParser.OtsState> statesPage, out int registrosProcesados)
		{
			registrosProcesados = 0;
			var perSensorState = new Dictionary<string, List<SensorState>>();
			foreach (var otsState in statesPage)
			{
				registrosProcesados++;
				foreach (var otsSensorState in otsState.States)
				{
					List<SensorState> states;
					var sensorId = otsSensorState.SensorId;
					if (!perSensorState.TryGetValue(sensorId, out states))
					{
						states = new List<SensorState>();
						perSensorState[sensorId] = states;
					}
					states.Add(new SensorState {At = otsState.TimeStamp, State = otsSensorState.State, Active = otsSensorState.Active});
				}
			}
			return perSensorState;
		}

		private void NotifyChanges(string stationId, Dictionary<string, List<SensorState>> perSensorState)
		{
			var daysChanged =
				perSensorState.Select(
					x => new {SensorId = x.Key, Days = x.Value.Select(d => d.At.Date).Distinct().ToArray()});
			var dayschanges = (from sensorDayChanged in daysChanged
				from day in sensorDayChanged.Days
				select new StationSensorHourlyStateChanged {StationId = stationId, SensorId = sensorDayChanged.SensorId, Day = day})
				.ToArray();
			var tasks = dayschanges.Select(x => hourlyEnqueuer.Enqueue(x)).ToArray();
			Task.WaitAll(tasks);
		}

		private void PersistAll(string stationId, Dictionary<string, List<SensorState>> perSensorState)
		{
			var tasks =
				perSensorState.Select(
					stationSensor => statePersister.Persist(stationId, stationSensor.Key, stationSensor.Value)).ToArray();
			Task.WaitAll(tasks);
		}
	}
}