using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using TeamLeave.Api.Controllers;
using TeamLeave.Application.Abstractions;
using TeamLeave.Domain.Common;
using TeamLeave.Infrastructure.Persistence;

namespace TeamLeave.ArchitectureTests;

/// <summary>
/// Règles de dépendance de step2_arch §3.3, vérifiées par réflexion (sans NetArchTest, voir step3_backend.md),
/// et traçabilité RB-01 à RB-28 (ENF-I3, §8.2).
/// </summary>
public sealed class LayeringTests
{
    private static readonly Assembly Domain = typeof(DomainException).Assembly;
    private static readonly Assembly Application = typeof(UseCaseExecutor).Assembly;
    private static readonly Assembly Infrastructure = typeof(TeamLeaveDbContext).Assembly;
    private static readonly Assembly Api = typeof(LeaveRequestsController).Assembly;

    private static IReadOnlyList<string> References(Assembly assembly) =>
        assembly.GetReferencedAssemblies().Select(a => a.Name!).ToList();

    [Fact]
    public void Domain_depends_on_the_BCL_only()
    {
        var offending = References(Domain)
            .Where(n => !(n.StartsWith("System", StringComparison.Ordinal) || n is "netstandard" or "mscorlib"))
            .ToList();

        Assert.Empty(offending);
    }

    [Fact]
    public void Application_does_not_depend_on_infrastructure_api_aspnetcore_or_providers()
    {
        var offending = References(Application)
            .Where(n => n is "TeamLeave.Infrastructure" or "TeamLeave.Api"
                || n.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal)
                || n.StartsWith("Microsoft.EntityFrameworkCore.SqlServer", StringComparison.Ordinal)
                || n.StartsWith("Microsoft.EntityFrameworkCore.Sqlite", StringComparison.Ordinal)
                || n.StartsWith("Microsoft.Data.", StringComparison.Ordinal)
                || n == "System.Net.Mail")
            .ToList();

        Assert.Empty(offending);
    }

    [Fact]
    public void Infrastructure_does_not_depend_on_api()
    {
        Assert.DoesNotContain("TeamLeave.Api", References(Infrastructure));
    }

    [Fact]
    public void Controllers_never_expose_domain_entities_nor_use_the_dbcontext()
    {
        var controllers = Api.GetTypes().Where(t => typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract).ToList();
        Assert.NotEmpty(controllers);

        foreach (var controller in controllers)
        {
            var ctorParams = controller.GetConstructors().SelectMany(c => c.GetParameters()).Select(p => p.ParameterType);
            Assert.DoesNotContain(ctorParams, t => t == typeof(TeamLeaveDbContext) || t == typeof(ITeamLeaveDbContext));

            foreach (var action in controller.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                var types = action.GetParameters().Select(p => p.ParameterType).Append(action.ReturnType);
                foreach (var type in types.SelectMany(Flatten))
                {
                    Assert.False(
                        type.Assembly == Domain && !type.IsEnum,
                        $"{controller.Name}.{action.Name} expose le type du domaine {type.Name}.");
                }
            }
        }
    }

    [Fact]
    public void Domain_and_application_never_read_the_system_clock()
    {
        var root = RepositoryRoot();
        var files = new[] { "src/TeamLeave.Domain", "src/TeamLeave.Application" }
            .SelectMany(dir => Directory.EnumerateFiles(Path.Combine(root, dir), "*.cs", SearchOption.AllDirectories))
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal));

        var offending = files.Where(f => Regex.IsMatch(File.ReadAllText(f), @"DateTime(Offset)?\.(Now|UtcNow|Today)\b")).ToList();

        Assert.Empty(offending);
    }

    [Fact]
    public void Every_business_rule_RB01_to_RB28_is_referenced_by_at_least_one_test()
    {
        var root = RepositoryRoot();
        var text = string.Concat(Directory
            .EnumerateFiles(Path.Combine(root, "tests"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Select(File.ReadAllText));
        var referenced = Regex.Matches(text, @"Trait\(""Rule"", ""(RB-\d{2})""\)").Select(m => m.Groups[1].Value).ToHashSet();

        var missing = Enumerable.Range(1, 28).Select(i => $"RB-{i:00}").Where(rb => !referenced.Contains(rb)).ToList();

        Assert.Empty(missing);
    }

    private static IEnumerable<Type> Flatten(Type type)
    {
        yield return type;
        if (type.IsGenericType)
        {
            foreach (var arg in type.GetGenericArguments().SelectMany(Flatten))
            {
                yield return arg;
            }
        }

        if (type.IsArray)
        {
            foreach (var inner in Flatten(type.GetElementType()!))
            {
                yield return inner;
            }
        }
    }

    private static string RepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "TeamLeave.sln")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("TeamLeave.sln introuvable.");
    }
}
