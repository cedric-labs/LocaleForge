using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace LocaleForge.Currencies;

/// <summary>
/// Describes a supported currency.
/// </summary>
/// <param name="Code">Three-letter ISO-4217 code (for example <c>EUR</c>, <c>USD</c>).</param>
/// <param name="Symbol">Currency symbol (for example <c>€</c>, <c>$</c>).</param>
/// <param name="DisplayName">Full display name.</param>
/// <param name="FlagEmoji">Matching flag emoji.</param>
public record CurrencyInfo(string Code, string Symbol, string DisplayName, string FlagEmoji = "");

/// <summary>
/// Resolves and formats the configured currencies.
/// </summary>
public interface ICurrencyService
{
    /// <summary>
    /// Returns the supported currencies.
    /// </summary>
    /// <returns>Read-only collection of the configured currencies.</returns>
    IReadOnlyList<CurrencyInfo> GetSupportedCurrencies();

    /// <summary>
    /// Resolves the full description of a currency from its ISO code.
    /// </summary>
    /// <param name="code">Three-letter ISO code (for example EUR, USD).</param>
    /// <returns>The matching currency, or the default currency.</returns>
    CurrencyInfo GetCurrency(string? code);

    /// <summary>
    /// Gets the symbol of a currency code.
    /// </summary>
    /// <param name="code">ISO currency code.</param>
    /// <returns>Currency symbol (for example €, $).</returns>
    string GetSymbol(string? code);

    /// <summary>
    /// Formats a decimal amount with the symbol and separators of the target currency and culture.
    /// </summary>
    /// <param name="amount">Amount to format.</param>
    /// <param name="currencyCode">Target currency code.</param>
    /// <param name="includeCents">Whether the cents are shown.</param>
    /// <param name="culture">Optional culture used for formatting.</param>
    /// <returns>The formatted amount.</returns>
    string FormatAmount(decimal amount, string? currencyCode = null, bool includeCents = true, CultureInfo? culture = null);
}

/// <summary>
/// Currency service backed by <see cref="IConfiguration"/> (<c>Currencies:Supported</c>),
/// falling back to the main international banking currencies.
/// </summary>
/// <param name="configuration">Configuration applicative.</param>
public class CurrencyService(IConfiguration configuration) : ICurrencyService
{
    private static readonly JsonSerializerOptions s_jsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly Lazy<List<CurrencyInfo>> _cachedCurrencies = new(() => LoadCurrencies(configuration));

    /// <summary>
    /// Reference list of the 15 default international currencies.
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
    /// Resolves, among the default currencies, the symbol of an ISO code without a service instance.
    /// </summary>
    /// <param name="code">Three-letter ISO code.</param>
    /// <returns>The currency symbol, or the normalized code.</returns>
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
    /// Formats a decimal amount with an explicit currency symbol, avoiding the accounting notation in parentheses.
    /// </summary>
    /// <param name="amount">Amount to format.</param>
    /// <param name="symbol">Currency symbol.</param>
    /// <param name="includeCents">Whether the cents are shown.</param>
    /// <param name="culture">Optional culture.</param>
    /// <returns>The formatted amount.</returns>
    public static string FormatWithSymbol(decimal amount, string symbol, bool includeCents = true, CultureInfo? culture = null)
    {
        var activeCulture = culture ?? CultureInfo.CurrentCulture;
        var nfi = (NumberFormatInfo)activeCulture.NumberFormat.Clone();
        nfi.CurrencySymbol = symbol;

        // English cultures show negative amounts in accounting parentheses; -$100 is preferred to ($100)
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
            // An invalid configuration JSON falls back to the default currency list
        }

        return [.. DefaultCurrencies];
    }
}

/// <summary>
/// Extension methods that format any amount in a target currency.
/// </summary>
public static class CurrencyExtensions
{
    private static ICurrencyService? s_service;

    /// <summary>
    /// Sets the active <see cref="ICurrencyService"/> instance used by the <see cref="ToCurrency(decimal, string?, bool, CultureInfo?)"/> extension methods.
    /// </summary>
    /// <param name="service">Active currency service.</param>
    public static void Configure(ICurrencyService? service)
    {
        s_service = service;
    }

    /// <summary>
    /// Formats a decimal amount in the given currency.
    /// </summary>
    /// <param name="amount">Amount to format.</param>
    /// <param name="currencyCode">ISO currency code.</param>
    /// <param name="includeCents">Whether the cents are shown.</param>
    /// <param name="culture">Optional culture.</param>
    /// <returns>The formatted amount.</returns>
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
    /// Formats a nullable decimal amount in the given currency.
    /// </summary>
    /// <param name="amount">Nullable amount to format.</param>
    /// <param name="currencyCode">ISO currency code.</param>
    /// <param name="includeCents">Whether the cents are shown.</param>
    /// <param name="culture">Optional culture.</param>
    /// <returns>The formatted amount, or an empty string when <c>null</c>.</returns>
    public static string ToCurrency(this decimal? amount, string? currencyCode = null, bool includeCents = true, CultureInfo? culture = null)
    {
        if (!amount.HasValue)
        {
            return string.Empty;
        }

        return amount.Value.ToCurrency(currencyCode, includeCents, culture);
    }
}
