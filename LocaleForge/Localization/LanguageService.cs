using System.Globalization;

namespace LocaleForge.Localization;

/// <summary>
/// Describes a supported language.
/// </summary>
/// <param name="Code">ISO language code (for example <c>fr</c>, <c>en</c>).</param>
/// <param name="DisplayName">Native display name preceded by the flag (for example <c>🇫🇷 Français</c>).</param>
/// <param name="FlagEmoji">Matching flag emoji.</param>
public record LanguageInfo(string Code, string DisplayName, string FlagEmoji);

/// <summary>
/// Discovers and describes the supported languages.
/// </summary>
public interface ILanguageService
{
    /// <summary>
    /// Returns the available languages, in display order.
    /// </summary>
    /// <returns>Read-only collection of the languages.</returns>
    IReadOnlyList<LanguageInfo> GetSupportedLanguages();

    /// <summary>
    /// Checks whether a culture code is supported.
    /// </summary>
    /// <param name="code">ISO language code.</param>
    /// <returns><c>true</c> when the language is supported; otherwise <c>false</c>.</returns>
    bool IsSupported(string? code);
}

/// <summary>
/// Lists the available languages by discovering the JSON localization files.
/// </summary>
/// <param name="localesPath">Folder that holds the <c>*.json</c> language files.</param>
public class LanguageService(string localesPath) : ILanguageService
{
    private readonly Lazy<List<LanguageInfo>> _supportedLanguages = new(() => DiscoverLanguages(localesPath));

    /// <inheritdoc />
    public IReadOnlyList<LanguageInfo> GetSupportedLanguages() => _supportedLanguages.Value;

    /// <inheritdoc />
    public bool IsSupported(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        var cleanCode = code.Trim().ToLowerInvariant();
        return _supportedLanguages.Value.Exists(l => l.Code.Equals(cleanCode, StringComparison.OrdinalIgnoreCase));
    }

    private static List<LanguageInfo> DiscoverLanguages(string localesPath)
    {
        var result = new List<LanguageInfo>();

        if (!Directory.Exists(localesPath))
        {
            return [new("fr", "Français", "🇫🇷"), new("en", "English", "🇬🇧")];
        }

        var jsonFiles = Directory.GetFiles(localesPath, "*.json");
        foreach (var file in jsonFiles)
        {
            var code = Path.GetFileNameWithoutExtension(file).ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(code))
            {
                continue;
            }

            try
            {
                var culture = CultureInfo.GetCultureInfo(code);
                var nativeName = culture.NativeName;
                if (!string.IsNullOrEmpty(nativeName))
                {
                    nativeName = char.ToUpperInvariant(nativeName[0]) + (nativeName.Length > 1 ? nativeName[1..] : string.Empty);
                }

                var flag = GetFlagForLanguage(code);
                var displayName = $"{flag} {nativeName}".Trim();
                result.Add(new LanguageInfo(code, displayName, flag));
            }
            catch (CultureNotFoundException)
            {
                // A culture unknown to the host OS keeps its raw code as display name instead of failing the discovery
                var flag = GetFlagForLanguage(code);
                result.Add(new LanguageInfo(code, $"{flag} {code.ToUpperInvariant()}".Trim(), flag));
            }
        }

        if (result.Count == 0)
        {
            result.Add(new LanguageInfo("fr", "🇫🇷 Français", "🇫🇷"));
        }

        return result
            .OrderBy(l => l.Code switch
            {
                "fr" => 0,
                "en" => 1,
                _ => 2
            })
            .ThenBy(l => l.Code, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Resolves the flag emoji of an ISO language code.
    /// </summary>
    /// <param name="code">Two-letter ISO language code.</param>
    /// <returns>The flag emoji, or a globe by default.</returns>
    public static string GetFlagForLanguage(string code)
    {
        ArgumentNullException.ThrowIfNull(code);
        return code.ToLowerInvariant() switch
        {
            "fr" => "🇫🇷",
            "en" => "🇬🇧",
            "es" => "🇪🇸",
            "de" => "🇩🇪",
            "it" => "🇮🇹",
            "pt" => "🇵🇹",
            "nl" => "🇳🇱",
            "ja" => "🇯🇵",
            "zh" => "🇨🇳",
            "ru" => "🇷🇺",
            "ar" => "🇸🇦",
            "ko" => "🇰🇷",
            "pl" => "🇵🇱",
            "sv" => "🇸🇪",
            "no" => "🇳🇴",
            "da" => "🇩🇰",
            "fi" => "🇫🇮",
            "el" => "🇬🇷",
            _ => "🌐"
        };
    }
}
