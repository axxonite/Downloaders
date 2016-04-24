namespace RentalsDownloader
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Text;
	using System.Text.RegularExpressions;
	using System.Xml.Serialization;
	using Common;
	using JetBrains.Annotations;
	using WebRobotFramework;

	[Serializable, PublicAPI]
	public sealed class Rental : DownloadItem
	{
		public static readonly string ReportTemplate;
		static readonly Regex NeighborhoodRegex = new Regex(@"(?:Los Angeles */ *)?([\w\s]+)");
		static readonly Regex NumberRegex = new Regex("(\\d[\\d\\.,]*)");
		static readonly string PhotoTemplate;
		static readonly string RentalTemplate;
		static readonly XmlSerializer XmlSerializer = new XmlSerializer(typeof(Rental));

		static Rental()
		{
			ReportTemplate = File.ReadAllText($@"{RentalsDownloaderService.DownloadPath}template\template.html", Encoding.UTF8);
			RentalTemplate = Regex.Match(ReportTemplate, "<rental>(.*)</rental>", RegexOptions.Singleline).Groups[1].Captures[0].Value;
			PhotoTemplate = Regex.Match(ReportTemplate, "<photo>(.*)</photo>", RegexOptions.Singleline).Groups[1].Captures[0].Value;
		}

		public bool AcceptsCats => Pets != "No pets" && Pets != "Dog ok";
		public string Address { get; set; }
		public List<string> Amenities { get; set; }
		public string Available { get; set; }
		public string Bathrooms { get; set; }
		public string Bedrooms { get; set; }
		public int BedroomsAsInt => (int)GetDoubleValue(Bedrooms);
		public string Called { get; set; }
		public string Comments { get; set; }
		public string Contact { get; set; }
		public string Deposit { get; set; }
		public string Email { get; set; }
		public string Furnished { get; set; }
		public bool HasParking => Parking != "Street parking";
		public override string Id { get; set; }
		public string LeaseType { get; set; }
		public string ListingInfo { get; set; }
		public string ListingType { get; set; }
		public string Location { get; set; }
		public string Neighboorhood => NeighborhoodRegex.Match(Location).Groups[1].Captures[0].Value;
		public string Parking { get; set; }
		public string Pets { get; set; }
		public List<string> Photos { get; set; }
		public string Rating { get; set; } = "";
		public int RatingAsInt => (int)GetDoubleValue(Rating);
		public string Rent { get; set; }
		public double RentAsDouble => GetDoubleValue(Rent);
		public double RentPerSquareFeet => SquareFootageAsInt > 0 && RentAsDouble > 0 ? RentAsDouble / SquareFootageAsInt : double.MaxValue;
		public string SpreadsheetHyperlink => $"HYPERLINK(\"{Url}\",\"Link\")";
		public string SquareFootage { get; set; }
		public int SquareFootageAsInt => (int)GetDoubleValue(SquareFootage);
		public string StructureType { get; set; }
		public string Title { get; set; }
		public string UnitDetails { get; set; }
		public override string Url => $"http://www.westsiderentals.com/listingdetail/{Id}/";
		public string Visited { get; set; }
		public string WalkScore { get; set; }

		public override void Parse(SeleniumInterface selenium)
		{
			Title = GetNodeText(selenium, "//div[contains(@class,'address-info')]//h1");
			ListingInfo = GetNodeText(selenium, "//div[contains(@class,'address-info')]//h3");
			Amenities = selenium.SelectNodes("//div[contains(@class,'address-info')]//li").Select(l => l.InnerText).ToList();
			Photos = selenium.SelectNodes("//div[@id='carousel']//img").Select(l => l.GetAttributeValue("src", "")).ToList();
			Address = GetNodeText(selenium, "//div[contains(@class, 'address-info')]/div[@class='bigText']");
			Contact = GetNodeText(selenium, "//div[contains(@class, 'address-info')]/div[@class='medText']");
			Email = GetNodeText(selenium, "//div[contains(@class, 'address-info')]//a[@class='mailtoLink']");
			WalkScore = GetNodeText(selenium, "//div[@class='walkscore']//span");
			var details = selenium.SelectNodes("//div[@class='col-md-6']").Select(l => Regex.Replace(l.InnerText, "[\r\t\n]", "")).ToList();
			Rent = GetRentalItem(details, "Square Footage");
			Location = GetRentalItem(details, "City/Area");
			Rent = GetRentalItem(details, "Rent");
			SquareFootage = GetRentalItem(details, "Square Footage");
			Bedrooms = GetRentalItem(details, "Bedrooms");
			Pets = GetRentalItem(details, "Pets");
			StructureType = GetRentalItem(details, "Structure Type");
			Parking = GetRentalItem(details, "Parking");
			Available = GetRentalItem(details, "Available");
			Bathrooms = GetRentalItem(details, "Bathrooms");
			Furnished = GetRentalItem(details, "Furnished");
			LeaseType = GetRentalItem(details, "Lease Type");
			UnitDetails = GetRentalItem(details, "Unit Details");
			ListingType = GetRentalItem(details, "Listing Type");
		}

		public void SaveToSpreadsheet(WorkSheet worksheet, int rowIndex)
		{
			worksheet.SetCell(rowIndex, "Address", Address);
			worksheet.SetCell(rowIndex, "Neighborhood", Neighboorhood);
			worksheet.SetCell(rowIndex, "SqFeet", SquareFootageAsInt);
			worksheet.SetCell(rowIndex, "Rooms", BedroomsAsInt);
			worksheet.SetCell(rowIndex, "Price", RentAsDouble);
			worksheet.SetCell(rowIndex, "Structure", UnitDetails);
			worksheet.SetCell(rowIndex, "Comments", Comments);
			worksheet.SetCell(rowIndex, "V", Visited);
			worksheet.SetCell(rowIndex, "C", Called);
			if (Rating != "")
				worksheet.SetCell(rowIndex, "R", RatingAsInt);
			worksheet.SetCellFormula(rowIndex, "Link", $"={SpreadsheetHyperlink}");
			worksheet.SetCellFormula(rowIndex, "Map", $"=HYPERLINK(\"http://maps.google.com/?q={Address}\",\"Map\")");
			worksheet.SetCellFormula(rowIndex, "Redfin", $"=HYPERLINK(\"https://www.google.com/search?q={Address} Redfin\",\"Redfin\")");
			worksheet.SetCell(rowIndex, "Contact", Contact);
			worksheet.SetCell(rowIndex, "D", Amenities.Any(a => a.Contains("dishwasher", StringComparison.OrdinalIgnoreCase)) ? "X" : "");
			worksheet.SetCell(rowIndex, "L", HasLaundry());
		}

		public override void Serialize(StreamWriter writer) => XmlSerializer.Serialize(writer, this);

		public string ToHtml(int index)
		{
			var rentalHtml = RentalTemplate;
			rentalHtml = rentalHtml.Replace("[Url]", Url);
			rentalHtml = rentalHtml.Replace("[Title]", $"{index}. {Title}");
			rentalHtml = rentalHtml.Replace("[Location]", Location);
			rentalHtml = rentalHtml.Replace("[Price]", Rent);
			rentalHtml = rentalHtml.Replace("[SquareFeet]", RentPerSquareFeet < double.MaxValue ? $"{SquareFootage} ({RentPerSquareFeet:F2}$/sq.ft.)" : SquareFootage);
			rentalHtml = rentalHtml.Replace("[Bedrooms]", Bedrooms);
			rentalHtml = rentalHtml.Replace("[Availability]", Available);
			rentalHtml = rentalHtml.Replace("[Pets]", Pets);
			rentalHtml = rentalHtml.Replace("[Type]", StructureType);
			rentalHtml = rentalHtml.Replace("[Parking]", Parking);
			rentalHtml = rentalHtml.Replace("[Address]", Address);
			rentalHtml = rentalHtml.Replace("[Contact]", Contact);
			rentalHtml = rentalHtml.Replace("[Photos]", Photos.Aggregate("", (current, photo) => current + PhotoTemplate.Replace("[PhotoURL]", photo)));
			return rentalHtml;
		}

		static double GetDoubleValue(string value)
		{
			var match = NumberRegex.Match(value);
			if (!match.Success)
				return 0;
			double squareFootage;
			return double.TryParse(match.Groups[1].Captures[0].Value, out squareFootage) ? squareFootage : 0;
		}

		static string GetRentalItem(IEnumerable<string> items, string prefix)
			=> items.FirstOrDefault(attribute => attribute.StartsWith(prefix + ":", StringComparison.Ordinal))?.Substring(prefix.Length + 1).Trim() ?? "";

		string HasLaundry()
		{
			if (!Amenities.Any(a => a.Contains("laundry", StringComparison.OrdinalIgnoreCase) || a.Contains("washer", StringComparison.OrdinalIgnoreCase) || a.Contains("dryer", StringComparison.OrdinalIgnoreCase)))
				return "";
			return Amenities.Any(a => a.Contains("hook", StringComparison.OrdinalIgnoreCase)) ? "H" : "X";
		}
	}
}