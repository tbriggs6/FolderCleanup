using UtilitySuite.Core.Abstractions;

namespace UtilitySuite.Modules.SystemInfo;

public sealed class CurrentTimeAction : IUtilityAction
{
    public string Id => "current-time";

    public string DisplayName => "Current Timestamp";

    public string Description => "Shows local and UTC timestamps.";
    public IReadOnlyDictionary<string, string> InputSchema => new Dictionary<string, string>();

    public Task<string> ExecuteAsync(UtilityActionContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult($"Local: {DateTime.Now:O}{Environment.NewLine}UTC  : {DateTime.UtcNow:O}");
    }
}
