using A4ImagePrintTool.Models;
using A4ImagePrintTool.Services;
using A4ImagePrintTool.Utilities;

namespace A4ImagePrintTool;

public class Program
{
    public static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        PrintSettings settings = ConfigurationLoader.LoadPrintSettings();

        while (true)
        {
            Console.WriteLine("=== A4 Image Print Tool ===");
            Console.WriteLine();

            PrintLayoutOptions options = CollectUserOptions(settings);

            DisplayConfiguration(options);

            if (PromptYesNo("Proceed? (Y/N): ", defaultYes: true))
            {
                Console.WriteLine();
                Console.WriteLine("Generating document...");

                try
                {
                    // Ensure the output path is unique by appending (1), (2), etc. if already exists
                    string finalOutputPath = InputValidator.GetUniqueFilePath(options.OutputPath);
                    if (!string.Equals(finalOutputPath, options.OutputPath, StringComparison.OrdinalIgnoreCase))
                    {
                        options = options with { OutputPath = finalOutputPath };
                    }

                    var dimensions = ImageInfoReader.ReadDimensions(options.ImagePath);
                    var layoutService = new ImageLayoutService();
                    var layout = layoutService.Calculate(options, dimensions.WidthPx, dimensions.HeightPx);

                    var wordService = new WordDocumentService();
                    wordService.GenerateDocument(options, layout);

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("✓ Document generated successfully.");
                    Console.ResetColor();
                    Console.WriteLine($"Saved to: {Path.GetFullPath(options.OutputPath)}");
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error generating document: {ex.Message}");
                    Console.ResetColor();
                }

                break;
            }

            Console.WriteLine();
            Console.WriteLine("Configuration cancelled. Let's re-enter the settings.\n");
        }
    }

    private static PrintLayoutOptions CollectUserOptions(PrintSettings settings)
    {
        string imagePath = PromptImagePath(settings.DefaultImagePath);
        int rows = PromptPositiveInt("Rows:", val => InputValidator.ValidateRows(val, settings.MaxRows));
        int columns = PromptPositiveInt("Columns:", val => InputValidator.ValidateColumns(val, settings.MaxColumns));
        bool showCuttingLines = PromptYesNo("Show cutting lines? (Y/N): ", defaultYes: settings.ShowCuttingLines);
        string outputPath = PromptOutputPath(imagePath, settings.OutputSuffix, settings.DefaultOutputDir);

        return PrintLayoutOptions.FromSettings(
            settings,
            imagePath,
            rows,
            columns,
            outputPath,
            showCuttingLines
        );
    }

    private static string PromptImagePath(string? defaultImagePath = null)
    {
        while (true)
        {
            if (!string.IsNullOrWhiteSpace(defaultImagePath) && File.Exists(defaultImagePath))
            {
                Console.WriteLine($"Image path (press Enter for default: '{defaultImagePath}'):");
            }
            else
            {
                Console.WriteLine("Image path:");
            }
            Console.Write("> ");
            string? input = Console.ReadLine();

            string chosenPath = string.IsNullOrWhiteSpace(input) && !string.IsNullOrWhiteSpace(defaultImagePath)
                ? defaultImagePath
                : input?.Trim('\"', '\'').Trim() ?? string.Empty;

            var (isValid, errorMessage) = InputValidator.ValidateImagePath(chosenPath);
            if (isValid)
            {
                return chosenPath;
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(errorMessage);
            Console.ResetColor();
            Console.WriteLine();
        }
    }

    private static int PromptPositiveInt(string prompt, Func<int, (bool IsValid, string? ErrorMessage)> validator)
    {
        while (true)
        {
            Console.WriteLine(prompt);
            Console.Write("> ");
            string? input = Console.ReadLine();

            if (!int.TryParse(input, out int value))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Invalid number. Please enter a positive integer.");
                Console.ResetColor();
                Console.WriteLine();
                continue;
            }

            var (isValid, errorMessage) = validator(value);
            if (isValid)
            {
                return value;
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(errorMessage);
            Console.ResetColor();
            Console.WriteLine();
        }
    }

    private static bool PromptYesNo(string prompt, bool defaultYes)
    {
        while (true)
        {
            Console.Write(prompt);
            string? input = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(input))
            {
                return defaultYes;
            }

            if (input.Equals("Y", StringComparison.OrdinalIgnoreCase) || input.Equals("yes", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (input.Equals("N", StringComparison.OrdinalIgnoreCase) || input.Equals("no", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Please enter Y or N.");
            Console.ResetColor();
        }
    }

    private static string PromptOutputPath(string imagePath, string suffix = "-A4", string? defaultOutputDir = null)
    {
        string defaultPath = InputValidator.GenerateDefaultOutputPath(imagePath, suffix, defaultOutputDir);

        while (true)
        {
            Console.WriteLine($"Output path (press Enter for default: '{defaultPath}'):");
            Console.Write("> ");
            string? input = Console.ReadLine();

            string chosenPath = string.IsNullOrWhiteSpace(input) ? defaultPath : input.Trim('\"', '\'').Trim();

            var (isValid, errorMessage) = InputValidator.ValidateOutputPath(chosenPath);
            if (isValid)
            {
                return chosenPath;
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(errorMessage);
            Console.ResetColor();
            Console.WriteLine();
        }
    }

    private static void DisplayConfiguration(PrintLayoutOptions options)
    {
        Console.WriteLine();
        Console.WriteLine("Configuration:");
        Console.WriteLine($"  Image:          {Path.GetFileName(options.ImagePath)}");
        Console.WriteLine($"  Layout:         {options.Rows} rows × {options.Columns} columns");
        Console.WriteLine($"  Total images:   {options.TotalImages}");
        Console.WriteLine($"  Cutting lines:  {(options.ShowCuttingLines ? "Yes" : "No")}");
        Console.WriteLine($"  Output:         {options.OutputPath}");
        Console.WriteLine();
    }
}
