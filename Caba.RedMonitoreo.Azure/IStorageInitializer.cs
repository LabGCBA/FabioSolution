namespace Caba.RedMonitoreo.Azure
{
	public interface IStorageInitializer
	{
		void Initialize();
		void Drop();
	}
}