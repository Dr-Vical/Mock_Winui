using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using RswareDesign.Controls;
using RswareDesign.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using WinRT;
using WinRT.Interop;

namespace RswareDesign;

public sealed partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; } = new();

    private DesktopAcrylicController? _acrylicController;
    private SystemBackdropConfiguration? _backdropConfig;

    // 패널 관리
    private readonly Dictionary<string, CompareParameterPanel> _panels = new();
    private readonly Dictionary<string, SolidColorBrush> _panelAccents = new();

    public MainWindow()
    {
        this.InitializeComponent();

        this.Title = "RswareDesign";
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(TitleBarGrid);
        SetWindowSize(1920, 1080);

        TrySetTransparentBackdrop();

        RootGrid.Loaded += (_, _) =>
        {
            RootGrid.RequestedTheme = ElementTheme.Dark;
            InitPanelAccents();
            RebuildPanelLayout();
        };

        BuildDriveTree();

        ViewModel.PanelLayoutChanged += () =>
            DispatcherQueue.TryEnqueue(RebuildPanelLayout);

        ViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ViewModel.IsConnected))
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    StatusDot.Fill = ViewModel.IsConnected
                        ? (SolidColorBrush)Application.Current.Resources["SuccessBrush"]
                        : (SolidColorBrush)Application.Current.Resources["ErrorBrush"];
                });
            }
        };
    }

    // ═══════════════════════════════════════════════════════
    //  패널 액센트 색상 초기화
    // ═══════════════════════════════════════════════════════
    private void InitPanelAccents()
    {
        _panelAccents["A"] = (SolidColorBrush)Application.Current.Resources["PanelAAccent"];
        _panelAccents["B"] = (SolidColorBrush)Application.Current.Resources["PanelBAccent"];
        _panelAccents["C"] = (SolidColorBrush)Application.Current.Resources["PanelCAccent"];
        _panelAccents["D"] = (SolidColorBrush)Application.Current.Resources["PanelDAccent"];
    }

    // ═══════════════════════════════════════════════════════
    //  동적 패널 레이아웃 (1개=전체, 2개=50/50, 3~4개=2x2)
    // ═══════════════════════════════════════════════════════
    private void RebuildPanelLayout()
    {
        CenterPanelGrid.Children.Clear();
        CenterPanelGrid.RowDefinitions.Clear();
        CenterPanelGrid.ColumnDefinitions.Clear();

        string[] allPanels = { "A", "B", "C", "D" };
        var visible = allPanels.Where(p => ViewModel.IsPanelVisible(p)).ToList();

        // 보이지 않는 패널 제거
        foreach (var key in _panels.Keys.Where(k => !visible.Contains(k)).ToList())
            _panels.Remove(key);

        // 패널 생성/업데이트
        foreach (var id in visible)
        {
            if (!_panels.ContainsKey(id))
            {
                var panel = new CompareParameterPanel();
                panel.Configure(id, $"Panel {id}", _panelAccents[id],
                    ViewModel.GetPanelParameters(id), ViewModel.GetPanelNodeName(id));
                panel.CloseRequested += (_, panelId) => ViewModel.TogglePanel(panelId);
                _panels[id] = panel;
            }
            else
            {
                _panels[id].UpdateNodeName(ViewModel.GetPanelNodeName(id));
            }
        }

        // 그리드 레이아웃 구성
        int count = visible.Count;
        if (count == 1)
        {
            CenterPanelGrid.Children.Add(_panels[visible[0]]);
        }
        else if (count == 2)
        {
            CenterPanelGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            CenterPanelGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var p0 = _panels[visible[0]];
            p0.Margin = new Thickness(0, 0, 2, 0);
            Grid.SetColumn(p0, 0);
            CenterPanelGrid.Children.Add(p0);

            var p1 = _panels[visible[1]];
            p1.Margin = new Thickness(2, 0, 0, 0);
            Grid.SetColumn(p1, 1);
            CenterPanelGrid.Children.Add(p1);
        }
        else // 3 or 4
        {
            CenterPanelGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            CenterPanelGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            CenterPanelGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            CenterPanelGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            int[] cols = { 0, 1, 0, 1 };
            int[] rows = { 0, 0, 1, 1 };
            Thickness[] margins =
            {
                new(0, 0, 2, 2), new(2, 0, 0, 2),
                new(0, 2, 2, 0), new(2, 2, 0, 0)
            };

            for (int i = 0; i < visible.Count; i++)
            {
                var p = _panels[visible[i]];
                p.Margin = margins[i];
                Grid.SetColumn(p, cols[i]);
                Grid.SetRow(p, rows[i]);
                CenterPanelGrid.Children.Add(p);
            }
        }

        // A/B/C/D 버튼 비주얼 업데이트
        UpdatePanelButtons();
    }

    // ═══════════════════════════════════════════════════════
    //  A/B/C/D 버튼 토글 비주얼
    // ═══════════════════════════════════════════════════════
    private void UpdatePanelButtons()
    {
        Button[] tabs = { PanelABtn, PanelBBtn, PanelCBtn, PanelDBtn };
        string[] ids = { "A", "B", "C", "D" };

        for (int i = 0; i < tabs.Length; i++)
        {
            if (ViewModel.IsPanelVisible(ids[i]))
            {
                tabs[i].Background = _panelAccents.GetValueOrDefault(ids[i],
                    new SolidColorBrush(Colors.Gray));
                tabs[i].Foreground = new SolidColorBrush(Colors.White);
            }
            else
            {
                tabs[i].Background = new SolidColorBrush(Colors.Transparent);
                tabs[i].Foreground = new SolidColorBrush(
                    Microsoft.UI.ColorHelper.FromArgb(0x99, 0xFF, 0xFF, 0xFF));
            }
        }
    }

    // ═══════════════════════════════════════════════════════
    //  Backdrop
    // ═══════════════════════════════════════════════════════
    private void TrySetTransparentBackdrop()
    {
        if (!DesktopAcrylicController.IsSupported()) return;

        _backdropConfig = new SystemBackdropConfiguration
        {
            IsInputActive = true
        };

        ((FrameworkElement)this.Content).ActualThemeChanged += (s, _) =>
        {
            if (_backdropConfig != null)
                _backdropConfig.Theme = s.ActualTheme switch
                {
                    ElementTheme.Dark => SystemBackdropTheme.Dark,
                    ElementTheme.Light => SystemBackdropTheme.Light,
                    _ => SystemBackdropTheme.Default
                };
        };

        _acrylicController = new DesktopAcrylicController
        {
            TintColor = Windows.UI.Color.FromArgb(255, 10, 10, 18),
            TintOpacity = 0.15f,
            LuminosityOpacity = 0.0f,
            FallbackColor = Windows.UI.Color.FromArgb(40, 10, 10, 18)
        };

        _acrylicController.AddSystemBackdropTarget(
            this.As<ICompositionSupportsSystemBackdrop>());
        _acrylicController.SetSystemBackdropConfiguration(_backdropConfig);
    }

    private void SetWindowSize(int width, int height)
    {
        var hWnd = WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);
        appWindow.Resize(new Windows.Graphics.SizeInt32(width, height));
    }

    // ═══════════════════════════════════════════════════════
    //  Drive Tree
    // ═══════════════════════════════════════════════════════
    private void BuildDriveTree()
    {
        var onlineRoot = new TreeViewNode { Content = "On Line Drives", IsExpanded = true };
        var drive = new TreeViewNode { Content = "Drive", IsExpanded = true };

        drive.Children.Add(new TreeViewNode { Content = "Mode Configuration" });
        drive.Children.Add(new TreeViewNode { Content = "Motor" });

        var pidTuning = new TreeViewNode { Content = "PID Tuning", IsExpanded = true };
        pidTuning.Children.Add(new TreeViewNode { Content = "Tuningless" });
        pidTuning.Children.Add(new TreeViewNode { Content = "Resonant Suppression" });
        pidTuning.Children.Add(new TreeViewNode { Content = "Vibration Suppression" });
        pidTuning.Children.Add(new TreeViewNode { Content = "Encoders" });
        drive.Children.Add(pidTuning);

        drive.Children.Add(new TreeViewNode { Content = "Digital Inputs" });
        drive.Children.Add(new TreeViewNode { Content = "Digital Outputs" });
        drive.Children.Add(new TreeViewNode { Content = "Analog Outputs" });
        drive.Children.Add(new TreeViewNode { Content = "ECAT Homing" });
        drive.Children.Add(new TreeViewNode { Content = "Monitor" });
        drive.Children.Add(new TreeViewNode { Content = "Oscilloscope" });
        drive.Children.Add(new TreeViewNode { Content = "Faults" });
        drive.Children.Add(new TreeViewNode { Content = "Fully Closed System" });
        drive.Children.Add(new TreeViewNode { Content = "ServiceInfo" });
        drive.Children.Add(new TreeViewNode { Content = "Control Panel" });

        onlineRoot.Children.Add(drive);
        DriveTree.RootNodes.Add(onlineRoot);

        var offlineRoot = new TreeViewNode { Content = "Off Line : Unsaved", IsExpanded = true };
        var group = new TreeViewNode { Content = "Group", IsExpanded = true };
        group.Children.Add(new TreeViewNode { Content = "Group 0 : Basic" });
        group.Children.Add(new TreeViewNode { Content = "Group 1 : Gain" });
        group.Children.Add(new TreeViewNode { Content = "Group 2 : Velocity" });
        group.Children.Add(new TreeViewNode { Content = "Group 3 : Position" });
        group.Children.Add(new TreeViewNode { Content = "Group 4 : Current" });
        group.Children.Add(new TreeViewNode { Content = "Group 5 : Auxiliary" });
        offlineRoot.Children.Add(group);
        DriveTree.RootNodes.Add(offlineRoot);
    }

    // ═══════════════════════════════════════════════════════
    //  Tree 선택 → 활성 패널에 로드
    // ═══════════════════════════════════════════════════════
    private void OnTreeItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
    {
        if (args.InvokedItem is TreeViewNode node && node.Content is string name)
        {
            ViewModel.SelectNode(name);
            // 활성 패널의 노드명 업데이트
            if (_panels.TryGetValue(ViewModel.ActivePanel, out var panel))
                panel.UpdateNodeName(name);
        }
    }

    // ═══════════════════════════════════════════════════════
    //  A/B/C/D 패널 토글
    // ═══════════════════════════════════════════════════════
    private void OnPanelTabClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button clicked || clicked.Tag is not string tag) return;

        // 단순 토글: 보이면 닫고, 안 보이면 열기
        ViewModel.TogglePanel(tag);
    }

    // ═══════════════════════════════════════════════════════
    //  Quick Actions
    // ═══════════════════════════════════════════════════════
    private void OnEnableClick(object sender, RoutedEventArgs e) => ViewModel.EnableCommand.Execute(null);
    private void OnDisableAllClick(object sender, RoutedEventArgs e) => ViewModel.DisableAllCommand.Execute(null);
    private void OnClearFaultAllClick(object sender, RoutedEventArgs e) => ViewModel.ClearFaultAllCommand.Execute(null);

    // ═══════════════════════════════════════════════════════
    //  Action Buttons
    // ═══════════════════════════════════════════════════════
    private void OnReadAllClick(object sender, RoutedEventArgs e) => ViewModel.ReadAllCommand.Execute(null);
    private void OnWriteAllClick(object sender, RoutedEventArgs e) => ViewModel.WriteAllCommand.Execute(null);
    private void OnSaveToFlashClick(object sender, RoutedEventArgs e) => ViewModel.SaveToFlashCommand.Execute(null);
    private void OnCompareClick(object sender, RoutedEventArgs e) => ViewModel.CompareParamsCommand.Execute(null);
    private void OnExportClick(object sender, RoutedEventArgs e) => ViewModel.ExportParamsCommand.Execute(null);
    private void OnRevertClick(object sender, RoutedEventArgs e) => ViewModel.RevertParamsCommand.Execute(null);

    // ═══════════════════════════════════════════════════════
    //  Theme
    // ═══════════════════════════════════════════════════════
    private void OnThemeClick(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuFlyoutItem item || item.Tag is not string tag) return;

        var theme = tag switch
        {
            "Dark" => ElementTheme.Dark,
            "Light" => ElementTheme.Light,
            _ => ElementTheme.Default
        };
        RootGrid.RequestedTheme = theme;

        if (_acrylicController != null)
        {
            _acrylicController.TintColor = theme == ElementTheme.Light
                ? Windows.UI.Color.FromArgb(255, 240, 240, 245)
                : Windows.UI.Color.FromArgb(255, 10, 10, 18);
        }
    }

    // ═══════════════════════════════════════════════════════
    //  투명도 슬라이더
    // ═══════════════════════════════════════════════════════
    private void OnOpacitySliderChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (OpacityOverlay != null)
            OpacityOverlay.Opacity = e.NewValue / 100.0 * 0.92;
        if (OpacityLabel != null)
            OpacityLabel.Text = $"{(int)e.NewValue}%";
    }

    private void OnExitClick(object sender, RoutedEventArgs e) => this.Close();

    private async void OnAboutClick(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "About RswareDesign",
            Content = "RswareDesign v1.2.1\nServo Drive Parameter Configuration Tool\n\n(c) Rsware",
            CloseButtonText = "OK",
            XamlRoot = RootGrid.XamlRoot
        };
        await dialog.ShowAsync();
    }
}
