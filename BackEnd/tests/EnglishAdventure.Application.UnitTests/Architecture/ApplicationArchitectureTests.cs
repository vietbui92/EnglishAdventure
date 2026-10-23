namespace EnglishAdventure.Application.UnitTests.Architecture;

/// <summary>
/// Enforces the Application layer rule (ADR-2): Application depends only on Domain and abstraction-level packages,
/// never on Infrastructure, Api, EF Core or ASP.NET Core. Checked by reflection (no architecture-test package).
/// </summary>
public sealed class ApplicationArchitectureTests
{
    private static readonly string[] ForbiddenPrefixes =
    [
        "EnglishAdventure.Infrastructure",
        "EnglishAdventure.Api",
        "Microsoft.EntityFrameworkCore",
        "Microsoft.AspNetCore"
    ];

    [Fact]
    public void Application_does_not_reference_infrastructure_api_ef_core_or_asp_net_core()
    {
        var violations = typeof(DependencyInjection).Assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name!)
            .Where(name => ForbiddenPrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal)))
            .ToList();

        Assert.Empty(violations);
    }
}
