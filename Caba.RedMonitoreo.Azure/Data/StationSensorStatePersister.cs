using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Caba.RedMonitoreo.Common;
using Caba.RedMonitoreo.Data;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace Caba.RedMonitoreo.Azure.Data
{
	public class StationSensorStatePersister: IStationSensorStatePersister
	{
		private readonly CloudStorageAccount account;
		public StationSensorStatePersister(CloudStorageAccount account)
		{
			if (account == null) throw new ArgumentNullException(nameof(account));
			this.account = account;
		}

		public IEnumerable<SensorState> DayStates(string stationId, string sensorId, DateTime day)
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
                yield return new SensorState
                {
                    At = tableEntity[nameof(SensorState.At)].DateTimeOffsetValue.GetValueOrDefault(),
                    State = tableEntity[nameof(SensorState.State)].DoubleValue.GetValueOrDefault(),
                    Active = tableEntity[nameof(SensorState.Active)].BooleanValue.GetValueOrDefault(),
                };
			}
		}

		public async Task Persist(string stationId, string sensorId, IEnumerable<SensorState> states)
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
				select new { Day= d.Key, DayStates= d.ToList() };
			foreach (var partition in partitions)
			{
				var day = partition.Day;
				var sensorStates = partition.DayStates;
				await PersistStatesOfDay(table, day, sensorStates);
			}
		}

		public async Task Persist(string stationId, string sensorId, DateTimeOffset at, double state)
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
			var partition = $"{at:yyyyMMdd}";
			var rowKey = $"{at:HHmm}";
			var tableEntity = GetTableEntity(partition, rowKey, at, state);
			var operation = TableOperation.InsertOrReplace(tableEntity);
			await table.ExecuteAsync(operation);
		}

		private static DynamicTableEntity GetTableEntity(string partition, string rowKey, DateTimeOffset at, double state)
		{
			// NOTE: El datetimeoffset en Azure table pierde el Offset y por lo tanto se encuentra desfasado respecto al momento real del relevamiento
			var eventTime = at.DateTime;
			var tableEntity = new DynamicTableEntity(partition, rowKey)
			{
				Properties = new Dictionary<string, EntityProperty>
				{
					[nameof(SensorState.At)] = new EntityProperty(eventTime),
					[nameof(SensorState.State)] = new EntityProperty(state),
                    [nameof(SensorState.Active)] = new EntityProperty(true),
				}
			};
			return tableEntity;
		}

		private async Task PersistStatesOfDay(CloudTable table, DateTime day, IEnumerable<SensorState> sensorStates)
		{
			var partition = $"{day:yyyyMMdd}";
			foreach (var statesPage in sensorStates.PagedBy(100))
			{
				var operation = new TableBatchOperation();
				foreach (var state in statesPage)
				{
					var rowKey = $"{state.At:HHmm}";
					var tableEntity = GetTableEntity(partition, rowKey, state.At, state.State);
					operation.InsertOrReplace(tableEntity);
				}
				await table.ExecuteBatchAsync(operation);
			}
		}

		public static string GetValidTableName(string stationId, string sensorId)
		{
			// TODO: Controlar/reducir nombre compuesto : TableName	3-63 alphanumeric	case-insensitive	
			return $"{stationId}{sensorId}".ToLowerInvariant();
		}
	}
}