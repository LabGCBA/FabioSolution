using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Caba.RedMonitoreo.IO;
using NUnit.Framework;
using SharpTestsEx;

namespace Caba.RedMonitoreoTests.IOTests
{
	public class OtsFileParserTests
	{
		[Test]
		public void WhenEmptyThenNoResult()
		{
			var parser = new OtsFileParser();
			using (Stream stream = new MemoryStream())
			{
				IEnumerable<OtsFileParser.OtsState> states = parser.Parse(stream);
				var actual = states.ToArray();
				actual.Should().Be.Empty();
			}
		}

		[Test]
		public void WhenInvalidThenNoResult()
		{
			var parser = new OtsFileParser();
			using (Stream stream = new MemoryStream())
			{
				using (var writer = new StreamWriter(stream, Encoding.ASCII, 1024, true))
				{
					writer.Write("Cualquier cosa");
				}
				stream.Seek(0, SeekOrigin.Begin);
				IEnumerable<OtsFileParser.OtsState> states = parser.Parse(stream);
				var actual = states.ToArray();
				actual.Should().Be.Empty();
			}
		}

		[Test]
		public void WhenValidThenResultForEachBodyLine()
		{
			var lines = new[]
			{
				"20141029,01:46,3.00,3.00,3.00,3.00,3.00,3.00,3.00,3.00,3.00,3.00,3.00,4.00",
				"20141029,01:47,3.00,3.00,3.00,3.00,3.00,3.00,3.00,3.00,3.00,3.00,3.00,3.00",
				"20141029,01:48,0.20,0.30,0.30,0.20,0.20,0.20,0.30,0.30,0.30,0.30,0.30,0.30",
				"20141029,01:49,10.00,11.00,13.00,15.00,16.00,16.00,17.00,16.00,15.00,16.00,16.00,17.00",
				"20141029,01:50,147.00,260.00,160.00,246.00,221.00,225.00,244.00,237.00,225.00,259.00,200.00,257.00",
				"20141029,01:51,3.90,5.60,6.50,1.00,1.50,0.70,2.30,1.10,0.90,4.00,1.10,9.00",
				"20141029,01:54,1004.00,1004.00,1005.00,1005.00,1005.00,1005.00,1005.00,1006.00,1006.00,1005.00,1006.00,1006.00",
				"20141029,01:55,1.30,1.30,1.10,1.10,0.90,1.00,0.80,0.70,0.70,0.90,0.80,0.80",
				"20141029,01:56,0.20,0.20,0.20,0.20,0.20,0.20,0.20,0.20,0.20,0.20,0.20,0.20",
			};
			var parser = new OtsFileParser();
			using (Stream stream = new MemoryStream())
			{
				WriteValidFile(stream, lines);
				stream.Seek(0, SeekOrigin.Begin);
				IEnumerable<OtsFileParser.OtsState> states = parser.Parse(stream);
				states.Should().Have.Count.EqualTo(9);
			}
		}

		[Test]
		public void WhenContainsLinesAllZeroThenResultForEachNoZeroBodyLine()
		{
			var lines = new[]
			{
				"20141029,01:46,3.00,3.00,3.00,3.00,3.00,3.00,3.00,3.00,3.00,3.00,3.00,4.00",
				"20141029,01:47,3.00,3.00,3.00,3.00,3.00,3.00,3.00,3.00,3.00,3.00,3.00,3.00",
				"20141029,01:48,0.20,0.30,0.30,0.20,0.20,0.20,0.30,0.30,0.30,0.30,0.30,0.30",
				"20141029,01:49,10.00,11.00,13.00,15.00,16.00,16.00,17.00,16.00,15.00,16.00,16.00,17.00",
				"20141029,01:50,147.00,260.00,160.00,246.00,221.00,225.00,244.00,237.00,225.00,259.00,200.00,257.00",
				"20141029,01:51,3.90,5.60,6.50,1.00,1.50,0.70,2.30,1.10,0.90,4.00,1.10,9.00",
				"20141029,01:52,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00",
				"20141029,01:53,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00",
				"20141029,01:54,1004.00,1004.00,1005.00,1005.00,1005.00,1005.00,1005.00,1006.00,1006.00,1005.00,1006.00,1006.00",
				"20141029,01:55,1.30,1.30,1.10,1.10,0.90,1.00,0.80,0.70,0.70,0.90,0.80,0.80",
				"20141029,01:56,0.20,0.20,0.20,0.20,0.20,0.20,0.20,0.20,0.20,0.20,0.20,0.20",
				"20141029,01:57,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00",
				"20141029,01:58,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00",
				"20141029,01:59,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00",
				"20141029,02:00,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00,0.00",
			};
			var parser = new OtsFileParser();
			using (Stream stream = new MemoryStream())
			{
				WriteValidFile(stream, lines);
				stream.Seek(0, SeekOrigin.Begin);
				IEnumerable<OtsFileParser.OtsState> states = parser.Parse(stream);
				states.Should().Have.Count.EqualTo(9);
			}
		}

