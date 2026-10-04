namespace A4ImagePrintTool.Models;

/// <summary>
/// Computed layout dimensions in millimeters.
/// Pure geometry without dependencies on Word or Open XML units.
/// </summary>
public record CalculatedLayout
{
    // A4 Page dimensions (mm)
    public double PageWidthMm { get; init; } = 210.0;
    public double PageHeightMm { get; init; } = 297.0;

    // Available printable area (mm)
    public required double AvailableWidthMm { get; init; }
    public required double AvailableHeightMm { get; init; }

    // Grid layout
    public required int Rows { get; init; }
    public required int Columns { get; init; }

    // Table cell dimensions (mm)
    public required double CellWidthMm { get; init; }
    public required double CellHeightMm { get; init; }

    // Rendered image dimensions preserving original aspect ratio (mm)
    public required double RenderedImageWidthMm { get; init; }
    public required double RenderedImageHeightMm { get; init; }
}
