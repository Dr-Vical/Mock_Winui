using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using RswareDesign.ViewModels;
using System;
using System.Runtime.InteropServices;
using WinRT;
using WinRT.Interop;

namespace RswareDesign;

public sealed partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; } = new();

    private DesktopAcrylicController? _acrylicController;
    private SystemBackdropConfiguration? _backdropConfig;

    public MainWindow()
    {
        this.InitializeComponent();

        this.Title = "RswareDesign";
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(TitleBarGrid);
        SetWindowSize(1920, 1080);

        // 투명 배경 적용
        TrySetTransparentBackdrop();

        RootGrid.Loaded += (_, _) => RootGrid.RequestedTheme = ElementTheme.Dark;

        BuildDriveTree();

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

    /// <summary>
    /// DesktopAcrylicController 직접 설정 — Tint/Luminosity를 0에 가깝게 하여
    /// 창 뒤의 바탕화면/다른 창이 비치는 투명 효과 구현
    /// </summary>
    private void TrySetTransparentBackdrop()
    {
        if (!DesktopAcrylicController.IsSupported()) return;

        _backdropConfig = new SystemBackdropConfiguration();

        // 테마 변경 추적
        this.Activated += (s, args) =>
        {
            if (_backdropConfig != null)
            {
                _backdropConfig.IsInputActive = args.WindowActivationState != WindowActivationState.Deactivated;
            }
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
            // 핵심: Tint와 Luminosity를 극도로 낮춰 투명하게
            TintColor = Windows.UI.Color.FromArgb(255, 10, 10, 18),  // 아주 약한 다크 틴트
            TintOpacity = 0.15f,      // 틴트 불투명도 (0 = 완전투명, 1 = 불투명)
            LuminosityOpacity = 0.0f, // 밝기 불투명도 (0 = 뒤가 완전히 보임)
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
    //  Tree 선택
    // ═══════════════════════════════════════════════════════
    private void OnTreeItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
    {
        if (args.InvokedItem is TreeViewNode node && node.Content is string name)
            ViewModel.SelectNode(name);
    }

    // ═══════════════════════════════════════════════════════
    //  A/B/C/D 패널 탭
    // ═══════════════════════════════════════════════════════
    private void OnPanelTabClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button clicked || clicked.Tag is not string tag) return;

        Button[] tabs = { PanelABtn, PanelBBtn, PanelCBtn, PanelDBtn };
        SolidColorBrush[] accents =
        {
            (SolidColorBrush)Application.Current.Resources["PanelAAccent"],
            (SolidColorBrush)Application.Current.Resources["PanelBAccent"],
            (SolidColorBrush)Application.Current.Resources["PanelCAccent"],
            (SolidColorBrush)Application.Current.Resources["PanelDAccent"]
        };

        for (int i = 0; i < tabs.Length; i++)
        {
            if (tabs[i] == clicked)
            {
                tabs[i].Background = accents[i];
                tabs[i].Foreground = new SolidColorBrush(Colors.White);
            }
            else
            {
                tabs[i].Background = new SolidColorBrush(Colors.Transparent);
                tabs[i].Foreground = (SolidColorBrush)Application.Current.Resources["TextSecondary"];
            }
        }
        ViewModel.ActivePanel = tag;
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
    //  Menu: Theme — 투명도 유지하면서 테마 전환
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

        // 테마에 맞춰 투명 틴트 색상 조정
        if (_acrylicController != null)
        {
            _acrylicController.TintColor = theme == ElementTheme.Light
                ? Windows.UI.Color.FromArgb(255, 240, 240, 245)
                : Windows.UI.Color.FromArgb(255, 10, 10, 18);
        }
    }

    // ═══════════════════════════════════════════════════════
    //  투명도 슬라이더 — OpacityOverlay.Opacity 제어 (즉시 반영, 부드러운 그라데이션)
    //  0% = 완전 투명 (배경 비침), 100% = 불투명 (어두운 창)
    // ═══════════════════════════════════════════════════════
    private void OnOpacitySliderChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (OpacityOverlay != null)
            OpacityOverlay.Opacity = e.NewValue / 100.0 * 0.92;  // 최대 0.92 (완전 검정 방지)
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
