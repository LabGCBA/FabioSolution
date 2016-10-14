using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Caba.RedMonitoreo.Data;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace Caba.RedMonitoreo.Azure.Data
{
	public class StationsSensorsYearActivityPersister: IStationsSensorsYearActivityPersister
	{
		public const string StationsTableName = "cabastations";
		private const string allStationsPartitionKey = "allstations";
		private readonly CloudStorageAccount account;
		public StationsSensorsYearActivityPersister(CloudStorageAccount account)
		{
			if (account == null) throw new ArgumentNullException(nameof(account));
			this.account = account;
		}

		public IEnumerable<Station> Stations()
		{
			var tableClient = account.CreateCloudTableClient();
			var table = tableClient.GetTableReference(StationsTableName);
			TableQuery<DynamicTableEntity> query = new TableQuery<DynamicTableEntity>()
				.Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, allStationsPartitionKey));
			var entities = table.ExecuteQuery(query);
			foreach (var tableEntity in entities)
			{
				yield return new Station
				{
					Id = tableEntity.RowKey,
				};
			}
		}

		public IEnumerable<Sensor> Sensors(string stationId)
		{
			var tableClient = account.CreateCloudTableClient();
			var table = tableClient.GetTableReference(StationsTableName);
			TableQuery<DynamicTableEntity> query = new TableQuery<DynamicTableEntity>()
				.Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, NormalizeKey(stationId)));
			var entities = table.ExecuteQuery(query);
			foreach (var tableEntity in entities)
			{
				yield return new Sensor
				{
					Id = tableEntity.RowKey,
				};
			}
		}

		public IEnumerable<int> AvailableHistoryOfSensosr(string stationId, string sensorId)
		{
			var tableClient = account.CreateCloudTableClient();
			var table = tableClient.GetTableReference(StationsTableName);
			TableQuery<DynamicTableEntity> query = new TableQuery<DynamicTableEntity>()
				.Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, GetPartitionKeyOfSensor(stationId, sensorId)));
			var entities = table.ExecuteQuery(query);
			foreach (var tableEntity in entities)
			{
				int year;
				yield return int.TryParse(tableEntity.RowKey, out year) ? year:0;
			}
		}

		public async Task Persist(string stationId, string sensorId, int year)
		{
			if (string.IsNullOrWhiteSpace(stationId))
			{
				throw new ArgumentOutOfRangeException(nameof(stationId), "StationId no especificado");
			}
			if (string.IsNullOrWhiteSpace(sensorId))
			{
				throw new ArgumentOutOfRangeException(nameof(sensorId), "SensorId no especificado");
			}
			var nstationId = NormalizeKey(stationId);
			var nsensorId = NormalizeKey(sensorId);
			var tableClient = account.CreateCloudTableClient();
			var table = tableClient.GetTableReference(StationsTableName);

			// registra la estación
			var stationEntity = new DynamicTableEntity(allStationsPartitionKey, nstationId)
            {
                Properties = new Dictionary<string, EntityProperty>
                {
                    [nameof(SensorState.Active)] = new EntityProperty(true),
                }
            };
			await table.ExecuteAsync(TableOperation.InsertOrReplace(stationEntity));

			// registra sensor de la estación
			var sensorEntity = new DynamicTableEntity(nstationId, nsensorId);
			await table.ExecuteAsync(TableOperation.InsertOrReplace(sensorEntity));

			// registra el año de actividad de un sensor en una estación
			var sensorYearEntity = new DynamicTableEntity(GetPartitionKeyOfSensor(stationId, sensorId), year.ToString("D4"));
			await table.ExecuteAsync(TableOperation.InsertOrReplace(sensorYearEntity));
		}

		private static string GetPartitionKeyOfSensor(string stationId, string sensorId)
		{
			return NormalizeKey(stationId + sensorId);
		}

		private static string NormalizeKey(string key)
		{
			return key?.ToLowerInvariant();
		}
	}
}