using System.Text;

namespace Tedwren.Application.Export;

/// <summary>Renders a <see cref="TabularSheet"/> to RFC-4180 CSV (SUB-10). Data-store agnostic and pure.</summary>
public static class CsvWriter
{
    /// <summary>Writes the sheet's header and rows as a CSV string.</summary>
    public static string Write(TabularSheet sheet)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(',', sheet.Headers.Select(Escape)));
        foreach (var row in sheet.Rows)
        {
            sb.AppendLine(string.Join(',', row.Select(Escape)));
        }

        return sb.ToString();
    }

    /// <summary>Quotes a field when it contains a comma, quote or newline, doubling embedded quotes.</summary>
    private static string Escape(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r')
            ? "\"" + value.Replace("\"", "\"\"") + "\""
            : value;
    }
}
