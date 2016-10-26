using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Caba.RedMonitoreo.Common;
using Caba.RedMonitoreo.Data;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace Caba.RedMonitoreo.Azure.Data
{
	public class StationSensorHourlyStatePersister: IStationSensorHourlyStatePersister
	{
		private readonly CloudStorageAccount account;
		public StationSensorHourlyStatePersister(CloudStorageAccount account)
		{
			if (account == null) throw new ArgumentNullException(nameof(account));
			this.account = account;
		}

		public IEnumerable<SensorStateHourly> DayStates(string stationId, string sensorId, DateTime day)
		{
			var tableName = GetValidTableName(stationId, sensorId);
			var tableClient = account.CreateCloudTableClient();
			var table = tableClient.GetTableReference(tableName);
			if (!table.Exists())
			{
				yield break;
			}
			var partition = $"{day:yyyyMMdd}";
			TableQuery<DynamicTableEntity> query = new TableQuery<DynamicTableEntity>()
				.Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partition));
			var entities = table.ExecuteQuery(query);
			foreach (var tableEntity in entities)
			{
				yield return new SensorStateHourly
				{
					At = tableEntity[nameof(SensorState.At)].DateTimeOffsetValue.GetValueOrDefault(),
					State = tableEntity[nameof(SensorState.State)].DoubleValue.GetValueOrDefault(),
                    Active = tableEntity[nameof(SensorState.Active)].BooleanValue.GetValueOrDefault(),
                    Max = tableEntity.Properties.Keys.Contains(nameof(SensorStateHourly.Max)) ? tableEntity[nameof(SensorStateHourly.Max)].DoubleValue.GetValueOrDefault() : 0,
                    Min = tableEntity.Properties.Keys.Contains(nameof(SensorStateHourly.Min)) ? tableEntity[nameof(SensorStateHourly.Min)].DoubleValue.GetValueOrDefault() : 0,
                    EightHour = tableEntity.Properties.Keys.Contains(nameof(SensorStateHourly.EightHour)) ? tableEntity[nameof(SensorStateHourly.EightHour)].DoubleValue.GetValueOrDefault() : 0,
                    FullDay = tableEntity.Properties.Keys.Contains(nameof(SensorStateHourly.FullDay)) ? tableEntity[nameof(SensorStateHourly.FullDay)].DoubleValue.GetValueOrDefault() : 0
                };
			}
		}

		public async Task Persist(string stationId, string sensorId, IEnumerable<SensorStateHourly> states)
		{
			if (string.IsNullOrWhiteSpace(stationId))
			{
				throw new ArgumentOutOfRangeException(nameof(stationId), "StationId no especificado");
			}
			if (string.IsNullOrWhiteSpace(sensorId))
			{
				throw new ArgumentOutOfRangeException(nameof(sensorId), "SensorId no especificado");
			}
			var tableName = GetValidTableName(stationId, sensorId);
			var tableClient = account.CreateCloudTableClient();
			var table = tableClient.GetTableReference(tableName);
			table.CreateIfNotExists();
			var partitions = from state in states
				group state by state.At.Date
				into d
				select new {Day = d.Key, DayStates = d.ToList()};
			foreach (var partition in partitions)
			{
				var day = partition.Day;
				var sensorStates = partition.DayStates;
				await PersistStatesOfDay(table, day, sensorStates);
			}
		}

		private async Task PersistStatesOfDay(CloudTable table, DateTime day, List<SensorStateHourly> sensorStates)
		{
			var partition = $"{day:yyyyMMdd}";
			foreach (var statesPage in sensorStates.PagedBy(100))
			{
				var operation = new TableBatchOperation();
				foreach (var state in statesPage)
				{
					var rowKey = $"{state.At:HHmm}";
					var tableEntity = new DynamicTableEntity(partition, rowKey)
					{
						Properties = new Dictionary<string, EntityProperty>
						{
							[nameof(SensorState.At)] = new EntityProperty(state.At),
							[nameof(SensorState.State)] = new EntityProperty(state.State),
                            [nameof(SensorState.Active)] = new EntityProperty(state.Active),
                            [nameof(SensorStateHourly.Max)] = new EntityProperty(state.Max),
                            [nameof(SensorStateHourly.Min)] = new EntityProperty(state.Min),
                            [nameof(SensorStateHourly.EightHour)] = new EntityProperty(state.EightHour),
                            [nameof(SensorStateHourly.FullDay)] = new EntityProperty(state.FullDay)
                        }
					};
					operation.InsertOrReplace(tableEntity);
				}
				await table.ExecuteBatchAsync(operation);
			}
		}

		public static string GetValidTableName(string stationId, string sensorId)
		{
			// TODO: Controlar/reducir nombre compuesto : TableName	3-63 alphanumeric	case-insensitive	
			return $"{stationId}{sensorId}".ToLowerInvariant() + "TrueHourly";
		}
	}
}