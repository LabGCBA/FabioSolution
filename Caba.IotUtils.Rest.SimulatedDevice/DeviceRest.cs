using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

namespace Caba.IotUtils.Rest.SimulatedDevice
{
	public class DeviceRest
	{
		private static readonly DateTime epochTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
		public static void CreateIfNotExists(HttpClient httpClient, string deviceId)
		{
			if (Exists(httpClient, deviceId))
			{
				return;
			}
			var jsonMessage =
				$"{{\"deviceId\": \"{deviceId}\", \"status\": \"enabled\", \"statusReason\": \"Listo para enviar info\"}}";

			var request = new HttpRequestMessage(HttpMethod.Put, $"devices/{deviceId}?api-version=2016-02-03")
			{
				Content = new StringContent(jsonMessage, Encoding.ASCII, "application/json")
			};
			var response = httpClient.SendAsync(request).Result;
			if (response.IsSuccessStatusCode)
			{
				Console.WriteLine("CREADO DEVICE {0}", jsonMessage);
			}
			else
			{
				Console.WriteLine("ERROR CREANDO DEVICE ({0}) {1}", response.StatusCode, jsonMessage);
			}
		}

		public static bool Exists(HttpClient httpClient, string deviceId)
		{
			var request = new HttpRequestMessage(HttpMethod.Get, $"devices/{deviceId}?api-version=2016-02-03");
			request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
			var response = httpClient.SendAsync(request).Result;
			return response.IsSuccessStatusCode;
		}

		public static string SharedAccessSignature(string hostUrl, string policyName, string policyAccessKey, TimeSpan timeToLive)
		{
			if (string.IsNullOrWhiteSpace(hostUrl))
			{
				throw new ArgumentNullException(nameof(hostUrl));
			}

			var expires = Convert.ToInt64(DateTime.UtcNow.Add(timeToLive).Subtract(epochTime).TotalSeconds).ToString(CultureInfo.InvariantCulture);
			var resourceUri = WebUtility.UrlEncode(hostUrl.ToLowerInvariant());
			var toSign = string.Concat(resourceUri, "\n", expires);
			var signed = Sign(toSign, policyAccessKey);

			var sb = new StringBuilder();
			sb.Append("sr=").Append(resourceUri)
				.Append("&sig=").Append(WebUtility.UrlEncode(signed))
				.Append("&se=").Append(expires);
			if (!string.IsNullOrEmpty(policyName))
			{
				sb.Append("&skn=").Append(WebUtility.UrlEncode(policyName));
			}
			return sb.ToString();
		}

		private static string Sign(string requestString, string key)
		{
			using (var hmacshA256 = new HMACSHA256(Convert.FromBase64String(key)))
			{
				var hash = hmacshA256.ComputeHash(Encoding.UTF8.GetBytes(requestString));
				return Convert.ToBase64String(hash);
			}
		}
	}
}