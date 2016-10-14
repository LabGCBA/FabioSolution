using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Caba.RedMonitoreo.IO
{
	public class  OtsFileParser
	{
		private const NumberStyles numberStyles = NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite;

		private static readonly Dictionary<string, OtsSensor> sensorMap = new Dictionary<string, OtsSensor>
		{
			["NO"] = new OtsSensor {SensorId = "NitricOxide", UnitInFile = "ppb"},
			["NO2"] = new OtsSensor {SensorId = "NitricDioxide", UnitInFile = "ppb"},
			["NOx"] = new OtsSensor {SensorId = "MonoNitrogenOxide", UnitInFile = "ppb"},
			["CO"] = new OtsSensor {SensorId = "CarbonOxide", UnitInFile = "ppb"},
			["PM10"] = new OtsSensor {SensorId = "ParticulateMatter", UnitInFile = "ug/m3"},
			["Wind Direction"] = new OtsSensor {SensorId = "WindDirection", UnitInFile = "Degrees"},
			["WindDirection"] = new OtsSensor {SensorId = "WindDirection", UnitInFile = "Degrees"},
			["Wind Speed"] = new OtsSensor {SensorId = "WindSpeed", UnitInFile = "m/s"},
			["WindSpeed"] = new OtsSensor {SensorId = "WindSpeed", UnitInFile = "m/s"},
			["Temperature"] = new OtsSensor {SensorId = "Temperature", UnitInFile = "Celsius"},
			["Relative Humidity"] = new OtsSensor {SensorId = "RelativeHumidity", UnitInFile = @"%"},
			["Rel Humidity"] = new OtsSensor {SensorId = "RelativeHumidity", UnitInFile = @"%"},
			["Atm Pressure"] = new OtsSensor {SensorId = "AtmPressure", UnitInFile = "hPa"},
			["Global Radiation"] = new OtsSensor {SensorId = "GlobalRadiation", UnitInFile = "W/m2"},
			["Rain"] = new OtsSensor {SensorId = "Rain", UnitInFile = "mm"},
			["Ozon"] = new OtsSensor {SensorId = "Ozon", UnitInFile = "ppb"},
			["UV-A"] = new OtsSensor {SensorId = "UVA", UnitInFile = "x"},
            ["SO2"] = new OtsSensor { SensorId = "SulfurDioxide", UnitInFile = "ppb"},
            ["H2S"] = new OtsSensor { SensorId = "SulfhidricAcid", UnitInFile = "ppb"},
            ["PM25"] = new OtsSensor { SensorId= "FineParticulateMatter", UnitInFile= "ug/m3"},
		};
		public class OtsState
		{
			public DateTime TimeStamp { get; set; }
			public IList<OtsSensorState> States { get; set; } = new List<OtsSensorState>();
		}

		public class OtsSensorState
		{
			public string SensorId { get; set; }
			public double State { get; set; }
            public bool Active { get; set; }
		}

		private class OtsSensor
		{
			public string SensorId { get; set; }
			public string UnitInFile { get; set; }
		}

		public IEnumerable<OtsState> Parse(Stream stream)
		{
			using (var reader = new StreamReader(stream, Encoding.UTF8, false, 2048, true))
			{
				var line = reader.ReadLine() ?? "";
				if (!"[BOF]".Equals(line.Trim()))
				{
					return Enumerable.Empty<OtsState>();
				}
				line = reader.ReadLine() ?? "";
				OtsSensor[] sensors = null;
				if ("[BOH]".Equals(line.Trim()))
				{
					sensors = ParseHeader(reader).ToArray();
				}
				line = reader.ReadLine() ?? "";
				if (sensors != null && sensors.Length > 0 && "[BOD]".Equals(line.Trim()))
				{
					return ParseData(reader, sensors);
				}
			}
			return Enumerable.Empty<OtsState>();
		}

		private IEnumerable<OtsState> ParseData(StreamReader reader, OtsSensor[] sensors)
		{
			var timeStampElements = 2;
			string line;
			while ((line = reader.ReadLine()) != null && !"[EOD]".Equals(line))
			{
				var lineItems = line.Trim().Split(',');
				if (lineItems.Length < timeStampElements)
				{
					// linea sin TimeStamp
					continue;
				}
				DateTime timeStamp;
				if (!DateTime.TryParseExact(lineItems[0]+lineItems[1], "yyyyMMddHH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out timeStamp))
				{
					// linea con timestamp no valido

					continue;
				}

				var state = new OtsState { TimeStamp = timeStamp};
				for (int i = timeStampElements; i < lineItems.Length; i++)
				{
					double value;
					var sensorIndex = i - timeStampElements;
					if (sensorIndex < sensors.Length && double.TryParse(lineItems[i], numberStyles, CultureInfo.InvariantCulture, out value))
					{
						// TODO: eventual conversión de unidad de medida entre lo que está en el file y lo que se establezca como unidad de default
						state.States.Add(new OtsSensorState { SensorId = sensors[sensorIndex].SensorId, State = value});
					}
				}
				if (state.States.Count> 0 && !state.States.All(x => 0D.Equals(x.State)))
				{
					yield return state;
				}
			}
		}

		private IEnumerable<OtsSensor> ParseHeader(StreamReader reader)
		{
			string line;
			var sensorNameLines = new List<string>();
			while ((line = reader.ReadLine()) != null && !"[EOH]".Equals(line.Trim()))
			{
				if (line.StartsWith("Station.Parameters(") && line.Contains(").Name"))
				{
					sensorNameLines.Add(line);
				}
			}

			foreach (var code in sensorNameLines.Select(GetSensorCode))
			{
				OtsSensor sensor;
				yield return sensorMap.TryGetValue(code, out sensor) ? sensor : new OtsSensor { SensorId = "Unknown", UnitInFile = "" };
			}
		}

		private static string GetSensorCode(string line)
		{
			var indexOfEqual = line.IndexOf("=");
			if (indexOfEqual > 0)
			{
				var nameValue = line.Substring(indexOfEqual + 1, line.Length - (indexOfEqual + 1));
				return nameValue.Trim().Trim('"').Split(';').FirstOrDefault();
			}
			return "";
		}
	}
}