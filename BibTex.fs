module Cite.BibTex
open System.IO

// Resources for understanding BibTex
// https://www.bibtex.com/format/
// http://tug.ctan.org/info/bibtex/tamethebeast/ttb_en.pdf

type BibTexEntryType =
    /// Any article published in a periodical
    | Article
    /// A book
    | Book
    /// Like a book, but without a publisher
    | Booklet
    /// A conference paper
    | Conference
    /// A section or chapter in a book
    | InBook
    /// An article in a collection (titled section in a book)
    | InCollection
    /// A conference paper (same as Conference)
    | InProceedings
    /// A technical manual
    | Manual
    /// A Masters thesis
    | MastersThesis
    /// When nothing else fits
    | Misc
    /// A PhD thesis
    | PhdThesis
    /// The whole conference proceedings
    | Proceedings
    /// A technical report, government report or white paper
    | TechReport
    /// A work that has not yet been officially published
    | Unpublished

type BibTexEntry =
    { /// The key to reference this entry in a text
      Citekey: string
      /// The type of this entry
      EntryType: BibTexEntryType
      /// (Non-standard) Filename for which Cite generated this item
      CiteFilename: string option
      /// Title of the work
      Title: string option
      /// List of authors of the work, where the authors are names in BibTex format
      Author: string list
      /// List of editors of the work, where the editors are names in BibTex format
      Editor: string list
      /// The name of the publisher
      Publisher: string option
      /// Year of publication or writing (can be a range, separated by --)
      Year: string option
      /// Month of publication or writing (preferred: three letter abbreviation, other options possible)
      Month: string option
      /// (Non-standard) Day of the month of writing or publication
      Day: string option
      /// Name of the institution that published or sponsored a report (only for type TechReport)
      Institution: string option
      /// Address of publisher or institution, usually just the city and/or state/country
      Address: string option
      /// Organization that organised or sponsored the conference (only for manuals and conference papers)
      Organization: string option
      /// The school or university awarding the degree for theses
      School: string option
      /// The title of the containing book for InBook and InCollection
      Booktitle: string option
      /// The chapter number for the section in a book
      Chapter: string option
      /// The edition of a book or manual (usually an ordinal, but numbers are sometimes used too)
      Edition: string option
      /// Name of a series or set of books, for Book or InX type entries
      Series: string option
      /// Name of the journal in which an article was published
      Journal: string option 
      /// The nubmer of a report, or the issue of a journal
      Number: string option
      /// The volume number of a journal or multi-volume book
      Volume: string option
      /// Page numbers or range, for article in an issue of a journal, or section in a book (use -- for a range)
      Pages: string option
      /// The type of report (eg Government Report) or the type of thesis
      Type: string option
      /// Indicator for unusual publications of how it was published, really only for Misc type entries
      HowPublished: string option
      /// An annotation (brief, descriptive paragraph) for use in an annotated bibliography
      Annote: string option
      /// Additional data, often used for URLs
      Note: string option
      /// (Non-standard) Digital Object Identifier for the work
      DOI: string option
      /// (Non-standard) International Standard Serial Number for a journal or magazine
      ISSN: string option
      /// (Non-standard) International Standard Book Number for a book or report
      ISBN: string option
      /// (Non-standard) The URL of a webpage
      URL: string option }

module Render =
    let notEmpty ls = not (List.isEmpty ls)

    let render (value: 'a) (w: TextWriter) = w.Write(value); w
    let newline (w: TextWriter) = w.WriteLine(); w
    let renderCond condition render = if condition then render else id
    let renderOptional optionValue render =
        match optionValue with
        | Some value -> render value
        | None -> id

    let renderEntryType = function
        | Article -> render "article"
        | Book -> render "book"
        | Booklet -> render "booklet"
        | Conference -> render "conference"
        | InBook -> render "inbook"
        | InCollection -> render "incollection"
        | InProceedings -> render "inproceedings"
        | Manual -> render "manual"
        | MastersThesis -> render "mastersthesis"
        | Misc -> render "misc"
        | PhdThesis -> render "phdthesis"
        | Proceedings -> render "proceedings"
        | TechReport -> render "techreport"
        | Unpublished -> render "unpublished"

    let renderFieldDecl (name: string) =
        render "  " >> render name >> render " = "

    let renderField name value =
        renderFieldDecl name
        >> render "{"
        >> render value // TODO: escape the value
        >> render "},"
        >> newline

    let renderOptionalField name value =
        renderOptional value (renderField name)

    let renderPersonList names =
        if names |> List.isEmpty then 
            id 
        else 
            let renderNames =
                names 
                |> List.map render 
                |> List.reduce (fun a b -> a >> render " and " >> b)
            render "{" >> renderNames >> render "}"

    let renderEntry entry =
        render "@" >> renderEntryType entry.EntryType >> render "{" >> render entry.Citekey >> render "," >> newline
        >> renderOptionalField "cite_filename" entry.CiteFilename
        >> renderOptionalField "title" entry.Title
        >> renderCond (notEmpty entry.Author) 
            (renderFieldDecl "author" >> renderPersonList entry.Author >> render "," >> newline)
        >> renderCond (notEmpty entry.Editor) 
            (renderFieldDecl "editor" >> renderPersonList entry.Editor >> render "," >> newline)
        >> renderOptionalField "publisher" entry.Publisher
        >> renderOptionalField "year" entry.Year
        >> renderOptionalField "month" entry.Month
        >> renderOptionalField "day" entry.Day
        >> renderOptionalField "institution" entry.Institution
        >> renderOptionalField "address" entry.Address
        >> renderOptionalField "organization" entry.Organization
        >> renderOptionalField "school" entry.School
        >> renderOptionalField "booktitle" entry.Booktitle
        >> renderOptionalField "chapter" entry.Chapter
        >> renderOptionalField "edition" entry.Edition
        >> renderOptionalField "series" entry.Series
        >> renderOptionalField "journal" entry.Journal
        >> renderOptionalField "number" entry.Number
        >> renderOptionalField "volume" entry.Volume
        >> renderOptionalField "pages" entry.Pages
        >> renderOptionalField "type" entry.Type
        >> renderOptionalField "howpublished" entry.HowPublished
        >> renderOptionalField "annote" entry.Annote
        >> renderOptionalField "note" entry.Note
        >> renderOptionalField "doi" entry.DOI
        >> renderOptionalField "issn" entry.ISSN
        >> renderOptionalField "isbn" entry.ISBN
        >> renderOptionalField "url" entry.URL
        >> render "}" >> newline >> newline
