using System.Web;
using System.Web.Mvc;

namespace Caba.RedMonitoreo.BulkServer
{
	public class FilterConfig
	{
		public static void RegisterGlobalFilters(GlobalFilterCollection filters)
		{
			filters.Add(new HandleErrorAttribute());
		}
	}
}
