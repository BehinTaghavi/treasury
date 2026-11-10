using System.Collections;
using System.Reflection;

namespace Xunit;

[AttributeUsage(AttributeTargets.Method)]
public class FactAttribute : Attribute
{
    public string? Skip { get; set; }
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class TheoryAttribute : FactAttribute;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class InlineDataAttribute(params object?[] data) : Attribute
{
    public object?[] Data { get; } = data;
}

public sealed class AssertException(string message) : Exception(message);

public static class Assert
{
    public static void True(bool condition, string? message = null)
    {
        if (!condition) throw new AssertException(message ?? "Expected true.");
    }

    public static void False(bool condition, string? message = null)
    {
        if (condition) throw new AssertException(message ?? "Expected false.");
    }

    public static void Equal<T>(T expected, T actual)
    {
        if (expected is IEnumerable e && actual is IEnumerable a && expected is not string)
        {
            Equal(e.Cast<object?>(), a.Cast<object?>());
            return;
        }

        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new AssertException($"Expected: {expected}{Environment.NewLine}Actual:   {actual}");
        }
    }

    public static void Equal<T>(IEnumerable<T> expected, IEnumerable<T> actual)
    {
        var e = expected.ToList();
        var a = actual.ToList();
        if (!e.SequenceEqual(a))
        {
            throw new AssertException($"Expected: [{string.Join(", ", e)}]{Environment.NewLine}Actual:   [{string.Join(", ", a)}]");
        }
    }

    public static void NotEqual<T>(T expected, T actual)
    {
        if (EqualityComparer<T>.Default.Equals(expected, actual)) throw new AssertException($"Did not expect: {actual}");
    }

    public static void Null(object? value)
    {
        if (value is not null) throw new AssertException($"Expected null, got {value}.");
    }

    public static void NotNull(object? value)
    {
        if (value is null) throw new AssertException("Expected a value, got null.");
    }

    public static void Empty(IEnumerable collection)
    {
        if (collection.Cast<object?>().Any()) throw new AssertException("Expected an empty collection.");
    }

    public static void NotEmpty(IEnumerable collection)
    {
        if (!collection.Cast<object?>().Any()) throw new AssertException("Expected a non-empty collection.");
    }

    public static T Single<T>(IEnumerable<T> collection)
    {
        var list = collection.ToList();
        if (list.Count != 1) throw new AssertException($"Expected exactly one item, found {list.Count}.");
        return list[0];
    }

    public static void Contains<T>(T expected, IEnumerable<T> collection)
    {
        if (!collection.Contains(expected)) throw new AssertException($"Collection does not contain {expected}.");
    }

    public static void Contains(string expected, string? actual)
    {
        if (actual is null || !actual.Contains(expected, StringComparison.Ordinal)) throw new AssertException($"'{actual}' does not contain '{expected}'.");
    }

    public static void DoesNotContain(string expected, string? actual)
    {
        if (actual is not null && actual.Contains(expected, StringComparison.Ordinal)) throw new AssertException($"'{actual}' should not contain '{expected}'.");
    }

    public static void DoesNotContain<T>(T expected, IEnumerable<T> collection)
    {
        if (collection.Contains(expected)) throw new AssertException($"Collection should not contain {expected}.");
    }

    public static T IsType<T>(object? value)
    {
        if (value is null || value.GetType() != typeof(T)) throw new AssertException($"Expected type {typeof(T).Name}, got {value?.GetType().Name ?? "null"}.");
        return (T)value;
    }

    public static T Throws<T>(Action action)
        where T : Exception
    {
        try
        {
            action();
        }
        catch (T ex) when (ex.GetType() == typeof(T))
        {
            return ex;
        }
        catch (Exception ex)
        {
            throw new AssertException($"Expected {typeof(T).Name}, got {ex.GetType().Name}: {ex.Message}");
        }

        throw new AssertException($"Expected {typeof(T).Name}, but nothing was thrown.");
    }

    public static async Task<T> ThrowsAsync<T>(Func<Task> action)
        where T : Exception
    {
        try
        {
            await action().ConfigureAwait(false);
        }
        catch (T ex) when (ex.GetType() == typeof(T))
        {
            return ex;
        }
        catch (Exception ex)
        {
            throw new AssertException($"Expected {typeof(T).Name}, got {ex.GetType().Name}: {ex.Message}");
        }

        throw new AssertException($"Expected {typeof(T).Name}, but nothing was thrown.");
    }
}

/// <summary>Finds [Fact]/[Theory] methods by reflection and runs them. Returns the number of failures.</summary>
public static class OfflineRunner
{
    public static async Task<int> RunAsync(Assembly assembly)
    {
        int passed = 0, failed = 0, skipped = 0;
        foreach (var type in assembly.GetTypes().Where(t => t.IsClass && !t.IsAbstract).OrderBy(t => t.FullName, StringComparer.Ordinal))
        {
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance).OrderBy(m => m.Name, StringComparer.Ordinal))
            {
                var fact = method.GetCustomAttribute<FactAttribute>();
                if (fact is null) continue;
                if (fact.Skip is not null)
                {
                    skipped++;
                    continue;
                }

                var cases = method.GetCustomAttributes<InlineDataAttribute>().Select(d => d.Data).ToList();
                if (cases.Count == 0) cases.Add([]);
                foreach (var args in cases)
                {
                    var name = $"{type.Name}.{method.Name}({string.Join(", ", args.Select(a => a?.ToString() ?? "null"))})";
                    try
                    {
                        var instance = Activator.CreateInstance(type);
                        var parameters = method.GetParameters();
                        var converted = args.Select((a, i) => a is null || parameters[i].ParameterType.IsInstanceOfType(a)
                            ? a
                            : Convert.ChangeType(a, Nullable.GetUnderlyingType(parameters[i].ParameterType) ?? parameters[i].ParameterType, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
                        var result = method.Invoke(instance, converted);
                        if (result is Task task) await task.ConfigureAwait(false);
                        (instance as IDisposable)?.Dispose();
                        passed++;
                    }
                    catch (Exception ex)
                    {
                        var inner = ex is TargetInvocationException { InnerException: { } ie } ? ie : ex;
                        failed++;
                        Console.WriteLine($"FAIL {name}{Environment.NewLine}     {inner.GetType().Name}: {inner.Message}");
                    }
                }
            }
        }

        Console.WriteLine($"{assembly.GetName().Name}: {passed} passed, {failed} failed, {skipped} skipped");
        return failed;
    }
}
