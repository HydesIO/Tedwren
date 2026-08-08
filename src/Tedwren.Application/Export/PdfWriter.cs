using System.Globalization;
using System.Text;

namespace Tedwren.Application.Export;

/// <summary>
/// Renders a <see cref="TabularSheet"/> to a minimal, valid PDF (SUB-16) using only the framework — no
/// external dependency, matching the framework-only Excel writer. Rows are laid out as monospaced (Courier)
/// text so columns align, and long reports paginate across A4 pages. This makes PDF a first-class pack output
/// alongside the web view and ZIP, all rendering identical content.
/// </summary>
public static class PdfWriter
{
    private const int PageHeight = 842;   // A4 points
    private const int TopMargin = 800;
    private const int LeftMargin = 50;
    private const int Leading = 14;
    private const int LinesPerPage = 52;

    /// <summary>Builds the PDF byte array for the sheet (title + header + rows).</summary>
    public static byte[] Write(TabularSheet sheet)
    {
        var lines = Layout(sheet);
        var pages = Paginate(lines);

        // Object layout: 1 Catalog, 2 Pages, 3 Font, then (Page, Contents) per page.
        var objects = new List<string>();
        var pageRefs = new List<int>();
        var firstPageObject = 4;
        for (var p = 0; p < pages.Count; p++)
        {
            pageRefs.Add(firstPageObject + p * 2);
        }

        objects.Add("<< /Type /Catalog /Pages 2 0 R >>");
        objects.Add("<< /Type /Pages /Kids [" + string.Join(' ', pageRefs.Select(r => $"{r} 0 R")) + "] /Count " + pages.Count + " >>");
        objects.Add("<< /Type /Font /Subtype /Type1 /BaseFont /Courier >>");

        foreach (var pageObject in pageRefs)
        {
            var contentObject = pageObject + 1;
            objects.Add($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 {PageHeight}] " +
                        $"/Resources << /Font << /F1 3 0 R >> >> /Contents {contentObject} 0 R >>");
            var stream = ContentStream(pages[pageRefs.IndexOf(pageObject)]);
            objects.Add($"<< /Length {Encoding.ASCII.GetByteCount(stream)} >>\nstream\n{stream}\nendstream");
        }

        return Assemble(objects);
    }

    /// <summary>Flattens the sheet to aligned monospaced text lines (title, header, rows).</summary>
    private static List<string> Layout(TabularSheet sheet)
    {
        var columns = sheet.Headers.Count;
        var widths = new int[columns];
        for (var c = 0; c < columns; c++)
        {
            widths[c] = sheet.Headers[c].Length;
        }

        foreach (var row in sheet.Rows)
        {
            for (var c = 0; c < columns && c < row.Count; c++)
            {
                widths[c] = Math.Max(widths[c], row[c].Length);
            }
        }

        string Format(IReadOnlyList<string> cells) =>
            string.Join("  ", Enumerable.Range(0, columns).Select(c => (c < cells.Count ? cells[c] : string.Empty).PadRight(widths[c])));

        var lines = new List<string> { sheet.Name, new string('=', sheet.Name.Length), string.Empty, Format(sheet.Headers) };
        lines.AddRange(sheet.Rows.Select(Format));
        return lines;
    }

    /// <summary>Splits lines into pages.</summary>
    private static List<List<string>> Paginate(List<string> lines)
    {
        var pages = new List<List<string>>();
        for (var i = 0; i < lines.Count; i += LinesPerPage)
        {
            pages.Add(lines.GetRange(i, Math.Min(LinesPerPage, lines.Count - i)));
        }

        return pages.Count == 0 ? new List<List<string>> { new() } : pages;
    }

    /// <summary>Builds a page's text content stream.</summary>
    private static string ContentStream(IReadOnlyList<string> lines)
    {
        var sb = new StringBuilder();
        sb.Append("BT /F1 10 Tf ").Append(LeftMargin).Append(' ').Append(TopMargin).Append(" Td ").Append(Leading).Append(" TL\n");
        foreach (var line in lines)
        {
            sb.Append('(').Append(EscapePdfText(line)).Append(") Tj T*\n");
        }

        sb.Append("ET");
        return sb.ToString();
    }

    /// <summary>Concatenates objects with a cross-reference table and trailer.</summary>
    private static byte[] Assemble(IReadOnlyList<string> objects)
    {
        var sb = new StringBuilder();
        sb.Append("%PDF-1.4\n");

        var offsets = new List<int>();
        for (var i = 0; i < objects.Count; i++)
        {
            offsets.Add(Encoding.ASCII.GetByteCount(sb.ToString()));
            sb.Append(i + 1).Append(" 0 obj\n").Append(objects[i]).Append("\nendobj\n");
        }

        var xrefOffset = Encoding.ASCII.GetByteCount(sb.ToString());
        sb.Append("xref\n0 ").Append(objects.Count + 1).Append('\n');
        sb.Append("0000000000 65535 f \n");
        foreach (var offset in offsets)
        {
            sb.Append(offset.ToString("D10", CultureInfo.InvariantCulture)).Append(" 00000 n \n");
        }

        sb.Append("trailer\n<< /Size ").Append(objects.Count + 1).Append(" /Root 1 0 R >>\n");
        sb.Append("startxref\n").Append(xrefOffset).Append("\n%%EOF");
        return Encoding.ASCII.GetBytes(sb.ToString());
    }

    /// <summary>Escapes the PDF string delimiters and non-ASCII characters in a text line.</summary>
    private static string EscapePdfText(string value)
    {
        var sb = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            switch (ch)
            {
                case '\\': sb.Append("\\\\"); break;
                case '(': sb.Append("\\("); break;
                case ')': sb.Append("\\)"); break;
                default:
                    sb.Append(ch < 32 || ch > 126 ? '?' : ch);
                    break;
            }
        }

        return sb.ToString();
    }
}
