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

[<EntryPoint>]
let main argv =
    let argParser = ArgumentParser.Create<Args>(programName = "cite", errorHandler = ProcessExiter())
    let args = argParser.Parse argv

    use crossref = new CrossrefClient()

    let outputPath = args.GetResult (<@ Output @>, defaultValue = "cite.bib")

    let works = 
        Directory.GetCurrentDirectory()
        |> Directory.GetFiles
        |> Seq.filter (fun file -> Path.GetExtension(file) = ".pdf")
        |> Seq.map (fun file -> Path.GetFileNameWithoutExtension(file) |> crossref.Query)
        |> Async.Parallel
        |> Async.RunSynchronously

    let writeBib =
        works
        |> Seq.choose (fun res -> match res with Ok work -> Some work | Error _ -> None)
        |> Seq.concat
        |> Seq.map convertWork
        |> Seq.map Render.renderEntry
        |> Seq.reduce (>>)

    use stream = new FileStream(outputPath, FileMode.Create)
    use bibWriter = new StreamWriter(stream)
    do writeBib bibWriter |> ignore
    
    0
