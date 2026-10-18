using System.Globalization;
using FluentAssertions;
using LocaleForge.Localization;
using Xunit;

namespace LocaleForge.Tests.Localization;

public sealed class JsonStringLocalizerTests : IDisposable
{
    private readonly string _tempDir;

    public JsonStringLocalizerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"localeforge_loc_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        const string frJson = """
            {
              "Common": {
                "Save": "Enregistrer",
                "Cancel": "Annuler",
                "Welcome": "Bienvenue {0} !"
              },
              "OnlyFr": "Seulement en français"
            }
            """;
        File.WriteAllText(Path.Combine(_tempDir, "fr.json"), frJson);

        const string enJson = """
            {
              "Common": {
                "Save": "Save",
                "Cancel": "Cancel",
                "Welcome": "Welcome {0}!"
              },
              "OnlyEn": "Only in English"
            }
            """;
        File.WriteAllText(Path.Combine(_tempDir, "en.json"), enJson);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            try
            {
                Directory.Delete(_tempDir, recursive: true);
            }
            catch (IOException)
            {
                // Ignorer le verrou temporaire du système de fichiers lors du nettoyage
            }
        }
    }

    [Fact]
    public void Indexer_WhenFrenchCultureActive_ReturnsFrenchTranslation()
    {
        var prevCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo("fr-FR");
            var localizer = new JsonStringLocalizer(_tempDir);

            var result = localizer["Common.Save"];

            result.Value.Should().Be("Enregistrer");
            result.ResourceNotFound.Should().BeFalse();
        }
        finally
        {
            CultureInfo.CurrentUICulture = prevCulture;
        }
    }

    [Fact]
    public void Indexer_WhenEnglishCultureActive_ReturnsEnglishTranslation()
    {
        var prevCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo("en-US");
            var localizer = new JsonStringLocalizer(_tempDir);

            var result = localizer["Common.Save"];

            result.Value.Should().Be("Save");
            result.ResourceNotFound.Should().BeFalse();
        }
        finally
        {
            CultureInfo.CurrentUICulture = prevCulture;
        }
    }

    [Fact]
    public void Indexer_WhenFormatArgumentsProvided_FormatsTranslatedTemplate()
    {
        var prevCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo("fr-FR");
            var localizer = new JsonStringLocalizer(_tempDir);

            var result = localizer["Common.Welcome", "Alice"];

            result.Value.Should().Be("Bienvenue Alice !");
            result.ResourceNotFound.Should().BeFalse();
        }
        finally
        {
            CultureInfo.CurrentUICulture = prevCulture;
        }
    }

    [Fact]
    public void Indexer_WhenKeyMissingInEnglish_FallsBackToFrench()
    {
        var prevCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo("en-US");
            var localizer = new JsonStringLocalizer(_tempDir);

            var result = localizer["OnlyFr"];

            result.Value.Should().Be("Seulement en français");
            result.ResourceNotFound.Should().BeFalse();
        }
        finally
        {
            CultureInfo.CurrentUICulture = prevCulture;
        }
    }

    [Fact]
    public void Indexer_WhenKeyMissingEverywhere_ReturnsKeyNameAndMarksNotFound()
    {
        var prevCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo("fr-FR");
            var localizer = new JsonStringLocalizer(_tempDir);

            var result = localizer["NonExistent.Key"];

            result.Value.Should().Be("NonExistent.Key");
            result.ResourceNotFound.Should().BeTrue();
        }
        finally
        {
            CultureInfo.CurrentUICulture = prevCulture;
        }
    }

    [Fact]
    public void GetAllStrings_WhenFrenchCultureActive_ReturnsAllFlattenedEntries()
    {
        var prevCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo("fr-FR");
            var localizer = new JsonStringLocalizer(_tempDir);

            var all = localizer.GetAllStrings(includeParentCultures: false).ToList();

            all.Should().Contain(s => s.Name == "Common.Save" && s.Value == "Enregistrer");
            all.Should().Contain(s => s.Name == "OnlyFr" && s.Value == "Seulement en français");
        }
        finally
        {
            CultureInfo.CurrentUICulture = prevCulture;
        }
    }

    [Fact]
    public void Create_WhenCalledOnFactory_ReturnsCachedLocalizer()
    {
        var factory = new JsonStringLocalizerFactory(_tempDir);

        var locFromType = factory.Create(typeof(JsonStringLocalizerTests));
        var locFromName = factory.Create("Common", "LocaleForge");

        locFromType.Should().NotBeNull();
        locFromName.Should().NotBeNull();
        locFromType["Common.Save"].Value.Should().NotBeEmpty();
    }

    [Fact]
    public void Indexer_WhenThirdLanguageMissingKeyInFrench_FallsBackToEnglish()
    {
        var prevCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo("de-DE");
            var localizer = new JsonStringLocalizer(_tempDir);

            var result = localizer["OnlyEn"];
            result.Value.Should().Be("Only in English");
            result.ResourceNotFound.Should().BeFalse();
        }
        finally
        {
            CultureInfo.CurrentUICulture = prevCulture;
        }
    }

    [Fact]
    public void Indexer_WhenNonStringValuesAndMalformedJson_HandlesGracefully()
    {
        const string jsonWithNumbers = """
            {
              "App": {
                "MaxItems": 50,
                "IsActive": true
              }
            }
            """;
        File.WriteAllText(Path.Combine(_tempDir, "it.json"), jsonWithNumbers);

        var prevCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo("it-IT");
            var localizer = new JsonStringLocalizer(_tempDir);

            localizer["App.MaxItems"].Value.Should().Be("50");
            localizer["App.IsActive"].Value.Should().Be("True");

            File.WriteAllText(Path.Combine(_tempDir, "nl.json"), "{ invalid json ");
            CultureInfo.CurrentUICulture = new CultureInfo("nl-NL");
            var localizerNl = new JsonStringLocalizer(_tempDir);

            localizerNl["Common.Save"].Value.Should().Be("Enregistrer");
        }
        finally
        {
            CultureInfo.CurrentUICulture = prevCulture;
        }
    }
}
