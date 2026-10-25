<p align="right">
  <b>English</b> | <a href="FRAMEWORK.fr.md">Français</a>
</p>

# LocaleForge — Framework Specification & AI Documentation

This document is the technical manifest intended for AI agents and software engineers. It outlines all capabilities, architectural components, and public contracts exposed by **LocaleForge**.

---

## 🎯 Scope & Responsibility

LocaleForge is a shared .NET library dedicated to **hierarchical JSON localization (`IStringLocalizer`), dynamic language discovery, and ISO-4217 multi-currency formatting**. It is 100% domain- and database-agnostic.

---

## 🧱 Components & Detailed Capabilities

### 1. `LocaleForge.Localization` — JSON Localization & Language Discovery
- **`JsonStringLocalizer` (`IStringLocalizer`) & `JsonStringLocalizerFactory` (`IStringLocalizerFactory`)**:
  - Thread-safe lazy loading and memory caching (`ConcurrentDictionary<string, Lazy<Dictionary<string, string>>>`) of localized JSON resource files (`{culture}.json`).
  - Automatic recursive flattening of nested JSON hierarchies (e.g., `{"Common": {"Save": "Save"}}` $\rightarrow$ `Common.Save`).
  - Hierarchical fallback chain: active culture $\rightarrow$ `fr` $\rightarrow$ `en` $\rightarrow$ raw translation key.
  - Zero-reflection parsing with full support for JSON scalars (strings, numbers, booleans) and graceful tolerance of missing or malformed files.
- **`LanguageInfo` (`record`)**:
  - `string Code`: ISO-639-1 language code (`fr`, `en`, `es`, `de`, etc.).
  - `string DisplayName`: Native capitalized language label preceded by its flag emoji (`🇫🇷 Français`, `🇬🇧 English`).
  - `string FlagEmoji`: Associated national flag emoji.
- **`ILanguageService` & `LanguageService`**:
  - `GetSupportedLanguages()`: Dynamically discovers `*.json` locale files in the designated resources folder, resolves native culture display names via `CultureInfo` (with graceful fallback on `CultureNotFoundException`), and sorts by priority (`fr`, `en`, then alphabetical).
  - `IsSupported(string? code)`: Case-insensitive check whether a language is available.
  - `GetFlagForLanguage(string code)`: Resolves national flag emojis for standard ISO-639-1 codes.

### 2. `LocaleForge.Currencies` — ISO-4217 Currencies & Monetary Formatting
- **`CurrencyInfo` (`record`)**:
  - `string Code`, `string Symbol`, `string DisplayName`, `string FlagEmoji`.
- **`ICurrencyService` & `CurrencyService`**:
  - Dynamically loads supported currency lists from `IConfiguration["Currencies:Supported"]` with automatic fallback to `DefaultCurrencies` (15 international currencies: `EUR`, `USD`, `GBP`, `CHF`, `CAD`, `JPY`, `AUD`, `SEK`, `NOK`, `DKK`, `PLN`, `BRL`, `MAD`, `TND`, `DZD`).
  - `GetSupportedCurrencies()`, `GetCurrency(string? code)`, `GetSymbol(string? code)`, `GetStaticSymbol(string? code)`.
  - `FormatAmount(decimal amount, string? currencyCode, bool includeCents, CultureInfo? culture)`: Formats amounts according to target culture while replacing accounting parenthetical negative notation `($100.00)` with explicit minus signs `-$100.00`.
- **`CurrencyExtensions`**:
  - `Configure(ICurrencyService? service)`
  - `ToCurrency(this decimal amount, ...)` and `ToCurrency(this decimal? amount, ...)`.

---

## 📋 Maintenance Protocol for AI Agents

All enhancements to LocaleForge must comply with:
1. **Backward Compatibility:** Preserve all existing public contracts. Use overloads or optional parameters.
2. **Code Cleanliness:** Zero warnings (`<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`), zero pragmas/suppressions.
3. **Testing Standards:** Test naming convention `TargetMethod_ScenarioOrCondition_ExpectedResult`.
4. **Documentation:** Update both `FRAMEWORK.md` and `FRAMEWORK.fr.md` immediately upon introducing or modifying capabilities.
5. **Validation:** Full pass of `./verify.ps1`.
