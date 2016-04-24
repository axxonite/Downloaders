namespace RentalsDownloader
{
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using OfficeOpenXml;
	using WebRobotFramework;

	sealed class ReportPage
	{
		readonly string _title;

		public ReportPage(string title) { _title = title; }

		public List<Rental> Rentals { get; } = new List<Rental>();

		public void Save()
		{
			if (!Rentals.Any())
				return;
			Logger.Log($"Saving report page '{_title}'.");
			LoadExistingSpreadsheetValues(Rentals);
			var rentals =
				Rentals.OrderByDescending(r => r.RatingAsInt)
				       .ThenBy(r => r.AcceptsCats ? 0 : 1)
				       .ThenBy(r => r.RentPerSquareFeet)
				       .ThenByDescending(r => r.SquareFootageAsInt)
				       .ThenByDescending(r => r.BedroomsAsInt)
				       .ThenBy(r => r.HasParking ? 0 : 1)
				       .ThenBy(r => r.RentAsDouble)
				       .ToList();
			var index = 1;
			var rentalsHtml = rentals.Aggregate("", (current, rental) => current + rental.ToHtml(index++));
			File.WriteAllText($"{RentalsDownloaderService.DownloadPath}{_title}.html", Rental.ReportTemplate.Replace("[Rentals]", rentalsHtml));

			SaveSpreadSheet(rentals);
		}

		static string GetWorkSheetValue(WorkSheet worksheet, int row, string columnName, string existingValue)
			=> worksheet.Columns.ContainsKey(columnName) ? worksheet.Worksheet.Cells[row, worksheet.Columns[columnName]].Text : existingValue;

		static void LoadExistingSpreadsheetValues(IEnumerable<Rental> rentals)
		{
			var oldFileInfo = new FileInfo($"{RentalsDownloaderService.DownloadPath}_Favorite Neigboorhoods.xlsx");
			if (!oldFileInfo.Exists)
				return;
			using (var oldExcelSpreadSheet = new ExcelPackage(oldFileInfo))
			{
				var oldWorkSheet = new WorkSheet(oldExcelSpreadSheet.Workbook.Worksheets["Rentals"]);
				var rentalsDictionary = rentals.ToDictionary(r2 => r2.SpreadsheetHyperlink);
				for (var row = 2; row <= oldWorkSheet.Worksheet.Dimension.End.Row; row++)
				{
					var link = oldWorkSheet.Worksheet.Cells[row, oldWorkSheet.Columns["Link"]].Formula;
					Rental rental;
					if (!rentalsDictionary.TryGetValue(link, out rental))
						continue;
					rental.Comments = GetWorkSheetValue(oldWorkSheet, row, "Comments", rental.Comments);
					rental.Rating = GetWorkSheetValue(oldWorkSheet, row, "R", rental.Rating);
					rental.Visited = GetWorkSheetValue(oldWorkSheet, row, "V", rental.Visited);
					rental.Called = GetWorkSheetValue(oldWorkSheet, row, "C", rental.Called);
				}
			}
		}

		void SaveSpreadSheet(IEnumerable<Rental> rentals)
		{
			using (var newExcelSpreadsheet = new ExcelPackage(new FileInfo($"{RentalsDownloaderService.DownloadPath}template\\Rentals.xlsx")))
			{
				var fileInfo = new FileInfo($"{RentalsDownloaderService.DownloadPath}{_title}.xlsx");
				var worksheet = new WorkSheet(newExcelSpreadsheet.Workbook.Worksheets["Rentals"]);
				var rowIndex = 2;
				foreach (var rental in rentals)
					rental.SaveToSpreadsheet(worksheet, rowIndex++);
				newExcelSpreadsheet.SaveAs(fileInfo);
			}
		}
	}
}