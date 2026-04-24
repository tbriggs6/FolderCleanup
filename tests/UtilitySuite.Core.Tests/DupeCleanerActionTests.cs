using UtilitySuite.Core.Abstractions;
using UtilitySuite.Modules.Storage;

namespace UtilitySuite.Core.Tests;

public sealed class DupeCleanerActionTests : IDisposable
{
    private readonly string _tempRoot;

    public DupeCleanerActionTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "utilitysuite-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);
    }

    [Fact]
    public async Task Keeps_file_in_highest_precedence_folder_and_deletes_lower_precedence_duplicates()
    {
        var primary = CreateFolder("primary");
        var backup1 = CreateFolder("backup1");
        var backup2 = CreateFolder("backup2");

        var keepPath = Path.Combine(primary, "report.docx");
        var deletePath1 = Path.Combine(backup1, "report.docx");
        var deletePath2 = Path.Combine(backup2, "report.docx");

        await File.WriteAllTextAsync(keepPath, "primary");
        await File.WriteAllTextAsync(deletePath1, "backup1");
        await File.WriteAllTextAsync(deletePath2, "backup2");

        var action = CreateDupeCleanerAction();
        var context = new UtilityActionContext(new Dictionary<string, string>
        {
            ["folders"] = string.Join(Environment.NewLine, primary, backup1, backup2)
        });

        var result = await action.ExecuteAsync(context);

        Assert.True(File.Exists(keepPath));
        Assert.False(File.Exists(deletePath1));
        Assert.False(File.Exists(deletePath2));
        Assert.Contains("Deleted 2 duplicates from lower-precedence folders", result);
    }

    [Fact]
    public async Task Supports_recursive_duplicates_and_same_relative_path_matching()
    {
        var tier1 = CreateFolder("tier1");
        var tier2 = CreateFolder("tier2");

        var keepDir = Path.Combine(tier1, "wedding", "raw");
        var deleteDir = Path.Combine(tier2, "wedding", "raw");
        Directory.CreateDirectory(keepDir);
        Directory.CreateDirectory(deleteDir);

        var keepPath = Path.Combine(keepDir, "IMG_1001.CR3");
        var deletePath = Path.Combine(deleteDir, "IMG_1001.CR3");
        await File.WriteAllTextAsync(keepPath, "tier1");
        await File.WriteAllTextAsync(deletePath, "tier2");

        var action = CreateDupeCleanerAction();
        var context = new UtilityActionContext(new Dictionary<string, string>
        {
            ["folders"] = $"{tier1};{tier2}"
        });

        await action.ExecuteAsync(context);

        Assert.True(File.Exists(keepPath));
        Assert.False(File.Exists(deletePath));
    }

    [Fact]
    public async Task Leaves_unique_files_intact()
    {
        var priority = CreateFolder("priority");
        var secondary = CreateFolder("secondary");

        var unique1 = Path.Combine(priority, "unique-a.txt");
        var unique2 = Path.Combine(secondary, "unique-b.txt");
        await File.WriteAllTextAsync(unique1, "a");
        await File.WriteAllTextAsync(unique2, "b");

        var action = CreateDupeCleanerAction();
        var context = new UtilityActionContext(new Dictionary<string, string>
        {
            ["folders"] = $"{priority};{secondary}"
        });

        var result = await action.ExecuteAsync(context);

        Assert.True(File.Exists(unique1));
        Assert.True(File.Exists(unique2));
        Assert.Contains("Deleted 0 duplicates from lower-precedence folders", result);
    }

    [Fact]
    public async Task Returns_message_when_any_folder_is_missing()
    {
        var existing = CreateFolder("existing");
        var missing = Path.Combine(_tempRoot, "missing");

        var action = CreateDupeCleanerAction();
        var context = new UtilityActionContext(new Dictionary<string, string>
        {
            ["folders"] = $"{existing}{Environment.NewLine}{missing}"
        });

        var result = await action.ExecuteAsync(context);

        Assert.Contains("Folders do not exist", result);
    }

    [Fact]
    public async Task Ignores_duplicate_folder_entries_keeping_first_precedence_position()
    {
        var first = CreateFolder("first");
        var second = CreateFolder("second");

        var keepPath = Path.Combine(first, "archive.zip");
        var deletePath = Path.Combine(second, "archive.zip");
        await File.WriteAllTextAsync(keepPath, "first");
        await File.WriteAllTextAsync(deletePath, "second");

        var action = CreateDupeCleanerAction();
        var context = new UtilityActionContext(new Dictionary<string, string>
        {
            ["folders"] = $"{first};{first};{second}"
        });

        await action.ExecuteAsync(context);

        Assert.True(File.Exists(keepPath));
        Assert.False(File.Exists(deletePath));
    }

    [Fact]
    public async Task Does_not_traverse_directory_symlinks_outside_root()
    {
        var tier1 = CreateFolder("tier1");
        var tier2 = CreateFolder("tier2");

        var external = CreateFolder("external");
        var outsideFilePath = Path.Combine(external, "archive.zip");
        await File.WriteAllTextAsync(outsideFilePath, "outside");

        var insideFilePath = Path.Combine(tier1, "archive.zip");
        await File.WriteAllTextAsync(insideFilePath, "inside");

        var linkPath = Path.Combine(tier2, "external-link");
        if (!TryCreateDirectorySymlink(linkPath, external))
        {
            return;
        }

        var action = CreateDupeCleanerAction();
        var context = new UtilityActionContext(new Dictionary<string, string>
        {
            ["folders"] = $"{tier1};{tier2}"
        });

        await action.ExecuteAsync(context);

        Assert.True(File.Exists(outsideFilePath));
        Assert.True(Directory.Exists(linkPath));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
    }

    private string CreateFolder(string name)
    {
        var path = Path.Combine(_tempRoot, name);
        Directory.CreateDirectory(path);
        return path;
    }

    private static IUtilityAction CreateDupeCleanerAction()
    {
        var module = new DupeCleanerUtilityModule();
        return module.Actions.Single(action => action.Id == "dupe-cleaner");
    }

    private static bool TryCreateDirectorySymlink(string linkPath, string targetPath)
    {
        try
        {
            Directory.CreateSymbolicLink(linkPath, targetPath);
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (PlatformNotSupportedException)
        {
            return false;
        }
    }
}
