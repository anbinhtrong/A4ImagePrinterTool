using A4ImagePrintTool.Models;
using A4ImagePrintTool.Services;

namespace A4ImagePrintTool.Tests;

public class SampleGenerator
{
    [Fact]
    public void GenerateSampleA4Document()
    {
        string solutionRoot = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", ".."));
        string outputDir = Path.Combine(solutionRoot, "Output");
        Directory.CreateDirectory(outputDir);
        string outputPath = Path.Combine(outputDir, "lotus-A4-embedded-3x2.docx");
        string imagePath = Path.Combine(solutionRoot, "images", "lotus_incent.png");

        var options = new PrintLayoutOptions
        {
            ImagePath = imagePath,
            OutputPath = outputPath,
            Rows = 3,
            Columns = 2,
            ShowCuttingLines = true,
            MarginTopMm = 10.0,
            MarginBottomMm = 10.0,
            MarginLeftMm = 10.0,
            MarginRightMm = 10.0
        };

        var layoutService = new ImageLayoutService();
        var layout = layoutService.Calculate(options, 1000, 1500);

        var wordService = new WordDocumentService();
        try
        {
            wordService.GenerateDocument(options, layout);
        }
        catch (IOException)
        {
            outputPath = Path.Combine(outputDir, $"lotus-A4-embedded-3x2_{DateTime.Now.Ticks}.docx");
            options = options with { OutputPath = outputPath };
            wordService.GenerateDocument(options, layout);
        }

        Assert.True(File.Exists(outputPath));
    }
}
