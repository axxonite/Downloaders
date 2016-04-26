module Downloader

open System.IO
open Utilities

let create cachePath = 
    let cacheFiles = Directory.EnumerateFiles(cachePath, "*.xml")
    { cachePath = cachePath
      reportPages = Map.empty
      cache = 
          cacheFiles
          |> Seq.map (fun f -> Rental.xmlSerializer.Deserialize(new FileStream(f, FileMode.Open)) :?> Rental)
          |> Seq.map (fun r -> (r.download.id, r))
          |> Map.ofSeq }

let favoriteNeighborhoods = [ "Echo Park"; "Los Feliz"; "Silver Lake"; "Hancock Park"; "Hollywood"; "West Hollywood" ]

let download (downloader : Downloader) selenium query = 
    let addToReportPage title rental reportPages = 
        let page = reportPages |> getOrCreate title (fun () -> ReportPage.create title)
        let page' = { page with rentals = rental :: page.rentals }
        reportPages.Add(title, page')
    
    let keepItem rental = (rental.squareFootageInt = 0 || rental.squareFootageInt >= 1100) && not rental.photos.IsEmpty && rental.acceptsCats
    
    let retrieveRental rentalId downloader = 
        let cache, rental = 
            if downloader.cache.ContainsKey(rentalId) then (downloader.cache, downloader.cache.[rentalId])
            else 
                let url = Rental.getUrlForId rentalId
                let rental = Rental.download selenium downloader.cachePath url
                (downloader.cache.Add(rentalId, rental), rental)
        
        let reportPages = 
            if keepItem rental then 
                downloader.reportPages
                |> addToReportPage (query.region.ToString()) rental
                |> addToReportPage (query.region.ToString() + " - " + rental.neighborhood) rental
                |> if favoriteNeighborhoods |> List.contains rental.neighborhood then addToReportPage "_Favorite Neighborhoods" rental
                   else id
            else downloader.reportPages
        
        { downloader with cache = cache
                          reportPages = reportPages }
    
    let rec downloadItemsPage pageIndex downloader = 
        let ids = SearchQuery.retrieveItemIds selenium query pageIndex
        if ids |> Seq.isEmpty then downloader
        else 
            let downloader' = ids |> Seq.fold (fun d id -> retrieveRental id d) downloader
            downloadItemsPage (pageIndex + 1) downloader'
    
    if not (SearchQuery.isValid query) then downloader
    else downloadItemsPage 0 downloader
