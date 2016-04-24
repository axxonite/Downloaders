namespace VideoDownloader
{
	using System;
	using System.IO;
	using System.Linq;
	using MoreLinq;
	using WebRobotFramework;
	using WebClient = System.Net.WebClient;

	public class VideoDownloaderService : WebRobotService
	{
		const string DownloadPath = @"G:\\Videos\\Piano\\";

		public VideoDownloaderService() { Name = "Video Downloader"; }

		public override void Terminate() { }

		protected override void InitializationTask() { }

		protected override bool ProcessCommands(RobotTask task, string command)
		{
			var commandProcessed = ProcessCommand(task, command, "piano", "", args => DownloadPianoVideos());
			return commandProcessed;
		}

		void DownloadPianoVideos()
		{
			var selenium = new SeleniumInterface();
			var client = new WebClient();
			Logger.Log("Downloading main page...");
			var completedLinks = File.ReadAllLines($"{DownloadPath}Completed.txt").ToList();
			var links =
				selenium.GoTo("http://pianocareeracademy.com/forum/index.php/topic,207.msg1539.html")
				        .SelectNodes("//div[@class='post']")
				        .SelectMany(p => p.SelectNodes(".//a").Select(l => l.GetAttributeValue("href", "")))
				        .Except(completedLinks)
				        .ToList();
			Logger.Log($"Found {links.Count} links.");
			var linkIndex = 1;
			foreach (var link in links)
			{
				Logger.Log($"Downloading page '{link}' ({linkIndex++}/{links.Count})...");
				selenium.GoTo(link, 5000);
				var pageSaved = false;
				for (var frameIndex = 0; frameIndex < selenium.BaseFrameCount; frameIndex++)
				{
					var videoElement = selenium.SelectNode(".//video", frameIndex);
					if (videoElement != null && videoElement.GetAttributeValue("src") != "")
					{
						var title = RemoveInvalidCharacters(selenium.SelectNode(".//div[@class='title']//div[@class='headers']//h1", frameIndex).InnerText.Trim());
						var srcFilename = Path.GetFileNameWithoutExtension(new Uri(videoElement.GetAttributeValue("src")).LocalPath);
						if (!Directory.EnumerateFiles(DownloadPath, "*.*", SearchOption.TopDirectoryOnly).Select(Path.GetFileName).Any(f => f.ToLower().Contains(srcFilename.ToLower())))
						{
							Logger.Log($"Downloading video '{title}'...");
							File.Delete($"{DownloadPath}Temp.mp4");
							client.DownloadFile(videoElement.GetAttributeValue("src"), $"{DownloadPath}Temp.mp4");
							File.Move($"{DownloadPath}Temp.mp4", $"{DownloadPath}{title} ({srcFilename}).mp4");
						}
						else
							Logger.Log($"Video '{title}' already downloaded.");
						SaveWebPage(selenium, title);
						pageSaved = true;
					}
				}
				if (!pageSaved)
					SaveWebPage(selenium, RemoveInvalidCharacters(selenium.SelectNode(".//title").InnerText.Trim()));
				completedLinks.Add(link);
				File.WriteAllLines($"{DownloadPath}Completed.txt", completedLinks);
			}
		}

		string RemoveInvalidCharacters(string contents)
		{
			var invalidChars = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
			var result = contents;
			invalidChars.ForEach(c => result = result.Replace(c.ToString(), ""));
			return result;
		}

		void SaveWebPage(SeleniumInterface selenium, string title)
		{
			Logger.Log($"Saving page {title}.htm.");
			File.WriteAllText($"{DownloadPath}{title}.htm", selenium.Driver.PageSource);
		}
	}
}