using UtilitySuite.Core.Abstractions;

namespace UtilitySuite.Core.Runtime;

public sealed class UtilityRegistry
{
    private readonly Dictionary<string, IUtilityModule> _modules = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<IUtilityModule> GetModules()
    {
        foreach (IUtilityModule module in _modules.Values)
        {
            var seenActionIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (IUtilityAction action in module.Actions)
            {
                if (!seenActionIds.Add(action.Id))
                {
                    throw new InvalidOperationException(
                        $"Module '{module.Id}' has duplicate action id '{action.Id}'.");
                }
            }
        }

        return _modules.Values.OrderBy(module => module.DisplayName).ToArray();
    }

    public void Register(IUtilityModule module)
    {
        ArgumentNullException.ThrowIfNull(module);
        if (!_modules.TryAdd(module.Id, module))
        {
            throw new InvalidOperationException($"A module with id '{module.Id}' is already registered.");
        }
    }
}
