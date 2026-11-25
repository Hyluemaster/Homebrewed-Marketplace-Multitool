using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Homebrewed_Marketplace_Multitool.Models.Items;

namespace Homebrewed_Marketplace_Multitool.Models.Services;

public static class BookDocumentReader
{
    private static readonly System.Collections.Generic.HashSet<string> TextExtensions =
    new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt", ".md", ".log", ".csv", ".tsv", ".json", ".xml", ".html", ".htm",
        ".css", ".scss", ".js", ".jsx", ".ts", ".tsx", ".py", ".cs", ".fs", ".vb",
        ".java", ".c", ".h", ".cpp", ".hpp", ".go", ".rs", ".php", ".rb", ".sql",
        ".sh", ".ps1", ".yaml", ".yml", ".toml", ".ini", ".config", ".bat", ".cmd",
        ".kt", ".kts", ".swift", ".dart", ".lua", ".r", ".pl", ".properties", ".gradle",
        ".gitignore", ".dockerfile", ".diff", ".patch"
    };

    public static string Read(Book book)
    {
        if (book.Content is null || book.Content.Length == 0)
        {
            return "This book has no file contents stored.";
        }

        var extension = Path.GetExtension(book.Name).ToLowerInvariant();
        return extension switch
        {
            ".docx" => ReadWordDocument(book.Content),
            ".pptx" => ReadPresentation(book.Content),
            ".xlsx" => ReadSpreadsheet(book.Content),
            ".doc" or ".ppt" or ".xls" =>
                "Inline preview is not available for legacy Office files (.doc, .ppt, .xls).",
            ".pdf" => "Inline PDF preview is not available in the current reader.",
            ".png" or ".jpg" or ".jpeg" or ".gif" or ".bmp" or ".zip" or ".accdb" =>
                $"Inline preview is not available for {extension} files.",
            _ when TextExtensions.Contains(extension) => ReadText(book.Content, extension),
            _ => $"Inline preview is not available for {extension} files."
        };
    }

    private static string ReadText(byte[] content, string extension)
    {
        if (content.Take(Math.Min(content.Length, 4096)).Any(value => value == 0))
        {
            return $"Inline preview is not available for binary {extension} files.";
        }

        using var stream = new MemoryStream(content, writable: false);
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
    }

    private static string ReadWordDocument(byte[] content)
    {
        using var archive = OpenArchive(content);
        var document = ReadXml(archive, "word/document.xml");
        XNamespace word = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        var body = document.Root?.Element(word + "body")
            ?? throw new InvalidDataException("The Word document has no document body.");
        var output = new StringBuilder();

        foreach (var block in body.Elements())
        {
            if (block.Name == word + "p")
            {
                output.AppendLine(ReadWordParagraph(block, word));
            }
            else if (block.Name == word + "tbl")
            {
                foreach (var row in block.Elements(word + "tr"))
                {
                    var cells = row.Elements(word + "tc")
                        .Select(cell => string.Join(
                            " ",
                            cell.Elements(word + "p").Select(paragraph => ReadWordParagraph(paragraph, word))));
                    output.AppendLine(string.Join("\t", cells));
                }
            }
        }

        return GetNonEmptyPreview(output, "No readable text was found in this Word document.");
    }

    private static string ReadWordParagraph(XElement paragraph, XNamespace word)
    {
        var output = new StringBuilder();
        foreach (var element in paragraph.Descendants())
        {
            if (element.Name == word + "t")
            {
                output.Append(element.Value);
            }
            else if (element.Name == word + "tab")
            {
                output.Append('\t');
            }
            else if (element.Name == word + "br")
            {
                output.AppendLine();
            }
        }

        return output.ToString();
    }

    private static string ReadPresentation(byte[] content)
    {
        using var archive = OpenArchive(content);
        XNamespace drawing = "http://schemas.openxmlformats.org/drawingml/2006/main";
        var slides = archive.Entries
            .Where(entry => entry.FullName.StartsWith("ppt/slides/slide", StringComparison.OrdinalIgnoreCase)
                && entry.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            .OrderBy(entry => GetNumber(entry.Name))
            .ToArray();
        var output = new StringBuilder();

        for (var index = 0; index < slides.Length; index++)
        {
            var document = ReadXml(slides[index]);
            output.AppendLine($"Slide {index + 1}");
            foreach (var paragraph in document.Descendants(drawing + "p"))
            {
                var text = string.Concat(paragraph.Descendants(drawing + "t").Select(element => element.Value));
                if (!string.IsNullOrWhiteSpace(text))
                {
                    output.AppendLine(text);
                }
            }

            output.AppendLine();
        }

        return GetNonEmptyPreview(output, "No readable text was found in this presentation.");
    }

    private static string ReadSpreadsheet(byte[] content)
    {
        using var archive = OpenArchive(content);
        XNamespace spreadsheet = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var sharedStringsEntry = archive.GetEntry("xl/sharedStrings.xml");
        var sharedStrings = sharedStringsEntry is null
            ? Array.Empty<string>()
            : ReadXml(sharedStringsEntry)
                .Descendants(spreadsheet + "si")
                .Select(item => string.Concat(item.Descendants(spreadsheet + "t").Select(text => text.Value)))
                .ToArray();
        var sheets = archive.Entries
            .Where(entry => entry.FullName.StartsWith("xl/worksheets/sheet", StringComparison.OrdinalIgnoreCase)
                && entry.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            .OrderBy(entry => GetNumber(entry.Name))
            .ToArray();
        var output = new StringBuilder();

        for (var index = 0; index < sheets.Length; index++)
        {
            var document = ReadXml(sheets[index]);
            output.AppendLine($"Worksheet {index + 1}");

            foreach (var row in document.Descendants(spreadsheet + "row"))
            {
                var cells = row.Elements(spreadsheet + "c")
                    .Select(cell => (Column: GetColumnIndex((string?)cell.Attribute("r")), Value: ReadCell(cell, spreadsheet, sharedStrings)))
                    .Where(cell => cell.Column >= 0)
                    .OrderBy(cell => cell.Column)
                    .ToArray();

                if (cells.Length == 0)
                {
                    output.AppendLine();
                    continue;
                }

                var values = new string[cells[^1].Column + 1];
                foreach (var cell in cells)
                {
                    values[cell.Column] = cell.Value.Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');
                }

                output.AppendLine(string.Join("\t", values));
            }

            output.AppendLine();
        }

        return GetNonEmptyPreview(output, "No readable worksheet data was found in this workbook.");
    }

    private static string ReadCell(XElement cell, XNamespace spreadsheet, string[] sharedStrings)
    {
        var value = (string?)cell.Element(spreadsheet + "v") ?? string.Empty;
        if ((string?)cell.Attribute("t") == "s"
            && int.TryParse(value, out var sharedStringIndex)
            && sharedStringIndex >= 0
            && sharedStringIndex < sharedStrings.Length)
        {
            return sharedStrings[sharedStringIndex];
        }

        if ((string?)cell.Attribute("t") == "inlineStr")
        {
            return string.Concat(cell.Descendants(spreadsheet + "t").Select(text => text.Value));
        }

        return value;
    }

    private static int GetColumnIndex(string? cellReference)
    {
        if (string.IsNullOrEmpty(cellReference))
        {
            return -1;
        }

        var column = 0;
        foreach (var character in cellReference)
        {
            if (character is < 'A' or > 'Z' && character is < 'a' or > 'z')
            {
                break;
            }

            var upperCharacter = char.ToUpperInvariant(character);
            column = column * 26 + upperCharacter - 'A' + 1;
        }

        return column - 1;
    }

    private static int GetNumber(string name)
    {
        var digits = new string(name.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out var number) ? number : int.MaxValue;
    }

    private static ZipArchive OpenArchive(byte[] content) =>
        new(new MemoryStream(content, writable: false), ZipArchiveMode.Read);

    private static XDocument ReadXml(ZipArchive archive, string entryName)
    {
        var entry = archive.GetEntry(entryName)
            ?? throw new InvalidDataException($"The document is missing '{entryName}'.");
        return ReadXml(entry);
    }

    private static XDocument ReadXml(ZipArchiveEntry entry)
    {
        using var stream = entry.Open();
        return XDocument.Load(stream);
    }

    private static string GetNonEmptyPreview(StringBuilder output, string emptyMessage)
    {
        var text = output.ToString().Trim();
        return text.Length == 0 ? emptyMessage : text;
    }
}
