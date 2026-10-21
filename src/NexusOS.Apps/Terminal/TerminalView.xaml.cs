using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NexusOS.Core.Storage;

namespace NexusOS.Apps.Terminal;

public partial class TerminalView : UserControl
{
    private readonly IVirtualStorageService _storageService;
    private string _currentDirectory;
    private readonly List<string> _history = new();
    private int _historyIndex = -1;

    public TerminalView(IVirtualStorageService storageService)
    {
        InitializeComponent();
        _storageService = storageService;
        _storageService.Initialize();
        _currentDirectory = _storageService.WorkspaceRoot;

        PrintWelcomeBanner();
        Loaded += (s, e) => CommandLineInput.Focus();
    }

    private void PrintWelcomeBanner()
    {
        OutputTextBlock.Text =
            "=========================================================\n" +
            "  NEXUS OS TERMINAL SUBSYSTEM [Version 1.0.26300]\n" +
            "  Integrated Sandboxed Command Environment\n" +
            "  Type 'help' to view available commands.\n" +
            "=========================================================\n\n";
    }

    private async void CommandLineInput_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            var rawCmd = CommandLineInput.Text.Trim();
            CommandLineInput.Text = string.Empty;

            if (!string.IsNullOrEmpty(rawCmd))
            {
                _history.Add(rawCmd);
                _historyIndex = _history.Count;
            }

