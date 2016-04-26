[<AutoOpen>]
module DomainTypes

type Download = 
    { id : string
      url : string }

type Rental = 
    { download : Download
      address : string
      amenities : list<string>
      available : string
      bathrooms : string
      bedrooms : string
      bedroomsInt : int
      called : string
      comments : string
      contact : string
      deposit : string
      email : string
      furnished : string
      hasParking : bool
      leaseType : string
      listingInfo : string
      listingType : string
      location : string
      neighborhood : string
      parking : string
      pets : string
      acceptsCats : bool
      photos : list<string>
      rating : string
      ratingInt : int
      rent : string
      rentDouble : double
      rentPerSqFt : double option
      squareFootage : string
      squareFootageInt : int
      structureType : string
      title : string
      unitDetails : string
      visited : string
      walkScore : string }

type Region = 
    | None = 0
    | SantaMonica = 1
    | Hollywood = 5
    | Downtown = 13

type StructureType = 
    | NoStructureType
    | Apt
    | House
    | Loft

type searchQuery = 
    { minPrice : int
      maxPrice : int
      region : Region
      structureType : StructureType }

type ReportPage = 
    { title : string
      rentals : list<Rental> }

type Downloader = 
    { cache : Map<string, Rental>
      reportPages : Map<string, ReportPage>
      cachePath : string }
