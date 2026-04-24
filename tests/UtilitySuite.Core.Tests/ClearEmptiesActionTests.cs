using UtilitySuite.Core.Abstractions;
using UtilitySuite.Modules.Storage;

namespace UtilitySuite.Core.Tests;

public sealed class ClearEmptiesActionTests : IDisposable
{
    private readonly string _tempRoot;

    public ClearEmptiesActionTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "utilitysuite-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);
    }

    [Fact]
    public async Task Removes_empty_leaf_and_then_empty_parent_cascade()
    {
        var root = CreateFolder("root");
        var parent = Path.Combine(root, "a");
        var leaf = Path.Combine(parent, "b");
        Directory.CreateDirectory(leaf);

        var action = CreateClearEmptiesAction();
        var context = new UtilityActionContext(new Dictionary<string, string>
        {
            ["directory"] = root
        });

        var result = await action.ExecuteAsync(context);

        Assert.False(Directory.Exists(leaf));
        Assert.False(Directory.Exists(parent));
        Assert.True(Directory.Exists(root));
        Assert.Contains("Deleted 2 empty directories", result);
    }

    [Fact]
    public async Task Keeps_directories_that_contain_files()
    {
        var root = CreateFolder("root");
        var keepDir = Path.Combine(root, "keep");
        Directory.CreateDirectory(keepDir);
        await File.WriteAllTextAsync(Path.Combine(keepDir, "file.txt"), "content");

        var action = CreateClearEmptiesAction();
        var context = new UtilityActionContext(new Dictionary<string, string>
        {
            ["directory"] = root
        });

        var result = await action.ExecuteAsync(context);

        Assert.True(Directory.Exists(keepDir));
        Assert.Contains("Deleted 0 empty directories", result);
    }

    [Fact]
    public async Task Removes_empty_directories_from_nested_branches_only()
    {
        var root = CreateFolder("root");
        var removeBranch = Path.Combine(root, "remove", "x", "y");
        var keepBranch = Path.Combine(root, "keep", "k1");
        Directory.CreateDirectory(removeBranch);
        Directory.CreateDirectory(keepBranch);
        await File.WriteAllTextAsync(Path.Combine(keepBranch, "keep.txt"), "content");

        var action = CreateClearEmptiesAction();
        var context = new UtilityActionContext(new Dictionary<string, string>
        {
            ["directory"] = root
        });

        await action.ExecuteAsync(context);

        Assert.False(Directory.Exists(removeBranch));
        Assert.False(Directory.Exists(Path.Combine(root, "remove", "x")));
        Assert.False(Directory.Exists(Path.Combine(root, "remove")));
        Assert.True(Directory.Exists(keepBranch));
    }

    [Fact]
    public async Task Returns_message_when_directory_missing()
    {
        var action = CreateClearEmptiesAction();
        var context = new UtilityActionContext(new Dictionary<string, string>
        {
            ["directory"] = Path.Combine(_tempRoot, "does-not-exist")
        });

        var result = await action.ExecuteAsync(context);

        Assert.Contains("Directory does not exist", result);
    }

    [Fact]
    public async Task Does_not_delete_root_directory_even_when_empty()
    {
        var root = CreateFolder("root");

        var action = CreateClearEmptiesAction();
        var context = new UtilityActionContext(new Dictionary<string, string>
        {
            ["directory"] = root
        });

        await action.ExecuteAsync(context);

        Assert.True(Directory.Exists(root));
    }

    [Fact]
    public async Task Skips_directory_symlink_to_outside_root()
    {
        var root = CreateFolder("root");
        var outside = CreateFolder("outside");
        var outsideLeaf = Path.Combine(outside, "leaf");
        Directory.CreateDirectory(outsideLeaf);

        var linkPath = Path.Combine(root, "outside-link");
        if (!TryCreateDirectorySymlink(linkPath, outside))
        {
            return;
        }

        var action = CreateClearEmptiesAction();
        var context = new UtilityActionContext(new Dictionary<string, string>
        {
            ["directory"] = root
        });

        await action.ExecuteAsync(context);

        Assert.True(Directory.Exists(linkPath));
        Assert.True(Directory.Exists(outsideLeaf));
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

    private static IUtilityAction CreateClearEmptiesAction()
    {
        var module = new ClearEmptiesUtilityModule();
        return module.Actions.Single(action => action.Id == "clear-empties");
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
