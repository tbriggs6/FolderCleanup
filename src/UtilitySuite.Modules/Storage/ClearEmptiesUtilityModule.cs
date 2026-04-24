using UtilitySuite.Core.Abstractions;
using UtilitySuite.Modules.Safety;

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

        var rootValidation = RootPathSafety.ValidateRootDirectory(rootDirectory);
        if (!rootValidation.IsValid)
        {
            return Task.FromResult(rootValidation.ErrorMessage!);
        }

        var fullRoot = rootValidation.FullRootPath!;

        if (RootPathSafety.IsDirectoryLink(fullRoot))
        {
            return Task.FromResult(
                $"Directory is a link/reparse point and cannot be used as a root for safety: {fullRoot}");
        }

        var scannedDirectories = 1;
        var deletedDirectories = 0;
        var errors = 0;
        var skippedLinks = 0;

        CleanDirectory(
            fullRoot,
            fullRoot,
            ref scannedDirectories,
            ref deletedDirectories,
            ref errors,
            ref skippedLinks,
            cancellationToken);

        return Task.FromResult(
            $"Scanned {scannedDirectories} directories. Deleted {deletedDirectories} empty directories. " +
            $"Skipped {skippedLinks} link/reparse-point directories. Errors: {errors}. Root preserved: {fullRoot}");
    }

    private static bool CleanDirectory(
        string directory,
        string rootDirectory,
        ref int scannedDirectories,
        ref int deletedDirectories,
        ref int errors,
        ref int skippedLinks,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!RootPathSafety.IsPathWithinRoot(directory, rootDirectory))
        {
            errors++;
            return false;
        }

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
            if (!RootPathSafety.IsPathWithinRoot(childDirectory, rootDirectory))
            {
                errors++;
                continue;
            }

            if (RootPathSafety.IsDirectoryLink(childDirectory))
            {
                skippedLinks++;
                continue;
            }

            scannedDirectories++;

            var childIsEmpty = CleanDirectory(
                childDirectory,
                rootDirectory,
                ref scannedDirectories,
                ref deletedDirectories,
                ref errors,
                ref skippedLinks,
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
