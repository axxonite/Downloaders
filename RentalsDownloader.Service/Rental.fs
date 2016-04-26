module Rental

open Common
open System
open System.IO
open System.Text
open System.Text.RegularExpressions
open System.Xml.Serialization
open WebRobotFramework

let numberRegex = new Regex("(\\d[\\d\\.,]*)")
let neighboorhoodRegex = new Regex(@"(?:Los Angeles */ *)?([\w\s]+)")
let getUrlForId (id : string) = String.Format("http://www.westsiderentals.com/listingdetail/{0}/", id)
let xmlSerializer = new XmlSerializer(typeof<Rental>)

let download (selenium : SeleniumInterface) fileCachePath id = 
    let url = getUrlForId id
    
    let parse() = 
        let getNode xpath = 
            let node = selenium.SelectNode(xpath)
            if node = null then ""
            else node.InnerText.Trim()
        
        let selectNodes xpath f = 
            selenium.SelectNodes(xpath)
            |> Seq.map (f)
            |> Seq.toList
        
        let getDoubleValue s = 
            let m = numberRegex.Match(s)
            if not m.Success then 0.0
            else 
                let parsed, result = Double.TryParse(m.Groups.[1].Captures.[0].Value)
                if parsed then result
                else 0.0
        
        let details = selectNodes "//div[@class='col-md-6']" (fun l -> Regex.Replace(l.InnerText, "[\r\t\n]", ""))
        
        let getRentalItem prefix = 
            let attrib = details |> Seq.tryFind (fun attrib -> attrib.StartsWith(prefix + ":", StringComparison.Ordinal))
            match attrib with
            | Some a -> a.Trim()
            | None -> ""
        
        let location = getRentalItem "City/Area"
        let amenities = selectNodes "//div[contains(@class,'address-info')]//li" (fun l -> l.InnerText)
        let bedrooms = getRentalItem "Bedrooms"
        let parking = getRentalItem "Parking"
        let rent = getRentalItem "Rent"
        let rentDouble = getDoubleValue rent
        let squareFootage = getRentalItem "Square Footage"
        let squareFootageInt = int (getDoubleValue squareFootage)
        let pets = getRentalItem "Pets"
        
        let rentPerSqFt = 
            match (squareFootageInt, rentDouble) with
            | (sqFt, rent) when sqFt > 0 && rent > 0.0 -> Some(rent / double sqFt)
            | _ -> None
        { download = 
              { id = id
                url = url }
          title = getNode "//div[contains(@class,'address-info')]//h1"
          location = location
          neighborhood = neighboorhoodRegex.Match(location).Groups.[1].Captures.[0].Value
          address = getNode "//div[contains(@class, 'address-info')]/div[@class='bigText']"
          listingInfo = getNode "//div[contains(@class,'address-info')]//h3"
          amenities = amenities
          photos = selectNodes "//div[@id='carousel']//img" (fun l -> l.GetAttributeValue("src", ""))
          contact = getNode "//div[contains(@class, 'address-info')]/div[@class='medText']"
          email = getNode "//div[contains(@class, 'address-info')]//a[@class='mailtoLink']"
          walkScore = getNode "//div[@class='walkscore']//span"
          rent = rent
          rentDouble = rentDouble
          squareFootage = squareFootage
          squareFootageInt = squareFootageInt
          rentPerSqFt = rentPerSqFt
          bedrooms = bedrooms
          bedroomsInt = int (getDoubleValue bedrooms)
          pets = pets
          acceptsCats = pets <> "No pets" && pets <> "Dog ok"
          structureType = getRentalItem "Structure Type"
          parking = parking
          hasParking = parking <> "Street parking"
          available = getRentalItem "Available"
          bathrooms = getRentalItem "Bathrooms"
          furnished = getRentalItem "Furnished"
          leaseType = getRentalItem "Lease Type"
          unitDetails = getRentalItem "Unit Details"
          listingType = getRentalItem "Listing Type"
          deposit = "" // todo fill
          called = ""
          comments = ""
          rating = ""
          ratingInt = 0
          visited = "" }
    
    let saveFileToCache rental = 
        use writer = new StreamWriter(String.Format("{0}\\{1}.xml", fileCachePath, rental.download.id))
        xmlSerializer.Serialize(writer, rental)
    
    selenium.GoTo(url) |> ignore
    let rental = parse()
    saveFileToCache rental
    rental

