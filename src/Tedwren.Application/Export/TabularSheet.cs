namespace Tedwren.Application.Export;

/// <summary>
/// A simple, format-neutral tabular model — a named sheet with a header row and string cells — shared by the
/// CSV and Excel writers so a report is described once and rendered to either format (SUB-10). Cells that
/// parse as numbers are written as numeric cells in Excel so totals remain summable.
/// </summary>
public sealed record TabularSheet(string Name, IReadOnlyList<string> Headers, IReadOnlyList<IReadOnlyList<string>> Rows);