		[Test]
		public void WhenParseLineThenStateAsTimeStamp()
		{
			var lines = new[]
			{
				"20141029,01:46,3.00,3.00,3.00,3.00,3.00,3.00,3.00,3.00,3.00,3.00,3.00,4.00",
				"20141029,01:47,3.00,3.00,3.00,3.00,3.00,3.00,3.00,3.00,3.00,3.00,3.00,3.00",
			};
			var parser = new OtsFileParser();
			using (Stream stream = new MemoryStream())
			{
				WriteValidFile(stream, lines);
				stream.Seek(0, SeekOrigin.Begin);
				IEnumerable<OtsFileParser.OtsState> states = parser.Parse(stream);
				states.Select(x=> x.TimeStamp).Should().Have.SameValuesAs(new DateTime(2014,10,29,1,46,0)
					, new DateTime(2014, 10, 29, 1, 47, 0));
			}
		}

		[Test]
		public void WhenParseLineThenStateBySensorId()
		{
			var lines = new[]
			{
				"20141029,01:46,1.00,2.00,3.00,4.00,5.00,6.00,7.00,8.00,9.00,10.00,11.00,12.00",
			};
			var parser = new OtsFileParser();
			using (Stream stream = new MemoryStream())
			{
				WriteValidFile(stream, lines);
				stream.Seek(0, SeekOrigin.Begin);
				var states = parser.Parse(stream).First();
				states.States[0].SensorId.Should().Be("NitricOxide");
				states.States[1].SensorId.Should().Be("NitricDioxide");
				states.States[2].SensorId.Should().Be("MonoNitrogenOxide");
				states.States[3].SensorId.Should().Be("CarbonOxide");
				states.States[4].SensorId.Should().Be("ParticulateMatter");
				states.States[5].SensorId.Should().Be("WindDirection");
				states.States[6].SensorId.Should().Be("WindSpeed");
				states.States[7].SensorId.Should().Be("Temperature");
				states.States[8].SensorId.Should().Be("RelativeHumidity");
				states.States[9].SensorId.Should().Be("AtmPressure");
				states.States[10].SensorId.Should().Be("GlobalRadiation");
				states.States[11].SensorId.Should().Be("Rain");
				states.States[0].State.Should().Be(1D);
				states.States[1].State.Should().Be(2D);
				states.States[2].State.Should().Be(3D);
				states.States[3].State.Should().Be(4D);
				states.States[4].State.Should().Be(5D);
				states.States[5].State.Should().Be(6D);
				states.States[6].State.Should().Be(7D);
				states.States[7].State.Should().Be(8D);
				states.States[8].State.Should().Be(9D);
				states.States[9].State.Should().Be(10D);
				states.States[10].State.Should().Be(11D);
				states.States[11].State.Should().Be(12D);
			}
		}