            OutputTextBlock.Text += $"{PromptLabel.Text}{rawCmd}\n";
            await ExecuteCommandAsync(rawCmd);
            TerminalScrollViewer.ScrollToBottom();
        }
        else if (e.Key == Key.Up)
        {
            if (_history.Count > 0 && _historyIndex > 0)
            {
                _historyIndex--;
                CommandLineInput.Text = _history[_historyIndex];
                CommandLineInput.CaretIndex = CommandLineInput.Text.Length;
            }
        }
        else if (e.Key == Key.Down)
        {
            if (_historyIndex < _history.Count - 1)
            {
                _historyIndex++;
                CommandLineInput.Text = _history[_historyIndex];
                CommandLineInput.CaretIndex = CommandLineInput.Text.Length;
            }
            else
            {
                _historyIndex = _history.Count;
                CommandLineInput.Text = string.Empty;
            }
        }
    }

    private async Task ExecuteCommandAsync(string commandLine)
    {
        if (string.IsNullOrWhiteSpace(commandLine))
            return;

        var parts = commandLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var cmd = parts[0].ToLowerInvariant();
        var args = parts.Skip(1).ToArray();

        switch (cmd)
        {
            case "help":
                OutputTextBlock.Text +=
                    "Nexus OS Core Commands:\n" +
                    "  help              Show this help menu\n" +
                    "  clear, cls        Clear terminal buffer\n" +
                    "  sysinfo           Display hardware, OS and runtime specifications\n" +
                    "  ls, dir           List files and directories in current folder\n" +
                    "  cd <path>         Change current workspace directory\n" +
                    "  cat <file>        Read contents of a text file\n" +
                    "  mkdir <name>      Create a new directory\n" +
                    "  rm <name>         Delete a file or folder\n" +
                    "  ps                List active processes\n" +
                    "  host <command>    Safely bridge a command to host Windows PowerShell\n\n";
                break;

            case "clear" or "cls":
                OutputTextBlock.Text = string.Empty;
                break;

            case "sysinfo":
                OutputTextBlock.Text +=
                    $"OS Name:      NEXUS OS Desktop Environment\n" +
                    $"Kernel Base:  Windows NT (Build 26300)\n" +
                    $"Runtime:      .NET 10.0 WPF (DirectX Accelerated)\n" +
                    $"Logical CPUs: {Environment.ProcessorCount} Cores\n" +
                    $"Machine:      {Environment.MachineName}\n" +
                    $"User:         {Environment.UserName}\n" +
                    $"Working Dir:  {_currentDirectory}\n\n";
                break;

            case "ls" or "dir":
                try
                {
                    var items = _storageService.GetItems(_currentDirectory).ToList();
                    OutputTextBlock.Text += $"Directory listing of: {_currentDirectory}\n";
                    OutputTextBlock.Text += "Mode         Size         Name\n";
                    OutputTextBlock.Text += "----         ----         ----\n";
                    foreach (var item in items)
                    {
                        var mode = item.IsDirectory ? "<DIR> " : "FILE  ";
                        OutputTextBlock.Text += $"{mode}       {item.FormattedSize,-10}   {item.Name}\n";
                    }
                    OutputTextBlock.Text += $"\nTotal: {items.Count} items\n\n";
                }
                catch (Exception ex)
                {
                    OutputTextBlock.Text += $"Error: {ex.Message}\n\n";
                }
                break;

            case "cd":
                var target = args.Length > 0 ? args[0] : _storageService.WorkspaceRoot;
                var resolved = Path.IsPathRooted(target) ? target : Path.Combine(_currentDirectory, target);
                if (_storageService.Exists(resolved) && Directory.Exists(resolved))
                {
                    _currentDirectory = Path.GetFullPath(resolved);
                    var rel = Path.GetRelativePath(_storageService.WorkspaceRoot, _currentDirectory);
                    PromptLabel.Text = rel == "." ? "nexus@desktop:~$ " : $"nexus@desktop:~/{rel}$ ";
                }
                else
                {
                    OutputTextBlock.Text += $"cd: directory not found: {target}\n\n";
                }
                break;

            case "cat":
                if (args.Length == 0)
                {
                    OutputTextBlock.Text += "Usage: cat <filename>\n\n";
                }
                else
                {
                    var filePath = Path.Combine(_currentDirectory, args[0]);
                    if (File.Exists(filePath))
                    {
                        var text = await _storageService.ReadTextAsync(filePath);
                        OutputTextBlock.Text += text + "\n\n";
                    }
                    else
                    {
                        OutputTextBlock.Text += $"cat: file not found: {args[0]}\n\n";
                    }
                }
                break;

            case "mkdir":
                if (args.Length == 0)
                {
                    OutputTextBlock.Text += "Usage: mkdir <folder_name>\n\n";
                }
                else
                {
                    await _storageService.CreateFolderAsync(_currentDirectory, args[0]);
                    OutputTextBlock.Text += $"Directory created: {args[0]}\n\n";
                }
                break;

            case "rm":
                if (args.Length == 0)
                {
                    OutputTextBlock.Text += "Usage: rm <filename>\n\n";
                }
                else
                {
                    var p = Path.Combine(_currentDirectory, args[0]);
                    if (_storageService.Exists(p))
                    {
                        await _storageService.DeleteAsync(p);
                        OutputTextBlock.Text += $"Deleted: {args[0]}\n\n";
                    }
                    else
                    {
                        OutputTextBlock.Text += $"rm: path not found: {args[0]}\n\n";
                    }
                }
                break;

            case "ps":
                var procs = Process.GetProcesses().OrderByDescending(p => p.WorkingSet64).Take(10);
                OutputTextBlock.Text += "Top Active Host Processes:\n";
                OutputTextBlock.Text += "PID       Memory(MB)    Process Name\n";
                OutputTextBlock.Text += "---       ----------    ------------\n";
                foreach (var p in procs)
                {
                    try
                    {
                        OutputTextBlock.Text += $"{p.Id,-9} {p.WorkingSet64 / (1024 * 1024),-13} {p.ProcessName}\n";
                    }
                    catch
                    {
                    }
                }
                OutputTextBlock.Text += "\n";
                break;

            case "host":
                if (args.Length == 0)
                {
                    OutputTextBlock.Text += "Usage: host <powershell command>\n\n";
                }
                else
                {
                    var hostCmd = string.Join(" ", args);
                    var confirm = MessageBox.Show(
                        $"Execute on Host Windows PowerShell?\n\nCommand: {hostCmd}",
                        "Confirm Host Shell Command",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (confirm == MessageBoxResult.Yes)
                    {
                        try
                        {
                            var psi = new ProcessStartInfo
                            {
                                FileName = "powershell.exe",
                                Arguments = $"-NoProfile -Command \"{hostCmd}\"",
                                RedirectStandardOutput = true,
                                RedirectStandardError = true,
                                UseShellExecute = false,
                                CreateNoWindow = true
                            };

                            using var proc = Process.Start(psi);
                            if (proc != null)
                            {
                                var stdout = await proc.StandardOutput.ReadToEndAsync();
                                var stderr = await proc.StandardError.ReadToEndAsync();
                                await proc.WaitForExitAsync();

                                if (!string.IsNullOrEmpty(stdout)) OutputTextBlock.Text += stdout + "\n";
                                if (!string.IsNullOrEmpty(stderr)) OutputTextBlock.Text += $"[STDERR] {stderr}\n";
                            }
                        }
                        catch (Exception ex)
                        {
                            OutputTextBlock.Text += $"Error executing host command: {ex.Message}\n\n";
                        }
                    }
                    else
                    {
                        OutputTextBlock.Text += "Host command execution canceled.\n\n";
                    }
                }
                break;

            default:
                OutputTextBlock.Text += $"nexus: command not found: '{cmd}'. Type 'help' for command list.\n\n";
                break;
        }
    }

    private void HostPowerShellTab_Click(object sender, RoutedEventArgs e)
    {
        var confirm = MessageBox.Show(
            "Do you want to launch a native Windows PowerShell terminal window on your host PC?",
            "Launch Host PowerShell",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm == MessageBoxResult.Yes)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "powershell.exe",
                UseShellExecute = true
            });
        }
    }
}
