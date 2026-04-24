namespace UtilitySuite.Core.Abstractions;

public sealed class UtilityActionContext
{
    public UtilityActionContext(IReadOnlyDictionary<string, string>? inputs = null)
    {
        Inputs = inputs is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(inputs, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyDictionary<string, string> Inputs { get; }

    public string? Get(string key)
    {
        return Inputs.TryGetValue(key, out var value) ? value : null;
    }
}
