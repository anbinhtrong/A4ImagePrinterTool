using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit.Abstractions;
using A4ImagePrintTool.Models;
using A4ImagePrintTool.Services;

namespace A4ImagePrintTool.Tests;

public class DocumentInspectorTests
{
    private readonly ITestOutputHelper _output;

    public DocumentInspectorTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void InspectGeneratedSampleDocument()
    {
        string tempDocPath = Path.Combine(Path.GetTempPath(), $"inspect_test_{Guid.NewGuid():N}.docx");
        string tempImgPath = Path.Combine(Path.GetTempPath(), $"inspect_dummy_{Guid.NewGuid():N}.png");
        byte[] pngBytes =
        [
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
            0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
            0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
            0x08, 0x06, 0x00, 0x00, 0x00, 0x1F, 0x15, 0xC4, 0x89,
            0x00, 0x00, 0x00, 0x0A, 0x49, 0x44, 0x41, 0x54,
            0x78, 0x9C, 0x63, 0x00, 0x01, 0x00, 0x00, 0x05, 0x00, 0x01,
            0x0D, 0x0A, 0x2D, 0xB4, 0x00, 0x00, 0x00, 0x00,
            0x49, 0x45, 0x4E, 0x44, 0xAE, 0x42, 0x60, 0x82
        ];
        File.WriteAllBytes(tempImgPath, pngBytes);

        var options = new PrintLayoutOptions
        {
            ImagePath = tempImgPath,
            OutputPath = tempDocPath,
            Rows = 3,
            Columns = 2,
            ShowCuttingLines = true
        };
        var layoutService = new ImageLayoutService();
        var layout = layoutService.Calculate(options, 1000, 1500);
        new WordDocumentService().GenerateDocument(options, layout);

        Assert.True(File.Exists(tempDocPath));

        using var doc = WordprocessingDocument.Open(tempDocPath, false);
        var body = doc.MainDocumentPart!.Document.Body!;
        var section = body.Elements<SectionProperties>().First();
        var pageSize = section.Elements<PageSize>().First();
        var pageMargin = section.Elements<PageMargin>().First();

        var table = body.Elements<Table>().First();
        var rows = table.Elements<TableRow>().ToList();

        _output.WriteLine("=== A4 Page Setup ===");
        _output.WriteLine($"Page Width: {pageSize.Width} twips = {(double)pageSize.Width!.Value * 25.4 / 1440:F2} mm");
        _output.WriteLine($"Page Height: {pageSize.Height} twips = {(double)pageSize.Height!.Value * 25.4 / 1440:F2} mm");
        _output.WriteLine($"Page Margins (T/B/L/R): {pageMargin.Top}/{pageMargin.Bottom}/{pageMargin.Left}/{pageMargin.Right} twips = {(double)pageMargin.Top!.Value * 25.4 / 1440:F2} mm");

        _output.WriteLine("\n=== Table & Cell Dimensions ===");
        _output.WriteLine($"Grid: {rows.Count} rows x {rows[0].Elements<TableCell>().Count()} columns");

        var firstRowHeight = rows[0].Elements<TableRowProperties>().FirstOrDefault()?.Elements<TableRowHeight>().FirstOrDefault();
        if (firstRowHeight != null)
        {
            _output.WriteLine($"Row Height: {firstRowHeight.Val} twips = {(double)firstRowHeight.Val!.Value * 25.4 / 1440:F2} mm (HeightRule: {firstRowHeight.HeightType})");
        }
        else
        {
            _output.WriteLine("Row Height: Auto (Content-driven with CantSplit)");
        }

        var firstCellWidth = rows[0].Elements<TableCell>().First().Elements<TableCellProperties>().First().Elements<TableCellWidth>().First();
        _output.WriteLine($"Cell Width: {firstCellWidth.Width} twips = {(double)int.Parse(firstCellWidth.Width!.Value!) * 25.4 / 1440:F2} mm");
    }
}
