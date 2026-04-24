using UtilitySuite.Core.Abstractions;
using UtilitySuite.Modules.Safety;

namespace UtilitySuite.Modules.Photo;

public sealed class KeepRawUtilityModule : IUtilityModule
{
    public string Id => "photo";
    public string DisplayName => "Photo Utilities";
    public string Description => "Helpers for mixed RAW/JPG camera libraries.";
    public IReadOnlyList<IUtilityAction> Actions => [new KeepRawAction()];
}

public sealed class KeepRawAction : IUtilityAction
{
    public string Id => "keep-raw";
    public string DisplayName => "Keep Raw";
    public string Description =>
        "Recursively deletes .JPG files only when a same-name .CR3 exists in the same folder.";

    public IReadOnlyDictionary<string, string> InputSchema => new Dictionary<string, string>
    {
        ["directory"] = "Root directory to scan recursively."
    };

    public Task<string> ExecuteAsync(UtilityActionContext context, CancellationToken cancellationToken = default)
    {
        var rootDirectory = context.Get("directory")?.Trim();
        if (string.IsNullOrWhiteSpace(rootDirectory))
        {
            return Task.FromResult("Input 'directory' is required.");
        }

        if (!RootPathSafety.TryNormalizeRoot(rootDirectory, out var root, out var validationError))
        {
            return Task.FromResult(validationError ?? "Invalid directory path.");
        }

        if (root is null)
        {
            return Task.FromResult("Invalid directory path.");
        }

        if (!Directory.Exists(root))
        {
            return Task.FromResult($"Directory does not exist: {root}");
        }

        if (RootPathSafety.IsDirectoryLink(root))
        {
            return Task.FromResult(
                $"Directory is a link/reparse point and cannot be used as a root for safety: {root}");
        }

        var deleted = 0;
        var jpgCandidates = 0;
        var scannedDirectories = 0;
        var errors = 0;
        var pending = new Stack<string>();
        pending.Push(root);

        while (pending.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var directory = pending.Pop();
            if (!RootPathSafety.IsPathWithinRoot(directory, root))
            {
                errors++;
                continue;
            }

            if (RootPathSafety.IsDirectoryLink(directory)
                && !string.Equals(
                    directory,
                    root,
                    OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            {
                continue;
            }

            scannedDirectories++;

            var rawFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var jpgFiles = new List<string>();

            try
            {
                foreach (var filePath in Directory.EnumerateFiles(directory))
                {
                    if (!RootPathSafety.IsPathWithinRoot(filePath, root))
                    {
                        continue;
                    }

                    var extension = Path.GetExtension(filePath);
                    if (extension.Equals(".cr3", StringComparison.OrdinalIgnoreCase))
                    {
                        rawFileNames.Add(Path.GetFileNameWithoutExtension(filePath));
                    }
                    else if (extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase))
                    {
                        jpgFiles.Add(filePath);
                        jpgCandidates++;
                    }
                }
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

            foreach (var jpgFilePath in jpgFiles)
            {
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(jpgFilePath);
                if (!rawFileNames.Contains(fileNameWithoutExtension))
                {
                    continue;
                }

                try
                {
                    File.Delete(jpgFilePath);
                    deleted++;
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
                foreach (var childDirectory in Directory.EnumerateDirectories(directory))
                {
                    if (!RootPathSafety.IsPathWithinRoot(childDirectory, root))
                    {
                        continue;
                    }

                    if (RootPathSafety.IsDirectoryLink(childDirectory))
                    {
                        continue;
                    }

                    pending.Push(childDirectory);
                }
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

        var kept = jpgCandidates - deleted;
        return Task.FromResult(
            $"Scanned {scannedDirectories} directories. Checked {jpgCandidates} JPG files. " +
            $"Deleted {deleted} JPG files with matching CR3. Kept {kept} JPG files without matching CR3. " +
            $"Errors: {errors}.");
    }
}
