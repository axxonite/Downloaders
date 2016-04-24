namespace VideoDownloader
{
	using System;
	using System.Windows.Forms;
	using VideoDownloader.Properties;
	using ApplicationContext = WebRobotFramework.ApplicationContext;

	static class Program
	{
		[STAThread]
		static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			var applicationContext = new ApplicationContext(new VideoDownloaderService(), Resources.VideoIcon);
			Application.Run(applicationContext);
		}
	}
}