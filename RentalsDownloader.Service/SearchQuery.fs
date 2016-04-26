module SearchQuery

open HtmlAgilityPack
open System
open System.Text.RegularExpressions
open Utilities
open WebRobotFramework

let structureTypeQueryStrings = 
    [ { enum = StructureType.Apt
        string = "0%2C12%2C13%2C10%2C20%2C8%2C9%2C21" }
      { enum = StructureType.House
        string = "0%2C7%2C11" }
      { enum = StructureType.Loft
        string = "0%2C16" } ]

let listingIdRegex = new Regex("/listingdetail/(\\d+)")
let toString query = String.Format("{0} in {1} between {2} and {3}", query.structureType, query.region, query.minPrice, query.maxPrice)
let isValid query = query.region <> Region.None && query.structureType <> StructureType.NoStructureType
let itemsXPath = "//div[@class='image-wrapper']/a"

let retrieveItemIds (selenium : SeleniumInterface) query pageIndex = 
    let getItemId (node : HtmlNode) = listingIdRegex.Match(node.GetAttributeValue("href", "")).Groups.[1].Captures.[0].Value
    // logger
    let url = 
        String.Format
            ("http://www.westsiderentals.com/return-guest-results.cfm?pagenum={0}&resultlayout=photo&totalrecords=0&perpage=36&region_ID={1}&priceLow={2}&priceHigh={3}&property_type_id={4}&searchType=g&x=0&y=0", pageIndex, int query.region, 
             query.minPrice, query.maxPrice, matchEnum query.structureType structureTypeQueryStrings)
    selenium.GoTo(url) |> ignore
    let ids = selenium.SelectNodes(itemsXPath) |> Seq.map (fun n -> getItemId n)
    // logger
    ids
