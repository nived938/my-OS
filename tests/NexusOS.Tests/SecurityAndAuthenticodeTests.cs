using Microsoft.VisualStudio.TestTools.UnitTesting;
using NexusOS.Core.Security;
using NexusOS.Infrastructure.Security;

namespace NexusOS.Tests;

[TestClass]
public class SecurityAndAuthenticodeTests
{
    private string _testDir = string.Empty;
    private SecurityService _securityService = null!;

    [TestInitialize]
    public void Setup()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "NexusOS_SecTests_" + Guid.NewGuid().ToString("N"));
        _securityService = new SecurityService(_testDir);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_testDir))
        {
            try { Directory.Delete(_testDir, recursive: true); } catch { }
        }
    }

    [TestMethod]
    public void CalculateSha256_ReturnsDeterministicHash()
    {
        var tempFile = Path.Combine(_testDir, "test.bin");
        Directory.CreateDirectory(_testDir);
        File.WriteAllBytes(tempFile, new byte[] { 1, 2, 3, 4, 5 });

        var hash1 = _securityService.CalculateSha256(tempFile);
        var hash2 = _securityService.CalculateSha256(tempFile);

        Assert.IsFalse(string.IsNullOrEmpty(hash1));
        Assert.AreEqual(hash1, hash2);
        Assert.AreEqual(64, hash1.Length); // 256 bits = 64 hex chars
    }

    [TestMethod]
    public void InspectExecutable_OnUnsignedBinary_ReturnsCautionUnsigned()
    {
        var unsignedFile = Path.Combine(_testDir, "unsigned_dummy.exe");
        Directory.CreateDirectory(_testDir);
        File.WriteAllBytes(unsignedFile, new byte[] { 0x4D, 0x5A, 0x90, 0x00 }); // MZ header

        var report = _securityService.InspectExecutable(unsignedFile);

        Assert.IsFalse(report.IsSigned);
        Assert.AreEqual(SecurityRiskLevel.CautionUnsigned, report.RiskLevel);
        Assert.IsTrue(report.SecurityNotes.Any(n => n.Contains("no digital Authenticode signature")));
    }

    [TestMethod]
    public void InspectExecutable_OnRealWindowsSystemBinary_ExtractsAuthenticode()
    {
        var targetPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe");
        if (File.Exists(targetPath))
        {
            var report = _securityService.InspectExecutable(targetPath);
            Assert.IsTrue(report.FileSizeBytes > 0);
            Assert.IsFalse(string.IsNullOrEmpty(report.Sha256Hash));
            // Windows explorer.exe is signed by Microsoft
            Assert.IsTrue(report.IsSigned);
            Assert.IsNotNull(report.SignerSubject);
            Assert.IsTrue(report.SignerSubject.Contains("Microsoft"));
        }
    }

    [TestMethod]
    public void IsPathWithinAllowedBounds_DetectsDirectoryTraversal()
    {
        var safeRoot = @"C:\Users\TestUser\NexusOS\Workspace";
        var safePath = @"C:\Users\TestUser\NexusOS\Workspace\Documents\file.txt";
        var unsafeEscape = @"C:\Windows\System32\cmd.exe";

        var isSafe = _securityService.IsPathWithinAllowedBounds(safePath, new[] { safeRoot });
        var isUnsafe = _securityService.IsPathWithinAllowedBounds(unsafeEscape, new[] { safeRoot });

        Assert.IsTrue(isSafe);
        Assert.IsFalse(isUnsafe);
    }
}
