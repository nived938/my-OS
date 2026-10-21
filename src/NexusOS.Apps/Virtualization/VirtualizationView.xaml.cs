using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using NexusOS.Core.Virtualization;

namespace NexusOS.Apps.Virtualization;

public partial class VirtualizationView : UserControl
{
    private readonly IVirtualizationService _virtService;
    private List<VirtualMachineSpec> _vms = new();
    private VirtualMachineSpec? _selectedVm;

    public VirtualizationView(IVirtualizationService virtService)
    {
        InitializeComponent();
        _virtService = virtService;

        HypervisorStatusText.Text = _virtService.HypervisorDiagnostics;
        Loaded += VirtualizationView_Loaded;
    }

    private async void VirtualizationView_Loaded(object sender, RoutedEventArgs e)
    {
        await RefreshVmListAsync();
    }

    private async Task RefreshVmListAsync()
    {
        _vms = await _virtService.GetVirtualMachinesAsync();
        VmListBox.Items.Clear();

        foreach (var vm in _vms)
        {
            var item = new ListBoxItem
            {
                Content = $"{vm.Name} ({vm.State})",
                Tag = vm
            };
            VmListBox.Items.Add(item);
        }

        if (VmListBox.Items.Count > 0)
        {
            VmListBox.SelectedIndex = 0;
        }
    }

    private void VmListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (VmListBox.SelectedItem is ListBoxItem item && item.Tag is VirtualMachineSpec vm)
        {
            _selectedVm = vm;
            UpdateDetails(vm);
        }
    }

    private void UpdateDetails(VirtualMachineSpec vm)
    {
        VmNameText.Text = vm.Name;
        VmOsText.Text = vm.OperatingSystem;
        VmRamText.Text = $"{vm.MemoryMb} MB";
        VmCoresText.Text = $"{vm.CpuCores} Core(s)";
        VmDiagText.Text = vm.StatusMessage;

        VmStatusBadge.Text = $"Status: {vm.State}";
        VmStatusBadge.Foreground = vm.State switch
        {
            VmPowerState.Running => TryFindResource("NexusSuccessBrush") as Brush ?? Brushes.LightGreen,
            VmPowerState.Starting => TryFindResource("NexusAccentBrush") as Brush ?? Brushes.DodgerBlue,
            _ => TryFindResource("NexusWarningBrush") as Brush ?? Brushes.Orange
        };

        StartVmButton.IsEnabled = vm.State == VmPowerState.Stopped;
        StopVmButton.IsEnabled = vm.State == VmPowerState.Running;
    }

    private async void StartVm_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedVm == null) return;

        StartVmButton.IsEnabled = false;
        var success = await _virtService.StartVmAsync(_selectedVm.Id);
        UpdateDetails(_selectedVm);
        await RefreshVmListAsync();

        if (!success)
        {
            MessageBox.Show(_selectedVm.StatusMessage, "Virtualization Engine", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void StopVm_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedVm == null) return;

        StopVmButton.IsEnabled = false;
        await _virtService.StopVmAsync(_selectedVm.Id);
        UpdateDetails(_selectedVm);
        await RefreshVmListAsync();
    }

    private async void DeleteVm_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedVm == null) return;

        var confirm = MessageBox.Show($"Delete virtual machine '{_selectedVm.Name}'?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirm == MessageBoxResult.Yes)
        {
            await _virtService.DeleteVmAsync(_selectedVm.Id);
            await RefreshVmListAsync();
        }
    }

    private async void NewVm_Click(object sender, RoutedEventArgs e)
    {
        var vm = await _virtService.CreateVirtualMachineAsync("New Virtual Sandbox", "Linux x86_64", 2048, 2, "");
        await RefreshVmListAsync();
        MessageBox.Show($"Configured new VM: {vm.Name}", "Virtual Machine Wizard", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
