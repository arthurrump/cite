# Cite

Automatic generation of BibTex references based on descriptive PDF filenames, using the excellent [Crossref](https://www.crossref.org/) [metadata retrieval](https://www.crossref.org/services/metadata-retrieval/) [API](https://github.com/CrossRef/rest-api-doc). The program reads a list of all PDF files in a folder and uses the filename to query Crossref for the related metadata. This obviously works based if your PDF files have descriptive filenames, ideally including the full title and author, or some identifier. It will only find data for papers that have a DOI registration.