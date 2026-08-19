using System.Text.Json;
using ArchGuard.Domain;
using ArchGuard.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var policyPath = Path.Combine(
    builder.Environment.ContentRootPath,
    "data",
    "policies.json");

var repository = new PolicyRepository(policyPath);
var engine = new GovernanceEngine(repository.GetPolicies());

builder.Services.AddSingleton(repository);
builder.Services.AddSingleton(engine);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var endpoint = builder.Configuration["AzureOpenAI:Endpoint"];
var deployment = builder.Configuration["AzureOpenAI:Deployment"];

if (!string.IsNullOrWhiteSpace(endpoint) && !string.IsNullOrWhiteSpace(deployment))
{
    builder.Services.AddSingleton(new GovernanceAgent(
        endpoint,
        deployment,
        engine,
        repository));
}

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/api/health", () => Results.Ok(new
{
    status = "ok",
    service = "ArchGuard",
    timestamp = DateTimeOffset.UtcNow
}));

app.MapGet("/api/policies", (PolicyRepository repo) =>
    Results.Ok(repo.GetPolicies()));

app.MapGet("/api/scenarios/{name}", (string name) =>
{
    var fileName = name.ToLowerInvariant() switch
    {
        "bad" => "scenario-bad.json",
        "good" => "scenario-good.json",
        _ => null
    };

    if (fileName is null)
    {
        return Results.NotFound();
    }

    var path = Path.Combine(app.Environment.ContentRootPath, "data", fileName);
    return File.Exists(path)
        ? Results.File(path, "application/json")
        : Results.NotFound();
});

app.MapPost("/api/evaluate", (
    ArchitectureSubmission architecture,
    GovernanceEngine governance) =>
{
    var result = governance.Evaluate(architecture);
    return Results.Ok(result);
});

app.MapPost("/api/review", async (
    ArchitectureSubmission architecture,
    GovernanceEngine governance,
    IServiceProvider services) =>
{
    var deterministic = governance.Evaluate(architecture);
    var agent = services.GetService<GovernanceAgent>();

    if (agent is null)
    {
        return Results.Ok(new
        {
            deterministic,
            aiReview = "Azure OpenAI is not configured. Deterministic governance result is available."
        });
    }

    var review = await agent.ReviewAsync(architecture);

    return Results.Ok(new
    {
        deterministic,
        aiReview = review
    });
});

app.Run();
