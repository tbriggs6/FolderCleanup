using UtilitySuite.Core.Abstractions;

namespace UtilitySuite.Modules.Storage;

public sealed class ClearEmptiesUtilityModule : IUtilityModule
{
    public string Id => "clear-empties";
    public string DisplayName => "Clear Empties";
    public string Description => "Removes empty sub-directories and cleans upward as folders become empty.";
    public IReadOnlyList<IUtilityAction> Actions => [new ClearEmptiesAction()];
}

public sealed class ClearEmptiesAction : IUtilityAction
{
    public string Id => "clear-empties";
    public string DisplayName => "Clear Empties";
    public string Description =>
        "Recursively removes empty sub-directories and continues upward when parent folders become empty.";

    public IReadOnlyDictionary<string, string> InputSchema => new Dictionary<string, string>
    {
        ["directory"] = "Root directory to clean. The root itself is preserved."
    };

    public Task<string> ExecuteAsync(UtilityActionContext context, CancellationToken cancellationToken = default)
    {
        var rootDirectory = context.Get("directory")?.Trim();
        if (string.IsNullOrWhiteSpace(rootDirectory))
        {
            return Task.FromResult("Input 'directory' is required.");
        }

        if (!Directory.Exists(rootDirectory))
        {
            return Task.FromResult($"Directory does not exist: {rootDirectory}");
        }

        var fullRoot = Path.GetFullPath(rootDirectory);
        var scannedDirectories = 1;
        var deletedDirectories = 0;
        var errors = 0;

        CleanDirectory(
            fullRoot,
            ref scannedDirectories,
            ref deletedDirectories,
            ref errors,
            cancellationToken);

        return Task.FromResult(
            $"Scanned {scannedDirectories} directories. Deleted {deletedDirectories} empty directories. " +
            $"Errors: {errors}. Root preserved: {fullRoot}");
    }

    private static bool CleanDirectory(
        string directory,
        ref int scannedDirectories,
        ref int deletedDirectories,
        ref int errors,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string[] childDirectories;
        try
        {
            childDirectories = Directory.EnumerateDirectories(directory).ToArray();
        }
        catch (IOException)
        {
            errors++;
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            errors++;
            return false;
        }

        foreach (var childDirectory in childDirectories)
        {
            scannedDirectories++;

            var childIsEmpty = CleanDirectory(
                childDirectory,
                ref scannedDirectories,
                ref deletedDirectories,
                ref errors,
                cancellationToken);

            if (!childIsEmpty)
            {
                continue;
            }

            try
            {
                Directory.Delete(childDirectory);
                deletedDirectories++;
            }
            catch (IOException)
            {
                errors++;
            }
            catch (UnauthorizedAccessException)
            {
                errors++;
            }
        }

        try
        {
            return !Directory.EnumerateFileSystemEntries(directory).Any();
        }
        catch (IOException)
        {
            errors++;
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            errors++;
            return false;
        }
    }
}