		[Test]
		public void WhenParseLine1ThenStateBySensorId()
		{
			var lines = new[]
			{
				"20111122,00:01,-1.00,-2.00,-3.00,4,5.00,6,7,8,9,10,11,12,13",
			};
			var parser = new OtsFileParser();
			using (Stream stream = new MemoryStream())
			{
				WriteValidFile1(stream, lines);
				stream.Seek(0, SeekOrigin.Begin);
				var states = parser.Parse(stream).First();
				states.States[0].SensorId.Should().Be("MonoNitrogenOxide");
				states.States[1].SensorId.Should().Be("NitricDioxide");
				states.States[2].SensorId.Should().Be("NitricOxide");
				states.States[3].SensorId.Should().Be("CarbonOxide");
				states.States[4].SensorId.Should().Be("Ozon");
				states.States[5].SensorId.Should().Be("ParticulateMatter");
				states.States[6].SensorId.Should().Be("Temperature");
				states.States[7].SensorId.Should().Be("WindSpeed");
				states.States[8].SensorId.Should().Be("WindDirection");
				states.States[9].SensorId.Should().Be("GlobalRadiation");
				states.States[10].SensorId.Should().Be("UVA");
				states.States[11].SensorId.Should().Be("RelativeHumidity");
				states.States[12].SensorId.Should().Be("AtmPressure");
				states.States[0].State.Should().Be(-1D);
				states.States[1].State.Should().Be(-2D);
				states.States[2].State.Should().Be(-3D);
				states.States[3].State.Should().Be(4D);
				states.States[4].State.Should().Be(5D);
				states.States[5].State.Should().Be(6D);
				states.States[6].State.Should().Be(7D);
				states.States[7].State.Should().Be(8D);
				states.States[8].State.Should().Be(9D);
				states.States[9].State.Should().Be(10D);
				states.States[10].State.Should().Be(11D);
				states.States[11].State.Should().Be(12D);
				states.States[12].State.Should().Be(13D);
			}
		}

		private void WriteValidFile(Stream stream, IEnumerable<string> bodyLines)
		{
			using (var writer = new StreamWriter(stream, Encoding.UTF8, 1024, true))
			{
				writer.WriteLine("[BOF]");
				writer.WriteLine("[BOH]");
				writer.WriteLine("ASCIICode.Separator = 44");
				writer.WriteLine("ASCIICode.Decimal = 46");
				writer.WriteLine("MissingValues = \"\"");
				writer.WriteLine("Resolution=Min.01");
				writer.WriteLine("IntegrationPeriod = Forward");
				writer.WriteLine("DateFormat = YYYYMMDD");
				writer.WriteLine("TimeFormat = HH:NN");
				writer.WriteLine("Station.Name = \"Centenario\"");
				writer.WriteLine("Station.Parameters.Count = 12");
				writer.WriteLine("Station.Parameters(1).Name =\"NO;Roof;Conc\"");
				writer.WriteLine("Station.Parameters(1).Unit =\"ppb\"");
				writer.WriteLine("Station.Parameters(1).Position = 1");
				writer.WriteLine("Station.Parameters(2).Name =\"NO2;Roof;Conc\"");
				writer.WriteLine("Station.Parameters(2).Unit =\"ppb\"");
				writer.WriteLine("Station.Parameters(2).Position = 2");
				writer.WriteLine("Station.Parameters(3).Name =\"NOx;Roof;Conc\"");
				writer.WriteLine("Station.Parameters(3).Unit =\"ppb\"");
				writer.WriteLine("Station.Parameters(3).Position = 3");
				writer.WriteLine("Station.Parameters(4).Name =\"CO;Roof;Conc\"");
				writer.WriteLine("Station.Parameters(4).Unit =\"ppm\"");
				writer.WriteLine("Station.Parameters(4).Position = 4");
				writer.WriteLine("Station.Parameters(5).Name =\"PM10;Roof;Conc\"");
				writer.WriteLine("Station.Parameters(5).Unit =\"ug/m3\"");
				writer.WriteLine("Station.Parameters(5).Position = 5");
				writer.WriteLine("Station.Parameters(6).Name =\"Wind Direction;6 m;Value\"");
				writer.WriteLine("Station.Parameters(6).Unit =\"Degrees\"");
				writer.WriteLine("Station.Parameters(6).Position = 6");
				writer.WriteLine("Station.Parameters(7).Name =\"Wind Speed;Mast 6m;Value\"");
				writer.WriteLine("Station.Parameters(7).Unit =\"m/s\"");
				writer.WriteLine("Station.Parameters(7).Position = 7");
				writer.WriteLine("Station.Parameters(8).Name =\"Temperature;Mast 2m;Value\"");
				writer.WriteLine("Station.Parameters(8).Unit =\"Celsius\"");
				writer.WriteLine("Station.Parameters(8).Position = 8");
				writer.WriteLine("Station.Parameters(9).Name =\"Relative Humidity;Mast 2m;Value\"");
				writer.WriteLine("Station.Parameters(9).Unit =\"%\"");
				writer.WriteLine("Station.Parameters(9).Position = 9");
				writer.WriteLine("Station.Parameters(10).Name =\"Atm Pressure;Roof;Value\"");
				writer.WriteLine("Station.Parameters(10).Unit =\"hPa\"");
				writer.WriteLine("Station.Parameters(10).Position = 10");
				writer.WriteLine("Station.Parameters(11).Name =\"Global Radiation;Roof;Value\"");
				writer.WriteLine("Station.Parameters(11).Unit =\"W/m2\"");
				writer.WriteLine("Station.Parameters(11).Position = 11");
				writer.WriteLine("Station.Parameters(12).Name =\"Rain;Roof;Value\"");
				writer.WriteLine("Station.Parameters(12).Unit =\"mm\"");
				writer.WriteLine("Station.Parameters(12).Position = 12");
				writer.WriteLine("[EOH]");
				writer.WriteLine("[BOD]");
				foreach (var bodyLine in bodyLines)
				{
					writer.WriteLine(bodyLine);
				}
				writer.WriteLine("[EOD]");
				writer.WriteLine("[EOF]");
			}
		}

