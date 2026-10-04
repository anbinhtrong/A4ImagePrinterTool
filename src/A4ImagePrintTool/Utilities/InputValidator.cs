namespace A4ImagePrintTool.Utilities;

/// <summary>
/// Pure validation logic for user inputs.
/// Independent of System.Console.
/// </summary>
public static class InputValidator
{
    public const int MaxRows = 50;
    public const int MaxColumns = 50;

    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png",
        ".jpg",
        ".jpeg"
    };

    /// <summary>
    /// Validates the image file path and its extension.
    /// </summary>
    public static (bool IsValid, string? ErrorMessage) ValidateImagePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return (false, "Image path cannot be empty.");
        }

        string trimmedPath = path.Trim('\"', '\'').Trim();

        string extension = Path.GetExtension(trimmedPath);
        if (string.IsNullOrEmpty(extension) || !SupportedExtensions.Contains(extension))
        {
            return (false, $"Unsupported image format '{extension}'. Supported formats are: .png, .jpg, .jpeg.");
        }

        if (!File.Exists(trimmedPath))
        {
            return (false, $"Image file does not exist: '{trimmedPath}'");
        }

        return (true, null);
    }

    /// <summary>
    /// Validates the row count against an optional maxRows boundary.
    /// </summary>
    public static (bool IsValid, string? ErrorMessage) ValidateRows(int rows, int maxRows = MaxRows)
    {
        if (rows <= 0)
        {
            return (false, "Rows must be greater than zero.");
        }

        if (rows > maxRows)
        {
            return (false, $"Rows cannot exceed the maximum limit of {maxRows}.");
        }

        return (true, null);
    }

    /// <summary>
    /// Validates the column count against an optional maxColumns boundary.
    /// </summary>
    public static (bool IsValid, string? ErrorMessage) ValidateColumns(int columns, int maxColumns = MaxColumns)
    {
        if (columns <= 0)
        {
            return (false, "Columns must be greater than zero.");
        }

        if (columns > maxColumns)
        {
            return (false, $"Columns cannot exceed the maximum limit of {maxColumns}.");
        }

        return (true, null);
    }

    /// <summary>
    /// Validates the output Word file path.
    /// </summary>
    public static (bool IsValid, string? ErrorMessage) ValidateOutputPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return (false, "Output path cannot be empty.");
        }

        string trimmedPath = path.Trim('\"', '\'').Trim();

        string extension = Path.GetExtension(trimmedPath);
        if (!string.Equals(extension, ".docx", StringComparison.OrdinalIgnoreCase))
        {
            return (false, "Output file must have a .docx extension.");
        }

        try
        {
            string? dir = Path.GetDirectoryName(trimmedPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                // Verify directory path is structurally valid
                _ = Path.GetFullPath(dir);
            }
        }
        catch (Exception ex)
        {
            return (false, $"Invalid output path: {ex.Message}");
        }

        return (true, null);
    }

    /// <summary>
    /// Generates a default output file path based on the input image path, optional suffix, and optional output directory.
    /// Automatically ensures uniqueness by appending (1), (2), etc., if the file already exists on disk.
    /// e.g. "C:\Images\nhang.png" -> "D:\Output\nhang-A4.docx" or "D:\Output\nhang-A4 (1).docx"
    /// </summary>
    public static string GenerateDefaultOutputPath(string imagePath, string suffix = "-A4", string? outputDirectory = null)
    {
        string trimmedPath = imagePath.Trim('\"', '\'').Trim();
        string directory = !string.IsNullOrWhiteSpace(outputDirectory) 
            ? outputDirectory 
            : (Path.GetDirectoryName(trimmedPath) ?? string.Empty);
            
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(trimmedPath);

        string defaultFileName = $"{fileNameWithoutExtension}{suffix}.docx";
        string targetPath = string.IsNullOrEmpty(directory) ? defaultFileName : Path.Combine(directory, defaultFileName);

        return GetUniqueFilePath(targetPath);
    }

    /// <summary>
    /// Ensures that the specified file path does not conflict with an existing file by appending (1), (2), ...
    /// If the file does not exist, returns originalFilePath unchanged.
    /// </summary>
    public static string GetUniqueFilePath(string originalFilePath)
    {
        if (!File.Exists(originalFilePath))
        {
            return originalFilePath;
        }

        string? directory = Path.GetDirectoryName(originalFilePath);
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(originalFilePath);
        string extension = Path.GetExtension(originalFilePath);

        int counter = 1;
        while (true)
        {
            string candidateFileName = $"{fileNameWithoutExtension} ({counter}){extension}";
            string candidatePath = string.IsNullOrEmpty(directory) 
                ? candidateFileName 
                : Path.Combine(directory, candidateFileName);

            if (!File.Exists(candidatePath))
            {
                return candidatePath;
            }

            counter++;
        }
    }
}
