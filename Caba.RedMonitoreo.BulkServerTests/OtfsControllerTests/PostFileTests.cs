using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Caba.RedMonitoreo.BulkServer.Controllers;
using Caba.RedMonitoreo.Common;
using Caba.RedMonitoreo.IO;
using Caba.RedMonitoreo.Queues.Messages;
using NUnit.Framework;
using SharpTestsEx;

namespace Caba.RedMonitoreo.BulkServerTests.OtfsControllerTests
{
	public class PostFileTests
	{
		[Test]
		public async Task WhenPostNoPlainTextThenUnsupportedMediaType()
		{
			var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/api1/otfs");
			request.Content = new StreamContent(new MemoryStream());
			request.Content.Headers.ContentType= new MediaTypeHeaderValue("application/octet-stream");

			OtfsController controller = GetController();
			controller.Request = request;
			controller.Configuration = new HttpConfiguration();

			var response = await controller.Post("Centenario");
			response.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
		}

		[Test]
		public async Task WhenPostNoSizeThenBadResquest()
		{
			var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/api1/otfs");
			request.Content = new StreamContent(GetAsStrem(""));
			request.Content.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
			request.Content.Headers.ContentLength = 0;

			OtfsController controller = GetController();
			controller.Request = request;
			controller.Configuration = new HttpConfiguration();

			var response = await controller.Post("Centenario");
			response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
		}

		[Test]
		public async Task WhenFileConContentThenAcceptd()
		{
			var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/api1/otfs");
			var content = GetAsStrem("[BOF]" + Environment.NewLine);
			request.Content = new StreamContent(content);
			request.Content.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
			request.Content.Headers.ContentLength = content.Length;

			OtfsController controller = GetController();
			controller.Request = request;
			controller.Configuration = new HttpConfiguration();

			var response = await controller.Post("Centenario");
			response.StatusCode.Should().Be(HttpStatusCode.Accepted);
		}

		[Test]
		public async Task WhenPostThenStoreFile()
		{
			var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/api1/otfs");
			var content = GetAsStrem("[BOF]" + Environment.NewLine);
			request.Content = new StreamContent(content);
			request.Content.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
			request.Content.Headers.ContentLength = content.Length;

			var storeCalled = false;
			Action<string, Stream> assertAction = (stationName, stream) =>
			{
				stationName.Should().Be("Centenario");
				stream.Should().Not.Be.Null();
				storeCalled = true;
			}; 
			IStationFileStore fileStore = new StationFileStoreActionMock(assertAction);
			OtfsController controller = new OtfsController(fileStore, new EnqueuerMock<StationFileToProcess>());
			controller.Request = request;
			controller.Configuration = new HttpConfiguration();

			await controller.Post("Centenario");
			storeCalled.Should().Be.True();
		}

		[Test]
		public async Task WhenPostThenEnqueueFileToProcess()
		{
			var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/api1/otfs");
			var content = GetAsStrem("[BOF]" + Environment.NewLine);
			request.Content = new StreamContent(content);
			request.Content.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
			request.Content.Headers.ContentLength = content.Length;

			var enqueuer = new EnqueuerMock<StationFileToProcess>();
			OtfsController controller = new OtfsController(new NoOpStationFileStore("centenario/piiza.txt"), enqueuer);
			controller.Request = request;
			controller.Configuration = new HttpConfiguration();

			await controller.Post("Centenario");

			var actual = enqueuer.Messages.First();
			actual.FilePath.Should().Be("centenario/piiza.txt");
			actual.StationId.Should().Be("Centenario");
			actual.RecivedAt.Should().Not.Be(DateTimeOffset.MinValue);
		}

		private OtfsController GetController()
		{
			return new OtfsController(new NoOpStationFileStore(), new EnqueuerMock<StationFileToProcess>());
		}

		private Stream GetAsStrem(string content)
		{
			var stream = new MemoryStream();
			using (var writer = new StreamWriter(stream, Encoding.UTF8, 2024, true))
			{
				writer.Write(content);
				writer.Flush();
			}
			stream.Seek(0, SeekOrigin.Begin);
			return stream;
		}
	}

	public class StationFileStoreActionMock : IStationFileStore
	{
		private readonly Action<string, Stream> assertAction;

		public StationFileStoreActionMock(Action<string, Stream> assertAction)
		{
			this.assertAction = assertAction;
		}

		public Task<string> Store(string stationId, Stream fileContent)
		{
			assertAction(stationId, fileContent);
			return Task.FromResult("");
		}

		public Task<bool> TryGet(string filePath, Stream fileContent)
		{
			assertAction(filePath, fileContent);
			return Task.FromResult(true);
		}
	}
}