		private void WriteValidFile1(Stream stream, IEnumerable<string> bodyLines)
		{
			using (var writer = new StreamWriter(stream, Encoding.UTF8, 1024, true))
			{
				writer.WriteLine("[BOF]");
				writer.WriteLine("[BOH]");
				writer.WriteLine("ASCIICode.Separator = 44");
				writer.WriteLine("ASCIICode.Decimal = 46");
				writer.WriteLine("MissingValues = \"\"");
				writer.WriteLine("Resolution=Min.01");
				writer.WriteLine("IntegrationPeriod = Forward");
				writer.WriteLine("DateFormat = YYYYMMDD");
				writer.WriteLine("TimeFormat = HH:NN");
				writer.WriteLine("Station.Name = \"Cordoba\"");
				writer.WriteLine("Station.Parameters.Count = 13");
				writer.WriteLine("Station.Parameters(1).Name =\"NOx;Roof;Conc\"");
				writer.WriteLine("Station.Parameters(1).Unit =\"ppb\"");
				writer.WriteLine("Station.Parameters(1).Position = 1");
				writer.WriteLine("Station.Parameters(2).Name =\"NO2;Roof;Conc\"");
				writer.WriteLine("Station.Parameters(2).Unit =\"ppb\"");
				writer.WriteLine("Station.Parameters(2).Position = 2");
				writer.WriteLine("Station.Parameters(3).Name =\"NO;Roof;Conc\"");
				writer.WriteLine("Station.Parameters(3).Unit =\"ppb\"");
				writer.WriteLine("Station.Parameters(3).Position = 3");
				writer.WriteLine("Station.Parameters(4).Name =\"CO;Roof;Conc\"");
				writer.WriteLine("Station.Parameters(4).Unit =\"ppm\"");
				writer.WriteLine("Station.Parameters(4).Position = 4");
				writer.WriteLine("Station.Parameters(5).Name =\"Ozon;Roof;Conc\"");
				writer.WriteLine("Station.Parameters(5).Unit =\"ppb\"");
				writer.WriteLine("Station.Parameters(5).Position = 5");
				writer.WriteLine("Station.Parameters(6).Name =\"PM10;Roof;Conc\"");
				writer.WriteLine("Station.Parameters(6).Unit =\"ug/m3\"");
				writer.WriteLine("Station.Parameters(6).Position = 6");
				writer.WriteLine("Station.Parameters(7).Name =\"Temperature;Roof;Value\"");
				writer.WriteLine("Station.Parameters(7).Unit =\"C\"");
				writer.WriteLine("Station.Parameters(7).Position = 7");
				writer.WriteLine("Station.Parameters(8).Name =\"WindSpeed;Mast 6m;Value\"");
				writer.WriteLine("Station.Parameters(8).Unit =\"m/s\"");
				writer.WriteLine("Station.Parameters(8).Position = 8");
				writer.WriteLine("Station.Parameters(9).Name =\"WindDirection;Mast 6m;Value\"");
				writer.WriteLine("Station.Parameters(9).Unit =\"Degrees\"");
				writer.WriteLine("Station.Parameters(9).Position = 9");
				writer.WriteLine("Station.Parameters(10).Name =\"Global Radiation;Mast 6m;Value\"");
				writer.WriteLine("Station.Parameters(10).Unit =\"W/m2\"");
				writer.WriteLine("Station.Parameters(10).Position = 10");
				writer.WriteLine("Station.Parameters(11).Name =\"UV-A;Roof;Value\"");
				writer.WriteLine("Station.Parameters(11).Unit =\"x\"");
				writer.WriteLine("Station.Parameters(11).Position = 11");
				writer.WriteLine("Station.Parameters(12).Name =\"Rel Humidity;Roof;Value\"");
				writer.WriteLine("Station.Parameters(12).Unit =\"%\"");
				writer.WriteLine("Station.Parameters(12).Position = 12");
				writer.WriteLine("Station.Parameters(13).Name =\"Atm Pressure;Roof;Value\"");
				writer.WriteLine("Station.Parameters(13).Unit =\"hPa\"");
				writer.WriteLine("Station.Parameters(13).Position = 13");
				writer.WriteLine("[EOH]");
				writer.WriteLine("[BOD]");
				foreach (var bodyLine in bodyLines)
				{
					writer.WriteLine(bodyLine);
				}
				writer.WriteLine("[EOD]");
				writer.WriteLine("[EOF]");
			}

		}

