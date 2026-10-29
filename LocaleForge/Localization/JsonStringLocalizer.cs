using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Localization;

namespace LocaleForge.Localization;

/// <summary>
/// <see cref="IStringLocalizer"/> backed by JSON files in a resources folder (<c>{culture}.json</c>).
/// Flattens nested JSON objects into dotted keys and falls back across languages.
/// </summary>
/// <param name="resourcesPath">Folder that holds the JSON localization files.</param>
public class JsonStringLocalizer(string resourcesPath) : IStringLocalizer
{
    private static readonly JsonSerializerOptions s_jsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly ConcurrentDictionary<string, Lazy<Dictionary<string, string>>> _localizationCache = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public LocalizedString this[string name]
    {
        get
        {
            var value = GetString(name);
            return new LocalizedString(name, value ?? name, resourceNotFound: value is null);
        }
    }

    /// <inheritdoc />
    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            var format = GetString(name);
            var value = string.Format(CultureInfo.CurrentCulture, format ?? name, arguments);
            return new LocalizedString(name, value, resourceNotFound: format is null);
        }
    }

    /// <inheritdoc />
    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        var dict = LoadDictionary(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);
        return dict.Select(kv => new LocalizedString(kv.Key, kv.Value, resourceNotFound: false));
    }

    private string? GetString(string name)
    {
        var culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLowerInvariant();
        var dict = LoadDictionary(culture);

        if (dict.TryGetValue(name, out var value))
        {
            return value;
        }

        // Fallback 1: the default language "fr" when the current language is not "fr"
        if (!string.Equals(culture, "fr", StringComparison.OrdinalIgnoreCase))
        {
            var frDict = LoadDictionary("fr");
            if (frDict.TryGetValue(name, out var frValue))
            {
                return frValue;
            }
        }

        // Fallback 2: "en"
        if (!string.Equals(culture, "en", StringComparison.OrdinalIgnoreCase))
        {
            var enDict = LoadDictionary("en");
            if (enDict.TryGetValue(name, out var enValue))
            {
                return enValue;
            }
        }

        return null;
    }

    private Dictionary<string, string> LoadDictionary(string cultureCode)
    {
        return _localizationCache.GetOrAdd(cultureCode, code => new Lazy<Dictionary<string, string>>(() =>
        {
            var filePath = Path.Combine(resourcesPath, $"{code}.json");
            if (!File.Exists(filePath))
            {
                filePath = Path.Combine(resourcesPath, $"{CultureInfo.CurrentUICulture.Name}.json");
                if (!File.Exists(filePath))
                {
                    return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                }
            }

            try
            {
                var json = File.ReadAllText(filePath);
                var rawDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, s_jsonOptions);

                var flattened = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                if (rawDict is not null)
                {
                    FlattenJson(rawDict, string.Empty, flattened);
                }

                return flattened;
            }
            catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
            {
                // An empty dictionary lets the lookup fall back to the default language or to the key name
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
        })).Value;
    }

    private static void FlattenJson(Dictionary<string, JsonElement> dict, string prefix, Dictionary<string, string> result)
    {
        foreach (var (key, value) in dict)
        {
            var fullKey = string.IsNullOrEmpty(prefix) ? key : $"{prefix}.{key}";
            if (value.ValueKind == JsonValueKind.Object)
            {
                var subDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(value.GetRawText(), s_jsonOptions);
                if (subDict is not null)
                {
                    FlattenJson(subDict, fullKey, result);
                }
            }
            else if (value.ValueKind == JsonValueKind.String)
            {
                result[fullKey] = value.GetString() ?? string.Empty;
            }
            else
            {
                result[fullKey] = value.ToString();
            }
        }
    }
}

/// <summary>
/// <see cref="IStringLocalizerFactory"/> that creates and caches one shared <see cref="JsonStringLocalizer"/>.
/// </summary>
/// <param name="resourcesPath">Folder that holds the JSON localization files.</param>
public class JsonStringLocalizerFactory(string resourcesPath) : IStringLocalizerFactory
{
    private readonly ConcurrentDictionary<string, JsonStringLocalizer> _localizers = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public IStringLocalizer Create(Type resourceSource) => GetOrCreate();

    /// <inheritdoc />
    public IStringLocalizer Create(string baseName, string location) => GetOrCreate();

    private JsonStringLocalizer GetOrCreate() =>
        _localizers.GetOrAdd("global", _ => new JsonStringLocalizer(resourcesPath));
}
