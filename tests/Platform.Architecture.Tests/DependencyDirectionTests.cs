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
        "src/Platform.Eventing.Contracts/Platform.Eventing.Contracts.csproj",
        "src/Platform.Eventing.EfCore/Platform.Eventing.EfCore.csproj",
        "src/Platform.Caching/Platform.Caching.csproj",
        "src/Platform.Caching.Hybrid/Platform.Caching.Hybrid.csproj",
        "src/Platform.Caching.Redis/Platform.Caching.Redis.csproj",
        "src/Platform.Storage/Platform.Storage.csproj",
        "src/Platform.Storage.Local/Platform.Storage.Local.csproj",
        "src/Platform.Storage.S3/Platform.Storage.S3.csproj",
        "src/Platform.Quota/Platform.Quota.csproj",
        "src/Platform.Quota.Testing/Platform.Quota.Testing.csproj",
        "src/Platform.Idempotency/Platform.Idempotency.csproj",
        "src/Platform.Jobs/Platform.Jobs.csproj",
        "src/Platform.Mailing/Platform.Mailing.csproj",
        "src/Platform.RateLimiting/Platform.RateLimiting.csproj",
        "src/Platform.Web/Platform.Web.csproj",
        "src/Platform.Web.Telemetry/Platform.Web.Telemetry.csproj",
        "src/Platform.Web.Cors/Platform.Web.Cors.csproj",
        "src/Platform.Web.Resilience/Platform.Web.Resilience.csproj",
        "src/Platform.Web.OpenApi/Platform.Web.OpenApi.csproj",
        "src/Platform.Testing/Platform.Testing.csproj",
        "src/Platform.Persistence.EfCore/Platform.Persistence.EfCore.csproj",
        "src/Platform.Persistence.Postgres/Platform.Persistence.Postgres.csproj",
        "src/Platform.Identity.Contracts/Platform.Identity.Contracts.csproj",
        "src/Platform.Authorization/Platform.Authorization.csproj",
        "src/Platform.Identity.AspNetCore/Platform.Identity.AspNetCore.csproj",
        "src/Platform.Identity.EntityFrameworkCore/Platform.Identity.EntityFrameworkCore.csproj",
        "src/Platform.Identity.Testing/Platform.Identity.Testing.csproj",
        "src/Platform.Admin.Contracts/Platform.Admin.Contracts.csproj",
        "src/Platform.Admin.AspNetCore/Platform.Admin.AspNetCore.csproj",
        "src/Platform.Admin.Testing/Platform.Admin.Testing.csproj",
        "src/Platform.Billing/Platform.Billing.csproj",
        "src/Platform.Billing.Testing/Platform.Billing.Testing.csproj",
        "src/Platform.Billing.Stripe/Platform.Billing.Stripe.csproj",
        "src/Platform.Billing.LemonSqueezy/Platform.Billing.LemonSqueezy.csproj",
        "src/Platform.Ai.Contracts/Platform.Ai.Contracts.csproj",
        "src/Platform.Ai/Platform.Ai.csproj",
        "src/Platform.Ai.Testing/Platform.Ai.Testing.csproj",
        "src/Platform.Ai.OpenAiCompatible/Platform.Ai.OpenAiCompatible.csproj",
        "src/Platform.Ai.Anthropic/Platform.Ai.Anthropic.csproj",
        "src/Platform.Ai.Ollama/Platform.Ai.Ollama.csproj",
        "src/Platform.Webhooks.Contracts/Platform.Webhooks.Contracts.csproj",
        "src/Platform.Webhooks.AspNetCore/Platform.Webhooks.AspNetCore.csproj",
        "src/Platform.Webhooks.EfCore/Platform.Webhooks.EfCore.csproj",
        "src/Platform.Observability/Platform.Observability.csproj",
    };

    private static readonly string[] TestOnlyAssemblyNames =
    {
        "Platform.Architecture.Tests",
        "Platform.Core.Tests",
        "Platform.AspNetCore.Tests",
        "Platform.Billing.Contracts.Tests",
        "Platform.Eventing.Contracts.Tests",
        "Platform.Eventing.EfCore.Tests",
        "Platform.Caching.Tests",
        "Platform.Caching.Adapter.Tests",
        "Platform.Storage.Tests",
        "Platform.Quota.Tests",
        "Platform.Testing.Tests",
        "Platform.Web.Tests",
        "Platform.Web.Edge.Tests",
        "Platform.Persistence.EfCore.Tests",
        "Platform.Persistence.Postgres.Tests",
        "Platform.Identity.Tests",
        "Platform.Admin.Tests",
        "Platform.Billing.Tests",
        "Platform.Billing.ProviderAdapters.Tests",
        "Platform.Ai.Tests",
        "Platform.Ai.Adapter.Tests",
        "Platform.Webhooks.Tests",
        "Platform.Observability.Tests",
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

    private static readonly string[] ForbiddenWebhooksContractsPackagePrefixes =
    {
        "Microsoft.AspNetCore",
        "Microsoft.EntityFrameworkCore",
    };

    private static readonly string[] ForbiddenWebhooksAspNetCoreProjectSegments =
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
            || relativePath.Contains("Persistence.Postgres", StringComparison.OrdinalIgnoreCase)
            || relativePath.Contains("Identity.EntityFrameworkCore", StringComparison.OrdinalIgnoreCase)
            || relativePath.Contains("Eventing.EfCore", StringComparison.OrdinalIgnoreCase)
            || relativePath.Contains("Webhooks.EfCore", StringComparison.OrdinalIgnoreCase)
            || relativePath.Contains("Caching.Hybrid", StringComparison.OrdinalIgnoreCase)
            || relativePath.Contains("Caching.Redis", StringComparison.OrdinalIgnoreCase))
            return;
        if (relativePath.Contains("Billing.Stripe", StringComparison.OrdinalIgnoreCase)
            || relativePath.Contains("Billing.LemonSqueezy", StringComparison.OrdinalIgnoreCase)) return;
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

    [Theory]
    [InlineData("src/Platform.Identity.Contracts/Platform.Identity.Contracts.csproj")]
    [InlineData("src/Platform.Authorization/Platform.Authorization.csproj")]
    public void Identity_contract_projects_have_no_package_or_project_references(string relativePath)
    {
        Assert.Empty(ReadPackageReferences(relativePath));
        Assert.Empty(ReadProjectReferences(relativePath));
    }

    [Fact]
    public void Platform_Identity_EntityFrameworkCore_references_only_identity_contracts_and_persistence()
    {
        var references = ReadProjectReferences("src/Platform.Identity.EntityFrameworkCore/Platform.Identity.EntityFrameworkCore.csproj");
        Assert.Equal(2, references.Length);
        Assert.Contains("Platform.Identity.Contracts", references, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Platform.Persistence.EfCore", references, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain(ReadPackageReferences("src/Platform.Identity.EntityFrameworkCore/Platform.Identity.EntityFrameworkCore.csproj"),
            package => package.StartsWith("Stripe", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Platform_Admin_Contracts_has_no_package_or_project_references()
    {
        Assert.Empty(ReadPackageReferences("src/Platform.Admin.Contracts/Platform.Admin.Contracts.csproj"));
        Assert.Empty(ReadProjectReferences("src/Platform.Admin.Contracts/Platform.Admin.Contracts.csproj"));
    }

    [Fact]
    public void Platform_Admin_Testing_references_only_admin_contracts()
    {
        var references = ReadProjectReferences("src/Platform.Admin.Testing/Platform.Admin.Testing.csproj");
        Assert.Equal(["Platform.Admin.Contracts"], references);
    }

    [Fact]
    public void Platform_Billing_only_references_core_and_billing_contracts()
    {
        var references = ReadProjectReferences("src/Platform.Billing/Platform.Billing.csproj");
        Assert.Equal(2, references.Length);
        Assert.Contains("Platform.Core", references, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Platform.Billing.Contracts", references, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void Platform_Billing_Testing_references_only_public_billing_contracts_and_core()
    {
        var references = ReadProjectReferences("src/Platform.Billing.Testing/Platform.Billing.Testing.csproj");
        Assert.Equal(2, references.Length);
        Assert.Contains("Platform.Core", references, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Platform.Billing.Contracts", references, StringComparer.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("src/Platform.Billing.Stripe/Platform.Billing.Stripe.csproj")]
    [InlineData("src/Platform.Billing.LemonSqueezy/Platform.Billing.LemonSqueezy.csproj")]
    public void Billing_provider_adapters_reference_only_platform_billing(string relativePath)
    {
        Assert.Equal(["Platform.Billing"], ReadProjectReferences(relativePath));
        Assert.DoesNotContain(ReadProjectReferences(relativePath), reference => reference.Contains("Test", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Identity_contract_packages_do_not_reference_provider_sdks()
    {
        var paths = new[]
        {
            "src/Platform.Identity.Contracts/Platform.Identity.Contracts.csproj",
            "src/Platform.Authorization/Platform.Authorization.csproj",
        };
        var forbidden = new[] { "Microsoft.AspNetCore", "Microsoft.EntityFrameworkCore", "Stripe", "Twilio", "OpenIddict" };
        foreach (var path in paths)
            Assert.DoesNotContain(ReadPackageReferences(path), package => forbidden.Any(package.StartsWith));
    }

    [Fact]
    public void Only_Platform_AspNetCore_declares_a_FrameworkReference()
    {
        foreach (var project in ProductionProjects)
        {
            var references = ReadFrameworkReferences(project);
            if (project.EndsWith("Platform.AspNetCore.csproj", StringComparison.OrdinalIgnoreCase)
                || project.EndsWith("Platform.Web.csproj", StringComparison.OrdinalIgnoreCase)
                || project.EndsWith("Platform.Identity.AspNetCore.csproj", StringComparison.OrdinalIgnoreCase)
                || project.EndsWith("Platform.Admin.AspNetCore.csproj", StringComparison.OrdinalIgnoreCase)
                || project.EndsWith("Platform.Webhooks.AspNetCore.csproj", StringComparison.OrdinalIgnoreCase)
                || project.EndsWith("Platform.Web.Cors.csproj", StringComparison.OrdinalIgnoreCase)
                || project.EndsWith("Platform.Web.Resilience.csproj", StringComparison.OrdinalIgnoreCase)
                || project.EndsWith("Platform.Web.OpenApi.csproj", StringComparison.OrdinalIgnoreCase)
                || project.EndsWith("Platform.Observability.csproj", StringComparison.OrdinalIgnoreCase))
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
    public void Platform_Eventing_Contracts_only_references_Platform_Core()
    {
        var path = "src/Platform.Eventing.Contracts/Platform.Eventing.Contracts.csproj";
        var references = ReadProjectReferences(path);

        Assert.True(
            references.Length == 1 && references[0].Equals("Platform.Core", StringComparison.OrdinalIgnoreCase),
            "Platform.Eventing.Contracts must reference only Platform.Core but references: " + string.Join(", ", references));
        Assert.DoesNotContain(ReadPackageReferences(path), package =>
            package.StartsWith("Microsoft.AspNetCore", StringComparison.OrdinalIgnoreCase)
                || package.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.OrdinalIgnoreCase)
                || package.StartsWith("RabbitMQ", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Platform_Caching_only_references_Platform_Core()
    {
        var path = "src/Platform.Caching/Platform.Caching.csproj";
        Assert.Equal(["Platform.Core"], ReadProjectReferences(path));
        Assert.DoesNotContain(ReadPackageReferences(path), package =>
            package.StartsWith("Microsoft.AspNetCore", StringComparison.OrdinalIgnoreCase)
                || package.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.OrdinalIgnoreCase)
                || package.StartsWith("StackExchange.Redis", StringComparison.OrdinalIgnoreCase)
                || package.StartsWith("Microsoft.Extensions.Caching.Hybrid", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Platform_Storage_only_references_Platform_Core()
    {
        var path = "src/Platform.Storage/Platform.Storage.csproj";
        Assert.Equal(["Platform.Core"], ReadProjectReferences(path));
        Assert.DoesNotContain(ReadPackageReferences(path), package =>
            package.StartsWith("AWSSDK", StringComparison.OrdinalIgnoreCase)
                || package.StartsWith("Amazon", StringComparison.OrdinalIgnoreCase)
                || package.StartsWith("Microsoft.AspNetCore", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Platform_Quota_only_references_Platform_Core()
    {
        var path = "src/Platform.Quota/Platform.Quota.csproj";
        Assert.Equal(["Platform.Core"], ReadProjectReferences(path));
        Assert.DoesNotContain(ReadPackageReferences(path), package =>
            package.StartsWith("Microsoft.AspNetCore", StringComparison.OrdinalIgnoreCase)
                || package.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.OrdinalIgnoreCase)
                || package.StartsWith("StackExchange.Redis", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Platform_Quota_Testing_references_only_quota()
    {
        Assert.Equal(["Platform.Quota"], ReadProjectReferences("src/Platform.Quota.Testing/Platform.Quota.Testing.csproj"));
        Assert.Empty(ReadPackageReferences("src/Platform.Quota.Testing/Platform.Quota.Testing.csproj"));
    }

    [Theory]
    [InlineData("src/Platform.Storage.Local/Platform.Storage.Local.csproj")]
    [InlineData("src/Platform.Storage.S3/Platform.Storage.S3.csproj")]
    public void Platform_Storage_adapters_reference_only_base_storage(string path)
    {
        Assert.Equal(["Platform.Storage"], ReadProjectReferences(path));
        Assert.DoesNotContain(ReadProjectReferences(path), reference => reference.Contains("Test", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("src/Platform.Caching.Hybrid/Platform.Caching.Hybrid.csproj")]
    [InlineData("src/Platform.Caching.Redis/Platform.Caching.Redis.csproj")]
    public void Platform_Caching_adapters_reference_only_base_caching(string path)
    {
        Assert.Equal(["Platform.Caching"], ReadProjectReferences(path));
        Assert.DoesNotContain(ReadProjectReferences(path), reference => reference.Contains("Test", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Platform_Eventing_EfCore_references_only_core_and_eventing_contracts()
    {
        var references = ReadProjectReferences("src/Platform.Eventing.EfCore/Platform.Eventing.EfCore.csproj");

        Assert.Equal(2, references.Length);
        Assert.Contains("Platform.Core", references, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Platform.Eventing.Contracts", references, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain(references, reference => reference.Contains("Platform.Eventing.Tests", StringComparison.OrdinalIgnoreCase));
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

    [Fact]
    public void Platform_Webhooks_Contracts_does_not_reference_forbidden_packages()
    {
        var path = "src/Platform.Webhooks.Contracts/Platform.Webhooks.Contracts.csproj";
        var packages = ReadPackageReferences(path);
        var violations = packages
            .Where(p => ForbiddenWebhooksContractsPackagePrefixes.Any(prefix =>
                p.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            .ToArray();
        Assert.True(
            violations.Length == 0,
            "Platform.Webhooks.Contracts must not reference forbidden packages but references: " + string.Join(", ", violations));
    }

    [Fact]
    public void Platform_Webhooks_Contracts_only_references_Platform_Core()
    {
        var path = "src/Platform.Webhooks.Contracts/Platform.Webhooks.Contracts.csproj";
        Assert.Equal(["Platform.Core"], ReadProjectReferences(path));
    }

    [Fact]
    public void Platform_Webhooks_AspNetCore_does_not_reference_visual_flow_projects()
    {
        var fullPath = Path.Combine(RepositoryRoot, "src/Platform.Webhooks.AspNetCore/Platform.Webhooks.AspNetCore.csproj");
        var document = XDocument.Load(fullPath);

        var projectReferences = document
            .Descendants()
            .Where(e => string.Equals(e.Name.LocalName, "ProjectReference", StringComparison.OrdinalIgnoreCase))
            .Select(e => e.Attribute("Include")?.Value ?? string.Empty)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToArray();

        var violations = projectReferences
            .Where(reference => ForbiddenWebhooksAspNetCoreProjectSegments.Any(segment =>
                reference.Contains(segment, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        Assert.True(
            violations.Length == 0,
            "Platform.Webhooks.AspNetCore must not reference VisualFlow projects but references: " + string.Join(", ", violations));
    }

    [Fact]
    public void Platform_Web_Telemetry_does_not_reference_forbidden_packages()
    {
        var path = "src/Platform.Web.Telemetry/Platform.Web.Telemetry.csproj";
        var packages = ReadPackageReferences(path);
        var violations = packages
            .Where(p => p.StartsWith("Microsoft.AspNetCore", StringComparison.OrdinalIgnoreCase)
                || p.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.OrdinalIgnoreCase)
                || p.StartsWith("Swashbuckle", StringComparison.OrdinalIgnoreCase)
                || p.StartsWith("NSwag", StringComparison.OrdinalIgnoreCase)
                || p.StartsWith("Polly", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        Assert.True(
            violations.Length == 0,
            "Platform.Web.Telemetry must not reference forbidden packages but references: " + string.Join(", ", violations));
    }

    [Fact]
    public void Platform_Web_Telemetry_only_references_Platform_Core()
    {
        var references = ReadProjectReferences("src/Platform.Web.Telemetry/Platform.Web.Telemetry.csproj");

        Assert.True(
            references.Length == 1 && references[0].Equals("Platform.Core", StringComparison.OrdinalIgnoreCase),
            "Platform.Web.Telemetry must reference only Platform.Core but references: " + string.Join(", ", references));
    }

    [Fact]
    public void Platform_Web_Cors_does_not_reference_forbidden_packages()
    {
        var path = "src/Platform.Web.Cors/Platform.Web.Cors.csproj";
        var packages = ReadPackageReferences(path);
        var violations = packages
            .Where(p => p.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        Assert.True(
            violations.Length == 0,
            "Platform.Web.Cors must not reference forbidden packages but references: " + string.Join(", ", violations));
    }

    [Fact]
    public void Platform_Web_Cors_references_only_platform_core_and_telemetry()
    {
        var references = ReadProjectReferences("src/Platform.Web.Cors/Platform.Web.Cors.csproj");
        Assert.Equal(2, references.Length);
        Assert.Contains("Platform.Core", references, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Platform.Web.Telemetry", references, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void Platform_Web_Resilience_does_not_reference_forbidden_packages()
    {
        var path = "src/Platform.Web.Resilience/Platform.Web.Resilience.csproj";
        var packages = ReadPackageReferences(path);
        var violations = packages
            .Where(p => p.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.OrdinalIgnoreCase)
                || p.StartsWith("Polly", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        Assert.True(
            violations.Length == 0,
            "Platform.Web.Resilience must not reference forbidden packages but references: " + string.Join(", ", violations));
    }

    [Fact]
    public void Platform_Web_Resilience_references_only_platform_core_and_telemetry()
    {
        var references = ReadProjectReferences("src/Platform.Web.Resilience/Platform.Web.Resilience.csproj");
        Assert.Equal(2, references.Length);
        Assert.Contains("Platform.Core", references, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Platform.Web.Telemetry", references, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void Platform_Web_OpenApi_does_not_reference_forbidden_packages()
    {
        var path = "src/Platform.Web.OpenApi/Platform.Web.OpenApi.csproj";
        var packages = ReadPackageReferences(path);
        var violations = packages
            .Where(p => p.StartsWith("Swashbuckle", StringComparison.OrdinalIgnoreCase)
                || p.StartsWith("NSwag", StringComparison.OrdinalIgnoreCase)
                || p.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        Assert.True(
            violations.Length == 0,
            "Platform.Web.OpenApi must not reference forbidden packages but references: " + string.Join(", ", violations));
    }

    [Fact]
    public void Platform_Web_OpenApi_references_only_platform_core_and_telemetry()
    {
        var references = ReadProjectReferences("src/Platform.Web.OpenApi/Platform.Web.OpenApi.csproj");
        Assert.Equal(2, references.Length);
        Assert.Contains("Platform.Core", references, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Platform.Web.Telemetry", references, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void Platform_Observability_does_not_reference_forbidden_packages()
    {
        var path = "src/Platform.Observability/Platform.Observability.csproj";
        var packages = ReadPackageReferences(path);
        var violations = packages
            .Where(p => p.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.OrdinalIgnoreCase)
                || p.StartsWith("Npgsql", StringComparison.OrdinalIgnoreCase)
                || p.StartsWith("StackExchange.Redis", StringComparison.OrdinalIgnoreCase)
                || p.StartsWith("AWSSDK", StringComparison.OrdinalIgnoreCase)
                || p.StartsWith("OpenTelemetry", StringComparison.OrdinalIgnoreCase)
                || p.StartsWith("Serilog", StringComparison.OrdinalIgnoreCase)
                || p.StartsWith("Stripe", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        Assert.True(
            violations.Length == 0,
            "Platform.Observability must not reference forbidden packages but references: " + string.Join(", ", violations));
    }

    [Fact]
    public void Platform_Observability_references_only_platform_core_and_web_telemetry()
    {
        var references = ReadProjectReferences("src/Platform.Observability/Platform.Observability.csproj");
        Assert.Equal(2, references.Length);
        Assert.Contains("Platform.Core", references, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Platform.Web.Telemetry", references, StringComparer.OrdinalIgnoreCase);
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
