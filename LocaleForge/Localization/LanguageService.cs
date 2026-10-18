using System.Globalization;

namespace LocaleForge.Localization;

/// <summary>
/// Informations décrivant une langue supportée par le système.
/// </summary>
/// <param name="Code">Code ISO de la langue (ex: <c>fr</c>, <c>en</c>).</param>
/// <param name="DisplayName">Nom d'affichage natif précédé du drapeau (ex: <c>🇫🇷 Français</c>).</param>
/// <param name="FlagEmoji">Emoji du drapeau associé.</param>
public record LanguageInfo(string Code, string DisplayName, string FlagEmoji);

/// <summary>
/// Contrat de service pour la gestion et la découverte dynamique des langues supportées.
/// </summary>
public interface ILanguageService
{
    /// <summary>
    /// Retourne la liste ordonnée des langues disponibles dans le système.
    /// </summary>
    /// <returns>Collection en lecture seule des informations de langue.</returns>
    IReadOnlyList<LanguageInfo> GetSupportedLanguages();

    /// <summary>
    /// Vérifie si un code de culture donné est pris en charge.
    /// </summary>
    /// <param name="code">Code ISO de la langue.</param>
    /// <returns><c>true</c> si la langue est supportée ; sinon, <c>false</c>.</returns>
    bool IsSupported(string? code);
}

/// <summary>
/// Fournisseur des langues disponibles basé sur la découverte automatique des fichiers JSON de localisation.
/// </summary>
/// <param name="localesPath">Chemin du répertoire contenant les fichiers <c>*.json</c> de langue.</param>
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
                // Repli gracieux : si la culture n'est pas reconnue par l'OS hôte, utiliser le code brut
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
    /// Résout l'emoji de drapeau correspondant à un code langue ISO.
    /// </summary>
    /// <param name="code">Code langue ISO à 2 lettres.</param>
    /// <returns>Emoji du drapeau ou globe par défaut.</returns>
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
