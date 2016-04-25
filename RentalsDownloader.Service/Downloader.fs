module Downloader

open Rental
open SearchQuery
open System.Collections.Generic
open System.IO
open System.Xml.Serialization
open Utilities

type Downloader = 
    { cache : IDictionary<string, Rental>
      cachePath : string
      xmlSerializer : XmlSerializer }

let create cachePath = 
    let cacheFiles = Directory.EnumerateFiles(cachePath, "*.xml")
    let serializer = new XmlSerializer(typeof<Rental>)
    { cachePath = cachePath
      xmlSerializer = serializer
      cache = 
          cacheFiles
          |> Seq.map (fun f -> serializer.Deserialize(new FileStream(f, FileMode.Open)))
          |> Seq.cast<Rental>
          |> Seq.map (fun r -> (r.download.id, r))
          |> DictOfSeq }

let download downloader selenium query = 
    match isValid query with
    | false -> ()
    | true -> 
        // logger
        let retrieveItem downloader selenium id = 
            match downloader.cache.ContainsKey(id) with
            | true -> // Logger
                downloader.cache.[id]
            | false -> 
                // Logger
                let url = getUrlForId id
                let rental = download selenium downloader.cachePath url
                downloader.cache.Add(id, rental)
                rental
        
        let keepItem rental = (rental.squareFootageInt = 0 || rental.squareFootageInt >= 1100) && not rental.photos.IsEmpty && acceptsCats rental
        // todo add reports
        let itemDownloaded query rental = ()
        
        let retrieve downloader selenium id = 
            let rental = retrieveItem downloader selenium id
            match keepItem rental with
            | true -> itemDownloaded query rental
            | false -> ()
            ()
        
        let rec downloadItemsPage selenium query pageIndex = 
            let ids = retrieveItemIds selenium query pageIndex
            ids |> Seq.iter (fun id -> retrieve downloader selenium id)
            if ids |> Seq.isEmpty then ()
            else downloadItemsPage selenium query (pageIndex + 1)
        
        downloadItemsPage selenium query 0
