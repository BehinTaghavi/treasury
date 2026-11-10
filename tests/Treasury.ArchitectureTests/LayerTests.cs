using System.Reflection;
using Xunit;

namespace Treasury.ArchitectureTests;

/// <summary>Clean Architecture rule: dependencies point inward only. These tests fail the build if a layer reaches outward.</summary>
public sealed class LayerTests
{
    private static readonly Assembly Domain = typeof(Treasury.Cashflow.Domain.Instruments.Instrument).Assembly;
    private static readonly Assembly Application = typeof(Treasury.Cashflow.Application.Instruments.InstrumentUseCases).Assembly;
    private static readonly Assembly Infrastructure = typeof(Treasury.Cashflow.Infrastructure.InstrumentSnapshot).Assembly;
    private static readonly Assembly Api = typeof(Treasury.Cashflow.Api.Endpoints).Assembly;

    private static List<string> References(Assembly assembly) =>
        assembly.GetReferencedAssemblies().Select(a => a.Name!).ToList();

    [Fact]
    public void Domain_references_only_the_base_library_and_building_blocks()
    {
        var allowed = new[] { "System.Runtime", "System.Collections", "System.Linq", "System.Globalization", "System.Memory", "Treasury.BuildingBlocks.Domain" };
        var unexpected = References(Domain).Where(r => !allowed.Contains(r) && !r.StartsWith("System.", StringComparison.Ordinal)).ToList();
        Assert.Empty(unexpected);
        Assert.DoesNotContain("Microsoft.AspNetCore", string.Join(",", References(Domain)));
    }

    [Fact]
    public void Domain_does_not_know_application_infrastructure_or_web() =>
        Assert.True(References(Domain).All(r => !r.StartsWith("Treasury.Cashflow.", StringComparison.Ordinal)));

    [Fact]
    public void Application_depends_on_domain_but_not_on_infrastructure_or_web()
    {
        var refs = References(Application);
        Assert.Contains("Treasury.Cashflow.Domain", refs);
        Assert.DoesNotContain("Treasury.Cashflow.Infrastructure", refs);
        Assert.True(refs.All(r => !r.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal)));
    }

    [Fact]
    public void Infrastructure_does_not_depend_on_the_web_layer() =>
        Assert.True(References(Infrastructure).All(r => !r.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal) && r != "Treasury.Cashflow.Api"));

    [Fact]
    public void Domain_types_expose_no_public_setters()
    {
        var offenders = Domain.GetTypes()
            .Where(t => t.IsClass && t.IsPublic && !IsRecord(t))
            .SelectMany(t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.SetMethod is { IsPublic: true })
                .Select(p => $"{t.Name}.{p.Name}"))
            .ToList();
        Assert.Empty(offenders);
    }

    [Fact]
    public void The_api_assembly_is_loaded() => Assert.NotNull(Api);

    private static bool IsRecord(Type t) => t.GetMethod("<Clone>$") is not null;
}