let saveToSpreadsheet (worksheet : WorkSheet) row rental = 
    let strContainsAny (s : string) a = a |> List.exists (fun a' -> s.Contains(a', StringComparison.OrdinalIgnoreCase))
    let setCell a b = worksheet.SetCell(row, a, b)
    let setCellFormula a b = worksheet.SetCellFormula(row, a, b)
    setCell "Address" rental.address
    setCell "Neighborhood" rental.neighborhood
    setCell "SqFeet" rental.squareFootageInt
    setCell "Rooms" rental.bedroomsInt
    setCell "Price" rental.rentDouble
    setCell "Structure" rental.unitDetails
    setCell "Comments" rental.comments
    setCell "V" rental.visited
    setCell "C" rental.called
    if rental.rating <> "" then setCell "R" rental.ratingInt
    setCellFormula "Link" (String.Format("HYPERLINK(\"{0}\",\"Link\")", rental.download.url))
    setCellFormula "Map" (String.Format("=HYPERLINK(\"http://maps.google.com/?q={0}\",\"Map\")", rental.address))
    setCellFormula "Redfin" (String.Format("=HYPERLINK(\"https://www.google.com/search?q={0} Redfin\",\"Redfin\")", rental.address))
    setCell "Contact" rental.contact
    let hasDishwasher = 
        if rental.amenities |> List.exists (fun a -> strContainsAny a [ "dishwasher" ]) then "X"
        else ""
    setCell "D" hasDishwasher
    let hasLaundry = 
        match rental.amenities with
        | a when a |> List.forall (fun a -> not (strContainsAny a [ "laundry"; "washer"; "dryer" ])) -> ""
        | a when a |> List.exists (fun a -> strContainsAny a [ "hook" ]) -> "H"
        | _ -> "X"
    setCell "L" hasLaundry

let reportTemplate = File.ReadAllText(String.Format(@"{0}template\template.html", @"C:\Rentals\", Encoding.UTF8))
let rentalTemplate = Regex.Match(reportTemplate, "<rental>(.*)</rental>", RegexOptions.Singleline).Groups.[1].Captures.[0].Value
let photoTemplate = Regex.Match(reportTemplate, "<photo>(.*)</photo>", RegexOptions.Singleline).Groups.[1].Captures.[0].Value

let toHtml index rental = 
    let strReplace (a : string) b (s : string) = s.Replace(a, b)
    rentalTemplate
    |> strReplace "[Url]" rental.download.url
    |> strReplace "[Title]" (String.Format("{0}. {1}", index, rental.title))
    |> strReplace "[Location]" rental.location
    |> strReplace "[Price]" rental.rent
    |> strReplace "SquareFeet" (match rental.rentPerSqFt with
                                | Some x -> String.Format("{0} ({1:F2}$/sq.ft.)", rental.squareFootage, x)
                                | None -> rental.squareFootage)
    |> strReplace "[Bedrooms]" rental.bedrooms
    |> strReplace "[Availability]" rental.available
    |> strReplace "[Pets]" rental.pets
    |> strReplace "[Type]" rental.structureType
    |> strReplace "[Parking]" rental.parking
    |> strReplace "[Address]" rental.address
    |> strReplace "[Contact]" rental.contact
    |> strReplace "[Photos]" (rental.photos |> List.fold (fun s p -> s + photoTemplate.Replace("[PhotoURL]", p)) "")
