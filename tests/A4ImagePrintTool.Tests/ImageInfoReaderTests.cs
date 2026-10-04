using A4ImagePrintTool.Utilities;

namespace A4ImagePrintTool.Tests;

public class ImageInfoReaderTests
{
    [Fact]
    public void ReadDimensions_LotusIncentPng_ReturnsValidDimensions()
    {
        string samplePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "..", "images", "lotus_incent.png");
        Assert.True(File.Exists(samplePath));

        var dimensions = ImageInfoReader.ReadDimensions(samplePath);
        Assert.True(dimensions.WidthPx > 0);
        Assert.True(dimensions.HeightPx > 0);
    }
}
