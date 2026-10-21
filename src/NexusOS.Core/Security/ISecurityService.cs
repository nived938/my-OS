namespace NexusOS.Core.Security;

public enum SecurityRiskLevel
{
    VerifiedSafe,
    CautionUnsigned,
    Suspicious,
    HighRisk
}

public class AuthenticodeReport
{
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string Sha256Hash { get; set; } = string.Empty;
    public bool IsSigned { get; set; }
    public bool IsCertificateValid { get; set; }
    public string? SignerSubject { get; set; }
    public string? Issuer { get; set; }
    public DateTime? Timestamp { get; set; }
    public SecurityRiskLevel RiskLevel { get; set; } = SecurityRiskLevel.CautionUnsigned;
    public List<string> SecurityNotes { get; set; } = new();
}

public interface ISecurityService
{
    AuthenticodeReport InspectExecutable(string filePath);
    string CalculateSha256(string filePath);
    bool IsPathWithinAllowedBounds(string targetPath, IEnumerable<string> allowedRoots);
    void RecordAuditEvent(string action, string target, string result);
}
