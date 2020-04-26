module Cite.Program

open Cite.BibTex
open Cite.Convert
open Cite.Crossref

open Argu
open System.IO

type Args =
    | [<AltCommandLine("-o")>] Output of path: string 
    interface IArgParserTemplate with
        member this.Usage = 
            match this with
            | Output _ -> "output bibtex file"

module Async =
    let map f x =
        async { let! x = x in return f x }

[<EntryPoint>]
let main argv =
    let argParser = ArgumentParser.Create<Args>(programName = "cite", errorHandler = ProcessExiter())
    let args = argParser.Parse argv

    use crossref = new CrossrefClient()

    let outputPath = args.GetResult (<@ Output @>, defaultValue = "cite.bib")
    let currentBib = File.ReadAllText(outputPath)

    let newFiles = 
        Directory.GetCurrentDirectory()
        |> Directory.GetFiles
        |> Seq.filter (fun file -> Path.GetExtension(file) = ".pdf" && not (currentBib.Contains(Path.GetFileName file)))

    let works =
        newFiles
        |> Seq.map (fun file -> Path.GetFileNameWithoutExtension(file) |> crossref.Query |> Async.map (fun w -> file, w))
        |> Async.Parallel
        |> Async.RunSynchronously

    let writeBib =
        works
        |> Seq.choose (fun res -> match res with file, Ok work -> work |> List.tryHead |> Option.map (fun w -> file, w) | _, Error _ -> None)
        |> Seq.map (fun (file, work) -> { convertWork work with CiteFilename = Some (Path.GetFileName file) })
        |> Seq.map Render.renderEntry
        |> Seq.fold (>>) id

    use stream = new FileStream(outputPath, FileMode.Append)
    use bibWriter = new StreamWriter(stream)
    do writeBib bibWriter |> ignore
    
    0
