namespace ArchGuard.Domain;

public sealed record ArchitectureSubmission(
    string Name,
    string BusinessCapability,
    string Environment,
    IReadOnlyList<ArchitectureComponent> Components,
    IReadOnlyList<ArchitectureDependency> Dependencies,
    IReadOnlyList<DataAsset> DataAssets,
    IReadOnlyList<ExternalSystem> ExternalSystems);

public sealed record ArchitectureComponent(
    string Name,
    string Type,
    string Technology,
    Dictionary<string, string>? Properties = null);

public sealed record ArchitectureDependency(
    string From,
    string To,
    string Protocol);

public sealed record DataAsset(
    string Name,
    string Classification,
    string Store,
    bool ContainsPii,
    string Residency = "Unknown");

public sealed record ExternalSystem(
    string Name,
    string Type,
    string DataBoundary,
    bool Approved);

public sealed record GovernancePolicy(
    string Id,
    string Name,
    string Domain,
    string Severity,
    string Description,
    string Rule);

public sealed record GovernanceFinding(
    string PolicyId,
    string Severity,
    string Target,
    string Title,
    string Evidence,
    string Recommendation);

public sealed record GovernanceReport(
    string Architecture,
    int Score,
    string Status,
    IReadOnlyList<GovernanceFinding> Findings,
    DateTimeOffset EvaluatedAt)
{
    public int Critical => Findings.Count(x => x.Severity == "Critical");
    public int High => Findings.Count(x => x.Severity == "High");
    public int Medium => Findings.Count(x => x.Severity == "Medium");
    public int Low => Findings.Count(x => x.Severity == "Low");
}
