using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RswareDesign.Models;

namespace RswareDesign.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = "RswareDesign - [Drive - ECAT Homing]";

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private string _connectionStatus = "Disconnected";

    [ObservableProperty]
    private string _driveInfo = "CSD7N";

    [ObservableProperty]
    private string _modeInfo = "Offline";

    [ObservableProperty]
    private string _selectedPort = "COM3";

    [ObservableProperty]
    private string _selectedNodeName = "ECAT Homing";

    [ObservableProperty]
    private string _activePanel = "A";

    [ObservableProperty]
    private bool _isEnabled;

    [ObservableProperty]
    private bool _showHelps;

    [ObservableProperty]
    private bool _showStatus = true;

    [ObservableProperty]
    private bool _showCommands = true;

    [ObservableProperty]
    private string _actionMessage = "";

    // ═══════════════════════════════════════════════════════
    //  패널 표시/숨김 (토글)
    // ═══════════════════════════════════════════════════════
    [ObservableProperty]
    private bool _isPanelAVisible = true;

    [ObservableProperty]
    private bool _isPanelBVisible;

    [ObservableProperty]
    private bool _isPanelCVisible;

    [ObservableProperty]
    private bool _isPanelDVisible;

    // ═══════════════════════════════════════════════════════
    //  패널별 파라미터 컬렉션 + 노드 이름
    // ═══════════════════════════════════════════════════════
    public ObservableCollection<ParameterItem> PanelAParameters { get; } = new();
    public ObservableCollection<ParameterItem> PanelBParameters { get; } = new();
    public ObservableCollection<ParameterItem> PanelCParameters { get; } = new();
    public ObservableCollection<ParameterItem> PanelDParameters { get; } = new();

    private readonly Dictionary<string, string> _panelNodeNames = new()
    {
        { "A", "ECAT Homing" }, { "B", "" }, { "C", "" }, { "D", "" }
    };

    // 에러 로그 (활성 패널 기준)
    public ObservableCollection<StatusEntry> StatusEntries { get; } = new();

    // 노드별 파라미터/상태 저장소
    private readonly Dictionary<string, List<ParameterItem>> _parameterSets = new();
    private readonly Dictionary<string, List<StatusEntry>> _statusSets = new();

    // 패널 레이아웃 변경 알림
    public event Action? PanelLayoutChanged;

    public MainViewModel()
    {
        BuildAllParameterSets();
        LoadParametersForPanel("A", "ECAT Homing");
    }

    // ═══════════════════════════════════════════════════════
    //  패널 데이터 접근
    // ═══════════════════════════════════════════════════════
    public ObservableCollection<ParameterItem> GetPanelParameters(string panel) => panel switch
    {
        "B" => PanelBParameters,
        "C" => PanelCParameters,
        "D" => PanelDParameters,
        _ => PanelAParameters
    };

    public string GetPanelNodeName(string panel) =>
        _panelNodeNames.TryGetValue(panel, out var name) ? name : "";

    // ═══════════════════════════════════════════════════════
    //  트리 노드 선택 → 활성 패널에 로드
    // ═══════════════════════════════════════════════════════
    public void SelectNode(string nodeName)
    {
        SelectedNodeName = nodeName;
        Title = $"RswareDesign - [Drive - {nodeName}]";
        LoadParametersForPanel(ActivePanel, nodeName);
        RefreshStatusEntries(nodeName);
    }

    private void LoadParametersForPanel(string panel, string nodeName)
    {
        _panelNodeNames[panel] = nodeName;
        var collection = GetPanelParameters(panel);
        collection.Clear();
        if (_parameterSets.TryGetValue(nodeName, out var pList))
            foreach (var p in pList) collection.Add(p);
    }

    private void RefreshStatusEntries(string nodeName)
    {
        StatusEntries.Clear();
        if (_statusSets.TryGetValue(nodeName, out var sList))
            foreach (var s in sList) StatusEntries.Add(s);
        else
            StatusEntries.Add(new StatusEntry { Status = $"{nodeName} Status", Value = "OK", Units = "" });
    }

    // ═══════════════════════════════════════════════════════
    //  패널 표시/숨김 토글
    // ═══════════════════════════════════════════════════════
    public void TogglePanel(string panelId)
    {
        bool current = panelId switch
        {
            "A" => IsPanelAVisible,
            "B" => IsPanelBVisible,
            "C" => IsPanelCVisible,
            "D" => IsPanelDVisible,
            _ => false
        };

        // 끄려는 경우: 최소 1개 보장
        if (current && VisiblePanelCount() <= 1) return;

        switch (panelId)
        {
            case "A": IsPanelAVisible = !current; break;
            case "B": IsPanelBVisible = !current; break;
            case "C": IsPanelCVisible = !current; break;
            case "D": IsPanelDVisible = !current; break;
        }

        // 새로 켜진 패널에 데이터가 없으면 현재 노드 로드
        if (!current && string.IsNullOrEmpty(GetPanelNodeName(panelId)))
            LoadParametersForPanel(panelId, SelectedNodeName);

        // 활성 패널을 새로 켜진 패널로 변경
        if (!current)
            ActivePanel = panelId;

        PanelLayoutChanged?.Invoke();
    }

    public bool IsPanelVisible(string panelId) => panelId switch
    {
        "A" => IsPanelAVisible,
        "B" => IsPanelBVisible,
        "C" => IsPanelCVisible,
        "D" => IsPanelDVisible,
        _ => false
    };

    public int VisiblePanelCount()
    {
        int count = 0;
        if (IsPanelAVisible) count++;
        if (IsPanelBVisible) count++;
        if (IsPanelCVisible) count++;
        if (IsPanelDVisible) count++;
        return count;
    }

    // ═══════════════════════════════════════════════════════
    //  Commands
    // ═══════════════════════════════════════════════════════
    [RelayCommand]
    private void Enable()
    {
        IsEnabled = true;
        IsConnected = true;
        ConnectionStatus = "Connected";
        ModeInfo = "Enabled";
        ActionMessage = "Drive enabled";
    }

    [RelayCommand]
    private void DisableAll()
    {
        IsEnabled = false;
        ModeInfo = "Disabled";
        ActionMessage = "All drives disabled";
    }

    [RelayCommand]
    private void ClearFaultAll() => ActionMessage = "All faults cleared";

    [RelayCommand]
    private void ReadAll()
    {
        var count = GetPanelParameters(ActivePanel).Count;
        ActionMessage = $"Read All [{ActivePanel}] — {count} parameters loaded";
    }

    [RelayCommand]
    private void WriteAll()
    {
        var count = GetPanelParameters(ActivePanel).Count;
        ActionMessage = $"Write All [{ActivePanel}] — {count} parameters written";
    }

    [RelayCommand]
    private void SaveToFlash() => ActionMessage = $"Parameters [{ActivePanel}] saved to flash";

    [RelayCommand]
    private void CompareParams() => ActionMessage = "Compare panel opened";

    [RelayCommand]
    private void ExportParams() => ActionMessage = $"Parameters [{ActivePanel}] exported";

    [RelayCommand]
    private void RevertParams()
    {
        var nodeName = GetPanelNodeName(ActivePanel);
        if (!string.IsNullOrEmpty(nodeName))
            LoadParametersForPanel(ActivePanel, nodeName);
        ActionMessage = $"Parameters [{ActivePanel}] reverted to saved values";
    }

    // ═══════════════════════════════════════════════════════════
    //  각 노드별 목업 파라미터 세트 생성
    // ═══════════════════════════════════════════════════════════
    private void BuildAllParameterSets()
    {
        _parameterSets["Mode Configuration"] = new()
        {
            new() { FtNumber = "Ft-0.00", Name = "Control Mode", Value = "0", Unit = "", Default = "0", Min = "0", Max = "4" },
            new() { FtNumber = "Ft-0.01", Name = "Speed/Torque Selection", Value = "0", Unit = "", Default = "0", Min = "0", Max = "1" },
            new() { FtNumber = "Ft-0.02", Name = "Command Source", Value = "0", Unit = "", Default = "0", Min = "0", Max = "3" },
            new() { FtNumber = "Ft-0.03", Name = "Rotation Direction", Value = "0", Unit = "", Default = "0", Min = "0", Max = "1" },
            new() { FtNumber = "Ft-0.04", Name = "Analog Input Mode", Value = "0", Unit = "", Default = "0", Min = "0", Max = "2" },
        };
        _statusSets["Mode Configuration"] = new()
        {
            new() { Status = "Current Mode", Value = "Position", Units = "" },
            new() { Status = "Mode Error", Value = "No Error", Units = "" },
        };

        _parameterSets["Motor"] = new()
        {
            new() { FtNumber = "Ft-1.00", Name = "Motor Type", Value = "0", Unit = "", Default = "0", Min = "0", Max = "10" },
            new() { FtNumber = "Ft-1.01", Name = "Rated Current", Value = "3000", Unit = "mA", Default = "3000", Min = "0", Max = "50000" },
            new() { FtNumber = "Ft-1.02", Name = "Rated Speed", Value = "3000", Unit = "rpm", Default = "3000", Min = "0", Max = "10000" },
            new() { FtNumber = "Ft-1.03", Name = "Rated Torque", Value = "1270", Unit = "mN·m", Default = "1270", Min = "0", Max = "99999" },
            new() { FtNumber = "Ft-1.04", Name = "Encoder Resolution", Value = "131072", Unit = "pulse/rev", Default = "131072", Min = "1", Max = "8388608" },
            new() { FtNumber = "Ft-1.05", Name = "Number of Poles", Value = "10", Unit = "", Default = "10", Min = "2", Max = "100" },
            new() { FtNumber = "Ft-1.06", Name = "Rotor Inertia", Value = "0", Unit = "kg·cm²", Default = "0", Min = "0", Max = "99999" },
        };
        _statusSets["Motor"] = new()
        {
            new() { Status = "Motor Temperature", Value = "42", Units = "°C" },
            new() { Status = "Motor Status", Value = "Ready", Units = "" },
        };

        _parameterSets["PID Tuning"] = new()
        {
            new() { FtNumber = "Ft-2.00", Name = "Position P Gain", Value = "40", Unit = "Hz", Default = "40", Min = "1", Max = "5000" },
            new() { FtNumber = "Ft-2.01", Name = "Position I Gain", Value = "0", Unit = "ms", Default = "0", Min = "0", Max = "10000" },
            new() { FtNumber = "Ft-2.02", Name = "Velocity P Gain", Value = "300", Unit = "Hz", Default = "300", Min = "1", Max = "50000" },
            new() { FtNumber = "Ft-2.03", Name = "Velocity I Gain", Value = "30", Unit = "ms", Default = "30", Min = "0", Max = "10000" },
            new() { FtNumber = "Ft-2.04", Name = "Velocity Feed Forward", Value = "0", Unit = "%", Default = "0", Min = "0", Max = "100" },
            new() { FtNumber = "Ft-2.05", Name = "Torque Filter", Value = "500", Unit = "Hz", Default = "500", Min = "10", Max = "5000" },
        };
        _statusSets["PID Tuning"] = new()
        {
            new() { Status = "Position Error", Value = "0", Units = "pulse" },
            new() { Status = "Velocity Error", Value = "0", Units = "rpm" },
        };

        _parameterSets["Tuningless"] = new()
        {
            new() { FtNumber = "Ft-2.20", Name = "Tuningless Mode", Value = "1", Unit = "", Default = "1", Min = "0", Max = "2" },
            new() { FtNumber = "Ft-2.21", Name = "Machine Rigidity", Value = "5", Unit = "", Default = "5", Min = "1", Max = "15" },
            new() { FtNumber = "Ft-2.22", Name = "Inertia Ratio", Value = "100", Unit = "%", Default = "100", Min = "1", Max = "10000" },
        };

        _parameterSets["Resonant Suppression"] = new()
        {
            new() { FtNumber = "Ft-2.30", Name = "Notch Filter 1 Freq", Value = "5000", Unit = "Hz", Default = "5000", Min = "50", Max = "5000" },
            new() { FtNumber = "Ft-2.31", Name = "Notch Filter 1 Width", Value = "2", Unit = "", Default = "2", Min = "0", Max = "20" },
            new() { FtNumber = "Ft-2.32", Name = "Notch Filter 1 Depth", Value = "0", Unit = "dB", Default = "0", Min = "0", Max = "99" },
            new() { FtNumber = "Ft-2.33", Name = "Notch Filter 2 Freq", Value = "5000", Unit = "Hz", Default = "5000", Min = "50", Max = "5000" },
            new() { FtNumber = "Ft-2.34", Name = "Notch Filter 2 Width", Value = "2", Unit = "", Default = "2", Min = "0", Max = "20" },
        };

        _parameterSets["Vibration Suppression"] = new()
        {
            new() { FtNumber = "Ft-2.40", Name = "Vibration Supp. 1", Value = "0", Unit = "", Default = "0", Min = "0", Max = "1" },
            new() { FtNumber = "Ft-2.41", Name = "VS1 Frequency", Value = "100", Unit = "Hz", Default = "100", Min = "1", Max = "1000" },
            new() { FtNumber = "Ft-2.42", Name = "Vibration Supp. 2", Value = "0", Unit = "", Default = "0", Min = "0", Max = "1" },
            new() { FtNumber = "Ft-2.43", Name = "VS2 Frequency", Value = "100", Unit = "Hz", Default = "100", Min = "1", Max = "1000" },
        };

        _parameterSets["Encoders"] = new()
        {
            new() { FtNumber = "Ft-3.00", Name = "Encoder Type", Value = "0", Unit = "", Default = "0", Min = "0", Max = "5" },
            new() { FtNumber = "Ft-3.01", Name = "Encoder Resolution", Value = "131072", Unit = "pulse/rev", Default = "131072", Min = "1", Max = "8388608" },
            new() { FtNumber = "Ft-3.02", Name = "Encoder Direction", Value = "0", Unit = "", Default = "0", Min = "0", Max = "1" },
        };

        _parameterSets["Digital Inputs"] = new()
        {
            new() { FtNumber = "Ft-4.00", Name = "DI1 Function", Value = "0", Unit = "", Default = "0", Min = "0", Max = "50" },
            new() { FtNumber = "Ft-4.01", Name = "DI2 Function", Value = "0", Unit = "", Default = "0", Min = "0", Max = "50" },
            new() { FtNumber = "Ft-4.02", Name = "DI3 Function", Value = "0", Unit = "", Default = "0", Min = "0", Max = "50" },
            new() { FtNumber = "Ft-4.03", Name = "DI4 Function", Value = "0", Unit = "", Default = "0", Min = "0", Max = "50" },
            new() { FtNumber = "Ft-4.04", Name = "DI5 Function", Value = "0", Unit = "", Default = "0", Min = "0", Max = "50" },
            new() { FtNumber = "Ft-4.10", Name = "DI Active Level", Value = "0", Unit = "", Default = "0", Min = "0", Max = "31" },
        };
        _statusSets["Digital Inputs"] = new()
        {
            new() { Status = "DI1 State", Value = "OFF", Units = "" },
            new() { Status = "DI2 State", Value = "OFF", Units = "" },
            new() { Status = "DI3 State", Value = "OFF", Units = "" },
        };

        _parameterSets["Digital Outputs"] = new()
        {
            new() { FtNumber = "Ft-5.00", Name = "DO1 Function", Value = "0", Unit = "", Default = "0", Min = "0", Max = "50" },
            new() { FtNumber = "Ft-5.01", Name = "DO2 Function", Value = "0", Unit = "", Default = "0", Min = "0", Max = "50" },
            new() { FtNumber = "Ft-5.02", Name = "DO3 Function", Value = "0", Unit = "", Default = "0", Min = "0", Max = "50" },
            new() { FtNumber = "Ft-5.10", Name = "DO Active Level", Value = "0", Unit = "", Default = "0", Min = "0", Max = "7" },
        };
        _statusSets["Digital Outputs"] = new()
        {
            new() { Status = "DO1 State", Value = "OFF", Units = "" },
            new() { Status = "DO2 State", Value = "OFF", Units = "" },
        };

        _parameterSets["Analog Outputs"] = new()
        {
            new() { FtNumber = "Ft-6.00", Name = "AO1 Function", Value = "0", Unit = "", Default = "0", Min = "0", Max = "20" },
            new() { FtNumber = "Ft-6.01", Name = "AO1 Scale", Value = "100", Unit = "%", Default = "100", Min = "0", Max = "500" },
            new() { FtNumber = "Ft-6.02", Name = "AO1 Offset", Value = "0", Unit = "mV", Default = "0", Min = "-5000", Max = "5000" },
        };

        _parameterSets["ECAT Homing"] = new()
        {
            new() { FtNumber = "Ft-S.14", Name = "Abs Origin Offset", Value = "0", Unit = "pulse", Default = "0", Min = "-2147483647", Max = "2147483647" },
            new() { FtNumber = "Ft-S.15", Name = "Homing Method", Value = "0", Unit = "", Default = "0", Min = "-128", Max = "127" },
            new() { FtNumber = "Ft-S.16", Name = "Homing Time Out", Value = "0", Unit = "sec", Default = "0", Min = "0", Max = "500" },
            new() { FtNumber = "Ft-S.17", Name = "Homing Offset", Value = "0", Unit = "pulse", Default = "0", Min = "-2147483647", Max = "2147483647" },
            new() { FtNumber = "Ft-S.18", Name = "Homing Speed 1", Value = "0", Unit = "pulse/sec", Default = "0", Min = "0", Max = "2147483647" },
            new() { FtNumber = "Ft-S.19", Name = "Homing Speed 2", Value = "0", Unit = "pulse/sec", Default = "0", Min = "0", Max = "2147483647" },
            new() { FtNumber = "Ft-S.20", Name = "Homing Accel", Value = "0", Unit = "pulse/sec²", Default = "0", Min = "0", Max = "2147483647" },
            new() { FtNumber = "Ft-0.06", Name = "Abs Homing Done", Value = "0", Unit = "", Default = "0", Min = "0", Max = "1" },
        };
        _statusSets["ECAT Homing"] = new()
        {
            new() { Status = "ECAT Homing Status", Value = "0:IDLE", Units = "" },
            new() { Status = "ECAT Homing Error", Value = "No Error", Units = "" },
        };

        _parameterSets["Monitor"] = new()
        {
            new() { FtNumber = "MON-0.00", Name = "Position Command", Value = "0", Unit = "pulse", Default = "", Min = "", Max = "" },
            new() { FtNumber = "MON-0.01", Name = "Position Feedback", Value = "0", Unit = "pulse", Default = "", Min = "", Max = "" },
            new() { FtNumber = "MON-0.02", Name = "Position Error", Value = "0", Unit = "pulse", Default = "", Min = "", Max = "" },
            new() { FtNumber = "MON-0.03", Name = "Velocity Command", Value = "0", Unit = "rpm", Default = "", Min = "", Max = "" },
            new() { FtNumber = "MON-0.04", Name = "Velocity Feedback", Value = "0", Unit = "rpm", Default = "", Min = "", Max = "" },
            new() { FtNumber = "MON-0.05", Name = "Torque Command", Value = "0", Unit = "%", Default = "", Min = "", Max = "" },
            new() { FtNumber = "MON-0.06", Name = "Current (q-axis)", Value = "0", Unit = "mA", Default = "", Min = "", Max = "" },
            new() { FtNumber = "MON-0.07", Name = "DC Bus Voltage", Value = "310", Unit = "V", Default = "", Min = "", Max = "" },
        };
        _statusSets["Monitor"] = new()
        {
            new() { Status = "Motor Speed", Value = "0", Units = "rpm" },
            new() { Status = "Bus Voltage", Value = "310", Units = "V" },
            new() { Status = "Motor Temperature", Value = "38", Units = "°C" },
        };

        _parameterSets["Oscilloscope"] = new()
        {
            new() { FtNumber = "OSC-0.00", Name = "Trigger Source", Value = "0", Unit = "", Default = "0", Min = "0", Max = "7" },
            new() { FtNumber = "OSC-0.01", Name = "Trigger Level", Value = "0", Unit = "", Default = "0", Min = "-32768", Max = "32767" },
            new() { FtNumber = "OSC-0.02", Name = "Sample Rate", Value = "1", Unit = "ms", Default = "1", Min = "1", Max = "100" },
            new() { FtNumber = "OSC-0.03", Name = "CH1 Source", Value = "0", Unit = "", Default = "0", Min = "0", Max = "30" },
            new() { FtNumber = "OSC-0.04", Name = "CH2 Source", Value = "1", Unit = "", Default = "1", Min = "0", Max = "30" },
        };

        _parameterSets["Faults"] = new()
        {
            new() { FtNumber = "FLT-0.00", Name = "Fault History 1", Value = "0", Unit = "", Default = "", Min = "", Max = "" },
            new() { FtNumber = "FLT-0.01", Name = "Fault History 2", Value = "0", Unit = "", Default = "", Min = "", Max = "" },
            new() { FtNumber = "FLT-0.02", Name = "Fault History 3", Value = "0", Unit = "", Default = "", Min = "", Max = "" },
            new() { FtNumber = "FLT-0.03", Name = "Warning History 1", Value = "0", Unit = "", Default = "", Min = "", Max = "" },
            new() { FtNumber = "FLT-0.04", Name = "Warning History 2", Value = "0", Unit = "", Default = "", Min = "", Max = "" },
        };
        _statusSets["Faults"] = new()
        {
            new() { Status = "Active Fault", Value = "None", Units = "" },
            new() { Status = "Active Warning", Value = "None", Units = "" },
        };

        _parameterSets["Fully Closed System"] = new()
        {
            new() { FtNumber = "Ft-7.00", Name = "Fully Closed Mode", Value = "0", Unit = "", Default = "0", Min = "0", Max = "1" },
            new() { FtNumber = "Ft-7.01", Name = "Ext Encoder Res", Value = "10000", Unit = "pulse/rev", Default = "10000", Min = "1", Max = "8388608" },
            new() { FtNumber = "Ft-7.02", Name = "Ext Encoder Dir", Value = "0", Unit = "", Default = "0", Min = "0", Max = "1" },
        };

        _parameterSets["ServiceInfo"] = new()
        {
            new() { FtNumber = "SVC-0.00", Name = "Firmware Version", Value = "2.14", Unit = "", Default = "", Min = "", Max = "" },
            new() { FtNumber = "SVC-0.01", Name = "Serial Number", Value = "CSD7N-A01234", Unit = "", Default = "", Min = "", Max = "" },
            new() { FtNumber = "SVC-0.02", Name = "Operating Hours", Value = "1024", Unit = "h", Default = "", Min = "", Max = "" },
            new() { FtNumber = "SVC-0.03", Name = "Power-On Count", Value = "342", Unit = "", Default = "", Min = "", Max = "" },
        };

        _parameterSets["Control Panel"] = new()
        {
            new() { FtNumber = "CP-0.00", Name = "Jog Speed", Value = "100", Unit = "rpm", Default = "100", Min = "0", Max = "6000" },
            new() { FtNumber = "CP-0.01", Name = "Jog Acceleration", Value = "1000", Unit = "ms", Default = "1000", Min = "0", Max = "60000" },
            new() { FtNumber = "CP-0.02", Name = "Jog Deceleration", Value = "1000", Unit = "ms", Default = "1000", Min = "0", Max = "60000" },
        };

        _parameterSets["Group 0 : Basic"] = new()
        {
            new() { FtNumber = "Ft-0.00", Name = "Control Mode", Value = "0", Unit = "", Default = "0", Min = "0", Max = "4" },
            new() { FtNumber = "Ft-0.01", Name = "Speed/Torque Sel", Value = "0", Unit = "", Default = "0", Min = "0", Max = "1" },
            new() { FtNumber = "Ft-0.02", Name = "Command Source", Value = "0", Unit = "", Default = "0", Min = "0", Max = "3" },
        };
        _parameterSets["Group 1 : Gain"] = new()
        {
            new() { FtNumber = "Ft-2.00", Name = "Position P Gain", Value = "40", Unit = "Hz", Default = "40", Min = "1", Max = "5000" },
            new() { FtNumber = "Ft-2.02", Name = "Velocity P Gain", Value = "300", Unit = "Hz", Default = "300", Min = "1", Max = "50000" },
            new() { FtNumber = "Ft-2.03", Name = "Velocity I Gain", Value = "30", Unit = "ms", Default = "30", Min = "0", Max = "10000" },
        };
        _parameterSets["Group 2 : Velocity"] = new()
        {
            new() { FtNumber = "Ft-8.00", Name = "Speed Limit", Value = "3000", Unit = "rpm", Default = "3000", Min = "0", Max = "10000" },
            new() { FtNumber = "Ft-8.01", Name = "Accel Time", Value = "500", Unit = "ms", Default = "500", Min = "0", Max = "60000" },
            new() { FtNumber = "Ft-8.02", Name = "Decel Time", Value = "500", Unit = "ms", Default = "500", Min = "0", Max = "60000" },
        };
        _parameterSets["Group 3 : Position"] = new()
        {
            new() { FtNumber = "Ft-9.00", Name = "Pos Cmd Source", Value = "0", Unit = "", Default = "0", Min = "0", Max = "3" },
            new() { FtNumber = "Ft-9.01", Name = "E-Gear Numerator", Value = "1", Unit = "", Default = "1", Min = "1", Max = "32767" },
            new() { FtNumber = "Ft-9.02", Name = "E-Gear Denominator", Value = "1", Unit = "", Default = "1", Min = "1", Max = "32767" },
        };
        _parameterSets["Group 4 : Current"] = new()
        {
            new() { FtNumber = "Ft-A.00", Name = "Torque Limit (+)", Value = "300", Unit = "%", Default = "300", Min = "0", Max = "500" },
            new() { FtNumber = "Ft-A.01", Name = "Torque Limit (-)", Value = "300", Unit = "%", Default = "300", Min = "0", Max = "500" },
            new() { FtNumber = "Ft-A.02", Name = "Current Loop BW", Value = "2000", Unit = "Hz", Default = "2000", Min = "100", Max = "5000" },
        };
        _parameterSets["Group 5 : Auxiliary"] = new()
        {
            new() { FtNumber = "Ft-B.00", Name = "Comm Address", Value = "1", Unit = "", Default = "1", Min = "1", Max = "31" },
            new() { FtNumber = "Ft-B.01", Name = "Baud Rate", Value = "3", Unit = "", Default = "3", Min = "0", Max = "5" },
            new() { FtNumber = "Ft-B.02", Name = "Comm Protocol", Value = "0", Unit = "", Default = "0", Min = "0", Max = "2" },
        };
    }
}
