# LocaleForge

<p align="center">
  <a href="https://ko-fi.com/curvecoder"><img src="https://ko-fi.com/img/githubbutton_sm.svg" alt="Support curvecoder on Ko-fi" /></a>
</p>

Lightweight, high-performance hierarchical JSON localization library (`IStringLocalizer`) and ISO-4217 multi-currency formatter for modern .NET applications.

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Framework](https://img.shields.io/badge/.NET-8.0%20%7C%209.0-purple.svg)](https://dotnet.microsoft.com/)
[![Documentation: English](https://img.shields.io/badge/Docs-English-blue.svg)](FRAMEWORK.md)
[![Documentation: Français](https://img.shields.io/badge/Docs-Français-green.svg)](FRAMEWORK.fr.md)

> 📖 **Framework Documentation**: [English Specification](FRAMEWORK.md) | [Spécification en français](FRAMEWORK.fr.md)

---

## Key Capabilities

### 1. Hierarchical JSON Localization (`LocaleForge.Localization`)
- **`JsonStringLocalizer` (`IStringLocalizer`) & `JsonStringLocalizerFactory`**:
  - Thread-safe lazy loading and memory caching (`ConcurrentDictionary<string, Lazy<Dictionary<string, string>>>`) of localized JSON resource files (`{culture}.json`).
  - Automatic recursive flattening of nested JSON hierarchies (e.g., `{"Common": {"Save": "Save"}}` $\rightarrow$ `Common.Save`).
  - Automatic fallback hierarchy: active culture $\rightarrow$ primary fallback language $\rightarrow$ raw translation key.
  - Zero-reflection parsing with full support for JSON scalars and graceful tolerance of malformed or missing keys.
- **`ILanguageService` & `LanguageService`**:
  - Dynamic discovery of translation files in specified directories.
  - Native display name formatting with flag emojis (e.g. `🇺🇸 English`, `🇫🇷 Français`).

### 2. ISO-4217 Multi-Currency Engine (`LocaleForge.Currencies`)
- **`ICurrencyService` & `CurrencyService`**:
  - Preconfigured support for 15 international currencies (`EUR`, `USD`, `GBP`, `CHF`, `CAD`, `JPY`, `AUD`, `SEK`, `NOK`, `DKK`, `PLN`, `BRL`, `MAD`, `TND`, `DZD`) with dynamic configuration overrides via `IConfiguration["Currencies:Supported"]`.
  - **`FormatAmount`**: Formats monetary values adhering to target culture standards and explicit minus signs (avoids accounting parenthesis notation).
- **Extension Methods**:
  - `decimal.ToCurrency(currencyCode, includeCents, culture)` helper extensions.

---

## Installation

Add the package via the .NET CLI:

```bash
dotnet add package LocaleForge --version 1.0.0
```

---

## Quick Example

```csharp
using System.Globalization;
using LocaleForge.Currencies;
using LocaleForge.Localization;

// 1. Currency formatting
var currencyService = new CurrencyService();
string formatted = currencyService.FormatAmount(1250.50m, "EUR", includeCents: true, culture: CultureInfo.GetCultureInfo("fr-FR"));
// Output: 1 250,50 €

// 2. JSON String Localization
var factory = new JsonStringLocalizerFactory("Resources/Locales");
var localizer = factory.Create("Common", "Resources/Locales");
string text = localizer["Buttons.Submit"];
```

---

## License

This project is licensed under the [MIT License](LICENSE) &copy; 2026 curvecoder.
