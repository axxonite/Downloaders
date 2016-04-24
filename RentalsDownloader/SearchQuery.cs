namespace RentalsDownloader
{
	using System.Text.RegularExpressions;
	using Common;
	using HtmlAgilityPack;

	enum Region
	{
		None = 0,
		SantaMonica = 1,
		Hollywood = 5,
		Downtown = 13
	}

	enum StructureType
	{
		None,
		Apt,
		House,
		Loft
	}

	sealed class SearchQuery : WebRobotFramework.SearchQuery
	{
		static readonly Regex ListingIdRegex = new Regex("/listingdetail/(\\d+)");
		static readonly EnumStringPair[] StructureTypeQueryStrings =
			{
				new EnumStringPair(StructureType.Apt, "0%2C12%2C13%2C10%2C20%2C8%2C9%2C21"), new EnumStringPair(StructureType.House, "0%2C7%2C11"),
				new EnumStringPair(StructureType.Loft, "0%2C16")
			};

		public SearchQuery(Region region, StructureType type, int minPrice, int maxPrice)
		{
			Region = region;
			Type = type;
			MinPrice = minPrice;
			MaxPrice = maxPrice;
		}

		public override bool IsValid => Region != Region.None && Type != StructureType.None;
		public override string ItemsXPath => "//div[@class='image-wrapper']/a";
		public int MaxPrice { get; }
		public int MinPrice { get; }
		public Region Region { get; }
		public StructureType Type { get; }

		public override string GetItemId(HtmlNode node) => ListingIdRegex.Match(node.GetAttributeValue("href", "")).Groups[1].Captures[0].Value;

		public override string ToString() => $"{Type} in {Region} between {MinPrice}$ and {MaxPrice}$.";

		public override string ToUrl(int pageIndex)
			=>
				"http://www.westsiderentals.com/return-guest-results.cfm?" + $"pagenum={pageIndex}&resultlayout=photo&totalrecords=0&perpage=36&region_ID={(int)Region}&priceLow={MinPrice}&priceHigh={MaxPrice}&" +
				$"property_type_id={Type.Match(StructureTypeQueryStrings)}&searchType=g&x=0&y=0";
	}
}