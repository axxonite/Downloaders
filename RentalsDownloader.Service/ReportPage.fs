module ReportPage

open OfficeOpenXml
open System
open System.IO
open WebRobotFramework

let downloadPath = @"C:\Rentals\"

let create title = 
    { title = title
      rentals = list.Empty }

let save page = 
    if page.rentals |> List.isEmpty then ()
    else 
        let loadSpreadSheet rentals = 
            let fileInfo = new FileInfo(String.Format("{0}_Favorite Neighborhoods.xlsx", downloadPath))
            if not fileInfo.Exists then ()
            else 
                use spreadSheet = new ExcelPackage(fileInfo)
                let workSheet = new WorkSheet(spreadSheet.Workbook.Worksheets.["Rentals"])
                let rentalsDictionary = rentals |> toDict (fun r -> r.download.url)
                
                let loadRow row = 
                    let link = workSheet.Worksheet.Cells.[row, workSheet.Columns.["Link"]].Formula
                    let found, rental = rentalsDictionary.TryGetValue(link)
                    if not found then ()
                    else 
                        let getWorksheetValue column existingValue = 
                            if workSheet.Columns.ContainsKey(column) then workSheet.Worksheet.Cells.[row, workSheet.Columns.[column]].Text
                            else existingValue
                        
                        let rentals' = 
                            { rental with comments = getWorksheetValue "Comments" rental.comments
                                          rating = getWorksheetValue "R" rental.rating
                                          visited = getWorksheetValue "V" rental.visited
                                          called = getWorksheetValue "C" rental.called }
                        
                        rentalsDictionary.[link] = rentals' |> ignore
                for row in [ 2..workSheet.Worksheet.Dimension.End.Row ] do
                    loadRow row
        
        let saveSpreadSheet rentals = 
            use spreadSheet = new ExcelPackage(new FileInfo(String.Format(@"{0}template\Rentals.xlsx", downloadPath)))
            let fileInfo = new FileInfo(String.Format("{0}{1}.xlsx", downloadPath, page.title))
            let workSheet = new WorkSheet(spreadSheet.Workbook.Worksheets.["Rentals"])
            let rentalsWithRowIndices = rentals |> assignIndicesFunc (fun i -> i + 2)
            for (i, r) in rentalsWithRowIndices do
                Rental.saveToSpreadsheet workSheet i r
            spreadSheet.SaveAs(fileInfo)
        
        loadSpreadSheet page.rentals
        let rentals = page.rentals |> List.sortBy (fun r -> (-r.ratingInt, boolToInvInt (r.acceptsCats), r.rentPerSqFt, -r.squareFootageInt, -r.bedroomsInt, boolToInvInt r.hasParking, r.rentDouble))
        let rentalHtml = fst (rentals |> List.fold (fun (result, i) rental -> (result + Rental.toHtml i rental, i + 1)) ("", 0))
        File.WriteAllText(String.Format("{0}{1}.html", downloadPath, page.title), Rental.reportTemplate.Replace("[Rentals]", rentalHtml))
        saveSpreadSheet rentals
