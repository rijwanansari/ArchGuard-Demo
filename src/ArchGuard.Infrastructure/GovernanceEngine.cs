using ArchGuard.Domain;

namespace ArchGuard.Infrastructure;

public sealed class GovernanceEngine
{
    private readonly IReadOnlyList<GovernancePolicy> _policies;

    public GovernanceEngine(IReadOnlyList<GovernancePolicy> policies) => _policies = policies;

    public GovernanceReport Evaluate(ArchitectureSubmission a)
    {
        var findings = new List<GovernanceFinding>();

        foreach (var p in _policies)
        {
            switch (p.Id)
            {
                case "SEC-001": ApiAuthentication(a, p, findings); break;
                case "SEC-002": SecretsManagement(a, p, findings); break;
                case "DATA-001": PiiClassification(a, p, findings); break;
                case "DATA-002": DataResidency(a, p, findings); break;
                case "INT-001": IntegrationProtocol(a, p, findings); break;
                case "RES-001": MessagingResilience(a, p, findings); break;
                case "CLOUD-001": ApprovedTechnology(a, p, findings); break;
                case "AI-001": AiBoundary(a, p, findings); break;
                case "AI-002": AiPiiHandling(a, p, findings); break;
            }
        }

        var penalty = findings.Sum(x => x.Severity switch
        {
            "Critical" => 35,
            "High" => 18,
            "Medium" => 8,
            "Low" => 3,
            _ => 0
        });

        var score = Math.Max(0, 100 - penalty);
        var status = findings.Any(x => x.Severity == "Critical")
            ? "BLOCKED"
            : findings.Any(x => x.Severity == "High")
                ? "NEEDS_REVIEW"
                : "APPROVED";

        return new GovernanceReport(a.Name, score, status, findings, DateTimeOffset.UtcNow);
    }

    private static void ApiAuthentication(ArchitectureSubmission a, GovernancePolicy p, List<GovernanceFinding> f)
    {
        foreach (var c in a.Components.Where(x => x.Type.Equals("API", StringComparison.OrdinalIgnoreCase)))
        {
            var auth = c.Properties?.GetValueOrDefault("authentication");
            if (string.IsNullOrWhiteSpace(auth) || auth.Equals("None", StringComparison.OrdinalIgnoreCase))
                Add(f, p, c.Name, "API authentication is missing or disabled.",
                    $"authentication='{auth ?? "missing"}'",
                    "Use OAuth2/OIDC or another enterprise-approved mechanism.");
        }
    }

    private static void SecretsManagement(ArchitectureSubmission a, GovernancePolicy p, List<GovernanceFinding> f)
    {
        foreach (var c in a.Components)
        {
            var secrets = c.Properties?.GetValueOrDefault("secretsManagement");
            if (c.Type.Equals("API", StringComparison.OrdinalIgnoreCase) &&
                (string.IsNullOrWhiteSpace(secrets) || secrets.Equals("EnvironmentVariables", StringComparison.OrdinalIgnoreCase)))
                Add(f, p, c.Name, "Application secrets are not explicitly managed by an approved secret store.",
                    $"secretsManagement='{secrets ?? "missing"}'",
                    "Use Azure Key Vault or an approved enterprise secret-management service.");
        }
    }

    private static void PiiClassification(ArchitectureSubmission a, GovernancePolicy p, List<GovernanceFinding> f)
    {
        foreach (var d in a.DataAssets.Where(x => x.ContainsPii))
            if (!d.Classification.Equals("Confidential", StringComparison.OrdinalIgnoreCase))
                Add(f, p, d.Name, "PII is not classified as Confidential.",
                    $"containsPii=true; classification='{d.Classification}'",
                    "Classify PII as Confidential and apply the associated controls.");
    }

    private static void DataResidency(ArchitectureSubmission a, GovernancePolicy p, List<GovernanceFinding> f)
    {
        foreach (var d in a.DataAssets.Where(x => x.ContainsPii))
            if (d.Residency.Equals("Unknown", StringComparison.OrdinalIgnoreCase))
                Add(f, p, d.Name, "PII data residency is unknown.",
                    "residency='Unknown'",
                    "Declare the approved data region and verify service/data residency requirements.");
    }

