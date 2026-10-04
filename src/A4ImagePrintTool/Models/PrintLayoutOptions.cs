namespace A4ImagePrintTool.Models;

/// <summary>
/// Configuration options for generating an A4 image print document.
/// </summary>
public record PrintLayoutOptions
{
    public required string ImagePath { get; init; }
    public required int Rows { get; init; }
    public required int Columns { get; init; }
    public bool ShowCuttingLines { get; init; } = true;
    public required string OutputPath { get; init; }

    // Page dimensions in millimeters (default: 210mm x 297mm)
    public double PageWidthMm { get; init; } = 210.0;
    public double PageHeightMm { get; init; } = 297.0;

    // Page margins in millimeters (default: 10mm all around)
    public double MarginTopMm { get; init; } = 10.0;
    public double MarginBottomMm { get; init; } = 10.0;
    public double MarginLeftMm { get; init; } = 10.0;
    public double MarginRightMm { get; init; } = 10.0;

    /// <summary>
    /// Total number of images to be placed on the A4 page (Rows * Columns).
    /// </summary>
    public int TotalImages => Rows * Columns;

    /// <summary>
    /// Helper to instantiate PrintLayoutOptions pre-filled with page and margin dimensions from PrintSettings.
    /// </summary>
    public static PrintLayoutOptions FromSettings(
        PrintSettings settings,
        string imagePath,
        int rows,
        int columns,
        string outputPath,
        bool? showCuttingLines = null)
    {
        return new PrintLayoutOptions
        {
            ImagePath = imagePath,
            Rows = rows,
            Columns = columns,
            ShowCuttingLines = showCuttingLines ?? settings.ShowCuttingLines,
            OutputPath = outputPath,
            PageWidthMm = settings.PageWidthMm,
            PageHeightMm = settings.PageHeightMm,
            MarginTopMm = settings.MarginTopMm,
            MarginBottomMm = settings.MarginBottomMm,
            MarginLeftMm = settings.MarginLeftMm,
            MarginRightMm = settings.MarginRightMm
        };
    }
}
