namespace UtilitySuite.Core.Abstractions;

public interface IUtilityAction
{
    string Id { get; }
    string DisplayName { get; }
    string Description { get; }
    IReadOnlyDictionary<string, string> InputSchema { get; }
    Task<string> ExecuteAsync(UtilityActionContext context, CancellationToken cancellationToken = default);
}
