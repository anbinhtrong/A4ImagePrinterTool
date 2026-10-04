using A4ImagePrintTool.Models;

namespace A4ImagePrintTool.Services;

/// <summary>
/// Service responsible for pure layout calculations on an A4 page.
/// Independent of any file I/O or Open XML units.
/// </summary>
public class ImageLayoutService
{
    public const double A4WidthMm = 210.0;
    public const double A4HeightMm = 297.0;

    /// <summary>
    /// Computes cell dimensions and proportional image fit based on layout options and raw image pixel dimensions.
    /// </summary>
    /// <param name="options">Layout parameters including rows, columns, and margins.</param>
    /// <param name="imageWidthPx">Original width in pixels.</param>
    /// <param name="imageHeightPx">Original height in pixels.</param>
    /// <returns>A <see cref="CalculatedLayout"/> containing millimeter measurements.</returns>
    public CalculatedLayout Calculate(PrintLayoutOptions options, int imageWidthPx, int imageHeightPx)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.Rows <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Rows must be greater than zero.");
        }

        if (options.Columns <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Columns must be greater than zero.");
        }

        if (imageWidthPx <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(imageWidthPx), "Image width in pixels must be greater than zero.");
        }

        if (imageHeightPx <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(imageHeightPx), "Image height in pixels must be greater than zero.");
        }

        double pageWidth = options.PageWidthMm > 0 ? options.PageWidthMm : A4WidthMm;
        double pageHeight = options.PageHeightMm > 0 ? options.PageHeightMm : A4HeightMm;

        double availableWidthMm = pageWidth - options.MarginLeftMm - options.MarginRightMm;
        double availableHeightMm = pageHeight - options.MarginTopMm - options.MarginBottomMm;

        if (availableWidthMm <= 0 || availableHeightMm <= 0)
        {
            throw new InvalidOperationException("Page margins exceed the physical dimensions of the A4 page.");
        }

        double cellWidthMm = availableWidthMm / options.Columns;
        double cellHeightMm = availableHeightMm / options.Rows;

        // Proportional aspect ratio fitting:
        // Calculate max scale factors along width and height
        double scaleX = cellWidthMm / imageWidthPx;
        double scaleY = cellHeightMm / imageHeightPx;

        // Use the smaller scale so the image fits fully inside the cell without clipping or distortion
        double scale = Math.Min(scaleX, scaleY);

        double renderedWidthMm = imageWidthPx * scale;
        double renderedHeightMm = imageHeightPx * scale;

        return new CalculatedLayout
        {
            PageWidthMm = pageWidth,
            PageHeightMm = pageHeight,
            AvailableWidthMm = availableWidthMm,
            AvailableHeightMm = availableHeightMm,
            Rows = options.Rows,
            Columns = options.Columns,
            CellWidthMm = cellWidthMm,
            CellHeightMm = cellHeightMm,
            RenderedImageWidthMm = renderedWidthMm,
            RenderedImageHeightMm = renderedHeightMm
        };
    }
}
