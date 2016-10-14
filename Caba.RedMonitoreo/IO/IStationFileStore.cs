using System.IO;
using System.Threading.Tasks;

namespace Caba.RedMonitoreo.IO
{
	public interface IStationFileStore
	{
		/// <summary>
		/// Graba un file
		/// </summary>
		/// <param name="stationId">Id de la estación</param>
		/// <param name="fileContent">contenido del file</param>
		/// <returns>Uri/Path del file</returns>
		Task<string> Store(string stationId, Stream fileContent);

		/// <summary>
		/// Dado el Uri/Path completo de un file, devuelve el stream del file
		/// </summary>
		/// <param name="filePath">Uri/Path del file</param>
		/// <param name="fileContent">Stream del file (posicionado al final del stream)</param>
		/// <returns>true si el file existe</returns>
		Task<bool> TryGet(string filePath, Stream fileContent);
	}
}