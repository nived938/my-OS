using NexusOS.Core.Storage;

namespace NexusOS.Infrastructure.Storage;

public class VirtualStorageService : IVirtualStorageService
{
    private string _workspaceRoot = string.Empty;

    public string WorkspaceRoot => _workspaceRoot;
    public string DownloadsRoot => Path.Combine(_workspaceRoot, "Downloads");
    public string DesktopRoot => Path.Combine(_workspaceRoot, "Desktop");
    public string DocumentsRoot => Path.Combine(_workspaceRoot, "Documents");

    public void Initialize(string? customWorkspaceRoot = null)
    {
        _workspaceRoot = customWorkspaceRoot ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "NexusOS", "Workspace");

        if (!Directory.Exists(_workspaceRoot))
        {
            Directory.CreateDirectory(_workspaceRoot);
        }

        // Ensure default directories exist
        var defaultDirs = new[] { "Desktop", "Documents", "Downloads", "Pictures", "Music", "Videos" };
        foreach (var dir in defaultDirs)
        {
            var p = Path.Combine(_workspaceRoot, dir);
            if (!Directory.Exists(p))
            {
                Directory.CreateDirectory(p);
            }
        }

        // Create sample getting started readme if workspace is fresh
        var welcomeFile = Path.Combine(DesktopRoot, "Welcome to NEXUS OS.txt");
        if (!File.Exists(welcomeFile))
        {
            File.WriteAllText(welcomeFile, 
                "Welcome to NEXUS OS!\n\n" +
                "NEXUS OS is a high-performance, customizable Windows-like desktop operating system that runs inside Windows.\n\n" +
                "Key Features:\n" +
                "- Drag and drop files from your real Windows desktop/File Explorer directly into this window.\n" +
                "- Open Nexus Explorer to manage your sandboxed workspace.\n" +
                "- Browse the web with Nexus Navigator (powered by Microsoft WebView2).\n" +
                "- Play built-in games (Minesweeper, Snake, Solitaire, Sudoku, 2D Platformer).\n" +
                "- Customize themes, wallpapers, blur, and window styling in Nexus Settings.\n" +
                "- Safe executable inspection with Authenticode signature analysis.\n");
        }
    }

    public IEnumerable<NexusStorageItem> GetItems(string path)
    {
        var targetDir = ResolvePath(path);
        if (!Directory.Exists(targetDir))
            return Enumerable.Empty<NexusStorageItem>();

        var result = new List<NexusStorageItem>();

        try
        {
            var dirInfo = new DirectoryInfo(targetDir);
            
            foreach (var subDir in dirInfo.GetDirectories())
            {
                result.Add(new NexusStorageItem
                {
                    Name = subDir.Name,
                    FullPath = subDir.FullName,
                    RelativePath = Path.GetRelativePath(_workspaceRoot, subDir.FullName),
                    IsDirectory = true,
                    DateModified = subDir.LastWriteTime,
                    IconKey = "Folder",
                    SizeBytes = 0
                });
            }

            foreach (var file in dirInfo.GetFiles())
            {
                result.Add(new NexusStorageItem
                {
                    Name = file.Name,
                    FullPath = file.FullName,
                    RelativePath = Path.GetRelativePath(_workspaceRoot, file.FullName),
                    IsDirectory = false,
                    SizeBytes = file.Length,
                    DateModified = file.LastWriteTime,
                    Extension = file.Extension.ToLowerInvariant(),
                    IconKey = GetIconKeyForExtension(file.Extension)
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error reading directory {path}: {ex.Message}");
        }

        return result;
    }

    public Task<NexusStorageItem> CreateFolderAsync(string parentPath, string folderName)
    {
        var resolvedParent = ResolvePath(parentPath);
        var newPath = Path.Combine(resolvedParent, folderName);
        if (!Directory.Exists(newPath))
        {
            Directory.CreateDirectory(newPath);
        }

        var dirInfo = new DirectoryInfo(newPath);
        var item = new NexusStorageItem
        {
            Name = dirInfo.Name,
            FullPath = dirInfo.FullName,
            RelativePath = Path.GetRelativePath(_workspaceRoot, dirInfo.FullName),
            IsDirectory = true,
            DateModified = dirInfo.LastWriteTime,
            IconKey = "Folder"
        };

        return Task.FromResult(item);
    }

    public Task DeleteAsync(string path)
    {
        var resolved = ResolvePath(path);
        if (File.Exists(resolved))
        {
            File.Delete(resolved);
        }
        else if (Directory.Exists(resolved))
        {
            Directory.Delete(resolved, recursive: true);
        }
        return Task.CompletedTask;
    }

    public Task RenameAsync(string path, string newName)
    {
        var resolved = ResolvePath(path);
        if (File.Exists(resolved))
        {
            var parent = Path.GetDirectoryName(resolved) ?? _workspaceRoot;
            var dest = Path.Combine(parent, newName);
            File.Move(resolved, dest);
        }
        else if (Directory.Exists(resolved))
        {
            var parent = Directory.GetParent(resolved)?.FullName ?? _workspaceRoot;
            var dest = Path.Combine(parent, newName);
            Directory.Move(resolved, dest);
        }
        return Task.CompletedTask;
    }

    public Task CopyAsync(string sourcePath, string destDirectory)
    {
        var resolvedSource = ResolvePath(sourcePath);
        var resolvedDestDir = ResolvePath(destDirectory);

        if (File.Exists(resolvedSource))
        {
            var fileName = Path.GetFileName(resolvedSource);
            var destFile = Path.Combine(resolvedDestDir, fileName);
            File.Copy(resolvedSource, destFile, overwrite: true);
        }
        else if (Directory.Exists(resolvedSource))
        {
            CopyDirectoryRecursively(resolvedSource, Path.Combine(resolvedDestDir, Path.GetFileName(resolvedSource)));
        }

        return Task.CompletedTask;
    }

    public Task MoveAsync(string sourcePath, string destDirectory)
    {
        var resolvedSource = ResolvePath(sourcePath);
        var resolvedDestDir = ResolvePath(destDirectory);

        if (File.Exists(resolvedSource))
        {
            var fileName = Path.GetFileName(resolvedSource);
            var destFile = Path.Combine(resolvedDestDir, fileName);
            File.Move(resolvedSource, destFile, overwrite: true);
        }
        else if (Directory.Exists(resolvedSource))
        {
            var destFolder = Path.Combine(resolvedDestDir, Path.GetFileName(resolvedSource));
            Directory.Move(resolvedSource, destFolder);
        }

        return Task.CompletedTask;
    }

    public async Task<NexusStorageItem> ImportFromHostAsync(string hostPath, string targetDirectory, IProgress<double>? progress = null)
    {
        if (!File.Exists(hostPath) && !Directory.Exists(hostPath))
        {
            throw new FileNotFoundException($"Host path does not exist: {hostPath}");
        }

        var resolvedTargetDir = ResolvePath(targetDirectory);
        if (!Directory.Exists(resolvedTargetDir))
        {
            Directory.CreateDirectory(resolvedTargetDir);
        }

        if (File.Exists(hostPath))
        {
            var fileInfo = new FileInfo(hostPath);
            var destPath = Path.Combine(resolvedTargetDir, fileInfo.Name);

            // Stream copy with real progress reporting
            const int bufferSize = 64 * 1024;
            var buffer = new byte[bufferSize];
            long totalBytes = fileInfo.Length;
            long copiedBytes = 0;

            await using (var sourceStream = new FileStream(hostPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            await using (var destStream = new FileStream(destPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                int bytesRead;
                while ((bytesRead = await sourceStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await destStream.WriteAsync(buffer, 0, bytesRead);
                    copiedBytes += bytesRead;
                    if (totalBytes > 0 && progress != null)
                    {
                        progress.Report((double)copiedBytes / totalBytes);
                    }
                }
            }

            var importedFileInfo = new FileInfo(destPath);
            return new NexusStorageItem
            {
                Name = importedFileInfo.Name,
                FullPath = importedFileInfo.FullName,
                RelativePath = Path.GetRelativePath(_workspaceRoot, importedFileInfo.FullName),
                IsDirectory = false,
                SizeBytes = importedFileInfo.Length,
                DateModified = importedFileInfo.LastWriteTime,
                Extension = importedFileInfo.Extension.ToLowerInvariant(),
                IconKey = GetIconKeyForExtension(importedFileInfo.Extension),
                IsHostFile = true
            };
        }
        else
        {
            var dirInfo = new DirectoryInfo(hostPath);
            var destDir = Path.Combine(resolvedTargetDir, dirInfo.Name);
            CopyDirectoryRecursively(hostPath, destDir);

            return new NexusStorageItem
            {
                Name = dirInfo.Name,
                FullPath = destDir,
                RelativePath = Path.GetRelativePath(_workspaceRoot, destDir),
                IsDirectory = true,
                DateModified = DateTime.Now,
                IconKey = "Folder",
                IsHostFile = true
            };
        }
    }

    public async Task<string> ReadTextAsync(string path)
    {
        var resolved = ResolvePath(path);
        return await File.ReadAllTextAsync(resolved);
    }

    public async Task WriteTextAsync(string path, string content)
    {
        var resolved = ResolvePath(path);
        var dir = Path.GetDirectoryName(resolved);
        if (dir != null && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        await File.WriteAllTextAsync(resolved, content);
    }

    public bool Exists(string path)
    {
        var resolved = ResolvePath(path);
        return File.Exists(resolved) || Directory.Exists(resolved);
    }

    private string ResolvePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return _workspaceRoot;

        if (Path.IsPathRooted(path))
            return path;

        return Path.Combine(_workspaceRoot, path);
    }

    private static void CopyDirectoryRecursively(string sourceDir, string targetDir)
    {
        Directory.CreateDirectory(targetDir);

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var dest = Path.Combine(targetDir, Path.GetFileName(file));
            File.Copy(file, dest, true);
        }

        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            var dest = Path.Combine(targetDir, Path.GetFileName(dir));
            CopyDirectoryRecursively(dir, dest);
        }
    }

    private static string GetIconKeyForExtension(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".txt" or ".md" or ".log" => "FileText",
            ".png" or ".jpg" or ".jpeg" or ".bmp" or ".gif" or ".svg" or ".webp" => "FileImage",
            ".mp3" or ".wav" or ".flac" or ".ogg" => "FileAudio",
            ".mp4" or ".mkv" or ".avi" or ".mov" => "FileVideo",
            ".zip" or ".rar" or ".7z" or ".tar" or ".gz" => "FileArchive",
            ".pdf" => "FilePdf",
            ".exe" or ".msi" => "FileExe",
            ".cs" or ".js" or ".ts" or ".py" or ".cpp" or ".rs" or ".json" or ".xml" => "FileCode",
            _ => "File"
        };
    }
}
