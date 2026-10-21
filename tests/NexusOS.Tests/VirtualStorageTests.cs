using Microsoft.VisualStudio.TestTools.UnitTesting;
using NexusOS.Infrastructure.Storage;

namespace NexusOS.Tests;

[TestClass]
public class VirtualStorageTests
{
    private string _testDir = string.Empty;
    private VirtualStorageService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "NexusOS_StorageTests_" + Guid.NewGuid().ToString("N"));
        _service = new VirtualStorageService();
        _service.Initialize(_testDir);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_testDir))
        {
            try
            {
                Directory.Delete(_testDir, recursive: true);
            }
            catch
            {
            }
        }
    }

    [TestMethod]
    public void Initialize_CreatesStandardDirectories()
    {
        Assert.IsTrue(Directory.Exists(_service.WorkspaceRoot));
        Assert.IsTrue(Directory.Exists(_service.DesktopRoot));
        Assert.IsTrue(Directory.Exists(_service.DocumentsRoot));
        Assert.IsTrue(Directory.Exists(_service.DownloadsRoot));
    }

    [TestMethod]
    public async Task CreateFolder_And_WriteReadText_Succeeds()
    {
        var folder = await _service.CreateFolderAsync(_service.DocumentsRoot, "Projects");
        Assert.IsTrue(Directory.Exists(folder.FullPath));

        var filePath = Path.Combine(folder.FullPath, "notes.txt");
        await _service.WriteTextAsync(filePath, "Hello NEXUS OS");

        var text = await _service.ReadTextAsync(filePath);
        Assert.AreEqual("Hello NEXUS OS", text);

        var items = _service.GetItems(folder.FullPath).ToList();
        Assert.AreEqual(1, items.Count);
        Assert.AreEqual("notes.txt", items[0].Name);
        Assert.IsFalse(items[0].IsDirectory);
    }

    [TestMethod]
    public async Task ImportFromHost_CopiesFile_WithCorrectSizeAndName()
    {
        // Create simulated host file
        var hostFile = Path.Combine(Path.GetTempPath(), "host_sample_" + Guid.NewGuid().ToString("N") + ".txt");
        await File.WriteAllTextAsync(hostFile, "Sample content from host Windows system.");

        try
        {
            var imported = await _service.ImportFromHostAsync(hostFile, _service.DesktopRoot);

            Assert.AreEqual(Path.GetFileName(hostFile), imported.Name);
            Assert.IsTrue(File.Exists(imported.FullPath));
            Assert.IsTrue(imported.SizeBytes > 0);
            Assert.IsTrue(imported.IsHostFile);

            var readContent = await _service.ReadTextAsync(imported.FullPath);
            Assert.AreEqual("Sample content from host Windows system.", readContent);
        }
        finally
        {
            if (File.Exists(hostFile)) File.Delete(hostFile);
        }
    }
}
