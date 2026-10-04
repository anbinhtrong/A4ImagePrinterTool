using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit.Abstractions;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;
using A4ImagePrintTool.Models;
using A4ImagePrintTool.Services;

namespace A4ImagePrintTool.Tests;

public class EmbeddedDocumentInspectorTests
{
    private readonly ITestOutputHelper _output;

    public EmbeddedDocumentInspectorTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void InspectEmbeddedSampleDocument()
    {
        string solutionRoot = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", ".."));
        string outputDir = Path.Combine(solutionRoot, "Output");
        string? path = Directory.GetFiles(outputDir, "lotus-A4-embedded-3x2*.docx")
            .Where(f => !Path.GetFileName(f).StartsWith("~$"))
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();

        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            var options = new PrintLayoutOptions
            {
                ImagePath = Path.Combine(solutionRoot, "images", "lotus_incent.png"),
                OutputPath = Path.Combine(outputDir, "lotus-A4-embedded-3x2.docx"),
                Rows = 3,
                Columns = 2,
                ShowCuttingLines = true
            };
            if (!File.Exists(options.ImagePath))
            {
                // Create a temporary png if images folder was not cloned yet
                string tempPng = Path.Combine(Path.GetTempPath(), "sample_lotus.png");
                byte[] dummyBytes = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52, 0x00, 0x00, 0x03, 0xE8, 0x00, 0x00, 0x05, 0xDC, 0x08, 0x06, 0x00, 0x00, 0x00, 0x1F, 0x15, 0xC4, 0x89, 0x00, 0x00, 0x00, 0x0A, 0x49, 0x44, 0x41, 0x54, 0x78, 0x9C, 0x63, 0x00, 0x01, 0x00, 0x00, 0x05, 0x00, 0x01, 0x0D, 0x0A, 0x2D, 0xB4, 0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44, 0xAE, 0x42, 0x60, 0x82];
                File.WriteAllBytes(tempPng, dummyBytes);
                options = options with { ImagePath = tempPng };
            }
            var layoutService = new ImageLayoutService();
            var layout = layoutService.Calculate(options, 1000, 1500);
            new WordDocumentService().GenerateDocument(options, layout);
            path = options.OutputPath;
        }

        Assert.True(File.Exists(path));

        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var doc = WordprocessingDocument.Open(stream, false);
        var mainPart = doc.MainDocumentPart!;
        var body = mainPart.Document.Body!;
        var section = body.Elements<SectionProperties>().First();
        var pageSize = section.Elements<PageSize>().First();
        var pageMargin = section.Elements<PageMargin>().First();

        var table = body.Elements<Table>().First();
        var rows = table.Elements<TableRow>().ToList();

        _output.WriteLine("=== A4 Page Setup ===");
        _output.WriteLine($"Page Width: {pageSize.Width} twips = {(double)pageSize.Width!.Value * 25.4 / 1440:F2} mm");
        _output.WriteLine($"Page Height: {pageSize.Height} twips = {(double)pageSize.Height!.Value * 25.4 / 1440:F2} mm");
        _output.WriteLine($"Page Margins (T/B/L/R): {pageMargin.Top}/{pageMargin.Bottom}/{pageMargin.Left}/{pageMargin.Right} twips = {(double)pageMargin.Top!.Value * 25.4 / 1440:F2} mm");

        _output.WriteLine("\n=== Table Grid ===");
        _output.WriteLine($"Grid: {rows.Count} rows x {rows[0].Elements<TableCell>().Count()} columns (Total: {rows.Count * rows[0].Elements<TableCell>().Count()} cells)");

        var firstDrawing = rows[0].Descendants<Drawing>().First();
        var extent = firstDrawing.Elements<DW.Inline>().First().Extent!;
        double widthMm = (double)extent.Cx!.Value / 36000.0;
        double heightMm = (double)extent.Cy!.Value / 36000.0;

        _output.WriteLine("\n=== Embedded DrawingML Image Sizing ===");
        _output.WriteLine($"Image Extent: {extent.Cx} x {extent.Cy} EMUs");
        _output.WriteLine($"Rendered Image Size: {widthMm:F2} mm x {heightMm:F2} mm");
        _output.WriteLine($"Aspect Ratio: {widthMm / heightMm:F4}");

        // Verify package image parts
        _output.WriteLine($"\n=== Package Image Parts ===");
        _output.WriteLine($"Unique ImagePart count in docx package: {mainPart.ImageParts.Count()} (shared across all cells)");
    }
}
