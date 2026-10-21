namespace NexusOS.Core.Virtualization;

public enum HypervisorProvider
{
    None,
    HyperV,
    Qemu,
    VirtualBox
}

public enum VmPowerState
{
    Stopped,
    Starting,
    Running,
    Paused,
    Error
}

public class VirtualMachineSpec
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "Nexus Guest VM";
    public string OperatingSystem { get; set; } = "Linux / Windows";
    public int MemoryMb { get; set; } = 4096;
    public int CpuCores { get; set; } = 2;
    public string VirtualDiskPath { get; set; } = string.Empty;
    public string IsoImagePath { get; set; } = string.Empty;
    public VmPowerState State { get; set; } = VmPowerState.Stopped;
    public HypervisorProvider Hypervisor { get; set; } = HypervisorProvider.None;
    public string StatusMessage { get; set; } = "Ready";
}

public interface IVirtualizationService
{
    bool IsHypervisorAvailable { get; }
    HypervisorProvider DetectedHypervisor { get; }
    string HypervisorDiagnostics { get; }

    Task<List<VirtualMachineSpec>> GetVirtualMachinesAsync();
    Task<VirtualMachineSpec> CreateVirtualMachineAsync(string name, string osType, int ramMb, int cores, string isoPath);
    Task<bool> StartVmAsync(string vmId);
    Task<bool> StopVmAsync(string vmId);
    Task<bool> DeleteVmAsync(string vmId);
}
