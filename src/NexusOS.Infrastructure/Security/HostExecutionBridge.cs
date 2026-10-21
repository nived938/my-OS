using System.Diagnostics;
using NexusOS.Core.Security;

namespace NexusOS.Infrastructure.Security;

public class HostExecutionResult
{
    public bool Success { get; set; }
    public int? ProcessId { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class HostExecutionBridge
{
    private readonly ISecurityService _securityService;

    public HostExecutionBridge(ISecurityService securityService)
    {
        _securityService = securityService;
    }

    public Task<HostExecutionResult> LaunchExecutableAsync(string filePath, string[]? arguments = null, bool runAsAdmin = false)
    {
        if (!File.Exists(filePath))
        {
            return Task.FromResult(new HostExecutionResult
            {
                Success = false,
                Message = $"Executable file not found at: {filePath}"
            });
        }

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true,
                WorkingDirectory = Path.GetDirectoryName(filePath) ?? string.Empty
            };

            if (arguments != null && arguments.Length > 0)
            {
                startInfo.Arguments = string.Join(" ", arguments);
            }

            if (runAsAdmin)
            {
                startInfo.Verb = "runas";
            }

            var proc = Process.Start(startInfo);
            if (proc == null)
            {
                _securityService.RecordAuditEvent("HOST_EXECUTE_FAIL", filePath, "Process.Start returned null");
                return Task.FromResult(new HostExecutionResult
                {
                    Success = false,
                    Message = "Windows Shell failed to start the process."
                });
            }

            _securityService.RecordAuditEvent("HOST_EXECUTE_SUCCESS", filePath, $"PID: {proc.Id}");
            return Task.FromResult(new HostExecutionResult
            {
                Success = true,
                ProcessId = proc.Id,
                Message = $"Launched successfully (Process ID: {proc.Id})"
            });
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            // The operation was canceled by the user (UAC prompt declined or dismissed)
            _securityService.RecordAuditEvent("HOST_EXECUTE_CANCEL", filePath, "User declined elevation or canceled prompt");
            return Task.FromResult(new HostExecutionResult
            {
                Success = false,
                Message = "Launch was canceled by the user."
            });
        }
        catch (Exception ex)
        {
            _securityService.RecordAuditEvent("HOST_EXECUTE_ERROR", filePath, ex.Message);
            return Task.FromResult(new HostExecutionResult
            {
                Success = false,
                Message = $"Error starting process: {ex.Message}"
            });
        }
    }
}
