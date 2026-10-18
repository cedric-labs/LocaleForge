using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace LocaleForge.Currencies;

/// <summary>
/// Informations décrivant une devise monétaire prise en charge.
/// </summary>
/// <param name="Code">Code ISO-4217 à 3 lettres (ex: <c>EUR</c>, <c>USD</c>).</param>
/// <param name="Symbol">Symbole monétaire (ex: <c>€</c>, <c>$</c>).</param>
/// <param name="DisplayName">Libellé complet d'affichage.</param>
/// <param name="FlagEmoji">Emoji du drapeau associé.</param>
public record CurrencyInfo(string Code, string Symbol, string DisplayName, string FlagEmoji = "");

/// <summary>
/// Service de gestion et de résolution dynamique des devises configurables.
/// </summary>
public interface ICurrencyService
{
    /// <summary>
    /// Retourne la liste des devises supportées.
    /// </summary>
    /// <returns>Collection en lecture seule des devises configurées.</returns>
    IReadOnlyList<CurrencyInfo> GetSupportedCurrencies();

    /// <summary>
    /// Résout les informations complètes d'une devise à partir de son code ISO.
    /// </summary>
    /// <param name="code">Code ISO à 3 lettres (ex: EUR, USD).</param>
    /// <returns>Informations de devise correspondante ou devise par défaut.</returns>
    CurrencyInfo GetCurrency(string? code);

    /// <summary>
    /// Récupère le symbole monétaire associé à un code devise.
    /// </summary>
    /// <param name="code">Code ISO de la devise.</param>
    /// <returns>Symbole de devise (ex: €, $).</returns>
    string GetSymbol(string? code);

    /// <summary>
    /// Formate un montant décimal avec le symbole et les séparateurs de la devise et culture cibles.
    /// </summary>
    /// <param name="amount">Montant à formater.</param>
    /// <param name="currencyCode">Code de la devise cible.</param>
    /// <param name="includeCents">Indique s'il faut afficher les centimes.</param>
    /// <param name="culture">Culture régionale optionnelle pour le formatage.</param>
    /// <returns>Montant mis en forme sous forme textuelle.</returns>
    string FormatAmount(decimal amount, string? currencyCode = null, bool includeCents = true, CultureInfo? culture = null);
}

/// <summary>
/// Implémentation du service de devises s'appuyant sur <see cref="IConfiguration"/> (<c>Currencies:Supported</c>)
/// avec un repli transparent sur les principales devises bancaires internationales.
/// </summary>
/// <param name="configuration">Configuration applicative.</param>
public class CurrencyService(IConfiguration configuration) : ICurrencyService
{
    private static readonly JsonSerializerOptions s_jsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly Lazy<List<CurrencyInfo>> _cachedCurrencies = new(() => LoadCurrencies(configuration));

    /// <summary>
    /// Liste de référence des 15 devises internationales par défaut.
    /// </summary>
    public static readonly IReadOnlyList<CurrencyInfo> DefaultCurrencies =
    [
        new("EUR", "€", "Euro (EUR)", "🇪🇺"),
        new("USD", "$", "Dollar américain (USD)", "🇺🇸"),
        new("GBP", "£", "Livre sterling (GBP)", "🇬🇧"),
        new("CHF", "CHF", "Franc suisse (CHF)", "🇨🇭"),
        new("CAD", "$ CA", "Dollar canadien (CAD)", "🇨🇦"),
        new("JPY", "¥", "Yen japonais (JPY)", "🇯🇵"),
        new("AUD", "$ AU", "Dollar australien (AUD)", "🇦🇺"),
        new("SEK", "kr", "Couronne suédoise (SEK)", "🇸🇪"),
        new("NOK", "kr", "Couronne norvégienne (NOK)", "🇳🇴"),
        new("DKK", "kr", "Couronne danoise (DKK)", "🇩🇰"),
        new("PLN", "zł", "Złoty polonais (PLN)", "🇵🇱"),
        new("BRL", "R$", "Real brésilien (BRL)", "🇧🇷"),
        new("MAD", "DH", "Dirham marocain (MAD)", "🇲🇦"),
        new("TND", "DT", "Dinar tunisien (DT)", "🇹🇳"),
        new("DZD", "DA", "Dinar algérien (DZD)", "🇩🇿")
    ];

    /// <inheritdoc />
    public IReadOnlyList<CurrencyInfo> GetSupportedCurrencies() => _cachedCurrencies.Value;

    /// <inheritdoc />
    public CurrencyInfo GetCurrency(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return _cachedCurrencies.Value[0];
        }

