using UtilitySuite.Core.Abstractions;
using UtilitySuite.Modules.Safety;

namespace UtilitySuite.Modules.Storage;

public sealed class DupeCleanerUtilityModule : IUtilityModule
{
    public string Id => "dupe-cleaner";
    public string DisplayName => "Dupe Cleaner";
    public string Description => "Deletes lower-precedence duplicates across backup folders by base file name.";
    public IReadOnlyList<IUtilityAction> Actions => [new DupeCleanerAction()];
}

public sealed class DupeCleanerAction : IUtilityAction
{
    public string Id => "dupe-cleaner";
    public string DisplayName => "Dupe Cleaner";
    public string Description =>
        "Given folders in precedence order (highest first), deletes duplicate files with the same base name from lower-precedence folders.";

    public IReadOnlyDictionary<string, string> InputSchema => new Dictionary<string, string>
    {
        ["folders"] = "Folders in precedence order (highest first), separated by new lines, ';', or ','."
    };

    public Task<string> ExecuteAsync(UtilityActionContext context, CancellationToken cancellationToken = default)
    {
        var folderInput = context.Get("folders");
        var parseResult = ParseFolders(folderInput);
        var orderedRoots = parseResult.Folders;

        if (orderedRoots.Count == 0)
        {
            return Task.FromResult("Input 'folders' is required. Provide at least one folder path.");
        }

        if (parseResult.InvalidEntries.Count > 0)
        {
            return Task.FromResult($"Invalid folder paths: {string.Join(", ", parseResult.InvalidEntries)}");
        }

        var missing = orderedRoots.Where(folder => !Directory.Exists(folder)).ToArray();
        if (missing.Length > 0)
        {
            if (missing.Length == 1)
            {
                return Task.FromResult($"Folder does not exist: {missing[0]}");
            }

            return Task.FromResult($"Folders do not exist: {string.Join(", ", missing)}");
        }

        var linkRoots = orderedRoots.Where(RootPathSafety.IsDirectoryLink).ToArray();
        if (linkRoots.Length > 0)
        {
            return Task.FromResult(
                $"Folder roots cannot be links/reparse points for safety: {string.Join(", ", linkRoots)}");
        }

        var filesByBaseName = new Dictionary<string, List<CandidateFile>>(StringComparer.OrdinalIgnoreCase);
        var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var scannedFiles = 0;
        var scanErrors = 0;
        var skippedLinks = 0;

        for (var index = 0; index < orderedRoots.Count; index++)
        {
            var scanResult = EnumerateFilesSafe(orderedRoots[index], cancellationToken);
            scanErrors += scanResult.Errors;
            skippedLinks += scanResult.SkippedLinks;

            foreach (var filePath in scanResult.Files)
            {
                if (!RootPathSafety.IsPathWithinRoot(filePath, orderedRoots[index]))
                {
                    scanErrors++;
                    continue;
                }

                if (!seenPaths.Add(filePath))
                {
                    continue;
                }

                scannedFiles++;

                var baseName = Path.GetFileNameWithoutExtension(filePath);
                if (string.IsNullOrWhiteSpace(baseName))
                {
                    continue;
                }

                if (!filesByBaseName.TryGetValue(baseName, out var candidates))
                {
                    candidates = [];
                    filesByBaseName[baseName] = candidates;
                }

                candidates.Add(new CandidateFile(filePath, index));
            }
        }

        var duplicateGroups = 0;
        var deletedFiles = 0;
        var deleteErrors = 0;

        foreach (var pair in filesByBaseName)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var candidates = pair.Value;
            if (candidates.Count <= 1)
            {
                continue;
            }

            duplicateGroups++;

            var ordered = candidates
                .OrderBy(candidate => candidate.PrecedenceIndex)
                .ThenBy(candidate => candidate.Path, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var winner = ordered[0];
            foreach (var candidate in ordered.Skip(1))
            {
                if (string.Equals(candidate.Path, winner.Path, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!RootPathSafety.IsPathWithinRoot(candidate.Path, orderedRoots[candidate.PrecedenceIndex]))
                {
                    deleteErrors++;
                    continue;
                }

                try
                {
                    File.Delete(candidate.Path);
                    deletedFiles++;
                }
                catch (IOException)
                {
                    deleteErrors++;
                }
                catch (UnauthorizedAccessException)
                {
                    deleteErrors++;
                }
            }
        }

        return Task.FromResult(
            $"Scanned {orderedRoots.Count} folders and {scannedFiles} files. " +
            $"Found {duplicateGroups} duplicate base-name groups. Deleted {deletedFiles} duplicates from lower-precedence folders. " +
            $"Skipped {skippedLinks} link/reparse-point directories. Scan errors: {scanErrors}. Delete errors: {deleteErrors}.");
    }

    private static FolderParseResult ParseFolders(string? rawInput)
    {
        if (string.IsNullOrWhiteSpace(rawInput))
        {
            return new FolderParseResult([], []);
        }

        var deduped = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var ordered = new List<string>();
        var invalidEntries = new List<string>();

        var parts = rawInput.Split(
            ['\n', '\r', ';', ','],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var part in parts)
        {
            var path = part.Trim().Trim('"');
            if (string.IsNullOrWhiteSpace(path))
            {
                continue;
            }

            string normalized;
            try
            {
                normalized = RootPathSafety.NormalizeRoot(path);
            }
            catch (Exception)
            {
                invalidEntries.Add(path);
                continue;
            }

            if (deduped.Add(normalized))
            {
                ordered.Add(normalized);
            }
        }

        return new FolderParseResult(ordered, invalidEntries);
    }

    private static FolderScanResult EnumerateFilesSafe(string rootFolder, CancellationToken cancellationToken)
    {
        var collectedFiles = new List<string>();
        var errors = 0;
        var skippedLinks = 0;
        var pending = new Stack<string>();
        pending.Push(rootFolder);

        while (pending.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var current = pending.Pop();

            if (!RootPathSafety.IsPathWithinRoot(current, rootFolder))
            {
                errors++;
                continue;
            }

            if (RootPathSafety.IsDirectoryLink(current) && !string.Equals(current, rootFolder, StringComparison.OrdinalIgnoreCase))
            {
                skippedLinks++;
                continue;
            }

            IEnumerable<string> discoveredFiles;
            try
            {
                discoveredFiles = Directory.EnumerateFiles(current);
            }
            catch (IOException)
            {
                errors++;
                continue;
            }
            catch (UnauthorizedAccessException)
            {
                errors++;
                continue;
            }

            foreach (var file in discoveredFiles)
            {
                collectedFiles.Add(file);
            }

            IEnumerable<string> childDirectories;
            try
            {
                childDirectories = Directory.EnumerateDirectories(current);
            }
            catch (IOException)
            {
                errors++;
                continue;
            }
            catch (UnauthorizedAccessException)
            {
                errors++;
                continue;
            }

            foreach (var child in childDirectories)
            {
                if (!RootPathSafety.IsPathWithinRoot(child, rootFolder))
                {
                    errors++;
                    continue;
                }

                if (RootPathSafety.IsDirectoryLink(child))
                {
                    skippedLinks++;
                    continue;
                }

                pending.Push(child);
            }
        }

        return new FolderScanResult(collectedFiles, errors, skippedLinks);
    }

    private sealed record FolderParseResult(IReadOnlyList<string> Folders, IReadOnlyList<string> InvalidEntries);
    private sealed record FolderScanResult(IReadOnlyList<string> Files, int Errors, int SkippedLinks);
    private sealed record CandidateFile(string Path, int PrecedenceIndex);
}
