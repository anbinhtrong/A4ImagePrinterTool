namespace A4ImagePrintTool.Models;

/// <summary>
/// Strongly typed configuration options representing printing and document layout defaults.
/// </summary>
public record PrintSettings
{
    public double PageWidthMm { get; init; } = 210.0;
    public double PageHeightMm { get; init; } = 297.0;

    public double MarginTopMm { get; init; } = 10.0;
    public double MarginBottomMm { get; init; } = 10.0;
    public double MarginLeftMm { get; init; } = 10.0;
    public double MarginRightMm { get; init; } = 10.0;

    public int MaxRows { get; init; } = 10;
    public int MaxColumns { get; init; } = 10;

    public bool ShowCuttingLines { get; init; } = true;
    public string OutputSuffix { get; init; } = "-A4";
    public string? DefaultImagePath { get; init; }
    public string? DefaultOutputDir { get; init; }
}