        [Test]
        public void ParseUnFile()
        {
            var content =
@"[BOF]
[BOH]
ASCIICode.Separator = 44
ASCIICode.Decimal = 46
MissingValues = """"
Resolution=Min.01
IntegrationPeriod = Forward
DateFormat = YYYYMMDD
TimeFormat = HH:NN
Station.Name = ""Centenario""
Station.Parameters.Count = 12
Station.Parameters(1).Name = ""NO;Roof;Conc""
Station.Parameters(1).Unit = ""ppb""
Station.Parameters(1).Position = 1
Station.Parameters(2).Name = ""NO2;Roof;Conc""
Station.Parameters(2).Unit = ""ppb""
Station.Parameters(2).Position = 2
Station.Parameters(3).Name = ""NOx;Roof;Conc""
Station.Parameters(3).Unit = ""ppb""
Station.Parameters(3).Position = 3
Station.Parameters(4).Name = ""CO;Roof;Conc""
Station.Parameters(4).Unit = ""ppm""
Station.Parameters(4).Position = 4
Station.Parameters(5).Name = ""PM10;Roof;Conc""
Station.Parameters(5).Unit = ""ug/m3""
Station.Parameters(5).Position = 5
Station.Parameters(6).Name = ""Wind Direction;6 m;Value""
Station.Parameters(6).Unit = ""Degrees""
Station.Parameters(6).Position = 6
Station.Parameters(7).Name = ""Wind Speed;Mast 6m;Value""
Station.Parameters(7).Unit = ""m/s""
Station.Parameters(7).Position = 7
Station.Parameters(8).Name = ""Temperature;Mast 2m;Value""
Station.Parameters(8).Unit = ""Celsius""
Station.Parameters(8).Position = 8
Station.Parameters(9).Name = ""Rel Humidity;Mast 2m;Value""
Station.Parameters(9).Unit = ""%""
Station.Parameters(9).Position = 9
Station.Parameters(10).Name = ""Atm Pressure;Roof;Value""
Station.Parameters(10).Unit = ""hPa""
Station.Parameters(10).Position = 10
Station.Parameters(11).Name = ""Global Radiation;Roof;Value""
Station.Parameters(11).Unit = ""W/m2""
Station.Parameters(11).Position = 11
Station.Parameters(12).Name = ""Rain;Roof;Value""
Station.Parameters(12).Unit = ""mm""
Station.Parameters(12).Position = 12
[EOH]
[BOD]
20160901,13:28,12,19,31,0.6,28,182,1.1,14.4,35,1100,59.0,0.0
[EOD]
[EOF]";
            var parser = new OtsFileParser();
            using (Stream stream = new MemoryStream())
            {
                var bcontent = Encoding.UTF8.GetBytes(content);
                stream.Write(bcontent, 0, bcontent.Length);
                stream.Seek(0, SeekOrigin.Begin);
                IEnumerable<OtsFileParser.OtsState> states = parser.Parse(stream);
                states.Should().Have.Count.EqualTo(1);
            }
        }
    }
}