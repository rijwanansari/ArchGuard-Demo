using System.Text.Json;
using ArchGuard.Domain;

namespace ArchGuard.Infrastructure;

public sealed class PolicyRepository
{
    private readonly string _path;

    public PolicyRepository(string path) => _path = path;

    public IReadOnlyList<GovernancePolicy> GetPolicies()
    {
        var json = File.ReadAllText(_path);
        return JsonSerializer.Deserialize<List<GovernancePolicy>>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
    }
}
