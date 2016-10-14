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
	public class StationSensorStateAdder: IStationSensorStateAdder
	{
        private readonly CloudStorageAccount account;
		public StationSensorStateAdder(CloudStorageAccount account)
		{
			if (account == null) throw new ArgumentNullException(nameof(account));
			this.account = account;
		}

        private static DynamicTableEntity GetTableEntity(string partition, string rowKey, DateTimeOffset at, double state)
        {
            // NOTE: El datetimeoffset en Azure table pierde el Offset y por lo tanto se encuentra desfasado respecto al momento real del relevamiento
            var eventTime = at.DateTime;
            var tableEntity = new DynamicTableEntity(partition, rowKey)
            {
                Properties = new Dictionary<string, EntityProperty>
                {
                    [nameof(SensorState.At)] = new EntityProperty(at),
                    [nameof(SensorState.State)] = new EntityProperty(state),
                    [nameof(SensorState.Active)] = new EntityProperty(true),
                }
            };
            return tableEntity;
        }

        public async Task Add(string stationId, string sensorId, string partition, string rowkey)
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
            if (!table.Exists())
                throw new ArgumentOutOfRangeException(nameof(tableName), "Table no especificado");
            TableQuery<DynamicTableEntity> query = new TableQuery<DynamicTableEntity>()
                .Where(TableQuery.CombineFilters(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partition),TableOperators.And, TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, rowkey)));
            var entities = table.ExecuteQuery(query);
            var entity = entities.ElementAt(0);
            SensorState sensorEntity = new SensorState
                {
                    At = entity[nameof(SensorState.At)].DateTimeOffsetValue.GetValueOrDefault(),
                    State = entity[nameof(SensorState.State)].DoubleValue.GetValueOrDefault(),
                    Active = entity[nameof(SensorState.Active)].BooleanValue.GetValueOrDefault(),
                };
            var tableEntity = GetTableEntity(partition, rowkey, sensorEntity.At, sensorEntity.State);
			var operation = TableOperation.InsertOrReplace(tableEntity);
            await table.ExecuteAsync(operation);
		}
        

        public static string GetValidTableName(string stationId, string sensorId)
		{
			// TODO: Controlar/reducir nombre compuesto : TableName	3-63 alphanumeric	case-insensitive	
			return $"{stationId}{sensorId}".ToLowerInvariant();
		}
	}
}