using System.Xml.Linq;

namespace Platform.Architecture.Tests;

public class DependencyDirectionTests
{
    private static readonly string RepositoryRoot = LocateRepositoryRoot();

    private static readonly string[] ProductionProjects =
    {
        "src/Platform.Core/Platform.Core.csproj",
        "src/Platform.AspNetCore/Platform.AspNetCore.csproj",
        "src/Platform.Billing.Contracts/Platform.Billing.Contracts.csproj",
        "src/Platform.Eventing/Platform.Eventing.csproj",
        "src/Platform.Idempotency/Platform.Idempotency.csproj",
        "src/Platform.Jobs/Platform.Jobs.csproj",
        "src/Platform.Mailing/Platform.Mailing.csproj",
        "src/Platform.RateLimiting/Platform.RateLimiting.csproj",
        "src/Platform.Web/Platform.Web.csproj",
        "src/Platform.Testing/Platform.Testing.csproj",
        "src/Platform.Persistence.EfCore/Platform.Persistence.EfCore.csproj",
        "src/Platform.Persistence.Postgres/Platform.Persistence.Postgres.csproj",
    };

    private static readonly string[] TestOnlyAssemblyNames =
    {
        "Platform.Architecture.Tests",
        "Platform.Core.Tests",
        "Platform.AspNetCore.Tests",
        "Platform.Billing.Contracts.Tests",
        "Platform.Testing.Tests",
        "Platform.Web.Tests",
        "Platform.Persistence.EfCore.Tests",
        "Platform.Persistence.Postgres.Tests",
    };

    private static readonly string[] FrameworkIndependentProjects =
    {
        "src/Platform.Core/Platform.Core.csproj",
        "src/Platform.Billing.Contracts/Platform.Billing.Contracts.csproj",
    };

    private static readonly string[] ForbiddenProductionPackagePrefixes =
    {
        "Microsoft.AspNetCore",
        "Microsoft.EntityFrameworkCore",
        "Stripe",
        "Stripe.net",
    };

    private static readonly string[] ForbiddenJobsPackagePrefixes =
    {
        "Microsoft.AspNetCore",
        "Microsoft.EntityFrameworkCore",
        "Hangfire",
        "Quartz",
    };

    private static readonly string[] ForbiddenJobsProjectSegments =
    {
        "VisualFlow",
    };

    private static readonly string[] ForbiddenMailingPackagePrefixes =
    {
        "Microsoft.AspNetCore",
        "Microsoft.EntityFrameworkCore",
        "SendGrid",
        "Mailgun",
        "Smtp",
        "Razor",
        "Liquid",
    };

    private static readonly string[] ForbiddenMailingProjectSegments =
    {
        "VisualFlow",
    };

    private static readonly string[] ForbiddenEventingPackagePrefixes =
    {
        "Microsoft.AspNetCore",
        "Microsoft.EntityFrameworkCore",
        "RabbitMQ",
    };

    private static readonly string[] ForbiddenEventingProjectSegments =
    {
        "VisualFlow",
    };

    private static readonly string[] ForbiddenIdempotencyPackagePrefixes =
    {
        "Microsoft.AspNetCore",
        "Microsoft.EntityFrameworkCore",
        "StackExchange.Redis",
    };

    private static readonly string[] ForbiddenIdempotencyProjectSegments =
    {
        "VisualFlow",
    };

    private static readonly string[] ForbiddenRateLimitingPackagePrefixes =
    {
        "Microsoft.AspNetCore",
        "Microsoft.EntityFrameworkCore",
        "StackExchange.Redis",
    };

    private static readonly string[] ForbiddenRateLimitingProjectSegments =
    {
        "VisualFlow",
    };

    private static readonly string[] ForbiddenPersistenceEfCorePackagePrefixes =
    {
        "Microsoft.AspNetCore",
        "Npgsql",
        "Stripe",
        "StackExchange.Redis",
        "VisualFlow",
    };

    public static IEnumerable<object[]> ProductionProjectsData() =>
        ProductionProjects.Select(p => new object[] { p });

