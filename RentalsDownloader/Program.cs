namespace RentalsDownloader
{
	using System;
	using System.Windows.Forms;
	using ApplicationContext = WebRobotFramework.ApplicationContext;

	static class Program
	{
		[STAThread]
		static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			var applicationContext = new ApplicationContext(new RentalsDownloaderService());
			Application.Run(applicationContext);
		}
	}
}