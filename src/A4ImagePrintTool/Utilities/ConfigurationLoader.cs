using Microsoft.Extensions.Configuration;
using A4ImagePrintTool.Models;

namespace A4ImagePrintTool.Utilities;

/// <summary>
/// Helper to load and bind configuration settings from appsettings.json.
/// Falls back to sensible defaults if the file or section is missing.
/// </summary>
public static class ConfigurationLoader
{
    public const string SettingsSection = "PrintSettings";

    /// <summary>
    /// Loads PrintSettings from the specified base directory and JSON file.
    /// </summary>
    public static PrintSettings LoadPrintSettings(string? basePath = null, string configFileName = "appsettings.json")
    {
        basePath ??= AppContext.BaseDirectory;

        var builder = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile(configFileName, optional: true, reloadOnChange: false);

        IConfiguration configuration = builder.Build();

        var settings = new PrintSettings();
        IConfigurationSection section = configuration.GetSection(SettingsSection);
        if (section.Exists())
        {
            section.Bind(settings);
        }

        return settings;
    }
}
