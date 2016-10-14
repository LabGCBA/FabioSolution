using System;
using System.IO;
using System.Threading.Tasks;
using Caba.RedMonitoreo.IO;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Caba.RedMonitoreo.Azure.IO
{
	public class BlobStationFileStore: IStationFileStore
	{
		public const string StationContainerPrefix = "station-";
		private readonly CloudStorageAccount account;

		public BlobStationFileStore(string connectionString)
			: this(CloudStorageAccount.Parse(connectionString)) {}

		public BlobStationFileStore(CloudStorageAccount account)
		{
			if (account == null) throw new ArgumentNullException(nameof(account));
			this.account = account;
		}

		public async Task<string> Store(string stationId, Stream fileContent)
		{
			if (string.IsNullOrWhiteSpace(stationId)) throw new ArgumentNullException(nameof(stationId));
			if (fileContent == null) throw new ArgumentNullException(nameof(fileContent));
			CloudBlobClient blobStorageType = account.CreateCloudBlobClient();
			CloudBlobContainer container = blobStorageType.GetContainerReference(GetContainerName(stationId));
			CreateContainer(container);
			var today = DateTime.UtcNow;
			string blobAddressUri = string.Join("/", today.Year.ToString("D4"), today.Month.ToString("D2"),
				today.Day.ToString("D2"), Guid.NewGuid().ToString("n") + ".txt");

			ICloudBlob blobReference = container.GetBlockBlobReference(blobAddressUri);
			var blobProperties = blobReference.Properties;
			blobProperties.ContentType = "text/plain";
			await blobReference.UploadFromStreamAsync(fileContent);

			return blobReference.Uri.ToString();
		}

		public async Task<bool> TryGet(string filePath, Stream fileContent)
		{
			Uri fileUri;
			if (!Uri.TryCreate(filePath, UriKind.RelativeOrAbsolute, out fileUri))
			{
				return false;
			}
			CloudBlobClient blobClient = account.CreateCloudBlobClient();
			var blobReference = await blobClient.GetBlobReferenceFromServerAsync(fileUri);
			if (!blobReference.Exists())
			{
				return false;
			}
			await blobReference.DownloadToStreamAsync(fileContent);
			return true;
		}

		private string GetContainerName(string stationId) => StationContainerPrefix + stationId.ToLowerInvariant();

		private void CreateContainer(CloudBlobContainer container)
		{
			container.CreateIfNotExists(BlobContainerPublicAccessType.Off, null, null);
		}
	}
}