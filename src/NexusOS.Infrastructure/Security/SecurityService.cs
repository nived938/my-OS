using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using NexusOS.Core.Security;

namespace NexusOS.Infrastructure.Security;

public class SecurityService : ISecurityService
{
    private readonly string _auditLogPath;

    public SecurityService(string? customLogDir = null)
    {
        var logDir = customLogDir ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "NexusOS", "Logs");
        if (!Directory.Exists(logDir))
        {
            Directory.CreateDirectory(logDir);
        }
        _auditLogPath = Path.Combine(logDir, "security_audit.log");
    }

    public AuthenticodeReport InspectExecutable(string filePath)
    {
        var report = new AuthenticodeReport
        {
            FilePath = filePath,
            FileName = Path.GetFileName(filePath)
        };

        if (!File.Exists(filePath))
        {
            report.RiskLevel = SecurityRiskLevel.HighRisk;
            report.SecurityNotes.Add("File not found on disk.");
            return report;
        }

        var fileInfo = new FileInfo(filePath);
        report.FileSizeBytes = fileInfo.Length;
        report.Sha256Hash = CalculateSha256(filePath);

        try
        {
            // Inspect digital certificate via X509Certificate2 Authenticode
#pragma warning disable SYSLIB0057
            using var cert = new X509Certificate2(X509Certificate.CreateFromSignedFile(filePath));
#pragma warning restore SYSLIB0057
            report.IsSigned = true;
            report.SignerSubject = cert.Subject;
            report.Issuer = cert.Issuer;
            
            // Basic validity check
            var now = DateTime.Now;
            var isTimeValid = now >= cert.NotBefore && now <= cert.NotAfter;
            
            using var chain = new X509Chain();
            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck; // Offline check friendly
            var isValidChain = chain.Build(cert);

            report.IsCertificateValid = isTimeValid && isValidChain;

            if (report.IsCertificateValid)
            {
                report.RiskLevel = SecurityRiskLevel.VerifiedSafe;
                report.SecurityNotes.Add($"Valid digital signature by: {cert.GetNameInfo(X509NameType.SimpleName, false)}");
                report.SecurityNotes.Add($"Issued by: {cert.GetNameInfo(X509NameType.SimpleName, true)}");
                report.SecurityNotes.Add($"Valid from {cert.NotBefore:d} to {cert.NotAfter:d}");
            }
            else
            {
                report.RiskLevel = SecurityRiskLevel.Suspicious;
                report.SecurityNotes.Add("Digital signature is present, but certificate chain verification failed or certificate has expired.");
            }
        }
        catch (CryptographicException)
        {
            // File is not signed
            report.IsSigned = false;
            report.IsCertificateValid = false;
            report.RiskLevel = SecurityRiskLevel.CautionUnsigned;
            report.SecurityNotes.Add("File has no digital Authenticode signature.");
            report.SecurityNotes.Add("Unsigned executables can be modified and originate from unverified authors.");
        }
        catch (Exception ex)
        {
            report.IsSigned = false;
            report.RiskLevel = SecurityRiskLevel.Suspicious;
            report.SecurityNotes.Add($"Error during signature analysis: {ex.Message}");
        }

        RecordAuditEvent("INSPECT_EXECUTABLE", filePath, $"{report.RiskLevel} (Signed: {report.IsSigned})");
        return report;
    }

    public string CalculateSha256(string filePath)
    {
        if (!File.Exists(filePath)) return string.Empty;

        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(filePath);
        var hashBytes = sha256.ComputeHash(stream);
        return Convert.ToHexString(hashBytes);
    }

    public bool IsPathWithinAllowedBounds(string targetPath, IEnumerable<string> allowedRoots)
    {
        var fullTarget = Path.GetFullPath(targetPath);
        foreach (var root in allowedRoots)
        {
            var fullRoot = Path.GetFullPath(root);
            if (fullTarget.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    public void RecordAuditEvent(string action, string target, string result)
    {
        try
        {
            var entry = $"[{DateTime.UtcNow:O}] ACTION={action} | TARGET={target} | RESULT={result}{Environment.NewLine}";
            File.AppendAllText(_auditLogPath, entry);
        }
        catch
        {
            // Suppress audit logging failures
        }
    }
}