    private static void IntegrationProtocol(ArchitectureSubmission a, GovernancePolicy p, List<GovernanceFinding> f)
    {
        var approved = new[] { "HTTPS", "REST", "ServiceBus", "EventGrid" };
        foreach (var d in a.Dependencies)
            if (!approved.Contains(d.Protocol, StringComparer.OrdinalIgnoreCase))
                Add(f, p, $"{d.From}->{d.To}", "Integration protocol is not approved.",
                    $"protocol='{d.Protocol}'",
                    "Use HTTPS/REST or an approved enterprise messaging pattern.");
    }

    private static void MessagingResilience(ArchitectureSubmission a, GovernancePolicy p, List<GovernanceFinding> f)
    {
        foreach (var c in a.Components.Where(x =>
                     x.Type.Equals("Messaging", StringComparison.OrdinalIgnoreCase) ||
                     x.Technology.Contains("Service Bus", StringComparison.OrdinalIgnoreCase)))
        {
            var retry = c.Properties?.GetValueOrDefault("retryPolicy");
            var dlq = c.Properties?.GetValueOrDefault("deadLetterQueue");
            if (string.IsNullOrWhiteSpace(retry) || string.IsNullOrWhiteSpace(dlq))
                Add(f, p, c.Name, "Messaging resilience controls are incomplete.",
                    $"retryPolicy='{retry ?? "missing"}'; deadLetterQueue='{dlq ?? "missing"}'",
                    "Configure bounded exponential retry and a dead-letter queue.");
        }
    }

    private static void ApprovedTechnology(ArchitectureSubmission a, GovernancePolicy p, List<GovernanceFinding> f)
    {
        var approved = new[]
        {
            "Azure App Service", "Azure Functions", "Azure Service Bus",
            "Azure SQL", "Azure Cosmos DB", "Azure OpenAI", "Azure API Management"
        };

        foreach (var c in a.Components)
            if (!approved.Contains(c.Technology, StringComparer.OrdinalIgnoreCase))
                Add(f, p, c.Name, "Technology is outside the sample approved catalog.",
                    $"technology='{c.Technology}'",
                    "Use an approved technology or submit a documented exception.");
    }

    private static void AiBoundary(ArchitectureSubmission a, GovernancePolicy p, List<GovernanceFinding> f)
    {
        var hasAi = a.Components.Any(x =>
            x.Type.Equals("AI", StringComparison.OrdinalIgnoreCase) ||
            x.Technology.Contains("OpenAI", StringComparison.OrdinalIgnoreCase));

        if (!hasAi) return;

        var aiSystems = a.ExternalSystems.Where(x =>
            x.Type.Equals("AI", StringComparison.OrdinalIgnoreCase) ||
            x.Name.Contains("OpenAI", StringComparison.OrdinalIgnoreCase));

        if (!aiSystems.Any() || aiSystems.Any(x => !x.Approved))
            Add(f, p, "AI-DATA-BOUNDARY", "AI data boundary is not approved.",
                "AI component exists but no approved AI external-system boundary is declared.",
                "Use an enterprise-approved AI boundary and explicitly define permitted data classes.");
    }

    private static void AiPiiHandling(ArchitectureSubmission a, GovernancePolicy p, List<GovernanceFinding> f)
    {
        var hasAi = a.Components.Any(x =>
            x.Type.Equals("AI", StringComparison.OrdinalIgnoreCase) ||
            x.Technology.Contains("OpenAI", StringComparison.OrdinalIgnoreCase));

        if (!hasAi) return;

        var pii = a.DataAssets.Where(x => x.ContainsPii).ToList();
        if (pii.Count == 0) return;

        var ai = a.Components.FirstOrDefault(x =>
            x.Type.Equals("AI", StringComparison.OrdinalIgnoreCase) ||
            x.Technology.Contains("OpenAI", StringComparison.OrdinalIgnoreCase));

        var policy = ai?.Properties?.GetValueOrDefault("dataPolicy");
        if (string.IsNullOrWhiteSpace(policy) || policy.Equals("PIIAllowed", StringComparison.OrdinalIgnoreCase))
            Add(f, p, ai?.Name ?? "AI", "AI component has no explicit PII handling policy.",
                $"PII assets={pii.Count}; dataPolicy='{policy ?? "missing"}'",
                "Redact PII before inference or use an explicitly approved AI boundary with a documented exception.");
    }

    private static void Add(
        List<GovernanceFinding> f,
        GovernancePolicy p,
        string target,
        string title,
        string evidence,
        string recommendation) =>
        f.Add(new GovernanceFinding(p.Id, p.Severity, target, title, evidence, recommendation));
}
