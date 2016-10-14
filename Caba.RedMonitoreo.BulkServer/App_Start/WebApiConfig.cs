using System.Web.Http;
using Caba.RedMonitoreo.Common;

namespace Caba.RedMonitoreo.BulkServer
{
	public static class WebApiConfig
	{
		public static void Register(HttpConfiguration config)
		{
			config.MapHttpAttributeRoutes();

			var container = new SimpleServiceContainer();
			container.RegisterAllServices();
			config.UseSimpleDependencyResolver(container);

			config.Routes.MapHttpRoute(
					name: "DefaultApi",
					routeTemplate: "api1/{controller}/{id}",
					defaults: new { id = RouteParameter.Optional }
			);
		}
	}
}
