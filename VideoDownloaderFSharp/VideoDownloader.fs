namespace VideoDownloaderFSharp

open System.IO
open System
open WebRobotFramework

type VideoDownloaderService = 
    inherit (WebRobotService)
    member this.Name = "Video Downloader"

    member this.DownloadPianoVideos = 
        let DownloadPath = @"G:\\Videos\\Piano\\"
        let selenium = SeleniumInterface()
        let client = new WebClient()

        let processLink completedLinks linkIndex linkCount link =
            let saveWebPage title =
                Logger.Log(sprintf "Saving page %s.htm" title)
                File.WriteAllText(sprintf "%s%s.htm" DownloadPath title, selenium.Driver.PageSource)

            let removeInvalidCharacters (s:string) =
                let c = Array.append (Path.GetInvalidFileNameChars()) (Path.GetInvalidPathChars())
                s.ToCharArray() |> Array.filter (fun x -> not <| Array.exists ((=) x) c ) |> string

            let processFrame i =
                match selenium.SelectNode(".//video", i) with
                | v when v <> null && v.GetAttributeValue("src") <> "" ->
                    let title = removeInvalidCharacters (selenium.SelectNode(".//div[@class='title']//div[@class='headers']//h1", i).InnerText.Trim())
                    let uri = Uri(v.GetAttributeValue("src"))
                    let f = Path.GetFileNameWithoutExtension(uri.LocalPath).ToLower()
                    let fs = Directory.EnumerateFiles(DownloadPath, "*.*", SearchOption.TopDirectoryOnly) |> Seq.map (fun x -> Path.GetFileName(x).ToLower())
                    if not <| Seq.exists ((=) f) fs then
                        Logger.Log(sprintf"Downloading video '%s'..." title)
                        File.Delete(sprintf "%sTemp.mp4" DownloadPath)
                        client.DownloadFile(v.GetAttributeValue("src"), sprintf "%sTemp.mp4" DownloadPath)
                        File.Move(sprintf "%sTemp.mp4" DownloadPath, sprintf "%s%s (%s).mp4" DownloadPath title f)
                    else Logger.Log(sprintf "Video '%s' already downloaded." title)
                    //saveWebPage t
                    true
                | _ -> false

            Logger.Log(sprintf "Downloading page '%s' (%d/%d)..." link linkIndex linkCount)
            selenium.GoTo(link, 5000) |> ignore
            match List.fold (fun pageSaved i -> pageSaved || processFrame i) false [0..selenium.BaseFrameCount] with
                | false -> removeInvalidCharacters (selenium.SelectNode(".//title").InnerText.Trim()) |> saveWebPage
                | true -> ()
            File.WriteAllLines(sprintf "%sCompleted.txt" DownloadPath, Set.toArray completedLinks)
            (completedLinks.Add(link), linkIndex + 1)

        Logger.Log("Downloading main page...")
        let completed = File.ReadAllLines (sprintf "%sCompleted.txt" DownloadPath) |> Set.ofSeq
        let toProcess = selenium.GoTo("http://pianocareeracademy.com/forum/index.php/topic,207.msg1539.html").SelectNodes("//div[@class='post']") |> Seq.collect (fun p -> p.SelectNodes(".//a") |> Seq.map (fun l -> l.GetAttributeValue("href", ""))) |> Set.ofSeq |> Set.difference completed
        Logger.Log(sprintf "Found %d links." <| Set.count toProcess)
        let rec processLinks list state =
            match list with
            | head :: tail -> processLink (fst(state)) (snd(state)) toProcess.Count head |> processLinks tail
            | [] -> ()
        processLinks (Set.toList toProcess) (completed, 0)

    override this.ProcessCommands(task, command) =
        this.ProcessCommand(task, command, "piano", "", fun args -> this.DownloadPianoVideos )