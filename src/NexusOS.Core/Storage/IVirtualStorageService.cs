namespace NexusOS.Core.Storage;

public class NexusStorageItem
{
    public string Name { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public bool IsDirectory { get; set; }
    public long SizeBytes { get; set; }
    public DateTime DateModified { get; set; }
    public string Extension { get; set; } = string.Empty;
    public string IconKey { get; set; } = "File";
    public bool IsHostFile { get; set; }

    public string FormattedSize
    {
        get
        {
            if (IsDirectory) return "--";
            if (SizeBytes < 1024) return $"{SizeBytes} B";
            if (SizeBytes < 1024 * 1024) return $"{SizeBytes / 1024.0:F1} KB";
            if (SizeBytes < 1024 * 1024 * 1024) return $"{SizeBytes / (1024.0 * 1024):F1} MB";
            return $"{SizeBytes / (1024.0 * 1024 * 1024):F2} GB";
        }
    }
}

public interface IVirtualStorageService
{
    string WorkspaceRoot { get; }
    string DownloadsRoot { get; }
    string DesktopRoot { get; }
    string DocumentsRoot { get; }

    void Initialize(string? customWorkspaceRoot = null);
    IEnumerable<NexusStorageItem> GetItems(string path);
    Task<NexusStorageItem> CreateFolderAsync(string parentPath, string folderName);
    Task DeleteAsync(string path);
    Task RenameAsync(string path, string newName);
    Task CopyAsync(string sourcePath, string destDirectory);
    Task MoveAsync(string sourcePath, string destDirectory);
    Task<NexusStorageItem> ImportFromHostAsync(string hostPath, string targetDirectory, IProgress<double>? progress = null);
    Task<string> ReadTextAsync(string path);
    Task WriteTextAsync(string path, string content);
    bool Exists(string path);
}
