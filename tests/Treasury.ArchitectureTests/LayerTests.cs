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

    /// <summary>Every infrastructure assembly next to the tests (Infrastructure, and Infrastructure.Postgres when built with NuGet).</summary>
    [Fact]
    public void No_infrastructure_assembly_depends_on_the_web_layer()
    {
        var assemblies = Directory.GetFiles(AppContext.BaseDirectory, "Treasury.*.Infrastructure*.dll")
            .Select(f => Assembly.LoadFrom(f))
            .ToList();
        Assert.NotEmpty(assemblies);
        foreach (var assembly in assemblies)
        {
            var offending = References(assembly).Where(r => r.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal) || r.EndsWith(".Api", StringComparison.Ordinal)).ToList();
            Assert.True(offending.Count == 0, $"{assembly.GetName().Name} references {string.Join(", ", offending)}");
        }
    }

    private static readonly string[] Services = ["Cashflow", "Reference"];

    /// <summary>Every service follows the same layer rules (one folder per service, CLAUDE.md). Services do not reference each other.</summary>
    [Theory]
    [InlineData("Cashflow")]
    [InlineData("Reference")]
    public void Each_service_keeps_its_layers_and_does_not_reference_another_service(string service)
    {
        Assembly Load(string layer) => Assembly.LoadFrom(Path.Combine(AppContext.BaseDirectory, $"Treasury.{service}.{layer}.dll"));
        var domain = References(Load("Domain"));
        var application = References(Load("Application"));
        Assert.True(domain.All(r => r.StartsWith("System.", StringComparison.Ordinal) || r == "System" || r == "Treasury.BuildingBlocks.Domain"),
            $"{service}.Domain references {string.Join(", ", domain)}");
        Assert.Contains($"Treasury.{service}.Domain", application);
        Assert.True(application.All(r => !r.Contains(".Infrastructure", StringComparison.Ordinal) && !r.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal)));

        var others = Services.Where(s => s != service).ToList();
        foreach (var file in Directory.GetFiles(AppContext.BaseDirectory, $"Treasury.{service}.*.dll"))
        {
            var refs = References(Assembly.LoadFrom(file));
            Assert.True(refs.All(r => others.All(o => !r.StartsWith($"Treasury.{o}.", StringComparison.Ordinal))),
                $"{Path.GetFileName(file)} references another service: {string.Join(", ", refs)}");
        }
    }

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
