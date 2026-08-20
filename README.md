# ArchGuard — Enterprise Architecture Governance Agent

Event-ready, production-style demo for the session:

**Enterprise Architecture Governance in the AI Era**

## What this demo demonstrates

A developer submits a production architecture. ArchGuard:

1. Parses the architecture.
2. Runs deterministic governance policies.
3. Calculates a governance score.
4. Identifies critical/high/medium violations.
5. Uses Microsoft Agent Framework + Azure OpenAI to explain the findings when configured.
6. Uses agent tools to retrieve policies and technology guidance.
7. Recommends remediation.
8. Distinguishes machine-enforceable controls from human architect decisions.

## Important architecture principle

**The LLM is not the policy engine.**

- Deterministic code = authoritative compliance decision.
- Agent = reasoning, explanation and remediation.
- Human = approval for critical/high-risk exceptions.

## Technology

- .NET 10
- Microsoft Agent Framework
- Azure OpenAI chat deployment
- Azure Identity / Entra ID
- ASP.NET Core Minimal API
- Swagger / OpenAPI
- Static HTML dashboard
- xUnit

See the Microsoft Learn links in `docs/REFERENCES.md` for the Agent Framework and Azure OpenAI references used by this demo.

## Prerequisites

- .NET 10 SDK
- Azure OpenAI resource with a chat-capable deployment, optional for deterministic-only mode
- Azure CLI, optional when using Azure OpenAI with `DefaultAzureCredential`

Authenticate if you want to run the AI review path with Azure OpenAI:

```bash
az login
```

## Configuration

Set environment variables instead of putting secrets in source:

PowerShell:

```powershell
$env:AzureOpenAI__Endpoint="https://YOUR-RESOURCE.openai.azure.com/"
$env:AzureOpenAI__Deployment="YOUR-DEPLOYMENT"
```

macOS/Linux:

```bash
export AzureOpenAI__Endpoint="https://YOUR-RESOURCE.openai.azure.com/"
export AzureOpenAI__Deployment="YOUR-DEPLOYMENT"
```

Or put the values in `src/ArchGuard.Api/appsettings.json` locally.

For production, prefer Managed Identity rather than broad credential fallback.

## Run

```bash
dotnet restore ArchGuard.sln
dotnet build ArchGuard.sln
dotnet test ArchGuard.sln
dotnet run --project src/ArchGuard.Api/ArchGuard.Api.csproj
```

Open:

```text
http://localhost:5000
```

The actual port may be shown by ASP.NET Core at startup.

Swagger UI:

```text
http://localhost:5000/swagger
```

## API endpoints

- `GET /api/health` - service health check.
- `GET /api/policies` - current deterministic governance policies.
- `GET /api/scenarios/bad` - deliberately non-compliant demo architecture.
- `GET /api/scenarios/good` - remediated demo architecture.
- `POST /api/evaluate` - deterministic governance decision.
- `POST /api/review` - deterministic decision plus AI explanation when Azure OpenAI is configured.

The static dashboard in `src/ArchGuard.Api/wwwroot/index.html` loads its scenarios through `/api/scenarios/bad` and `/api/scenarios/good`.

## Event demo flow

### Act 1 — Bad architecture

Click **Load Bad Scenario**.

Ask the audience:

> "Would you approve this?"

Click **Evaluate Architecture**.

Expected result:

- BLOCKED
- Critical AI data-boundary issue
- Critical AI/PII issue
- API authentication violation
- secrets-management violation
- PII classification violation
- unknown data residency
- unapproved HTTP integration
- missing retry/DLQ

### Act 2 — Ask the agent

Click **Ask Governance Agent**.

Ask:

> "Why did you block this architecture?"

Then:

> "What should the team change?"

### Act 3 — Remediation

Click **Load Remediated Scenario**.

Evaluate again.

Expected:

```text
APPROVED — 100/100
```

### Act 4 — Architecture decision

Ask:

> "Should the agent automatically deploy this?"

Explain:

- Low-risk fixes can be automated.
- Architecture changes may require review.
- Security/privacy/production changes need human approval.

## Production evolution

This demo intentionally keeps external systems local so the event can run without a large Azure footprint.

Replace the local repositories with:

- Azure AI Search / Foundry IQ for policies, ADRs and reference architectures.
- Azure Cosmos DB or Azure SQL for governance decisions and audit.
- GitHub/Azure DevOps for pull requests.
- Azure Resource Graph for actual cloud topology.
- Defender for Cloud / Microsoft security APIs for security findings.
- MCP servers for external enterprise systems.
- Azure Service Bus for asynchronous governance jobs.
- Application Insights/OpenTelemetry for agent/tool telemetry.
- Microsoft Entra ID for user and service authorization.
- Durable Task / Agent Framework workflows for long-running human approval.

## Production security

Never give the agent unrestricted production write access.

Use two tool classes:

Read:
- get_policy
- get_architecture
- search_ADR
- get_technology_guidance
- get_security_findings

Write:
- create_exception
- modify_architecture
- create_pull_request
- change_cloud_resource
- deploy

Write operations should require approval and should be audited.

## Suggested production workflow

```text
PR / Architecture Portal
        |
        v
Architecture Intake
        |
        v
Governance Workflow
        |
  +-----+-----+-----+
  |     |     |     |
Security Data Cloud Integration
  |     |     |     |
  +-----+-----+-----+
        |
        v
Deterministic Policy Engine
        |
        v
Governance Agent
        |
   +----+----+
   |         |
 PASS      REVIEW/BLOCK
             |
             v
       Human Architect
             |
             v
       Audit + Evidence
```

## Important note

This is a **conference-ready production-style reference implementation**, not a claim that it is production-certified. Before enterprise deployment, add identity, authorization, persistent storage, centralized policy management, secrets management, observability, model/tool evaluation, rate limits, retries, PII controls and approval workflows.
