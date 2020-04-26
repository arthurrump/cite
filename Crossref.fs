module Cite.Crossref

open System
open System.Net.Http
open Thoth.Json.Net
open System.Threading

/// A partial date is an ordered list of year, month, day, where only year is required
type PartialDate = int list

type CrossrefContributor =
    { /// Family name of the person
      Family: string
      /// Optional given name(s) of the person
      Given: string option
      /// List of affeliation names (optional)
      Affiliation: string list }

type CrossrefWork =
    { /// All titles of the work, including translated
      Title: string list
      /// Short or abbreviated work titles (optional)
      ShortTitle: string list
      /// Subtitles, including translated (optional)
      Subtitle: string list
      /// The authors of the work (optional)
      Author: CrossrefContributor list
      /// Editors of the work (optional)
      Editor: CrossrefContributor list
      /// Chair for the work (optional)
      Chair: CrossrefContributor list
      /// Translators of the work (optional)
      Translator: CrossrefContributor list
      /// The publisher of the work
      Publisher: string
      /// DOI of the work
      DOI: string
      /// Type of work, one of https://api.crossref.org/v1/types
      Type: string
      /// Date of first publication, either print or online
      Issued: PartialDate
      /// Full titles of the containing work (book or journal) (optional)
      ContainerTitle: string list
      // Abbreviated titles of the containing work (optional)
      ShortContainerTitle: string list
      /// Issue number of an article's journal (optional)
      Issue: string option
      /// Volume number of an article's journal (optional)
      Volume: string option
      /// Page numbers of an article within its journal (optional)
      Page: string option
      /// ISBN for the work (optional)
      ISBN: string list }

module Decode = 
    let private optionalList o = Option.defaultValue [] o

    let partialDate: Decoder<PartialDate> = 
        Decode.field "date-parts" (Decode.list (Decode.list Decode.int)) 
        |> Decode.map List.exactlyOne

    let crossrefContributor: Decoder<CrossrefContributor> =
        Decode.object (fun get ->
            { Family = get.Required.Field "family" Decode.string
              Given = get.Optional.Field "given" (Decode.string)
              Affiliation = get.Optional.Field "affiliation" (Decode.list (Decode.field "name" Decode.string)) |> optionalList })
    
    let crossrefWork: Decoder<CrossrefWork> =
        Decode.object (fun get ->
            { Title = get.Required.Field "title" (Decode.list Decode.string)
              ShortTitle = get.Optional.Field "short-title" (Decode.list Decode.string) |> optionalList
              Subtitle = get.Optional.Field "subtitle" (Decode.list Decode.string) |> optionalList
              Author = get.Optional.Field "author" (Decode.list crossrefContributor) |> optionalList
              Editor = get.Optional.Field "editor" (Decode.list crossrefContributor) |> optionalList
              Chair = get.Optional.Field "chair" (Decode.list crossrefContributor) |> optionalList
              Translator = get.Optional.Field "translator" (Decode.list crossrefContributor) |> optionalList
              Publisher = get.Required.Field "publisher" Decode.string
              DOI = get.Required.Field "DOI" Decode.string
              Type = get.Required.Field "type" Decode.string
              Issued = get.Required.Field "issued" partialDate
              ContainerTitle = get.Optional.Field "container-title" (Decode.list Decode.string) |> optionalList
              ShortContainerTitle = get.Optional.Field "short-container-title" (Decode.list Decode.string) |> optionalList
              Issue = get.Optional.Field "issue" Decode.string
              Volume = get.Optional.Field "volume" Decode.string
              Page = get.Optional.Field "page" Decode.string
              ISBN = get.Optional.Field "ISBN" (Decode.list Decode.string) |> optionalList })

    let crossrefResponse: Decoder<CrossrefWork list> = 
        Decode.object (fun get ->
            match get.Required.Field "message-type" Decode.string with
            | "work-list" -> get.Required.At [ "message"; "items" ] (Decode.list crossrefWork)
            | "work" -> [get.Required.Field "message" crossrefWork]
            | other -> get.Required.Field "message-type" (Decode.fail (sprintf "Unknown value for field \"message-type\": \"%s\"" other)))

type private Limit = { Limit: int; Interval: int }

type CrossrefClient() =
    let random = Random()
    let http = new HttpClient()
    do http.DefaultRequestHeaders.Add("User-Agent", "Cite/0.1 (mailto: hello@arthurrump.com)")

    let limitLock = obj()
    let mutable limit: Limit option = None
    let mutable requestCounter = {| Start = DateTime.UnixEpoch; Counter = 0 |}

    let shouldWait () = lock limitLock (fun () -> 
        match limit with
        | None -> 0
        | Some limit ->
            let resume = requestCounter.Start.AddSeconds(float limit.Interval)
            let now = DateTime.UtcNow
            if resume > now then
                if requestCounter.Counter < limit.Limit then 
                    requestCounter <- {| requestCounter with Counter = requestCounter.Counter + 1 |}
                    0
                else 
                    (resume - now).Milliseconds + random.Next(1, 50)
            else 
                requestCounter <- {| Start = DateTime.UtcNow; Counter = 1 |}
                0
    )

    member _.Query (query: string, ?offset: int, ?ct: CancellationToken) =
        let ct = defaultArg ct Async.DefaultCancellationToken
        let url = sprintf "https://api.crossref.org/works?query=%s&rows=1&offset=%i" query (defaultArg offset 0)
        async {
            do! async {
                let mutable wait = shouldWait()
                while wait > 0 && not ct.IsCancellationRequested do
                    do! Async.Sleep wait
                    wait <- shouldWait()
            }

            let! res = http.GetAsync(url, ct) |> Async.AwaitTask

            match res.Headers.TryGetValues("X-Rate-Limit-Limit") with
            | (true, limitValue) -> 
                limit <- Some
                    { Limit = Int32.Parse(limitValue |> Seq.head)
                      Interval = Int32.Parse(res.Headers.GetValues("X-Rate-Limit-Interval") |> Seq.head |> (fun s -> s.TrimEnd('s'))) }
            | (false, _) -> ()

            let! body = res.Content.ReadAsStringAsync() |> Async.AwaitTask
            if res.IsSuccessStatusCode then
                return Decode.fromString Decode.crossrefResponse body
            else
                return Error (sprintf "HTTP Error: %O\nBody: %s" res.StatusCode body)
        }

    interface IDisposable with
        member _.Dispose() =
            http.Dispose()
