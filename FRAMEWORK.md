# LocaleForge — Spécification & Documentation IA du Framework

Ce document est le manifeste technique destiné à l'agent IA et aux développeurs. Il récapitule l'ensemble des capacités, composants et contrats publics exposés par **LocaleForge**.

---

## 🎯 Périmètre & Responsabilité

LocaleForge est la bibliothèque partagée .NET dédiée à la **localisation JSON (`IStringLocalizer`), à la découverte dynamique des langues et au formatage monétaire multi-devises ISO-4217**. Elle est 100% agnostique du domaine métier et des bases de données.

---

## 🧱 Composants & Capacités Détaillées

### 1. `LocaleForge.Localization` — Localisation JSON & Découverte de Langues
- **`JsonStringLocalizer` (`IStringLocalizer`) & `JsonStringLocalizerFactory` (`IStringLocalizerFactory`)** :
  - Chargement paresseux et mise en cache thread-safe (`ConcurrentDictionary<string, Lazy<Dictionary<string, string>>>`) des fichiers JSON de traduction (`{culture}.json`).
  - Aplatissement récursif des hiérarchies JSON imbriquées (ex: `{"Common": {"Save": "Enregistrer"}}` $\rightarrow$ `Common.Save`).
  - Chaîne de repli hiérarchique automatique : culture active $\rightarrow$ `fr` $\rightarrow$ `en` $\rightarrow$ clé brute.
  - Prise en charge des types scalaires JSON (chaînes, nombres, booléens) et tolérance aux fichiers JSON absents ou malformés.
- **`LanguageInfo` (`record`)** :
  - `string Code` : Code ISO de la langue (`fr`, `en`, `es`, `de`, etc.).
  - `string DisplayName` : Libellé natif capitalisé précédé de l'emoji du drapeau (`🇫🇷 Français`).
  - `string FlagEmoji` : Emoji du drapeau associé.
- **`ILanguageService` & `LanguageService`** :
  - `GetSupportedLanguages()` : Découvre dynamiquement les fichiers `*.json` dans le dossier de ressources, résout les noms natifs via `CultureInfo` (avec repli gracieux sur `CultureNotFoundException`) et trie par priorité (`fr`, `en`, puis ordre alphabétique).
  - `IsSupported(string? code)` : Vérifie de manière insensible à la casse si une langue est disponible.
  - `GetFlagForLanguage(string code)` : Résout l'emoji de drapeau pour les principaux codes ISO-639-1.

### 2. `LocaleForge.Currencies` — Devises ISO-4217 & Formatage Monétaire
- **`CurrencyInfo` (`record`)** :
  - `string Code`, `string Symbol`, `string DisplayName`, `string FlagEmoji`.
- **`ICurrencyService` & `CurrencyService`** :
  - Charge dynamiquement la liste des devises depuis `IConfiguration["Currencies:Supported"]` avec repli automatique sur `DefaultCurrencies` (15 devises internationales : `EUR`, `USD`, `GBP`, `CHF`, `CAD`, `JPY`, `AUD`, `SEK`, `NOK`, `DKK`, `PLN`, `BRL`, `MAD`, `TND`, `DZD`).
  - `GetSupportedCurrencies()`, `GetCurrency(string? code)`, `GetSymbol(string? code)`, `GetStaticSymbol(string? code)`.
  - `FormatAmount(decimal amount, string? currencyCode, bool includeCents, CultureInfo? culture)` : Formate les montants selon la culture cible et remplace la notation comptable négative entre parenthèses `($100.00)` par le signe moins explicite `-$100.00`.
- **`CurrencyExtensions`** :
  - `Configure(ICurrencyService? service)`
  - `ToCurrency(this decimal amount, ...)` et `ToCurrency(this decimal? amount, ...)`.

---

## 📋 Protocole de Maintenance par l'Agent IA

Toute modification de LocaleForge doit respecter :
1. **Rétrocompatibilité :** Conserver tous les contrats publics existants. Utiliser des surcharges ou des paramètres optionnels.
2. **Qualité :** Zéro warning (`<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`), zéro pragma/suppress.
3. **Tests :** Nommage `MethodeCible_ScenarioOuCondition_ResultatAttendu`.
4. **Mise à jour :** Compléter immédiatement ce fichier `FRAMEWORK.md` en cas d'ajout d'une nouvelle capacité.
5. **Validation :** Valider par `./verify.ps1`.