    [Theory]
    [MemberData(nameof(ProductionProjectsData))]
    public void Production_project_exists(string relativePath)
    {
        var fullPath = Path.Combine(RepositoryRoot, relativePath);
        Assert.True(File.Exists(fullPath), $"Project file not found: {relativePath}");
    }

    [Theory]
    [MemberData(nameof(ProductionProjectsData))]
    public void Production_project_does_not_reference_test_projects(string relativePath)
    {
        var references = ReadProjectReferences(relativePath);
        var leaks = references
            .Intersect(TestOnlyAssemblyNames, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Assert.True(
            leaks.Length == 0,
            $"{relativePath} must not reference test projects but references: {string.Join(", ", leaks)}");
    }

    [Theory]
    [MemberData(nameof(ProductionProjectsData))]
    public void Production_project_does_not_reference_forbidden_frameworks(string relativePath)
    {
        if (relativePath.Contains("Persistence.EfCore", StringComparison.OrdinalIgnoreCase)
            || relativePath.Contains("Persistence.Postgres", StringComparison.OrdinalIgnoreCase))
            return;
        var packages = ReadPackageReferences(relativePath);
        var violations = packages
            .Where(p => ForbiddenProductionPackagePrefixes.Any(prefix =>
                p.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"{relativePath} must not reference forbidden packages but references: {string.Join(", ", violations)}");
    }

    [Fact]
    public void Platform_Persistence_EfCore_only_references_Platform_Core()
    {
        var references = ReadProjectReferences("src/Platform.Persistence.EfCore/Platform.Persistence.EfCore.csproj");

        Assert.True(
            references.Length == 1 && references[0].Equals("Platform.Core", StringComparison.OrdinalIgnoreCase),
            "Platform.Persistence.EfCore must reference only Platform.Core but references: " + string.Join(", ", references));
    }

    [Fact]
    public void Platform_Persistence_EfCore_has_no_provider_specific_project_or_package_references()
    {
        var projectReferences = ReadProjectReferences("src/Platform.Persistence.EfCore/Platform.Persistence.EfCore.csproj");
        var packages = ReadPackageReferences("src/Platform.Persistence.EfCore/Platform.Persistence.EfCore.csproj");
        var violations = packages.Where(package => ForbiddenPersistenceEfCorePackagePrefixes.Any(prefix =>
            package.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))).ToArray();

        Assert.DoesNotContain(projectReferences, reference => reference.Contains("Postgres", StringComparison.OrdinalIgnoreCase));
        Assert.Empty(violations);
    }

    [Fact]
    public void Platform_Persistence_Postgres_only_references_EfCore_persistence()
    {
        var references = ReadProjectReferences("src/Platform.Persistence.Postgres/Platform.Persistence.Postgres.csproj");

        Assert.True(
            references.Length == 1 && references[0].Equals("Platform.Persistence.EfCore", StringComparison.OrdinalIgnoreCase),
            "Platform.Persistence.Postgres must reference only Platform.Persistence.EfCore but references: " + string.Join(", ", references));
    }

    [Theory]
    [InlineData("src/Platform.Core/Platform.Core.csproj")]
    [InlineData("src/Platform.Billing.Contracts/Platform.Billing.Contracts.csproj")]
    public void Framework_independent_project_has_no_project_references(string relativePath)
    {
        var references = ReadProjectReferences(relativePath);

        Assert.True(
            references.Length == 0,
            $"{relativePath} must not reference other projects but references: {string.Join(", ", references)}");
    }

    [Fact]
    public void Platform_AspNetCore_only_references_Platform_Core()
    {
        var references = ReadProjectReferences("src/Platform.AspNetCore/Platform.AspNetCore.csproj");

        Assert.True(
            references.Length == 1 && references[0].Equals("Platform.Core", StringComparison.OrdinalIgnoreCase),
            $"Platform.AspNetCore must reference only Platform.Core but references: {string.Join(", ", references)}");
    }

    [Fact]
    public void Platform_Testing_references_at_least_one_public_contract()
    {
        var references = ReadProjectReferences("src/Platform.Testing/Platform.Testing.csproj");

        Assert.Contains("Platform.Core", references, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void Platform_Core_has_no_package_references()
    {
        var packages = ReadPackageReferences("src/Platform.Core/Platform.Core.csproj");

        Assert.True(
            packages.Length == 0,
            "Platform.Core must remain dependency-light but references: " + string.Join(", ", packages));
    }

    [Fact]
    public void Platform_Billing_Contracts_has_no_package_references()
    {
        var packages = ReadPackageReferences("src/Platform.Billing.Contracts/Platform.Billing.Contracts.csproj");

        Assert.True(
            packages.Length == 0,
            "Platform.Billing.Contracts must remain dependency-light but references: " + string.Join(", ", packages));
    }

    [Fact]
    public void Only_Platform_AspNetCore_declares_a_FrameworkReference()
    {
        foreach (var project in ProductionProjects)
        {
            var references = ReadFrameworkReferences(project);
            if (project.EndsWith("Platform.AspNetCore.csproj", StringComparison.OrdinalIgnoreCase)
                || project.EndsWith("Platform.Web.csproj", StringComparison.OrdinalIgnoreCase))
            {
                Assert.True(
                    references.Length == 1 && references[0].Equals("Microsoft.AspNetCore.App", StringComparison.OrdinalIgnoreCase),
                    $"Platform.AspNetCore must declare only Microsoft.AspNetCore.App but declares: {string.Join(", ", references)}");
            }
            else
            {
                Assert.True(
                    references.Length == 0,
                    $"{project} must not declare FrameworkReferences but declares: {string.Join(", ", references)}");
            }
        }
    }

    [Fact]
    public void Platform_Web_only_references_Platform_AspNetCore()
    {
        var references = ReadProjectReferences("src/Platform.Web/Platform.Web.csproj");

        Assert.True(
            references.Length == 1 && references[0].Equals("Platform.AspNetCore", StringComparison.OrdinalIgnoreCase),
            "Platform.Web must reference only Platform.AspNetCore but references: " + string.Join(", ", references));
    }

    [Fact]
    public void Platform_Jobs_does_not_reference_forbidden_packages()
    {
        var packages = ReadPackageReferences("src/Platform.Jobs/Platform.Jobs.csproj");
        var violations = packages
            .Where(p => ForbiddenJobsPackagePrefixes.Any(prefix =>
                p.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        Assert.True(
            violations.Length == 0,
            "Platform.Jobs must not reference forbidden packages but references: " + string.Join(", ", violations));
    }

    [Fact]
    public void Platform_Jobs_only_references_Platform_Core()
    {
        var references = ReadProjectReferences("src/Platform.Jobs/Platform.Jobs.csproj");

        Assert.True(
            references.Length == 1 && references[0].Equals("Platform.Core", StringComparison.OrdinalIgnoreCase),
            "Platform.Jobs must reference only Platform.Core but references: " + string.Join(", ", references));
    }

    [Fact]
    public void Platform_Jobs_does_not_reference_visual_flow_projects()
    {
        var fullPath = Path.Combine(RepositoryRoot, "src/Platform.Jobs/Platform.Jobs.csproj");
        var document = XDocument.Load(fullPath);

        var projectReferences = document
            .Descendants()
            .Where(e => string.Equals(e.Name.LocalName, "ProjectReference", StringComparison.OrdinalIgnoreCase))
            .Select(e => e.Attribute("Include")?.Value ?? string.Empty)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToArray();

        var violations = projectReferences
            .Where(reference => ForbiddenJobsProjectSegments.Any(segment =>
                reference.Contains(segment, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        Assert.True(
            violations.Length == 0,
            "Platform.Jobs must not reference VisualFlow projects but references: " + string.Join(", ", violations));
    }

    [Fact]
    public void Platform_Mailing_does_not_reference_forbidden_packages()
    {
        var packages = ReadPackageReferences("src/Platform.Mailing/Platform.Mailing.csproj");
        var violations = packages
            .Where(p => ForbiddenMailingPackagePrefixes.Any(prefix =>
                p.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        Assert.True(
            violations.Length == 0,
            "Platform.Mailing must not reference forbidden packages but references: " + string.Join(", ", violations));
    }

    [Fact]
    public void Platform_Mailing_only_references_Platform_Core()
    {
        var references = ReadProjectReferences("src/Platform.Mailing/Platform.Mailing.csproj");

        Assert.True(
            references.Length == 1 && references[0].Equals("Platform.Core", StringComparison.OrdinalIgnoreCase),
            "Platform.Mailing must reference only Platform.Core but references: " + string.Join(", ", references));
    }

    [Fact]
    public void Platform_Mailing_does_not_reference_visual_flow_projects()
    {
        var fullPath = Path.Combine(RepositoryRoot, "src/Platform.Mailing/Platform.Mailing.csproj");
        var document = XDocument.Load(fullPath);

        var projectReferences = document
            .Descendants()
            .Where(e => string.Equals(e.Name.LocalName, "ProjectReference", StringComparison.OrdinalIgnoreCase))
            .Select(e => e.Attribute("Include")?.Value ?? string.Empty)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToArray();

        var violations = projectReferences
            .Where(reference => ForbiddenMailingProjectSegments.Any(segment =>
                reference.Contains(segment, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        Assert.True(
            violations.Length == 0,
            "Platform.Mailing must not reference VisualFlow projects but references: " + string.Join(", ", violations));
    }

    [Fact]
    public void Platform_Eventing_does_not_reference_forbidden_packages()
    {
        var packages = ReadPackageReferences("src/Platform.Eventing/Platform.Eventing.csproj");
        var violations = packages
            .Where(p => ForbiddenEventingPackagePrefixes.Any(prefix =>
                p.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        Assert.True(
            violations.Length == 0,
            "Platform.Eventing must not reference forbidden packages but references: " + string.Join(", ", violations));
    }

    [Fact]
    public void Platform_Eventing_only_references_Platform_Core()
    {
        var references = ReadProjectReferences("src/Platform.Eventing/Platform.Eventing.csproj");

        Assert.True(
            references.Length == 1 && references[0].Equals("Platform.Core", StringComparison.OrdinalIgnoreCase),
            "Platform.Eventing must reference only Platform.Core but references: " + string.Join(", ", references));
    }

    [Fact]
    public void Platform_Eventing_does_not_reference_visual_flow_projects()
    {
        var fullPath = Path.Combine(RepositoryRoot, "src/Platform.Eventing/Platform.Eventing.csproj");
        var document = XDocument.Load(fullPath);

        var projectReferences = document
            .Descendants()
            .Where(e => string.Equals(e.Name.LocalName, "ProjectReference", StringComparison.OrdinalIgnoreCase))
            .Select(e => e.Attribute("Include")?.Value ?? string.Empty)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToArray();

        var violations = projectReferences
            .Where(reference => ForbiddenEventingProjectSegments.Any(segment =>
                reference.Contains(segment, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        Assert.True(
            violations.Length == 0,
            "Platform.Eventing must not reference VisualFlow projects but references: " + string.Join(", ", violations));
    }

    [Fact]
    public void Platform_Idempotency_does_not_reference_forbidden_packages()
    {
        var packages = ReadPackageReferences("src/Platform.Idempotency/Platform.Idempotency.csproj");
        var violations = packages
            .Where(p => ForbiddenIdempotencyPackagePrefixes.Any(prefix =>
                p.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        Assert.True(
            violations.Length == 0,
            "Platform.Idempotency must not reference forbidden packages but references: " + string.Join(", ", violations));
    }

    [Fact]
    public void Platform_Idempotency_only_references_Platform_Core()
    {
        var references = ReadProjectReferences("src/Platform.Idempotency/Platform.Idempotency.csproj");

        Assert.True(
            references.Length == 1 && references[0].Equals("Platform.Core", StringComparison.OrdinalIgnoreCase),
            "Platform.Idempotency must reference only Platform.Core but references: " + string.Join(", ", references));
    }

    [Fact]
    public void Platform_Idempotency_does_not_reference_visual_flow_projects()
    {
        var fullPath = Path.Combine(RepositoryRoot, "src/Platform.Idempotency/Platform.Idempotency.csproj");
        var document = XDocument.Load(fullPath);

        var projectReferences = document
            .Descendants()
            .Where(e => string.Equals(e.Name.LocalName, "ProjectReference", StringComparison.OrdinalIgnoreCase))
            .Select(e => e.Attribute("Include")?.Value ?? string.Empty)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToArray();

        var violations = projectReferences
            .Where(reference => ForbiddenIdempotencyProjectSegments.Any(segment =>
                reference.Contains(segment, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        Assert.True(
            violations.Length == 0,
            "Platform.Idempotency must not reference VisualFlow projects but references: " + string.Join(", ", violations));
    }

    [Fact]
    public void Platform_RateLimiting_does_not_reference_forbidden_packages()
    {
        var packages = ReadPackageReferences("src/Platform.RateLimiting/Platform.RateLimiting.csproj");
        var violations = packages
            .Where(p => ForbiddenRateLimitingPackagePrefixes.Any(prefix =>
                p.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        Assert.True(
            violations.Length == 0,
            "Platform.RateLimiting must not reference forbidden packages but references: " + string.Join(", ", violations));
    }

    [Fact]
    public void Platform_RateLimiting_only_references_Platform_Core()
    {
        var references = ReadProjectReferences("src/Platform.RateLimiting/Platform.RateLimiting.csproj");

        Assert.True(
            references.Length == 1 && references[0].Equals("Platform.Core", StringComparison.OrdinalIgnoreCase),
            "Platform.RateLimiting must reference only Platform.Core but references: " + string.Join(", ", references));
    }

    [Fact]
    public void Platform_RateLimiting_does_not_reference_visual_flow_projects()
    {
        var fullPath = Path.Combine(RepositoryRoot, "src/Platform.RateLimiting/Platform.RateLimiting.csproj");
        var document = XDocument.Load(fullPath);

        var projectReferences = document
            .Descendants()
            .Where(e => string.Equals(e.Name.LocalName, "ProjectReference", StringComparison.OrdinalIgnoreCase))
            .Select(e => e.Attribute("Include")?.Value ?? string.Empty)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToArray();

        var violations = projectReferences
            .Where(reference => ForbiddenRateLimitingProjectSegments.Any(segment =>
                reference.Contains(segment, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        Assert.True(
            violations.Length == 0,
            "Platform.RateLimiting must not reference VisualFlow projects but references: " + string.Join(", ", violations));
    }

    private static string LocateRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Directory.Build.props"))
                && File.Exists(Path.Combine(current.FullName, "Platform.sln")))
            {
                return current.FullName;
            }
            current = current.Parent;
        }

        throw new InvalidOperationException(
            "Unable to locate the repository root from " + AppContext.BaseDirectory);
    }

    private static string[] ReadProjectReferences(string relativePath)
    {
        var fullPath = Path.Combine(RepositoryRoot, relativePath);
        var document = XDocument.Load(fullPath);

        return document
            .Descendants()
            .Where(e => string.Equals(e.Name.LocalName, "ProjectReference", StringComparison.OrdinalIgnoreCase))
            .Select(e => e.Attribute("Include")?.Value ?? string.Empty)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => Path.GetFileNameWithoutExtension(value.Split('\\', '/').Last()) ?? string.Empty)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToArray();
    }

    private static string[] ReadPackageReferences(string relativePath)
    {
        var fullPath = Path.Combine(RepositoryRoot, relativePath);
        var document = XDocument.Load(fullPath);

        return document
            .Descendants()
            .Where(e => string.Equals(e.Name.LocalName, "PackageReference", StringComparison.OrdinalIgnoreCase))
            .Select(e => e.Attribute("Include")?.Value ?? string.Empty)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToArray();
    }

    private static string[] ReadFrameworkReferences(string relativePath)
    {
        var fullPath = Path.Combine(RepositoryRoot, relativePath);
        var document = XDocument.Load(fullPath);

        return document
            .Descendants()
            .Where(e => string.Equals(e.Name.LocalName, "FrameworkReference", StringComparison.OrdinalIgnoreCase))
            .Select(e => e.Attribute("Include")?.Value ?? string.Empty)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToArray();
    }
}
