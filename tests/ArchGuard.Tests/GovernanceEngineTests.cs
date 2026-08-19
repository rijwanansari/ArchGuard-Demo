using System.Text.Json;
using ArchGuard.Domain;
using ArchGuard.Infrastructure;
using Xunit;

namespace ArchGuard.Tests;

public class GovernanceEngineTests
{
    [Fact]
    public void BadArchitecture_IsBlocked()
    {
        var policies = LoadPolicies();
        var architecture = Load("scenario-bad.json");

        var result = new GovernanceEngine(policies).Evaluate(architecture);

        Assert.Equal("BLOCKED", result.Status);
        Assert.True(result.Critical >= 1);
        Assert.Contains(result.Findings, x => x.PolicyId == "AI-001");
        Assert.Contains(result.Findings, x => x.PolicyId == "AI-002");
    }

    [Fact]
    public void RemediatedArchitecture_IsApproved()
    {
        var policies = LoadPolicies();
        var architecture = Load("scenario-good.json");

        var result = new GovernanceEngine(policies).Evaluate(architecture);

        Assert.Equal("APPROVED", result.Status);
        Assert.Empty(result.Findings);
        Assert.Equal(100, result.Score);
    }

    private static IReadOnlyList<GovernancePolicy> LoadPolicies()
    {
        var path = DataPath("policies.json");
        return new PolicyRepository(path).GetPolicies();
    }

    private static ArchitectureSubmission Load(string name)
    {
        var path = DataPath(name);
        return JsonSerializer.Deserialize<ArchitectureSubmission>(
            File.ReadAllText(path),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }

    private static string DataPath(string name) => Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "ArchGuard.Api", "data", name));
}
