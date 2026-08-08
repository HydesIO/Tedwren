using System.Globalization;
using System.IO.Compression;
using System.Text;

namespace Tedwren.Application.Export;

/// <summary>
/// Renders a <see cref="TabularSheet"/> to a minimal, valid Excel (.xlsx) workbook (SUB-10) using only the
/// framework's ZIP support — no external dependency. Cells that parse as numbers are written as numeric so
/// hours totals remain summable in Excel; everything else is written as an inline string. This keeps Excel
/// export a first-class output alongside CSV without pulling in a spreadsheet library.
/// </summary>
public static class XlsxWriter
{
    /// <summary>Builds the .xlsx byte array for a single-sheet workbook.</summary>
    public static byte[] Write(TabularSheet sheet)
    {
        using var buffer = new MemoryStream();
        using (var archive = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteEntry(archive, "[Content_Types].xml", ContentTypes);
            WriteEntry(archive, "_rels/.rels", RootRelationships);
            WriteEntry(archive, "xl/workbook.xml", Workbook(sheet.Name));
            WriteEntry(archive, "xl/_rels/workbook.xml.rels", WorkbookRelationships);
            WriteEntry(archive, "xl/worksheets/sheet1.xml", Worksheet(sheet));
        }

        return buffer.ToArray();
    }

    /// <summary>Writes one archive entry from a UTF-8 string.</summary>
    private static void WriteEntry(ZipArchive archive, string path, string content)
    {
        var entry = archive.CreateEntry(path, CompressionLevel.Optimal);
        using var stream = entry.Open();
        var bytes = Encoding.UTF8.GetBytes(content);
        stream.Write(bytes, 0, bytes.Length);
    }

    private const string ContentTypes =
        "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
        "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
        "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>" +
        "<Default Extension=\"xml\" ContentType=\"application/xml\"/>" +
        "<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>" +
        "<Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>" +
        "</Types>";

    private const string RootRelationships =
        "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
        "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
        "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/>" +
        "</Relationships>";

    private const string WorkbookRelationships =
        "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
        "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
        "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/>" +
        "</Relationships>";

    /// <summary>The workbook part naming the single sheet.</summary>
    private static string Workbook(string sheetName) =>
        "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
        "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" " +
        "xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">" +
        "<sheets><sheet name=\"" + XmlEscape(SafeSheetName(sheetName)) + "\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>";

    /// <summary>The worksheet part with the header row and data rows.</summary>
    private static string Worksheet(TabularSheet sheet)
    {
        var sb = new StringBuilder();
        sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
        sb.Append("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>");

        var rowIndex = 1;
        AppendRow(sb, rowIndex++, sheet.Headers.Select(h => (h, IsNumber: false)));
        foreach (var row in sheet.Rows)
        {
            AppendRow(sb, rowIndex++, row.Select(c => (c, IsNumber: IsNumeric(c))));
        }

        sb.Append("</sheetData></worksheet>");
        return sb.ToString();
    }

    /// <summary>Appends one row of cells, choosing numeric or inline-string cells.</summary>
    private static void AppendRow(StringBuilder sb, int rowIndex, IEnumerable<(string Value, bool IsNumber)> cells)
    {
        sb.Append("<row r=\"").Append(rowIndex).Append("\">");
        var columnIndex = 0;
        foreach (var (value, isNumber) in cells)
        {
            var reference = ColumnName(columnIndex++) + rowIndex;
            if (isNumber)
            {
                sb.Append("<c r=\"").Append(reference).Append("\"><v>").Append(value).Append("</v></c>");
            }
            else
            {
                sb.Append("<c r=\"").Append(reference).Append("\" t=\"inlineStr\"><is><t xml:space=\"preserve\">")
                    .Append(XmlEscape(value)).Append("</t></is></c>");
            }
        }

        sb.Append("</row>");
    }

    /// <summary>Whether a cell should be written as a numeric value (so it is summable in Excel).</summary>
    private static bool IsNumeric(string value) =>
        !string.IsNullOrEmpty(value) &&
        decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out _);

    /// <summary>Converts a zero-based column index to its spreadsheet column name (A, B, …, Z, AA).</summary>
    private static string ColumnName(int index)
    {
        var name = string.Empty;
        for (var i = index; i >= 0; i = i / 26 - 1)
        {
            name = (char)('A' + i % 26) + name;
        }

        return name;
    }

    /// <summary>Trims a sheet name to Excel's 31-character limit and strips invalid characters.</summary>
    private static string SafeSheetName(string name)
    {
        var cleaned = new string(name.Where(c => c is not ('\\' or '/' or '?' or '*' or '[' or ']' or ':')).ToArray());
        return cleaned.Length > 31 ? cleaned[..31] : cleaned;
    }

    /// <summary>Escapes XML special characters.</summary>
    private static string XmlEscape(string value) => value
        .Replace("&", "&amp;")
        .Replace("<", "&lt;")
        .Replace(">", "&gt;")
        .Replace("\"", "&quot;");
}
