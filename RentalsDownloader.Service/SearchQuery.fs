module SearchQuery

open HtmlAgilityPack
open System
open System.Text.RegularExpressions
open WebRobotFramework
open Utilities

type Region = 
    | None = 0
    | SantaMonica = 1
    | Hollywood = 5
    | Downtown = 13

type StructureType = 
    | None
    | Apt
    | House
    | Loft

type searchQuery = 
    { minPrice : int
      maxPrice : int
      region : Region
      structureType : StructureType }

let itemsXPath = "//div[@class='image-wrapper']/a"
let listingIdRegex = new Regex("/listingdetail/(\\d+)")

let structureTypeQueryStrings = 
    [ { enum = StructureType.Apt
        string = "0%2C12%2C13%2C10%2C20%2C8%2C9%2C21" }
      { enum = StructureType.House
        string = "0%2C7%2C11" }
      { enum = StructureType.Loft
        string = "0%2C16" } ]

let getItemId (node : HtmlNode) = listingIdRegex.Match(node.GetAttributeValue("href", "")).Groups.[1].Captures.[0].Value
let toUrl query index = 
    String.Format
        ("http://www.westsiderentals.com/return-guest-results.cfm?pagenum={0}&resultlayout=photo&totalrecords=0&perpage=36&region_ID={1}&priceLow={2}&priceHigh={3}&property_type_id={4}&searchType=g&x=0&y=0", index, int query.region, 
         query.minPrice, query.maxPrice, matchEnum query.structureType structureTypeQueryStrings)
let toString query = String.Format("{0} in {1} between {2} and {3}", query.structureType, query.region, query.minPrice, query.maxPrice)
let isValid query = query.region <> Region.None && query.structureType <> StructureType.None

let retrieveItemIds (selenium : SeleniumInterface) query pageIndex = 
    // logger
    let url = toUrl query pageIndex
    selenium.GoTo(url) |> ignore
    let ids = selenium.SelectNodes(itemsXPath) |> Seq.map (fun n -> getItemId n)
    // logger
    ids
