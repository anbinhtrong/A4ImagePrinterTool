using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using A4ImagePrintTool.Models;
using A4ImagePrintTool.Services;
using A4ImagePrintTool.Utilities;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;

namespace A4ImagePrintTool.Tests;

public class WordDocumentServiceTests : IDisposable
{
    private readonly string _testTempDir;
    private readonly WordDocumentService _wordService = new();
    private readonly ImageLayoutService _layoutService = new();
    private readonly string _dummyImagePath;

    public WordDocumentServiceTests()
    {
        _testTempDir = Path.Combine(Path.GetTempPath(), "WordDocumentServiceTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testTempDir);

        // Create a 1x1 valid PNG image for tests
        _dummyImagePath = Path.Combine(_testTempDir, "dummy.png");
        byte[] pngBytes =
        [
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, // Header
            0x00, 0x00, 0x00, 0x0D, // IHDR length (13)
            0x49, 0x48, 0x44, 0x52, // IHDR
            0x00, 0x00, 0x00, 0x01, // width: 1
            0x00, 0x00, 0x00, 0x01, // height: 1
            0x08, 0x06, 0x00, 0x00, 0x00, // bit depth, color type, etc.
            0x1F, 0x15, 0xC4, 0x89, // CRC
            0x00, 0x00, 0x00, 0x0A, // IDAT length (10)
            0x49, 0x44, 0x41, 0x54, // IDAT
            0x78, 0x9C, 0x63, 0x00, 0x01, 0x00, 0x00, 0x05, 0x00, 0x01, // compressed data
            0x0D, 0x0A, 0x2D, 0xB4, // CRC
            0x00, 0x00, 0x00, 0x00, // IEND length (0)
            0x49, 0x45, 0x4E, 0x44, // IEND
            0xAE, 0x42, 0x60, 0x82  // CRC
        ];
        File.WriteAllBytes(_dummyImagePath, pngBytes);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testTempDir))
        {
            Directory.Delete(_testTempDir, recursive: true);
        }
    }

    private (PrintLayoutOptions Options, CalculatedLayout Layout, string FilePath) GenerateTestDoc(
        int rows, int cols, int imgWidth = 1000, int imgHeight = 1000, bool cuttingLines = true)
    {
        string filePath = Path.Combine(_testTempDir, $"test_{rows}x{cols}_{imgWidth}x{imgHeight}.docx");
        var options = new PrintLayoutOptions
        {
            ImagePath = _dummyImagePath,
            OutputPath = filePath,
            Rows = rows,
            Columns = cols,
            ShowCuttingLines = cuttingLines,
            MarginTopMm = 10.0,
            MarginBottomMm = 10.0,
            MarginLeftMm = 10.0,
            MarginRightMm = 10.0
        };

        var layout = _layoutService.Calculate(options, imgWidth, imgHeight);
        _wordService.GenerateDocument(options, layout);

        return (options, layout, filePath);
    }

    [Fact]
    public void GenerateDocument_ConfiguresA4PageSizeAndMarginsCorrectly()
    {
        var (_, _, filePath) = GenerateTestDoc(2, 2);

        using var doc = WordprocessingDocument.Open(filePath, false);
        var body = doc.MainDocumentPart?.Document.Body;
        Assert.NotNull(body);

        var sectionProps = body.Elements<SectionProperties>().FirstOrDefault();
        Assert.NotNull(sectionProps);

        var pageSize = sectionProps.Elements<PageSize>().FirstOrDefault();
        Assert.NotNull(pageSize);
        Assert.Equal((uint)11906, pageSize.Width?.Value);
        Assert.Equal((uint)16838, pageSize.Height?.Value);
        Assert.Equal(PageOrientationValues.Portrait, pageSize.Orient?.Value);

        var pageMargin = sectionProps.Elements<PageMargin>().FirstOrDefault();
        Assert.NotNull(pageMargin);
        Assert.Equal(567, pageMargin.Top?.Value);
        Assert.Equal(567, pageMargin.Bottom?.Value);
        Assert.Equal((uint)567, pageMargin.Left?.Value);
        Assert.Equal((uint)567, pageMargin.Right?.Value);
    }

    [Theory]
    [InlineData(2, 2)]
    [InlineData(2, 3)]
    [InlineData(2, 4)]
    public void GenerateDocument_CreatesImagesInEveryCell(int rowsCount, int colsCount)
    {
        var (_, layout, filePath) = GenerateTestDoc(rowsCount, colsCount);

        using var doc = WordprocessingDocument.Open(filePath, false);
        var body = doc.MainDocumentPart?.Document.Body;
        Assert.NotNull(body);

        var table = body.Elements<Table>().FirstOrDefault();
        Assert.NotNull(table);

        var rows = table.Elements<TableRow>().ToList();
        Assert.Equal(rowsCount, rows.Count);

        int totalImagesFound = 0;
        foreach (var row in rows)
        {
            var cells = row.Elements<TableCell>().ToList();
            Assert.Equal(colsCount, cells.Count);

            foreach (var cell in cells)
            {
                var drawing = cell.Descendants<Drawing>().FirstOrDefault();
                Assert.NotNull(drawing);
                totalImagesFound++;

                var inline = drawing.Elements<DW.Inline>().FirstOrDefault();
                Assert.NotNull(inline);
                Assert.NotNull(inline.Extent);

                long expectedWidthEmu = UnitConverter.MmToEmu(layout.RenderedImageWidthMm * 0.96);
                long expectedHeightEmu = UnitConverter.MmToEmu(layout.RenderedImageHeightMm * 0.96);

                Assert.Equal(expectedWidthEmu, inline.Extent.Cx?.Value);
                Assert.Equal(expectedHeightEmu, inline.Extent.Cy?.Value);
            }
        }

        Assert.Equal(rowsCount * colsCount, totalImagesFound);
    }

    [Fact]
    public void GenerateDocument_PortraitImage_PreservesAspectRatio()
    {
        // 1000 x 1500 px (portrait)
        var (_, layout, filePath) = GenerateTestDoc(3, 2, imgWidth: 1000, imgHeight: 1500);

        using var doc = WordprocessingDocument.Open(filePath, false);
        var firstDrawing = doc.MainDocumentPart?.Document.Body?.Descendants<Drawing>().First();
        Assert.NotNull(firstDrawing);

        var extent = firstDrawing.Elements<DW.Inline>().First().Extent!;
        double renderedRatio = (double)extent.Cx!.Value / extent.Cy!.Value;
        double originalRatio = 1000.0 / 1500.0;

        Assert.Equal(originalRatio, renderedRatio, precision: 3);
    }

    [Fact]
    public void GenerateDocument_LandscapeImage_PreservesAspectRatio()
    {
        // 1600 x 900 px (landscape)
        var (_, layout, filePath) = GenerateTestDoc(3, 2, imgWidth: 1600, imgHeight: 900);

        using var doc = WordprocessingDocument.Open(filePath, false);
        var firstDrawing = doc.MainDocumentPart?.Document.Body?.Descendants<Drawing>().First();
        Assert.NotNull(firstDrawing);

        var extent = firstDrawing.Elements<DW.Inline>().First().Extent!;
        double renderedRatio = (double)extent.Cx!.Value / extent.Cy!.Value;
        double originalRatio = 1600.0 / 900.0;

        Assert.Equal(originalRatio, renderedRatio, precision: 3);
    }

    [Fact]
    public void GenerateDocument_SquareImage_PreservesAspectRatio()
    {
        // 1000 x 1000 px (square)
        var (_, layout, filePath) = GenerateTestDoc(2, 2, imgWidth: 1000, imgHeight: 1000);

        using var doc = WordprocessingDocument.Open(filePath, false);
        var firstDrawing = doc.MainDocumentPart?.Document.Body?.Descendants<Drawing>().First();
        Assert.NotNull(firstDrawing);

        var extent = firstDrawing.Elements<DW.Inline>().First().Extent!;
        Assert.Equal(extent.Cx!.Value, extent.Cy!.Value);
    }

    [Fact]
    public void GenerateDocument_ImagePartIsEmbeddedAndReferenced()
    {
        var (_, _, filePath) = GenerateTestDoc(2, 2);

        using var doc = WordprocessingDocument.Open(filePath, false);
        var mainPart = doc.MainDocumentPart;
        Assert.NotNull(mainPart);

        // Verify only 1 ImagePart is saved in package even though multiple cells reference it
        var imageParts = mainPart.ImageParts.ToList();
        Assert.Single(imageParts);
        Assert.Equal("image/png", imageParts[0].ContentType);
    }
}
