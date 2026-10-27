using AwesomeAssertions;
using LocaleForge.Localization;
using Xunit;

namespace LocaleForge.Tests.Localization;

public sealed class LanguageServiceTests
{
    private static readonly string[] ExpectedLangCodes = ["fr", "en", "es", "de"];

    [Fact]
    public void GetSupportedLanguages_WhenValidLocalesDirectoryExists_DiscoversLanguagesAndOrdersPriority()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"LocaleForgeLangs_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            File.WriteAllText(Path.Combine(tempDir, "fr.json"), "{}");
            File.WriteAllText(Path.Combine(tempDir, "en.json"), "{}");
            File.WriteAllText(Path.Combine(tempDir, "es.json"), "{}");
            File.WriteAllText(Path.Combine(tempDir, "de.json"), "{}");

            var service = new LanguageService(tempDir);
            var languages = service.GetSupportedLanguages();

            languages.Should().HaveCount(4);
            languages.Select(l => l.Code).Should().Contain(ExpectedLangCodes);
            languages[0].Code.Should().Be("fr");
            languages[1].Code.Should().Be("en");

            service.IsSupported("es").Should().BeTrue();
            service.IsSupported("ES").Should().BeTrue();
            service.IsSupported("de").Should().BeTrue();
            service.IsSupported("it").Should().BeFalse();
            service.IsSupported(null).Should().BeFalse();
            service.IsSupported("   ").Should().BeFalse();
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    [Fact]
    public void GetSupportedLanguages_WhenDirectoryDoesNotExist_ReturnsFallbackDefaults()
    {
        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"LocaleForgeNonExistent_{Guid.NewGuid():N}");
        var service = new LanguageService(nonExistentPath);

        var languages = service.GetSupportedLanguages();

        languages.Should().NotBeEmpty();
        languages.Select(l => l.Code).Should().Contain("fr");
    }

    [Fact]
    public void GetFlagForLanguage_WhenKnownOrUnknownLanguageCodesProvided_ReturnsExpectedEmoji()
    {
        LanguageService.GetFlagForLanguage("fr").Should().Be("🇫🇷");
        LanguageService.GetFlagForLanguage("en").Should().Be("🇬🇧");
        LanguageService.GetFlagForLanguage("es").Should().Be("🇪🇸");
        LanguageService.GetFlagForLanguage("de").Should().Be("🇩🇪");
        LanguageService.GetFlagForLanguage("ja").Should().Be("🇯🇵");
        LanguageService.GetFlagForLanguage("unknown").Should().Be("🌐");
    }
}
