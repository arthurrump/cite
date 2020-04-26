module Cite.Convert

open Cite.BibTex
open Cite.Crossref

let convertType crossrefType =
    match crossrefType with
    | "book-section" -> InCollection
    | "monograph" -> Misc
    | "report" -> TechReport
    | "peer-review" -> Article
    | "book-track" -> Book // ?
    | "journal-article" -> Article
    | "book-part" -> InBook
    | "other" -> Misc
    | "book" -> Book
    | "journal-volume" -> Book
    | "book-set" -> Book // ?
    | "reference-entry" -> Manual
    | "proceedings-article" -> InProceedings
    | "journal" -> Book // Maybe Proceedings?
    | "component" -> Misc
    | "book-chapter" -> InBook
    | "proceedings-series" -> Proceedings
    | "report-series" -> TechReport
    | "proceedings" -> Proceedings
    | "standard" -> Misc
    | "reference-book" -> Manual
    | "posted-content" -> Misc
    | "journal-issue" -> Book
    | "dissertation" -> PhdThesis // ?
    | "dataset" -> Misc
    | "book-series" -> Book // ?
    | "edited-book" -> Book
    | "standard-series" -> Misc
    | _ -> Misc

let getCitekey work =
    let name =
        [ work.Author; work.Editor; work.Chair; work.Translator ]
        |> Seq.concat
        |> Seq.tryHead
        |> Option.map (fun p -> p.Family)
        |> Option.defaultValue work.Publisher
    let year = work.Issued.[0]
    name + string year

let convertPerson person =
    [ Some person.Family; person.Given ]
    |> List.choose id
    |> String.concat ", "

let convertWork work =
    let entryType = convertType work.Type
    { Citekey = getCitekey work
      EntryType = entryType
      CiteFilename = None
      Title = work.Title |> List.tryHead
      Author = work.Author |> List.map convertPerson
      Editor = work.Editor |> List.map convertPerson
      Publisher = Some work.Publisher
      Year = work.Issued |> List.tryHead |> Option.map string
      Month = work.Issued |> List.tryItem 1 |> Option.map string
      Day = work.Issued |> List.tryItem 2 |> Option.map string
      Institution = None
      Address = None
      Organization = None
      School = None
      Booktitle = 
        if [ InBook; InCollection; InProceedings; Conference; Misc ] |> List.contains entryType
        then work.ContainerTitle |> List.tryHead
        else None
      Chapter = None
      Edition = None
      Series = None
      Journal =
        if entryType = Article
        then work.ContainerTitle |> List.tryHead
        else None
      Number = work.Issue
      Volume = work.Volume
      Pages = work.Page
      Type = None
      HowPublished = None
      Annote = None
      Note = None
      DOI = Some work.DOI
      ISSN = None
      ISBN = work.ISBN |> List.tryHead
      URL = None }
