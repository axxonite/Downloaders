namespace RentalsDownloader
{
	using Common;
	using WebRobotFramework;

	sealed class RentalsDownloaderService : WebRobotService
	{
		public const string DownloadPath = @"C:\Rentals\";
		RentalsDownloader _downloader = new RentalsDownloader($"{DownloadPath}Cache");

		public RentalsDownloaderService() { Name = "Rentals Downloader"; }

		protected override bool ProcessCommands(RobotTask task, string command) => ProcessCommand(task, command, "download", "", Download) || ProcessCommand(task, command, "Update", "", Update);

		void Download(string[] args)
		{
			new LoginForm(Selenium, @"http://www.westsiderentals.com/login", "username", "password", "//input[@class='btn_login']").Login("dfilion@hotmail.com", "dingd0ng111");
			const int MinPrice = 1500;
			const int MaxPrice = 3000;
			//const int MinPrice = 0;
			//const int MaxPrice = 1000;
			if (args.Length == 2)
				_downloader.Download(Selenium, new SearchQuery(args[0].ToEnum<Region>(), args[1].ToEnum<StructureType>(), MinPrice, MaxPrice));
			else
				EnumExtensions.ForEach<StructureType>(typeof(StructureType), s => _downloader.Download(Selenium, new SearchQuery(Region.Hollywood, s, MinPrice, MaxPrice)));
			_downloader.SaveReports();
		}

		void Update(string[] args) => _downloader.SaveReports();
	}
}