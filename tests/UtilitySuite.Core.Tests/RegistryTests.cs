using UtilitySuite.Core.Abstractions;
using UtilitySuite.Core.Runtime;

namespace UtilitySuite.Core.Tests;

public sealed class RegistryTests
{
    [Fact]
    public void Duplicate_module_id_throws()
    {
        var registry = new UtilityRegistry();
        registry.Register(new FakeModule("module-1"));

        Assert.Throws<InvalidOperationException>(() => registry.Register(new FakeModule("module-1")));
    }

    [Fact]
    public void Duplicate_action_id_throws()
    {
        var registry = new UtilityRegistry();
        registry.Register(new DuplicateActionModule());

        Assert.Throws<InvalidOperationException>(() => registry.GetModules());
    }

    private sealed class FakeModule(string id) : IUtilityModule
    {
        public string Id => id;
        public string DisplayName => "Fake";
        public string Description => "Fake module";
        public IReadOnlyList<IUtilityAction> Actions => [new FakeAction("action-1")];
    }

    private sealed class DuplicateActionModule : IUtilityModule
    {
        public string Id => "module-with-duplicate-actions";
        public string DisplayName => "Fake";
        public string Description => "Fake module";
        public IReadOnlyList<IUtilityAction> Actions =>
            [new FakeAction("action-1"), new FakeAction("action-1")];
    }

    private sealed class FakeAction(string id) : IUtilityAction
    {
        public string Id => id;
        public string DisplayName => "Action";
        public string Description => "Action";
        public IReadOnlyDictionary<string, string> InputSchema => new Dictionary<string, string>();

        public Task<string> ExecuteAsync(UtilityActionContext context, CancellationToken cancellationToken = default)
        {
            return Task.FromResult("ok");
        }
    }
}
