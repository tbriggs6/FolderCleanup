using UtilitySuite.Core.Abstractions;

namespace UtilitySuite.Modules.SystemInfo;

public sealed class DateTimeUtilityModule : IUtilityModule
{
    public string Id => "system";

    public string DisplayName => "System Utilities";

    public string Description => "Basic machine and time related tools.";

    public IReadOnlyList<IUtilityAction> Actions { get; } =
    [
        new CurrentTimeAction()
    ];
}
