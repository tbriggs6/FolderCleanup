namespace UtilitySuite.Modules.Safety;

internal static class RootPathSafety
{
    private static readonly StringComparison PathComparison =
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    public static string NormalizeRoot(string path)
    {
        return Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
    }

    public static RootValidationResult ValidateRootDirectory(string? path)
    {
        if (!TryNormalizeRoot(path, out var fullRoot, out var error))
        {
            return new RootValidationResult(false, null, error);
        }

        if (fullRoot is null)
        {
            return new RootValidationResult(false, null, "Invalid directory path.");
        }

        if (!Directory.Exists(fullRoot))
        {
            return new RootValidationResult(false, null, $"Directory does not exist: {fullRoot}");
        }

        if (IsDirectoryLink(fullRoot))
        {
            return new RootValidationResult(
                false,
                null,
                $"Directory is a link/reparse point and cannot be used as a root for safety: {fullRoot}");
        }

        return new RootValidationResult(true, fullRoot, null);
    }

    public static bool TryNormalizeRoot(string? path, out string? normalizedRoot, out string? error)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            normalizedRoot = null;
            error = "Input directory is required.";
            return false;
        }

        try
        {
            normalizedRoot = NormalizeRoot(path.Trim());
            error = null;
            return true;
        }
        catch (Exception)
        {
            normalizedRoot = null;
            error = $"Invalid directory path: {path}";
            return false;
        }
    }

    public static bool IsWithinRoot(string normalizedRoot, string candidatePath)
    {
        var normalizedCandidate = Path.TrimEndingDirectorySeparator(Path.GetFullPath(candidatePath));
        if (string.Equals(normalizedRoot, normalizedCandidate, PathComparison))
        {
            return true;
        }

        var relative = Path.GetRelativePath(normalizedRoot, normalizedCandidate);
        if (string.IsNullOrEmpty(relative) || relative == ".")
        {
            return true;
        }

        if (Path.IsPathRooted(relative))
        {
            return false;
        }

        return !relative.Equals("..", PathComparison)
            && !relative.StartsWith($"..{Path.DirectorySeparatorChar}", PathComparison)
            && !relative.StartsWith($"..{Path.AltDirectorySeparatorChar}", PathComparison);
    }

    public static bool TryIsDirectoryLink(string path, out bool isLink)
    {
        try
        {
            var attributes = File.GetAttributes(path);
            isLink = attributes.HasFlag(FileAttributes.Directory) && attributes.HasFlag(FileAttributes.ReparsePoint);
            return true;
        }
        catch (IOException)
        {
            isLink = false;
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            isLink = false;
            return false;
        }
    }

    public static bool IsPathWithinRoot(string candidatePath, string rootPath)
    {
        try
        {
            var normalizedRoot = NormalizeRoot(rootPath);
            return IsWithinRoot(normalizedRoot, candidatePath);
        }
        catch (Exception)
        {
            return false;
        }
    }

    public static bool IsUnderRoot(string candidatePath, string rootPath)
    {
        return IsPathWithinRoot(candidatePath, rootPath);
    }

    public static bool IsDirectoryLink(string path)
    {
        return TryIsDirectoryLink(path, out var isLink) && isLink;
    }

    public static bool IsDirectorySymlink(string path)
    {
        return IsDirectoryLink(path);
    }
}

internal sealed record RootValidationResult(bool IsValid, string? FullRootPath, string? ErrorMessage);
