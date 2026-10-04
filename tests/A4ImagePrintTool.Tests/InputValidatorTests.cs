using A4ImagePrintTool.Utilities;

namespace A4ImagePrintTool.Tests;

public class InputValidatorTests : IDisposable
{
    private readonly string _testTempDir;

    public InputValidatorTests()
    {
        _testTempDir = Path.Combine(Path.GetTempPath(), "A4ImagePrintTool_Tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testTempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testTempDir))
        {
            Directory.Delete(_testTempDir, recursive: true);
        }
    }

    private string CreateTempFile(string fileName)
    {
        string filePath = Path.Combine(_testTempDir, fileName);
        File.WriteAllText(filePath, "dummy-content");
        return filePath;
    }

    [Theory]
    [InlineData("test.png")]
    [InlineData("test.jpg")]
    [InlineData("test.jpeg")]
    [InlineData("TEST.PNG")]
    [InlineData("TEST.JPEG")]
    public void ValidateImagePath_ValidSupportedExtensions_ReturnsTrue(string fileName)
    {
        string filePath = CreateTempFile(fileName);
        var (isValid, errorMessage) = InputValidator.ValidateImagePath(filePath);

        Assert.True(isValid);
        Assert.Null(errorMessage);
    }

    [Fact]
    public void ValidateImagePath_MissingFile_ReturnsFalse()
    {
        string nonExistentPath = Path.Combine(_testTempDir, "missing.png");
        var (isValid, errorMessage) = InputValidator.ValidateImagePath(nonExistentPath);

        Assert.False(isValid);
        Assert.Contains("does not exist", errorMessage);
    }

    [Theory]
    [InlineData("image.gif")]
    [InlineData("image.bmp")]
    [InlineData("image.txt")]
    [InlineData("image.webp")]
    public void ValidateImagePath_UnsupportedExtension_ReturnsFalse(string fileName)
    {
        string filePath = CreateTempFile(fileName);
        var (isValid, errorMessage) = InputValidator.ValidateImagePath(filePath);

        Assert.False(isValid);
        Assert.Contains("Unsupported image format", errorMessage);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateImagePath_EmptyOrWhitespace_ReturnsFalse(string? path)
    {
        var (isValid, errorMessage) = InputValidator.ValidateImagePath(path);

        Assert.False(isValid);
        Assert.Equal("Image path cannot be empty.", errorMessage);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void ValidateRows_ZeroOrNegative_ReturnsFalse(int rows)
    {
        var (isValid, errorMessage) = InputValidator.ValidateRows(rows);

        Assert.False(isValid);
        Assert.Equal("Rows must be greater than zero.", errorMessage);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(InputValidator.MaxRows)]
    public void ValidateRows_WithinValidRange_ReturnsTrue(int rows)
    {
        var (isValid, errorMessage) = InputValidator.ValidateRows(rows);

        Assert.True(isValid);
        Assert.Null(errorMessage);
    }

    [Fact]
    public void ValidateRows_ExceedingMaximum_ReturnsFalse()
    {
        var (isValid, errorMessage) = InputValidator.ValidateRows(InputValidator.MaxRows + 1);

        Assert.False(isValid);
        Assert.Contains("cannot exceed the maximum limit", errorMessage);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void ValidateColumns_ZeroOrNegative_ReturnsFalse(int columns)
    {
        var (isValid, errorMessage) = InputValidator.ValidateColumns(columns);

        Assert.False(isValid);
        Assert.Equal("Columns must be greater than zero.", errorMessage);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(InputValidator.MaxColumns)]
    public void ValidateColumns_WithinValidRange_ReturnsTrue(int columns)
    {
        var (isValid, errorMessage) = InputValidator.ValidateColumns(columns);

        Assert.True(isValid);
        Assert.Null(errorMessage);
    }

    [Fact]
    public void ValidateColumns_ExceedingMaximum_ReturnsFalse()
    {
        var (isValid, errorMessage) = InputValidator.ValidateColumns(InputValidator.MaxColumns + 1);

        Assert.False(isValid);
        Assert.Contains("cannot exceed the maximum limit", errorMessage);
    }

    [Theory]
    [InlineData(@"C:\Output\document.docx")]
    [InlineData("document.docx")]
    [InlineData(@"subfolder\document.DOCX")]
    public void ValidateOutputPath_ValidDocx_ReturnsTrue(string path)
    {
        var (isValid, errorMessage) = InputValidator.ValidateOutputPath(path);

        Assert.True(isValid);
        Assert.Null(errorMessage);
    }

    [Theory]
    [InlineData(@"C:\Output\document.pdf")]
    [InlineData("document.txt")]
    [InlineData("document")]
    public void ValidateOutputPath_NonDocxExtension_ReturnsFalse(string path)
    {
        var (isValid, errorMessage) = InputValidator.ValidateOutputPath(path);

        Assert.False(isValid);
        Assert.Equal("Output file must have a .docx extension.", errorMessage);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateOutputPath_EmptyOrWhitespace_ReturnsFalse(string? path)
    {
        var (isValid, errorMessage) = InputValidator.ValidateOutputPath(path);

        Assert.False(isValid);
        Assert.Equal("Output path cannot be empty.", errorMessage);
    }

    [Fact]
    public void GenerateDefaultOutputPath_CreatesA4DocxSuffix()
    {
        string inputPath = @"C:\Images\nhang.png";
        string defaultOutput = InputValidator.GenerateDefaultOutputPath(inputPath);

        Assert.Equal(@"C:\Images\nhang-A4.docx", defaultOutput);
    }

    [Fact]
    public void GenerateDefaultOutputPath_HandlesRelativePathWithoutDirectory()
    {
        string inputPath = "photo.jpg";
        string defaultOutput = InputValidator.GenerateDefaultOutputPath(inputPath);

        Assert.Equal("photo-A4.docx", defaultOutput);
    }

    [Fact]
    public void GetUniqueFilePath_WhenFileDoesNotExist_ReturnsOriginalPath()
    {
        string filePath = Path.Combine(_testTempDir, "new_document.docx");
        string uniquePath = InputValidator.GetUniqueFilePath(filePath);

        Assert.Equal(filePath, uniquePath);
    }

    [Fact]
    public void GetUniqueFilePath_WhenFileExists_AppendsNumberOne()
    {
        string filePath = CreateTempFile("existing.docx");
        string uniquePath = InputValidator.GetUniqueFilePath(filePath);

        string expectedPath = Path.Combine(_testTempDir, "existing (1).docx");
        Assert.Equal(expectedPath, uniquePath);
    }

    [Fact]
    public void GetUniqueFilePath_WhenFileAndNumberOneExist_AppendsNumberTwo()
    {
        string filePath = CreateTempFile("existing.docx");
        _ = CreateTempFile("existing (1).docx");

        string uniquePath = InputValidator.GetUniqueFilePath(filePath);

        string expectedPath = Path.Combine(_testTempDir, "existing (2).docx");
        Assert.Equal(expectedPath, uniquePath);
    }
}
