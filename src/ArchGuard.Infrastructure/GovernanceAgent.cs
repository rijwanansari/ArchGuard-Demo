using System.ComponentModel;
using System.Text.Json;
using ArchGuard.Domain;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace ArchGuard.Infrastructure;

public sealed class GovernanceAgent
{
    private readonly AIAgent _agent;

    public GovernanceAgent(
        string endpoint,
        string deployment,
        GovernanceEngine engine,
        PolicyRepository policies)
    {
        var client = new AzureOpenAIClient(
            new Uri(endpoint),
            new DefaultAzureCredential());

        var tools = new List<AITool>
        {
            AIFunctionFactory.Create(
                (ArchitectureSubmission architecture) =>
                    JsonSerializer.Serialize(engine.Evaluate(architecture),
                        new JsonSerializerOptions { WriteIndented = true }),
                new AIFunctionFactoryOptions
                {
                    Name = "evaluate_architecture",
                    Description = "Run deterministic enterprise architecture governance policies."
                }),

            AIFunctionFactory.Create(
                () => JsonSerializer.Serialize(policies.GetPolicies(),
                    new JsonSerializerOptions { WriteIndented = true }),
                new AIFunctionFactoryOptions
                {
                    Name = "get_governance_policies",
                    Description = "Return current enterprise governance policies."
                }),

            AIFunctionFactory.Create(
                (string technology) =>
                    GetTechnologyGuidance(technology),
                new AIFunctionFactoryOptions
                {
                    Name = "lookup_technology_guidance",
                    Description = "Return enterprise guidance for a technology."
                })
        };

        _agent = client
            .GetChatClient(deployment)
            .AsIChatClient()
            .AsAIAgent(
                instructions: """
                        You are ArchGuard, an Enterprise Architecture Governance Agent.

                        Your job is to help a Senior Enterprise Architect review proposed
                        production architectures.

                        NON-NEGOTIABLE:
                        - Never invent an enterprise policy.
                        - Use evaluate_architecture for compliance decisions.
                        - The deterministic policy engine is authoritative for pass/fail.
                        - You are the reasoning and explanation layer.
                        - Separate policy violations from architectural recommendations.
                        - Do not claim a control exists unless evidence shows it.
                        - Critical security, privacy, compliance, or production-impacting changes
                          require human architect approval.

                        Produce:
                        1. Executive decision: APPROVED / NEEDS_REVIEW / BLOCKED.
                        2. Score.
                        3. Critical/high/medium findings.
                        4. Evidence.
                        5. Remediation plan.
                        6. Architecture risks not directly covered by policy.
                        7. Human decisions required.

                        Be concise, technical and suitable for an architecture review board.
                        """,
                name: "ArchitectureGovernanceAgent",
                description: "Reviews production architectures against enterprise governance policies.",
                tools: tools);

    }

    public async Task<string> ReviewAsync(ArchitectureSubmission architecture)
    {
        var json = JsonSerializer.Serialize(architecture,
            new JsonSerializerOptions { WriteIndented = true });

        var prompt = $"""
        Review this production architecture as an Enterprise Architecture Governance Board.

        Architecture:
        {json}

        First use the governance evaluation tool.
        Then explain the result and recommend remediation.
        Explicitly call out any issue that requires a human architect decision.
        """;

        var response = await _agent.RunAsync(prompt);
        return response.Text;
    }

    [Description("Return approved enterprise technology guidance for a named technology.")]
    private static string GetTechnologyGuidance(string technology)
    {
        var guidance = technology.ToLowerInvariant() switch
        {
            "azure service bus" =>
                "Preferred for asynchronous enterprise messaging. Use retry, DLQ, idempotency and observability.",
            "azure api management" =>
                "Preferred API management layer. Apply authentication, rate limits, policies and observability.",
            "azure openai" =>
                "Use approved enterprise endpoint, data classification, content safety and explicit AI data boundary.",
            "azure sql" =>
                "Use private networking where required, managed identity, encryption and backup/DR controls.",
            _ =>
                "No specific guidance found in the demo technology catalog. Check the enterprise reference architecture."
        };

        return guidance;
    }
}
