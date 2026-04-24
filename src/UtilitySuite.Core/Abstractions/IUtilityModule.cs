namespace UtilitySuite.Core.Abstractions;

public interface IUtilityModule
{
    string Id { get; }
    string DisplayName { get; }
    string Description { get; }
    IReadOnlyList<IUtilityAction> Actions { get; }
}
