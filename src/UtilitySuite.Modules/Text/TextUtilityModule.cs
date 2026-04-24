using UtilitySuite.Core.Abstractions;

namespace UtilitySuite.Modules.Text;

public sealed class TextUtilityModule : IUtilityModule
{
    public string Id => "text";
    public string DisplayName => "Text Utilities";
    public string Description => "Common text helpers.";
    public IReadOnlyList<IUtilityAction> Actions =>
    [
        new WordCountAction(),
        new ReverseTextAction()
    ];

    private sealed class WordCountAction : IUtilityAction
    {
        public string Id => "word-count";
        public string DisplayName => "Word Count";
        public string Description => "Counts words in a provided sentence.";
        public IReadOnlyDictionary<string, string> InputSchema => new Dictionary<string, string>
        {
            ["text"] = "Text to analyze."
        };

        public Task<string> ExecuteAsync(UtilityActionContext context, CancellationToken cancellationToken = default)
        {
            context.Inputs.TryGetValue("text", out var rawInput);
            if (string.IsNullOrWhiteSpace(rawInput))
            {
                return Task.FromResult("No text provided.");
            }

            var words = rawInput
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            return Task.FromResult($"Words: {words.Length}");
        }
    }

    private sealed class ReverseTextAction : IUtilityAction
    {
        public string Id => "reverse";
        public string DisplayName => "Reverse Text";
        public string Description => "Reverses the provided text.";
        public IReadOnlyDictionary<string, string> InputSchema => new Dictionary<string, string>
        {
            ["text"] = "Text to reverse."
        };

        public Task<string> ExecuteAsync(UtilityActionContext context, CancellationToken cancellationToken = default)
        {
            context.Inputs.TryGetValue("text", out var rawInput);
            if (string.IsNullOrWhiteSpace(rawInput))
            {
                return Task.FromResult("No text provided.");
            }

            var reversedChars = rawInput.ToCharArray();
            Array.Reverse(reversedChars);
            return Task.FromResult(new string(reversedChars));
        }
    }
}
