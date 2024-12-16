# PDF Search Application | FindInPDFs
### Build by No1Knows Technology

## Overview

PDF Search is a Windows desktop application that allows you to index and search through PDF files in a specified directory. Leveraging Lucene.NET for powerful full-text indexing and searching, this application provides a simple and efficient way to find content across multiple PDF documents.

## Features

- 🔍 Full-text PDF search across multiple files
- 📂 Directory-based indexing
- 🚀 Parallel processing for fast indexing
- 📄 Page-level search results
- 🖥️ Easy-to-use Windows Forms interface
- 💾 Persistent indexing with metadata tracking

## Prerequisites

- Windows OS
- .NET Framework
- Adobe Acrobat Reader (recommended, but optional)

## Dependencies

- Lucene.Net (v4.8)
- System.Text.Json
- Windows Forms

## Installation

1. Clone the repository
```bash
git clone https://github.com/yourusername/pdf-search.git
```

2. Open the solution in Visual Studio

3. Restore NuGet packages

4. Build the solution

## How to Use

### Indexing PDFs

1. After Installing the application.
2. Navigate to your specified directory where you have PDFs stored.
3. Right-Click on the folder name and select option `Open FindInPDFs here`.
4. After application launch you will be able to search any keyword or term. (On first launch application will take more time to index the PDF files in that directory and subdirectories).

### Searching PDFs

1. Enter a search term in the search box
2. Press Enter or click the Search button
3. View search results in the grid
4. Double-click a result to open the PDF at the specific page (Open page only work with Adobe Acrobat.)

## Key Components

### LuceneIndexer
- Manages PDF file indexing
- Stores indexes in the user's local application data
- Supports parallel processing
- Tracks file modifications to optimize re-indexing

### LuceneSearcher
- Performs full-text searches across indexed PDFs
- Returns page-level search results
- Uses Lucene.NET's advanced search capabilities

### Notable Features

- Unique index folder generation using SHA256 hashing
- Metadata tracking to avoid re-indexing unchanged files
- Thread-safe indexing and metadata updates
- Error handling and logging

## Configuration

- Index storage location: `%LocalAppData%\No1Knows\Index`
- PDF viewer: Defaults to Adobe Acrobat, falls back to system default

## Performance Considerations

- Initial indexing might take time depending on the number and size of PDFs
- Subsequent searches are very fast due to pre-built indexes
- Parallel processing helps speed up initial indexing

## Troubleshooting

- Ensure PDF files are readable and not password-protected
- Check console output for detailed indexing and search logs
- Use "Clean Indexes" button if you encounter persistent issues

## Contributing

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## License

Distributed under the MIT License. See `LICENSE` for more information.

## Contact

Syed Nadeem Hussain (Snag) - contact.no1knows@gmail.com | imsnag.1@gmail.com

Project Link: [https://github.com/Snag-hub/PDFSearch](https://github.com/Snag-hub/PDFSearch)

## Acknowledgments

- [Lucene.NET](https://lucenenet.apache.org/)
- [.NET Community](https://dotnet.microsoft.com/)
