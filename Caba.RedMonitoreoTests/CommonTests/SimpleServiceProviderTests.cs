using Caba.RedMonitoreo.Common;
using NUnit.Framework;
using SharpTestsEx;

namespace Caba.RedMonitoreoTests.CommonTests
{
	public class SimpleServiceProviderTests
	{
		[Test]
		public void WhenNoServiceRegisteredThenNull()
		{
			var factory = new SimpleServiceContainer();
			var actual = factory.GetInstance<IMyService>();
			actual.Should().Be.Null();
		}

		[Test]
		public void WhenServiceRegisteredThenGetInstance()
		{
			var factory = new SimpleServiceContainer();
			factory.RegisterTransient<IMyService>(x => new MyService());
			var actual = factory.GetInstance<IMyService>();
			actual.Should().Be.OfType<MyService>();
		}

		[Test]
		public void WhenTransientServiceThenGetNewInstance()
		{
			var factory = new SimpleServiceContainer();
			factory.RegisterTransient<IMyService>(x => new MyService());

			var first = factory.GetInstance<IMyService>();
			var actual = factory.GetInstance<IMyService>();
			actual.Should().Not.Be.SameInstanceAs(first);
		}

		[Test]
		public void WhenServiceRegisteredAsSingletonThenGetSameInstance()
		{
			var factory = new SimpleServiceContainer();
			factory.RegisterSingleton<IMyService>(x => new MyService());

			var first = factory.GetInstance<IMyService>();
			var actual = factory.GetInstance<IMyService>();
			actual.Should().Be.SameInstanceAs(first);
		}

		[Test]
		public void WhenInstanceRegisteredAsSingletonThenGetSameInstance()
		{
			var factory = new SimpleServiceContainer();
			var myInstance = new MyService();
			factory.RegisterSingleton<IMyService>(myInstance);

			var actual = factory.GetInstance<IMyService>();
			actual.Should().Be.SameInstanceAs(myInstance);
		}

		public interface IMyService {}

		public class MyService : IMyService {}
	}
}