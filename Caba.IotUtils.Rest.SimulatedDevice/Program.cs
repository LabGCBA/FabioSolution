using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace Caba.IotUtils.Rest.SimulatedDevice
{
	internal class Program
	{
		private static void Main(string[] args)
		{
			if (args == null || args.Length < 1 ||
					(args.Length == 1 && ("help".Equals(args[0], StringComparison.OrdinalIgnoreCase) ||
																"?".Equals(args[0]) ||
																"ayuda".Equals(args[0], StringComparison.OrdinalIgnoreCase))))
			{
				PrintHelp();
#if DEBUG
				Console.ReadLine();
#endif
				return;
			}
			var deviceId = args[0];
			var iotHubHostConfigurationKey = "caba:IoTHubService:host:";
			var iotHubPolicyNameConfigurationKey = "caba:IoTHubService:policyName:";
			var iotHubPolicyKeyConfigurationKey = "caba:IoTHubService:policyKey:";
			var iotHubSASConfigurationKey = "caba:IoTHubService:SAS:";
			ISensorSimulator[] sensors;

			iotHubHostConfigurationKey += "withouttime";
			iotHubPolicyNameConfigurationKey += "withouttime";
			iotHubPolicyKeyConfigurationKey += "withouttime";
			iotHubSASConfigurationKey += "withouttime";
			sensors = SimulatorsWithoutTime(deviceId).ToArray();
			
			using (var httpClient = new HttpClient())
			{
				var hostUrl = ConfigurationManager.AppSettings[iotHubHostConfigurationKey];
				httpClient.BaseAddress = new UriBuilder {Scheme = "https", Host = hostUrl }.Uri;
				var hubSharedAccessSignature = DeviceRest.SharedAccessSignature(hostUrl
					, ConfigurationManager.AppSettings[iotHubPolicyNameConfigurationKey]
					, ConfigurationManager.AppSettings[iotHubPolicyKeyConfigurationKey]
					, TimeSpan.FromDays(1));
				httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("SharedAccessSignature", hubSharedAccessSignature);

				DeviceRest.CreateIfNotExists(httpClient, deviceId);

				LoopMonitorSensors(deviceId, sensors, httpClient);
			}
		}

		private static void LoopMonitorSensors(string deviceId, ISensorSimulator[] sensors, HttpClient httpClient)
		{
			while (true)
			{
				foreach (var simulator in sensors)
				{
					SendToHub(httpClient, deviceId, simulator.GetJsonMessage());
				}
			}
		}

		private static void SendToHub(HttpClient httpClient, string deviceId, string jsonMessage)
		{
			var request = new HttpRequestMessage(HttpMethod.Post, $"devices/{deviceId}/messages/events?api-version=2016-02-03")
			{
				Content = new StringContent(jsonMessage, Encoding.ASCII, "application/json")
			};
			var response = httpClient.SendAsync(request).Result;
			if (response.IsSuccessStatusCode)
			{
				Console.WriteLine(jsonMessage);
			}
			else
			{
				Console.WriteLine("ERROR ({0}) {1}", response.StatusCode,jsonMessage);
			}
		}

		private static IEnumerable<ISensorSimulator> SimulatorsWithoutTime(string deviceId)
		{
			yield return new SensorSimulator(deviceId, "Temperature", 7D, 25D, x => Math.Round(x, 1));
			yield return new SensorSimulator(deviceId, "WindSpeed", 0.55D, 8.33D, x => Math.Round(x, 4));
			yield return new SensorSimulator(deviceId, "ParticulateMatter", 40D, 100D, x => Math.Round(x, 2));
			yield return new SensorSimulator(deviceId, "RelativeHumidity", 0D, 100D, x => Math.Round(x, 0));
		}

		private static void PrintHelp()
		{
			Console.WriteLine("restdevice <Registered Device Id>");
			Console.WriteLine("Parametros:");
			Console.WriteLine("<Registered Device Id> Id del device ya registrado en el IoT Hub");
			Console.WriteLine();
			Console.WriteLine("IMPORTANTE:");
			Console.WriteLine("Debe editar el file de configuración de la aplicación para ingresar la SAS.");
			Console.WriteLine();
			Console.WriteLine("Uso:");
			Console.WriteLine("restdevice Station0002");
		}
	}
}