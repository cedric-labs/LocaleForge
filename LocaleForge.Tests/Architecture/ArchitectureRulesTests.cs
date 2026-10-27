using AwesomeAssertions;
using LocaleForge.Localization;
using NetArchTest.Rules;
using Xunit;

namespace LocaleForge.Tests.Architecture;

public sealed class ArchitectureRulesTests
{
    [Fact]
    public void Types_WhenInspectingNames_DoNotUseCatchAllSuffixes()
    {
        var result = Types.InAssembly(typeof(JsonStringLocalizer).Assembly)
            .ShouldNot()
            .HaveNameEndingWith("Helper", StringComparison.OrdinalIgnoreCase)
            .And()
            .HaveNameEndingWith("Utils", StringComparison.OrdinalIgnoreCase)
            .And()
            .HaveNameEndingWith("Manager", StringComparison.OrdinalIgnoreCase)
            .And()
            .HaveNameEndingWith("Common", StringComparison.OrdinalIgnoreCase)
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Interfaces_WhenPublic_StartWithIPrefix()
    {
        var result = Types.InAssembly(typeof(JsonStringLocalizer).Assembly)
            .That()
            .AreInterfaces()
            .And()
            .ArePublic()
            .Should()
            .HaveNameStartingWith("I", StringComparison.Ordinal)
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }
}
