namespace RentalsDownloader
{
	using System.Collections.Generic;
	using System.Linq;
	using Common.Collections;
	using MoreLinq;
	using WebRobotFramework;

	sealed class RentalsDownloader : ItemsDownloader<SearchQuery, Rental>
	{
		string[] _favoriteNeighboorhoods = { "Echo Park", "Los Feliz", "Silver Lake", "Hancock Park", "Hollywood", "West Hollywood"};

		static readonly Dictionary<string, ReportPage> ReportPages = new Dictionary<string, ReportPage>();

		public RentalsDownloader(string cachePath) : base(cachePath) { }

		public void SaveReports() => ReportPages.ForEach(kvp => kvp.Value.Save());

		protected override void ItemDownloaded(SearchQuery query, Rental rental)
		{
			AddToReportPage(query.Region.ToString(), rental);
			AddToReportPage(query.Region + " - " + rental.Neighboorhood, rental);
			if ( _favoriteNeighboorhoods.Any(r => r == rental.Neighboorhood))
				AddToReportPage("_Favorite Neigboorhoods", rental);
		}

		protected override bool KeepItem(Rental rental) => (rental.SquareFootageAsInt == 0 || rental.SquareFootageAsInt >= 1100) && rental.Photos.Any() && rental.AcceptsCats;

		//protected override bool KeepItem(Rental rental) => rental.Photos.Any() && rental.AcceptsCats;

		static void AddToReportPage(string title, Rental rental) => ReportPages.GetValueOrAddNew(title, () => new ReportPage(title)).Rentals.Add(rental);
	}
}