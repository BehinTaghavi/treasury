using Npgsql;
using Treasury.Tests.Support;
using Xunit;

[assembly: AssemblyFixture(typeof(PostgresDatabase))]

namespace Treasury.Tests.Support;

// Shared by the PostgreSQL adapter test projects of every service (linked into each; see their .csproj).

/// <summary>
/// One fresh test database for the whole test run, built from the real schema scripts in db/postgres
/// the same way db/postgres/apply.sh does it: roles as administrator, every object as treasury_owner.
/// The adapters then act as their service's role (<see cref="ServiceAs"/>), so missing grants fail the tests too.
/// </summary>
public sealed class PostgresDatabase : IAsyncLifetime
{
    public const string ConnectionVariable = "TREASURY_TEST_PG";

    private readonly string? _admin = Environment.GetEnvironmentVariable(ConnectionVariable);
    private readonly string _name = $"treasury_adapter_test_{Environment.ProcessId}";

    /// <summary>Connection string of the cashflow service (role cashflow_svc) to the test database.</summary>
    public string Service => ServiceAs("cashflow_svc");

    /// <summary>Connection string that acts as <paramref name="role"/> (e.g. reference_svc) on the test database.</summary>
    public string ServiceAs(string role) => new NpgsqlConnectionStringBuilder(Admin) { Options = $"-c role={role}" }.ConnectionString;

    /// <summary>Administrator connection to the test database, for checks the service role may not make.</summary>
    public string Admin { get; private set; } = string.Empty;

    /// <summary>Skips the test when no database is configured on a developer machine; fails it on CI.</summary>
    public void Require()
    {
        if (!string.IsNullOrWhiteSpace(_admin))
        {
            return;
        }

        var reason = $"Set {ConnectionVariable} to run the PostgreSQL adapter tests (see the test project file).";
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI")))
        {
            Assert.Fail(reason);
        }

        Assert.Skip(reason);
    }

    public async ValueTask InitializeAsync()
    {
        if (string.IsNullOrWhiteSpace(_admin))
        {
            return;
        }

        // Test projects run in parallel and share the server's roles (000_roles.sql): one setup at a time.
        // The lock lives in the "postgres" database, which every fixture connects to, and ends with this connection.
        await using var server = new NpgsqlConnection(With(_admin, "postgres"));
        await server.OpenAsync();
        await Exec(server, "SELECT pg_advisory_lock(73170001)");
        await Exec(server, $"DROP DATABASE IF EXISTS \"{_name}\" WITH (FORCE)");
        await Exec(server, $"CREATE DATABASE \"{_name}\"");

        Admin = With(_admin, _name);
        var scripts = Directory.GetFiles(Path.Combine(RepositoryRoot(), "db", "postgres"), "0??_*.sql").Order(StringComparer.Ordinal).ToList();
        await using (var admin = new NpgsqlConnection(Admin))
        {
            await admin.OpenAsync();
            await Exec(admin, await File.ReadAllTextAsync(scripts.Single(s => Path.GetFileName(s) == "000_roles.sql")));
            await Exec(admin, $"GRANT CREATE ON DATABASE \"{_name}\" TO treasury_owner");
        }

        await using (var owner = new NpgsqlConnection(new NpgsqlConnectionStringBuilder(Admin) { Options = "-c role=treasury_owner" }.ConnectionString))
        {
            await owner.OpenAsync();
            foreach (var script in scripts.Where(s => Path.GetFileName(s) != "000_roles.sql"))
            {
                await Exec(owner, await File.ReadAllTextAsync(script));
            }
        }

        await using (var admin = new NpgsqlConnection(Admin))
        {
            await admin.OpenAsync();
            // Same years as deploy/docker-compose.yml, plus 1403 for the closed-year test.
            await Exec(admin, "SELECT cashflow.open_fiscal_year(1403, '2024-03-20', '2025-03-20')");
            await Exec(admin, "SELECT cashflow.open_fiscal_year(1404, '2025-03-21', '2026-03-20')");
            await Exec(admin, "SELECT cashflow.open_fiscal_year(1405, '2026-03-21', '2027-03-20')");
        }

    }

    public async ValueTask DisposeAsync()
    {
        if (string.IsNullOrWhiteSpace(_admin))
        {
            return;
        }

        NpgsqlConnection.ClearAllPools();
        await using var server = new NpgsqlConnection(With(_admin, "postgres"));
        await server.OpenAsync();
        await Exec(server, $"DROP DATABASE IF EXISTS \"{_name}\" WITH (FORCE)");
    }

    public async Task<T> ScalarAsync<T>(string sql, params (string Name, object Value)[] parameters)
    {
        await using var connection = new NpgsqlConnection(Admin);
        await connection.OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, connection);
        foreach (var (name, value) in parameters)
        {
            cmd.Parameters.AddWithValue(name, value);
        }

        return (T)(await cmd.ExecuteScalarAsync())!;
    }

    public async Task ExecuteAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(Admin);
        await connection.OpenAsync();
        await Exec(connection, sql);
    }

    private static string With(string connectionString, string database) =>
        new NpgsqlConnectionStringBuilder(connectionString) { Database = database }.ConnectionString;

    private static async Task Exec(NpgsqlConnection connection, string sql)
    {
        await using var cmd = new NpgsqlCommand(sql, connection);
        await cmd.ExecuteNonQueryAsync();
    }

    private static string RepositoryRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Treasury.slnx")))
            {
                return dir.FullName;
            }
        }

        throw new InvalidOperationException("Repository root (Treasury.slnx) not found above " + AppContext.BaseDirectory);
    }
}
