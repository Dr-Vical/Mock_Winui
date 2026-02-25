# RswareDesign WinUI 3 Mockup

## Project Overview
WinUI 3 + Glassmorphism 기반 서보 드라이브 파라미터 설정 도구 UI 목업.
비즈니스 로직 없음 (순수 UI 셸). 실제 구현은 별도 RSwareWPF 프로젝트(Prism 9 + DryIoc, Phase 11 완료).

## Tech Stack
- .NET 8 (net8.0-windows10.0.19041.0)
- Windows App SDK 1.6 (WinUI 3)
- CommunityToolkit.Mvvm 8.4.0 ([ObservableProperty], [RelayCommand])
- CommunityToolkit.WinUI.Controls.Segmented 8.2
- DesktopAcrylicController (글라스모피즘 배경)

## Build
- **MSBuild 필수** (dotnet build 안 됨 — MrtCore.PriGen.targets MSB4062 오류)
- VS 2022 Community: `"C:/Program Files/Microsoft Visual Studio/2022/Community/MSBuild/Current/Bin/MSBuild.exe" RswareDesign.csproj -p:Configuration=Debug -p:Platform=x64`
- VS 2022 Professional: `"C:/Program Files/Microsoft Visual Studio/2022/Professional/MSBuild/Current/Bin/MSBuild.exe" RswareDesign.csproj -p:Configuration=Debug -p:Platform=x64`
- 실행: `./bin/x64/Debug/net8.0-windows10.0.19041.0/RswareDesign.exe`
- WindowsAppSDKSelfContained=true (런타임 별도 설치 불필요)

## Git
- Remote: https://github.com/Dr-Vical/Mock_Winui.git
- Branch: master (default)
- WPF 관련 커밋은 2025-02-25 정리로 제거됨 (WinUI 3 커밋만 남음)

## Project Structure
```
RswareDesign/
├── App.xaml(.cs)                  # Application entry, Dark theme, GlassTheme resource
├── MainWindow.xaml(.cs)           # Main layout + code-behind (acrylic, tree, panels, events)
├── Themes/
│   └── GlassTheme.xaml            # Design tokens (glass, accent, text, semantic, grid, panel colors)
├── ViewModels/
│   └── MainViewModel.cs           # MVVM ViewModel (20+ mock parameter sets, panel toggle, commands)
├── Models/
│   ├── ParameterItem.cs           # { FtNumber, Name, Value, Unit, Default, Min, Max }
│   ├── StatusEntry.cs             # { Status, Value, Units }
│   └── DriveTreeNode.cs           # { Name, IconGlyph, IsExpanded, Children } (미사용)
├── Controls/
│   ├── CompareParameterPanel.xaml(.cs) # A/B/C/D 분할 패널 컨트롤
│   └── GlassRibbonButton.xaml(.cs)    # 커스텀 글라스 버튼 (미사용)
├── global.json                    # .NET 9 SDK pin
└── RswareDesign.csproj
```

## Window Layout
```
┌─────────────────────────────────────────────────┐
│  TitleBar (custom, ExtendsContentIntoTitleBar)  │
├─────────────────────────────────────────────────┤
│  MenuBar (File | Tools | Views | Help | Admin)  │
├─────────────────────────────────────────────────┤
│  CommandBar (Enable | Disable | ClearFault | Opacity Slider) │
├──────────┬──────────────────────┬───────────────┤
│          │  Center: A/B/C/D    │               │
│  Tree    │  Dynamic Panels     │  Actions      │
│  View    │  (1~4 split grid)   │  Panel        │
│  (260px) ├──────────────────────┤  (170px)      │
│          │  Error Log           │               │
│  [A][B]  │  (STATUS|VALUE|UNIT) │               │
│  [C][D]  │                      │               │
├──────────┴──────────────────────┴───────────────┤
│  StatusBar (Connection | Drive | Mode | Port)   │
└─────────────────────────────────────────────────┘
```

## Key Features (현재 구현됨)
1. **Glassmorphism UI**: DesktopAcrylicController + OpacityOverlay (슬라이더로 0~92% 제어)
2. **A/B/C/D Panel Splitting**: 최대 4분할, 동적 그리드 레이아웃 (1=전체, 2=50/50, 3~4=2x2)
3. **TreeView Navigation**: 좌측 드라이브 트리 → 선택 시 활성 패널에 파라미터 로드
4. **Mock Parameter Data**: 20+ 노드별 하드코딩 파라미터 (ECAT Homing, Motor, PID 등)
5. **Theme Switching**: Dark/Light/System Default (Views 메뉴)
6. **MVVM**: CommunityToolkit.Mvvm 기반, x:Bind 사용
7. **Action Buttons**: Read All, Write All, Save to Flash, Compare, Export, Revert (UI만, 로직 없음)

## A/B/C/D Panel System
- 좌측 트리 하단의 A/B/C/D 버튼으로 토글 (on/off)
- 최소 1개 패널 항상 유지
- 각 패널에 독립적 파라미터 컬렉션 (PanelAParameters ~ PanelDParameters)
- 각 패널 고유 액센트 색: A=Blue, B=Teal, C=Yellow, D=Red
- PanelLayoutChanged 이벤트 → RebuildPanelLayout()에서 Grid 재구성
- CompareParameterPanel 컨트롤: 헤더(이름+노드명+닫기) + 컬럼헤더 + ListView

## Design Token System (GlassTheme.xaml)
- Glass: GlassBrush(#1AFFFFFF), GlassHighBrush, GlassLowBrush, GlassBorderBrush
- Accent: AccentBrush(#FF60A5FA blue), SecondaryBrush(#FF34D399 teal)
- Semantic: SuccessBrush(green), WarningBrush(yellow), ErrorBrush(red)
- Panel Accents: PanelAAccent~PanelDAccent
- TreeView/ListView selection override: 40~60% AccentColor
- Typography: Segoe UI Variable (UI), Cascadia Code (data)

## Code Architecture Notes
- MainWindow.xaml.cs에 코드비하인드 로직 다수 (순수 MVVM은 아님)
  - TrySetTransparentBackdrop(): 아크릴 배경 설정
  - BuildDriveTree(): 트리뷰 하드코딩 구성
  - RebuildPanelLayout(): A/B/C/D 그리드 동적 재구성
  - UpdatePanelButtons(): 버튼 비주얼 토글
- MainViewModel.cs: ObservableObject 상속, 모든 커맨드 목업 (ActionMessage만 변경)
- BuildAllParameterSets(): 20+ 노드의 목업 파라미터 Dictionary로 관리
- DriveTreeNode.cs는 정의만 있고 실제 미사용 (TreeViewNode 직접 사용)

## Phase History
- **Phase 1** (commit 78bec20): A/B/C/D 패널 분할 + 동적 그리드 레이아웃
  - CompareParameterPanel 컨트롤 추가
  - 패널 토글 로직, 레이아웃 재구성
- Phase 2 이후: 미정

## RSwareWPF 프로젝트 (별도, Phase 11 완료)
- 위치: D:/RSware/RSwareWPF/ (다른 PC)
- C# WPF + Prism 9 + DryIoc, 22 프로젝트
- Phase 0~11 완료 (분석→인프라→코어→통신→드라이브12종→UI→메뉴→연결→JSON파라미터)
- 이 WinUI3 목업은 RSwareWPF의 UI 참조용 디자인 프로토타입

## Conventions
- Naming: PascalCase (public), _camelCase (private)
- XAML binding: x:Bind (compiled binding)
- Nullable: enabled
- File encoding: UTF-8 with BOM
