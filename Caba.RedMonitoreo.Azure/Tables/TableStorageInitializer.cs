using System;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace Caba.RedMonitoreo.Azure.Tables
{
	public class TableStorageInitializer : IStorageInitializer
	{
		private readonly CloudStorageAccount account;
		private readonly string entityTableName;

		public TableStorageInitializer(CloudStorageAccount account, string entityTableName)
		{
			if (account == null)
			{
				throw new ArgumentNullException(nameof(account));
			}
			if (entityTableName == null) throw new ArgumentNullException(nameof(entityTableName));
			this.account = account;
			this.entityTableName = entityTableName;
		}

		public void Initialize()
		{
			var tableClient = account.CreateCloudTableClient();
			var table = tableClient.GetTableReference(entityTableName);
			table.CreateIfNotExists();
		}

		public void Drop()
		{
			var tableClient = account.CreateCloudTableClient();
			var table = tableClient.GetTableReference(entityTableName);
			table.DeleteIfExists();
		}
	}

	public class TableStorageInitializer<TTableEntity> : TableStorageInitializer where TTableEntity : ITableEntity
	{
		public TableStorageInitializer(CloudStorageAccount account)
			: base(account, typeof(TTableEntity).AsTableStorageName()) {}
	}
}