using A4ImagePrintTool.Models;
using A4ImagePrintTool.Utilities;

namespace A4ImagePrintTool.Tests;

public class ConfigurationLoaderTests : IDisposable
{
    private readonly string _testTempDir;

    public ConfigurationLoaderTests()
    {
        _testTempDir = Path.Combine(Path.GetTempPath(), "ConfigTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testTempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testTempDir))
        {
            Directory.Delete(_testTempDir, recursive: true);
        }
    }

    [Fact]
    public void LoadPrintSettings_MissingJsonFile_ReturnsDefaultSettings()
    {
        // Non-existent directory or file
        var settings = ConfigurationLoader.LoadPrintSettings(_testTempDir, "non_existent.json");

        Assert.NotNull(settings);
        Assert.Equal(210.0, settings.PageWidthMm);
        Assert.Equal(297.0, settings.PageHeightMm);
        Assert.Equal(10.0, settings.MarginTopMm);
        Assert.Equal(10.0, settings.MarginBottomMm);
        Assert.Equal(10.0, settings.MarginLeftMm);
        Assert.Equal(10.0, settings.MarginRightMm);
        Assert.Equal(10, settings.MaxRows);
        Assert.Equal(10, settings.MaxColumns);
        Assert.True(settings.ShowCuttingLines);
        Assert.Equal("-A4", settings.OutputSuffix);
    }

    [Fact]
    public void LoadPrintSettings_ValidAppsettingsJson_BindsAllProperties()
    {
        string json = """
        {
          "PrintSettings": {
            "PageWidthMm": 210,
            "PageHeightMm": 297,
            "MarginTopMm": 15,
            "MarginBottomMm": 15,
            "MarginLeftMm": 12,
            "MarginRightMm": 12,
            "MaxRows": 8,
            "MaxColumns": 6,
            "ShowCuttingLines": false,
            "OutputSuffix": "-PRINT"
          }
        }
        """;

        string jsonPath = Path.Combine(_testTempDir, "appsettings.json");
        File.WriteAllText(jsonPath, json);

        var settings = ConfigurationLoader.LoadPrintSettings(_testTempDir, "appsettings.json");

        Assert.NotNull(settings);
        Assert.Equal(210.0, settings.PageWidthMm);
        Assert.Equal(297.0, settings.PageHeightMm);
        Assert.Equal(15.0, settings.MarginTopMm);
        Assert.Equal(15.0, settings.MarginBottomMm);
        Assert.Equal(12.0, settings.MarginLeftMm);
        Assert.Equal(12.0, settings.MarginRightMm);
        Assert.Equal(8, settings.MaxRows);
        Assert.Equal(6, settings.MaxColumns);
        Assert.False(settings.ShowCuttingLines);
        Assert.Equal("-PRINT", settings.OutputSuffix);
    }

    [Fact]
    public void LoadPrintSettings_AppsettingsInProject_LoadsSuccessfully()
    {
        // Load the actual appsettings.json deployed with the application project
        string projectDir = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "..", "src", "A4ImagePrintTool"));
        var settings = ConfigurationLoader.LoadPrintSettings(projectDir);

        Assert.NotNull(settings);
        Assert.Equal(210.0, settings.PageWidthMm);
        Assert.Equal(297.0, settings.PageHeightMm);
        Assert.Equal(10.0, settings.MarginTopMm);
        Assert.Equal(10.0, settings.MarginBottomMm);
        Assert.Equal(10.0, settings.MarginLeftMm);
        Assert.Equal(10.0, settings.MarginRightMm);
        Assert.Equal(10, settings.MaxRows);
        Assert.Equal(10, settings.MaxColumns);
        Assert.False(settings.ShowCuttingLines);
        Assert.Equal("-A4", settings.OutputSuffix);
        Assert.Equal(@"D:\Practices\ImageHandlers\ImagePrinter\images\lotus_incent.png", settings.DefaultImagePath);
    }

    [Fact]
    public void PrintLayoutOptions_FromSettings_CopiesConfiguredValues()
    {
        var settings = new PrintSettings
        {
            PageWidthMm = 210,
            PageHeightMm = 297,
            MarginTopMm = 12.5,
            MarginBottomMm = 12.5,
            MarginLeftMm = 15.0,
            MarginRightMm = 15.0,
            ShowCuttingLines = true
        };

        var options = PrintLayoutOptions.FromSettings(
            settings,
            imagePath: "image.png",
            rows: 3,
            columns: 2,
            outputPath: "doc.docx"
        );

        Assert.Equal("image.png", options.ImagePath);
        Assert.Equal(3, options.Rows);
        Assert.Equal(2, options.Columns);
        Assert.Equal("doc.docx", options.OutputPath);
        Assert.Equal(210.0, options.PageWidthMm);
        Assert.Equal(297.0, options.PageHeightMm);
        Assert.Equal(12.5, options.MarginTopMm);
        Assert.Equal(12.5, options.MarginBottomMm);
        Assert.Equal(15.0, options.MarginLeftMm);
        Assert.Equal(15.0, options.MarginRightMm);
        Assert.True(options.ShowCuttingLines);
        Assert.Equal(6, options.TotalImages);
    }
}
