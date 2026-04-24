using UtilitySuite.Core.Abstractions;
using UtilitySuite.Modules.Photo;

namespace UtilitySuite.Core.Tests;

public sealed class KeepRawActionTests : IDisposable
{
    private readonly string _tempRoot;

    public KeepRawActionTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "utilitysuite-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);
    }

    [Fact]
    public async Task Deletes_jpg_when_matching_cr3_exists_same_folder()
    {
        var folder = Path.Combine(_tempRoot, "shoot-1");
        Directory.CreateDirectory(folder);

        var cr3Path = Path.Combine(folder, "IMG_1950.CR3");
        var jpgPath = Path.Combine(folder, "IMG_1950.JPG");
        await File.WriteAllTextAsync(cr3Path, "raw");
        await File.WriteAllTextAsync(jpgPath, "jpg");

        var action = CreateKeepRawAction();
        var context = new UtilityActionContext(new Dictionary<string, string>
        {
            ["directory"] = folder
        });

        var result = await action.ExecuteAsync(context);

        Assert.False(File.Exists(jpgPath));
        Assert.True(File.Exists(cr3Path));
        Assert.Contains("Deleted 1 JPG files", result);
    }

    [Fact]
    public async Task Keeps_jpg_when_no_matching_cr3_exists()
    {
        var folder = Path.Combine(_tempRoot, "shoot-2");
        Directory.CreateDirectory(folder);

        var jpgPath = Path.Combine(folder, "IMG_1951.JPG");
        await File.WriteAllTextAsync(jpgPath, "jpg");

        var action = CreateKeepRawAction();
        var context = new UtilityActionContext(new Dictionary<string, string>
        {
            ["directory"] = folder
        });

        await action.ExecuteAsync(context);

        Assert.True(File.Exists(jpgPath));
    }

    [Fact]
    public async Task Only_deletes_when_match_is_in_same_folder()
    {
        var folderA = Path.Combine(_tempRoot, "a");
        var folderB = Path.Combine(_tempRoot, "b");
        Directory.CreateDirectory(folderA);
        Directory.CreateDirectory(folderB);

        var jpgPath = Path.Combine(folderA, "IMG_2000.JPG");
        var cr3PathInOtherFolder = Path.Combine(folderB, "IMG_2000.CR3");
        await File.WriteAllTextAsync(jpgPath, "jpg");
        await File.WriteAllTextAsync(cr3PathInOtherFolder, "raw");

        var action = CreateKeepRawAction();
        var context = new UtilityActionContext(new Dictionary<string, string>
        {
            ["directory"] = _tempRoot
        });

        await action.ExecuteAsync(context);

        Assert.True(File.Exists(jpgPath));
    }

    [Fact]
    public async Task Handles_recursive_subdirectories()
    {
        var nested = Path.Combine(_tempRoot, "session", "day1");
        Directory.CreateDirectory(nested);

        var cr3Path = Path.Combine(nested, "IMG_3000.cr3");
        var jpgPath = Path.Combine(nested, "IMG_3000.jpg");
        await File.WriteAllTextAsync(cr3Path, "raw");
        await File.WriteAllTextAsync(jpgPath, "jpg");

        var action = CreateKeepRawAction();
        var context = new UtilityActionContext(new Dictionary<string, string>
        {
            ["directory"] = _tempRoot
        });

        await action.ExecuteAsync(context);

        Assert.False(File.Exists(jpgPath));
        Assert.True(File.Exists(cr3Path));
    }

    [Fact]
    public async Task Returns_message_when_directory_missing()
    {
        var action = CreateKeepRawAction();
        var context = new UtilityActionContext(new Dictionary<string, string>
        {
            ["directory"] = Path.Combine(_tempRoot, "does-not-exist")
        });

        var result = await action.ExecuteAsync(context);

        Assert.Contains("Directory does not exist", result);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
    }

    private static IUtilityAction CreateKeepRawAction()
    {
        var module = new KeepRawUtilityModule();
        return module.Actions.Single(action => action.Id == "keep-raw");
    }
}
