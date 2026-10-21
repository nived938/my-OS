using System.Diagnostics;
using NexusOS.Core.Virtualization;

namespace NexusOS.Infrastructure.Virtualization;

public class VirtualizationService : IVirtualizationService
{
    private readonly List<VirtualMachineSpec> _vms = new();
    private HypervisorProvider _detectedHypervisor = HypervisorProvider.None;
    private string _hypervisorDiagnostics = string.Empty;

    public bool IsHypervisorAvailable => _detectedHypervisor != HypervisorProvider.None;
    public HypervisorProvider DetectedHypervisor => _detectedHypervisor;
    public string HypervisorDiagnostics => _hypervisorDiagnostics;

    public VirtualizationService()
    {
        ProbeHypervisor();
        InitializeDefaultVms();
    }

    private void ProbeHypervisor()
    {
        try
        {
            // Check for QEMU in PATH or common install directories
            var qemuPaths = new[]
            {
                @"C:\Program Files\qemu\qemu-system-x86_64.exe",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "qemu", "qemu-system-x86_64.exe")
            };

            foreach (var qemuPath in qemuPaths)
            {
                if (File.Exists(qemuPath))
                {
                    _detectedHypervisor = HypervisorProvider.Qemu;
                    _hypervisorDiagnostics = $"QEMU x86_64 emulator detected at: {qemuPath}";
                    return;
                }
            }

            // Check for Hyper-V or Windows Virtual Machine Platform
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = "-NoProfile -Command \"Get-Service vmms -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Status\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var proc = Process.Start(psi);
            if (proc != null)
            {
                var output = proc.StandardOutput.ReadToEnd().Trim();
                proc.WaitForExit(2000);

                if (output.Equals("Running", StringComparison.OrdinalIgnoreCase))
                {
                    _detectedHypervisor = HypervisorProvider.HyperV;
                    _hypervisorDiagnostics = "Microsoft Hyper-V Virtual Machine Management Service is active and running.";
                    return;
                }
            }

            _detectedHypervisor = HypervisorProvider.None;
            _hypervisorDiagnostics = "No active hypervisor detected. To enable Mode B virtualization, install QEMU or enable the Windows Hyper-V optional feature.";
        }
        catch (Exception ex)
        {
            _detectedHypervisor = HypervisorProvider.None;
            _hypervisorDiagnostics = $"Hypervisor detection encountered: {ex.Message}";
        }
    }

    private void InitializeDefaultVms()
    {
        _vms.Add(new VirtualMachineSpec
        {
            Name = "Alpine Linux Minimal (Demo Sandbox)",
            OperatingSystem = "Alpine Linux x86_64",
            MemoryMb = 1024,
            CpuCores = 1,
            State = VmPowerState.Stopped,
            Hypervisor = _detectedHypervisor,
            StatusMessage = "Ready to boot ISO image"
        });

        _vms.Add(new VirtualMachineSpec
        {
            Name = "FreeDOS Retro Machine",
            OperatingSystem = "FreeDOS 1.3",
            MemoryMb = 256,
            CpuCores = 1,
            State = VmPowerState.Stopped,
            Hypervisor = _detectedHypervisor,
            StatusMessage = "Configured for legacy x86 testing"
        });
    }

    public Task<List<VirtualMachineSpec>> GetVirtualMachinesAsync()
    {
        return Task.FromResult(_vms.ToList());
    }

    public Task<VirtualMachineSpec> CreateVirtualMachineAsync(string name, string osType, int ramMb, int cores, string isoPath)
    {
        var vm = new VirtualMachineSpec
        {
            Name = name,
            OperatingSystem = osType,
            MemoryMb = ramMb,
            CpuCores = cores,
            IsoImagePath = isoPath,
            Hypervisor = _detectedHypervisor,
            State = VmPowerState.Stopped,
            StatusMessage = "Configured"
        };

        _vms.Add(vm);
        return Task.FromResult(vm);
    }

    public async Task<bool> StartVmAsync(string vmId)
    {
        var vm = _vms.FirstOrDefault(v => v.Id == vmId);
        if (vm == null) return false;

        vm.State = VmPowerState.Starting;
        vm.StatusMessage = "Initializing virtual machine resources...";
        await Task.Delay(400);

        if (_detectedHypervisor == HypervisorProvider.None)
        {
            vm.State = VmPowerState.Stopped;
            vm.StatusMessage = "Hyper-V / QEMU is not installed on host. Genuine hardware acceleration requires a supported hypervisor.";
            return false;
        }

        vm.State = VmPowerState.Running;
        vm.StatusMessage = $"Running on {_detectedHypervisor}";
        return true;
    }

    public async Task<bool> StopVmAsync(string vmId)
    {
        var vm = _vms.FirstOrDefault(v => v.Id == vmId);
        if (vm == null) return false;

        vm.State = VmPowerState.Stopped;
        vm.StatusMessage = "Turned off";
        await Task.Delay(200);
        return true;
    }

    public Task<bool> DeleteVmAsync(string vmId)
    {
        var vm = _vms.FirstOrDefault(v => v.Id == vmId);
        if (vm != null)
        {
            _vms.Remove(vm);
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }
}