        var cleanCode = code.Trim().ToUpperInvariant();
        var match = _cachedCurrencies.Value.Find(c => c.Code.Equals(cleanCode, StringComparison.OrdinalIgnoreCase));
        return match ?? new CurrencyInfo(cleanCode, cleanCode, cleanCode);
    }

    /// <inheritdoc />
    public string GetSymbol(string? code) => GetCurrency(code).Symbol;

    /// <inheritdoc />
    public string FormatAmount(decimal amount, string? currencyCode = null, bool includeCents = true, CultureInfo? culture = null)
    {
        var currency = GetCurrency(currencyCode);
        return FormatWithSymbol(amount, currency.Symbol, includeCents, culture);
    }

    /// <summary>
    /// Résout de façon statique le symbole monétaire associé à un code ISO parmi les devises par défaut.
    /// </summary>
    /// <param name="code">Code ISO à 3 lettres.</param>
    /// <returns>Symbole monétaire ou code normalisé.</returns>
    public static string GetStaticSymbol(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return "€";
        }

        var clean = code.Trim().ToUpperInvariant();
        var match = DefaultCurrencies.FirstOrDefault(c => string.Equals(c.Code, clean, StringComparison.OrdinalIgnoreCase));
        return match?.Symbol ?? clean;
    }

    /// <summary>
    /// Formate un montant décimal avec un symbole monétaire explicite en évitant la notation comptable entre parenthèses.
    /// </summary>
    /// <param name="amount">Montant à formater.</param>
    /// <param name="symbol">Symbole monétaire.</param>
    /// <param name="includeCents">Indique s'il faut afficher les centimes.</param>
    /// <param name="culture">Culture régionale optionnelle.</param>
    /// <returns>Montant formaté.</returns>
    public static string FormatWithSymbol(decimal amount, string symbol, bool includeCents = true, CultureInfo? culture = null)
    {
        var activeCulture = culture ?? CultureInfo.CurrentCulture;
        var nfi = (NumberFormatInfo)activeCulture.NumberFormat.Clone();
        nfi.CurrencySymbol = symbol;

        // Évite les parenthèses comptables en anglais pour les montants négatifs : préfère -$100 à ($100)
        if (nfi.CurrencyNegativePattern == 0)
        {
            nfi.CurrencyNegativePattern = 1;
        }

        return amount.ToString(includeCents ? "C2" : "C0", nfi);
    }

    private static List<CurrencyInfo> LoadCurrencies(IConfiguration config)
    {
        try
        {
            var json = config["Currencies:Supported"];
            if (!string.IsNullOrWhiteSpace(json))
            {
                var list = JsonSerializer.Deserialize<List<CurrencyInfo>>(json, s_jsonOptions);
                if (list is { Count: > 0 })
                {
                    return list;
                }
            }
        }
        catch (JsonException)
        {
            // Repli sur la liste de devises par défaut si le JSON de configuration est invalide
        }

        return [.. DefaultCurrencies];
    }
}

/// <summary>
/// Méthodes d'extension permettant de formater tout montant monétaire selon la devise cible.
/// </summary>
public static class CurrencyExtensions
{
    private static ICurrencyService? s_service;

    /// <summary>
    /// Configure l'instance active de <see cref="ICurrencyService"/> utilisée par les méthodes d'extension <see cref="ToCurrency(decimal, string?, bool, CultureInfo?)"/>.
    /// </summary>
    /// <param name="service">Service de devises actif.</param>
    public static void Configure(ICurrencyService? service)
    {
        s_service = service;
    }

    /// <summary>
    /// Formate un montant décimal dans la devise spécifiée.
    /// </summary>
    /// <param name="amount">Montant à formater.</param>
    /// <param name="currencyCode">Code ISO de la devise.</param>
    /// <param name="includeCents">Indique s'il faut afficher les centimes.</param>
    /// <param name="culture">Culture régionale optionnelle.</param>
    /// <returns>Montant formaté.</returns>
    public static string ToCurrency(this decimal amount, string? currencyCode = null, bool includeCents = true, CultureInfo? culture = null)
    {
        if (s_service is not null)
        {
            return s_service.FormatAmount(amount, currencyCode, includeCents, culture);
        }

        var symbol = CurrencyService.GetStaticSymbol(currencyCode);
        return CurrencyService.FormatWithSymbol(amount, symbol, includeCents, culture);
    }

    /// <summary>
    /// Formate un montant décimal nullable dans la devise spécifiée.
    /// </summary>
    /// <param name="amount">Montant nullable à formater.</param>
    /// <param name="currencyCode">Code ISO de la devise.</param>
    /// <param name="includeCents">Indique s'il faut afficher les centimes.</param>
    /// <param name="culture">Culture régionale optionnelle.</param>
    /// <returns>Montant formaté ou chaîne vide si <c>null</c>.</returns>
    public static string ToCurrency(this decimal? amount, string? currencyCode = null, bool includeCents = true, CultureInfo? culture = null)
    {
        if (!amount.HasValue)
        {
            return string.Empty;
        }

        return amount.Value.ToCurrency(currencyCode, includeCents, culture);
    }
}
