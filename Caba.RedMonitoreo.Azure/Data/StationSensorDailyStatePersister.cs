using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Caba.RedMonitoreo.Data;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace Caba.RedMonitoreo.Azure.Data
{
	public class StationSensorDailyStatePersister: IStationSensorDailyStatePersister
	{
		private readonly CloudStorageAccount account;
		public StationSensorDailyStatePersister(CloudStorageAccount account)
		{
			if (account == null) throw new ArgumentNullException(nameof(account));
			this.account = account;
		}

		public IEnumerable<SensorDayAverageState> YearStates(string stationId, string sensorId, int year)
		{
			var tableName = GetValidTableName(stationId, sensorId);
			var tableClient = account.CreateCloudTableClient();
			var table = tableClient.GetTableReference(tableName);
			if (!table.Exists())
			{
				yield break;
			}
			var partition = year.ToString("D4");
			TableQuery<DynamicTableEntity> query = new TableQuery<DynamicTableEntity>()
				.Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partition));
			var entities = table.ExecuteQuery(query);
			foreach (var tableEntity in entities)
			{
				yield return new SensorDayAverageState
				{
					Day = tableEntity[nameof(SensorDayAverageState.Day)].DateTime.GetValueOrDefault(),
					Average = tableEntity[nameof(SensorDayAverageState.Average)].DoubleValue.GetValueOrDefault()
				};
			}
		}

		public Task Persist(string stationId, string sensorId, SensorDayAverageState state)
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
			var day = state.Day;
			var partition = $"{day.Year:D4}";
			var rowKey = $"{day.DayOfYear:D3}";
			var tableEntity = new DynamicTableEntity(partition, rowKey)
			{
				Properties = new Dictionary<string, EntityProperty>
				{
					[nameof(SensorDayAverageState.Day)] = new EntityProperty(state.Day),
					[nameof(SensorDayAverageState.Average)] = new EntityProperty(state.Average),
				}
			};
			var operation = TableOperation.InsertOrReplace(tableEntity);
			return table.ExecuteAsync(operation);
		}

		public static string GetValidTableName(string stationId, string sensorId)
		{
			// TODO: Controlar/reducir nombre compuesto : TableName	3-63 alphanumeric	case-insensitive	
			return $"{stationId}{sensorId}".ToLowerInvariant() + "Daily";
		}
	}
}