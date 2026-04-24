using UtilitySuite.Core.Abstractions;
using UtilitySuite.Modules.Photo;
using UtilitySuite.Modules.SystemInfo;
using UtilitySuite.Modules.Text;

namespace UtilitySuite.Modules;

public static class ModuleCatalog
{
    public static IReadOnlyList<IUtilityModule> GetDefaultModules()
    {
        return
        [
            new KeepRawUtilityModule(),
            new DateTimeUtilityModule(),
            new TextUtilityModule()
        ];
    }

    public static void RegisterDefaults(UtilitySuite.Core.Runtime.UtilityRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        foreach (var module in GetDefaultModules())
        {
            registry.Register(module);
        }
    }
}
