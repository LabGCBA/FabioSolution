using System;
using System.IO;
using System.Linq;

namespace Caba.RedMonitoreo.BulkImporter
{
	class Program
	{
		static void Main(string[] args)
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

			var rootFolder = args[0];
			if (!Directory.Exists(rootFolder))
			{
				Console.WriteLine("La carpeta '{0}' no existe.", rootFolder);
				return;
			}
			var stationsFolders = Directory.GetDirectories(rootFolder).Where(d=> !Path.GetFileName(d).StartsWith("_")).ToArray();
			if (stationsFolders.Length == 0)
			{
				Console.WriteLine("La carpeta '{0}' no contiene las carpetas de las estaciones.", rootFolder);
			}

			var importer = new OtsfFilesBulkImporter(stationsFolders
				, "http://bapocbulkserver.azurewebsites.net/api1/otfs/"
				, x=> Console.WriteLine(x)
				, x => Console.WriteLine(x));
			importer.Start();
		}

		private static void PrintHelp()
		{
			Console.WriteLine("otsfimport <Files root path>");
			Console.WriteLine("Parametros:");
			Console.WriteLine("<Files root path> Root path donde se encuentran los files *.otsf");
			Console.WriteLine("Cada sub carpeta de <Files root path> representa una estación.");
			Console.WriteLine("El nombre de la carpeta se usará como nombre de la estación.");
			Console.WriteLine("Todos los files *.otsf contenidos en cada subcarpeta serán enviados al server http://bapocbulkserver.azurewebsites.net/api1/otfs");
			Console.WriteLine();
			Console.WriteLine("Uso:");
			Console.WriteLine(@"otsfimport C:\MyFiles");
		}
	}
}
