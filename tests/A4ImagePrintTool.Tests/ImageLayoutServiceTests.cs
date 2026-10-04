using A4ImagePrintTool.Models;
using A4ImagePrintTool.Services;

namespace A4ImagePrintTool.Tests;

public class ImageLayoutServiceTests
{
    private readonly ImageLayoutService _service = new();

    private static PrintLayoutOptions CreateOptions(int rows, int cols, double margin = 10.0)
    {
        return new PrintLayoutOptions
        {
            ImagePath = "test.png",
            OutputPath = "output.docx",
            Rows = rows,
            Columns = cols,
            MarginTopMm = margin,
            MarginBottomMm = margin,
            MarginLeftMm = margin,
            MarginRightMm = margin
        };
    }

    [Fact]
    public void TotalImages_IsCalculatedCorrectly()
    {
        var options = CreateOptions(rows: 3, cols: 2);
        Assert.Equal(6, options.TotalImages);
    }

    [Theory]
    [InlineData(2, 2, 95.0, 138.5)]
    [InlineData(2, 3, 63.333333, 138.5)]
    [InlineData(2, 4, 47.5, 138.5)]
    [InlineData(3, 3, 63.333333, 92.333333)]
    [InlineData(3, 4, 47.5, 92.333333)]
    public void Calculate_CellDimensions_MatchGridConfigurations(int rows, int cols, double expectedCellWidth, double expectedCellHeight)
    {
        var options = CreateOptions(rows, cols);
        var layout = _service.Calculate(options, 1000, 1000);

        Assert.Equal(190.0, layout.AvailableWidthMm, precision: 3);
        Assert.Equal(277.0, layout.AvailableHeightMm, precision: 3);
        Assert.Equal(expectedCellWidth, layout.CellWidthMm, precision: 3);
        Assert.Equal(expectedCellHeight, layout.CellHeightMm, precision: 3);
    }

    [Fact]
    public void Calculate_PortraitImage_ScalesProportionallyConstrainedByHeight()
    {
        // 3 rows x 2 cols -> Cell: 95.0mm x 92.333mm
        // Image: 1000 x 1500 (aspect ratio 2:3)
        // scaleX = 95 / 1000 = 0.095
        // scaleY = 92.333 / 1500 = 0.061555
        // Constrained by height: RenderHeight = 92.333mm, RenderWidth = 1000 * 0.061555 = 61.555mm
        var options = CreateOptions(rows: 3, cols: 2);
        var layout = _service.Calculate(options, imageWidthPx: 1000, imageHeightPx: 1500);

        Assert.Equal(92.333, layout.RenderedImageHeightMm, precision: 2);
        Assert.Equal(61.555, layout.RenderedImageWidthMm, precision: 2);

        // Fits within cell
        Assert.True(layout.RenderedImageWidthMm <= layout.CellWidthMm);
        Assert.True(layout.RenderedImageHeightMm <= layout.CellHeightMm);

        // Aspect ratio preserved
        double originalRatio = 1000.0 / 1500.0;
        double renderedRatio = layout.RenderedImageWidthMm / layout.RenderedImageHeightMm;
        Assert.Equal(originalRatio, renderedRatio, precision: 4);
    }

    [Fact]
    public void Calculate_LandscapeImage_ScalesProportionallyConstrainedByWidth()
    {
        // 3 rows x 2 cols -> Cell: 95.0mm x 92.333mm
        // Image: 1600 x 900 (16:9 landscape)
        // scaleX = 95 / 1600 = 0.059375
        // scaleY = 92.333 / 900 = 0.10259
        // Constrained by width: RenderWidth = 95.0mm, RenderHeight = 900 * 0.059375 = 53.4375mm
        var options = CreateOptions(rows: 3, cols: 2);
        var layout = _service.Calculate(options, imageWidthPx: 1600, imageHeightPx: 900);

        Assert.Equal(95.0, layout.RenderedImageWidthMm, precision: 3);
        Assert.Equal(53.4375, layout.RenderedImageHeightMm, precision: 3);

        // Fits within cell
        Assert.True(layout.RenderedImageWidthMm <= layout.CellWidthMm);
        Assert.True(layout.RenderedImageHeightMm <= layout.CellHeightMm);

        // Aspect ratio preserved
        double originalRatio = 1600.0 / 900.0;
        double renderedRatio = layout.RenderedImageWidthMm / layout.RenderedImageHeightMm;
        Assert.Equal(originalRatio, renderedRatio, precision: 4);
    }

    [Fact]
    public void Calculate_SquareImage_ScalesProportionallyConstrainedByShorterDimension()
    {
        // 3 rows x 2 cols -> Cell: 95.0mm x 92.333mm
        // Cell height (92.333) < Cell width (95.0) -> Constrained by cell height
        var options = CreateOptions(rows: 3, cols: 2);
        var layout = _service.Calculate(options, imageWidthPx: 1000, imageHeightPx: 1000);

        Assert.Equal(92.333, layout.RenderedImageHeightMm, precision: 2);
        Assert.Equal(92.333, layout.RenderedImageWidthMm, precision: 2);

        // Fits within cell
        Assert.True(layout.RenderedImageWidthMm <= layout.CellWidthMm);
        Assert.True(layout.RenderedImageHeightMm <= layout.CellHeightMm);
    }

    [Theory]
    [InlineData(0, 2)]
    [InlineData(-1, 2)]
    [InlineData(2, 0)]
    [InlineData(2, -1)]
    public void Calculate_InvalidRowsOrColumns_ThrowsArgumentOutOfRangeException(int rows, int cols)
    {
        var options = CreateOptions(rows, cols);
        Assert.Throws<ArgumentOutOfRangeException>(() => _service.Calculate(options, 100, 100));
    }

    [Theory]
    [InlineData(0, 100)]
    [InlineData(-10, 100)]
    [InlineData(100, 0)]
    [InlineData(100, -5)]
    public void Calculate_InvalidImageDimensions_ThrowsArgumentOutOfRangeException(int widthPx, int heightPx)
    {
        var options = CreateOptions(2, 2);
        Assert.Throws<ArgumentOutOfRangeException>(() => _service.Calculate(options, widthPx, heightPx));
    }

    [Fact]
    public void Calculate_MarginsExceedingPage_ThrowsInvalidOperationException()
    {
        var options = CreateOptions(2, 2, margin: 150.0); // 150 + 150 = 300mm > 210mm width
        Assert.Throws<InvalidOperationException>(() => _service.Calculate(options, 100, 100));
    }
}
