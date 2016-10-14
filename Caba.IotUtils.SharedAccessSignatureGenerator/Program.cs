using System;
using System.Globalization;
using Microsoft.Azure.Devices.Common.Security;

namespace Caba.IotUtils.SharedAccessSignatureGenerator
{
	internal class Program
	{
		private static void Main(string[] args)
		{
			if (args == null || args.Length < 3 ||
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
			var ioTHubtarget = args[0];
			var policyName = args[1];
			var policyAccesskey = args[2];
			var days = 365D;
			if (args.Length >= 4)
			{
				double.TryParse(args[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out days);
			}

			var builder = new SharedAccessSignatureBuilder
			{
				KeyName = policyName,
				Key = policyAccesskey,
				Target = ioTHubtarget,
				TimeToLive = TimeSpan.FromDays(days)
			}.ToSignature();

			Console.WriteLine();
			Console.WriteLine(builder);
#if DEBUG
			Console.ReadLine();
#endif
		}

		private static void PrintHelp()
		{
			Console.WriteLine("sasgenerator <IoT Hub target> <Policy name> <Policy access key> [dias]");
			Console.WriteLine("Parametros:");
			Console.WriteLine("<IoT Hub target> 'Host name' del IoT hub");
			Console.WriteLine("<Policy name> nombre de la policy en 'Shared access policies'");
			Console.WriteLine("<Policy access key> 'Primary key' de la policy");
			Console.WriteLine("dias : cantidad de dias de validez de la signature (default=365)");
			Console.WriteLine();
			Console.WriteLine("Uso:");
			Console.WriteLine("sasgenerator cabamonitor.azure-devices.net iothubowner uUpGHQKU8o1buAtFFMxsCK+cKHGIw9UkUH7eaPVnMUA=");
			Console.WriteLine("sasgenerator cabamonitor.azure-devices.net iothubowner uUpGHQKU8o1buAtFFMxsCK+cKHGIw9UkUH7eaPVnMUA= 220");
		}
	}
}