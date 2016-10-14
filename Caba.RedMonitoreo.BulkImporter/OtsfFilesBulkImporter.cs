using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Caba.RedMonitoreo.BulkImporter
{
	public class OtsfFilesBulkImporter
	{
		private readonly Action<string> doNothingCallback = x => { };
		private readonly string[] stationsFolders;
		private readonly Action<string> importingCallback;
		private readonly Action<string> importedCallback;
		private readonly HttpClient httpClient;

		public OtsfFilesBulkImporter(string[] stationsFolders, string hostAction, Action<string> importingCallback=null, Action<string> importedCallback= null)
		{
			if (stationsFolders == null) throw new ArgumentNullException(nameof(stationsFolders));
			if (hostAction == null) throw new ArgumentNullException(nameof(hostAction));
			this.stationsFolders = stationsFolders;
			this.importingCallback = importingCallback ?? doNothingCallback;
			this.importedCallback = importedCallback ?? doNothingCallback;
			httpClient = new HttpClient
			{
				BaseAddress = new Uri(hostAction),
			};
		}

		public void Start()
		{
			StartAsync().GetAwaiter().GetResult();
		}

		public Task StartAsync()
		{
			var importTasks = stationsFolders.Select(x => ImportFolder(x)).ToArray();
			return Task.WhenAll(importTasks);
		}

		private Task ImportFolder(string path)
		{
			if (!Directory.Exists(path))
			{
				return Task.FromResult((object) null);
			}
			var stationName = Path.GetFileName(path).ToLowerInvariant();
			var importedPath = Path.Combine(path, "_imported");
			var errorsPath = Path.Combine(path, "_errors");
			Directory.CreateDirectory(importedPath);
			Directory.CreateDirectory(errorsPath);
			var files = Directory.GetFiles(path, "*.otsf", SearchOption.TopDirectoryOnly);
			var importTasks = files.Select(x => ImportFile(stationName, x, importedPath, errorsPath)).ToArray();
			return Task.WhenAll(importTasks);
		}

		private async Task ImportFile(string stationName, string filepath, string importedPath, string errorsPath)
		{
			if (!File.Exists(filepath))
			{
				return;
			}
			var fileName = Path.GetFileName(filepath);
			importingCallback($"Importando '{filepath}' ...");
			try
			{
				using (var stream = File.Open(filepath, FileMode.Open, FileAccess.Read))
				{
					var request = new HttpRequestMessage(HttpMethod.Post, stationName)
					{
						Content = new StreamContent(stream)
					};
					request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("text/plain");
					var response = await httpClient.SendAsync(request);
					if (response.IsSuccessStatusCode)
					{
						importedCallback($"Ok '{filepath}'");
						File.Move(filepath, Path.Combine(importedPath, fileName));
					}
					else
					{
						importedCallback($"ERROR IMPORTANDO '{filepath}' (Status code = {response.StatusCode}");
						File.Move(filepath, Path.Combine(errorsPath, fileName));
					}
				}
			}
			catch (Exception e)
			{
				importedCallback($"ERROR IMPORTANDO '{filepath}' {e.Message}");
				File.Move(filepath, Path.Combine(errorsPath, fileName));
			}
		}
	}
}
