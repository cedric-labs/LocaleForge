using System.Globalization;
using AwesomeAssertions;
using LocaleForge.Currencies;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace LocaleForge.Tests.Currencies;

public sealed class CurrencyServiceTests
{
    private static readonly string[] ExpectedDefaultCodes = ["EUR", "USD", "GBP", "CHF"];
    private static readonly string[] ExpectedCustomCodes = ["EUR", "BTC"];

    [Fact]
    public void GetSupportedCurrencies_WhenConfigurationIsEmpty_ReturnsDefaultCurrencies()
    {
        var config = new ConfigurationBuilder().Build();
        var service = new CurrencyService(config);

        var currencies = service.GetSupportedCurrencies();

        currencies.Should().NotBeEmpty();
        currencies.Select(c => c.Code).Should().Contain(ExpectedDefaultCodes);
    }

    [Fact]
    public void GetSupportedCurrencies_WhenCustomJsonConfigured_LoadsFromConfiguration()
    {
        const string json = """
            [
                {"Code": "EUR", "Symbol": "€", "DisplayName": "Euro", "FlagEmoji": "🇪🇺"},
                {"Code": "BTC", "Symbol": "₿", "DisplayName": "Bitcoin", "FlagEmoji": "🪙"}
            ]
            """;

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["Currencies:Supported"] = json
            })
            .Build();

        var service = new CurrencyService(config);
        var currencies = service.GetSupportedCurrencies();

        currencies.Should().HaveCount(2);
        currencies.Select(c => c.Code).Should().Contain(ExpectedCustomCodes);
        service.GetCurrency("BTC").Symbol.Should().Be("₿");
    }

    [Theory]
    [InlineData("EUR", "€")]
    [InlineData("USD", "$")]
    [InlineData("GBP", "£")]
    [InlineData("CHF", "CHF")]
    [InlineData("JPY", "¥")]
    public void GetSymbol_WhenKnownCodeProvided_ReturnsExpectedSymbol(string code, string expectedSymbol)
    {
        var config = new ConfigurationBuilder().Build();
        var service = new CurrencyService(config);

        service.GetSymbol(code).Should().Be(expectedSymbol);
    }

    [Fact]
    public void FormatAmount_WhenFormattedAcrossCultures_AppliesSymbolAndAvoidsAccountingParentheses()
    {
        var config = new ConfigurationBuilder().Build();
        var service = new CurrencyService(config);

        var fr = new CultureInfo("fr");
        var en = new CultureInfo("en-US");

        service.FormatAmount(1234.56m, "EUR", includeCents: true, fr)
            .Should().Contain("234,56").And.Contain("€");
        service.FormatAmount(1234.56m, "USD", includeCents: true, en)
            .Should().Be("$1,234.56");
        service.FormatAmount(1234.56m, "USD", includeCents: false, en)
            .Should().Be("$1,235");
        service.FormatAmount(-100m, "USD", includeCents: true, en)
            .Should().Be("-$100.00");
    }

    [Fact]
    public void ToCurrency_WhenConfiguredOrFallback_FormatsDecimalAndNullableDecimal()
    {
        var config = new ConfigurationBuilder().Build();
        var service = new CurrencyService(config);
        CurrencyExtensions.Configure(service);

        var en = new CultureInfo("en-US");
        100m.ToCurrency("USD", includeCents: true, en).Should().Be("$100.00");

        decimal? nullVal = null;
        nullVal.ToCurrency("USD").Should().BeEmpty();

        decimal? nonNullVal = 50m;
        nonNullVal.ToCurrency("USD", includeCents: true, en).Should().Be("$50.00");

        CurrencyExtensions.Configure(null);
        150.50m.ToCurrency("EUR", includeCents: true, en).Should().Contain("150.50").And.Contain("€");
    }
}
