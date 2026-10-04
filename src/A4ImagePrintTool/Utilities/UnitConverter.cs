namespace A4ImagePrintTool.Utilities;

/// <summary>
/// Helper conversions between metric units (millimeters) and Open XML unit systems.
/// </summary>
public static class UnitConverter
{
    // 1 inch = 25.4 mm
    // 1 inch = 72 points = 1440 twentieths of a point (dxa / twips)
    // 1440 / 25.4 = 56.69291338582677 twips per mm
    private const double TwipsPerMm = 1440.0 / 25.4;

    // 1 inch = 914,400 EMUs
    // 914,400 / 25.4 = 36,000 EMUs per mm
    private const double EmuPerMm = 36000.0;

    /// <summary>
    /// Converts millimeters to DXA / Twips (Twentieth of an imperial point).
    /// Used for page dimensions, margins, and table/cell/row measurements in WordprocessingML.
    /// </summary>
    public static uint MmToTwips(double mm)
    {
        return (uint)Math.Round(mm * TwipsPerMm);
    }

    /// <summary>
    /// Converts millimeters to DXA / Twips as an integer.
    /// </summary>
    public static int MmToTwipsInt(double mm)
    {
        return (int)Math.Round(mm * TwipsPerMm);
    }

    /// <summary>
    /// Converts millimeters to English Metric Units (EMU).
    /// Used for DrawingML graphic/image extents and coordinates.
    /// </summary>
    public static long MmToEmu(double mm)
    {
        return (long)Math.Round(mm * EmuPerMm);
    }
}
