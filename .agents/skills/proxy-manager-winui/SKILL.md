---
name: proxy-manager-winui
description: >-
  Standardized guidelines and architecture for building 11/10 modern Windows 11 desktop applications using WinUI 3, Windows App SDK, and .NET 10. Enforces Fluent Design, AppWindow custom titlebars, Mica backdrop, strict ban on raw icons/emojis, CommunityToolkit.Mvvm conventions, and zero code comments.
---

# Proxy Manager WinUI 3 Architecture & Design Guidelines

This skill provides architectural patterns, UI standards, and coding conventions for building native Windows 11 desktop applications using WinUI 3, Windows App SDK, and .NET 10.

## 1. Core Technology Stack

- **Target Framework**: `net10.0-windows10.0.19041.0` (minimum 10.0.17763.0)
- **UI Framework**: WinUI 3 (Windows App SDK 1.6+ unpackaged)
- **Deployment Mode**: Unpackaged (`<WindowsPackageType>None</WindowsPackageType>`)
- **MVVM Framework**: `CommunityToolkit.Mvvm` (Source Generators)
- **Dependency Injection**: `Microsoft.Extensions.Hosting`, `Microsoft.Extensions.DependencyInjection`
- **Iconography**: `Segoe Fluent Icons` (`FontIcon`), `SymbolIcon`, or Vector `PathIcon` Geometry

## 2. Strict UI/UX Guardrails (The 11/10 Standard)

### A. Iconography Rules
- **NEVER use raw emoji or unicode characters** as icons in the UI (such as raw boxes, arrows, or symbols).
- Always use styled, dedicated icon controls:
  1. `SymbolIcon` for standard system symbols (e.g. `Symbol="ViewAll"`, `Symbol="Setting"`).
  2. `FontIcon` with `FontFamily="{StaticResource SymbolThemeFontFamily}"` and valid Segoe Fluent Icons hex glyphs (e.g. `Glyph="&#xE80F;"`).
  3. `PathIcon` with vector SVG geometry paths for custom cellular/ADB/network hardware iconography.

### B. Fluent Design System & Visual Tokens
- **Backdrop**: Always configure `MicaBackdrop` with `MicaKind.BaseAlt` for modern desktop depth.
- **Corner Radius**:
  - Small controls (Buttons, TextBoxes, Badges): `4px`
  - Cards, Containers, Flyouts, Dialogs: `8px`
- **Typography**:
  - Primary Font: `Segoe UI Variable Text`, `Segoe UI Variable Display`
  - Display: `28px` SemiBold (KPI metrics, hero headers)
  - Subtitle / Page Header: `20px` SemiBold
  - Body: `14px` Regular
  - Caption / Micro-metrics: `11px` Regular / Medium
- **Color Palette**:
  - Canvas / Shell: Dynamic Mica Alt
  - Surface Cards: Semi-transparent acrylic brush with subtle 1px border (`#1F000000` Light / `#1FFFFFFF` Dark)
  - Accent / Primary: Windows 11 System Accent Brush (`SystemAccentColor`)
  - Status Indicators: Success (`#10B981`), Warning (`#F59E0B`), Danger (`#EF4444`), Offline/Muted (`#64748B`)

## 3. Window & TitleBar Architecture

Desktop applications must extend XAML content into the title bar while preserving native Windows 11 Snap Layouts:

```csharp
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;

namespace ProxyManager.App;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        SystemBackdrop = new MicaBackdrop { Kind = MicaKind.BaseAlt };
    }
}
```

TitleBar layout in XAML:

```xml
<Grid x:Name="AppTitleBar" Height="44" VerticalAlignment="Top">
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="Auto" />
        <ColumnDefinition Width="*" />
    </Grid.ColumnDefinitions>
    <StackPanel Orientation="Horizontal" VerticalAlignment="Center" Margin="16,0,0,0">
        <FontIcon FontFamily="{StaticResource SymbolThemeFontFamily}" Glyph="&#xE804;" FontSize="16" Margin="0,0,8,0" />
        <TextBlock Text="MobiFlux" FontWeight="SemiBold" FontSize="12" />
    </StackPanel>
</Grid>
```

## 4. MVVM Architecture with CommunityToolkit.Mvvm

ViewModels must use C# source generators to eliminate boilerplate and ensure reactive UI binding:

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ProxyManager.Presentation.ViewModels;

public sealed partial class DashboardViewModel : ObservableObject
{
    private readonly IProxyApiClient _api;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string statusMessage = "Ready";

    [ObservableProperty]
    private int activeProxies;

    [ObservableProperty]
    private int activeDevices;

    public DashboardViewModel(IProxyApiClient api)
    {
        _api = api;
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsLoading = true;
        try
        {
            var summary = await _api.GetSummaryAsync();
            ActiveProxies = summary.ActiveProxies;
            ActiveDevices = summary.ActiveDevices;
            StatusMessage = "Telemetry updated successfully";
        }
        catch
        {
            StatusMessage = "Control plane service unavailable";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
```

## 5. Architectural Boundaries

- **Desktop Shell Responsibility**: Presentation, Navigation, Dialogs, Real-time telemetry, Local client state.
- **Do NOT replicate Domain/EF Core/SQLite in Desktop**: The Desktop application communicates with the local control plane daemon (`MobiFlux.Service`) via REST / SignalR.
- **Do NOT create CQRS commands for UI actions**: Expanding sidebars, changing tabs, and opening modals are handled directly in the ViewModel/View layer.

## 6. Coding Discipline

- **Zero Comments**: Source code must contain no comments (`//`, `/* */`, `<!-- -->`).
- **Clean Naming**: Classes and variables must be self-explanatory.
- **Strict Ban on Inline Imports**: Never use inline qualified namespace imports (e.g. `MobiFlux.Application.Features.Proxies.UpdateProxyPool.UpdateProxyPoolCommand`). All imports must be declared at the top of the file using top-level `using` directives.

## 7. Strict Prohibition of Mock & Hardcoded Data

- **Zero Mock / Hardcoded Data**: Source code, ViewModels, Controllers, and Services must NEVER inject fake mock data (such as synthetic phones, fake serials, fake random throughput numbers, or simulated random IP generation).
- **Truthful Hardware & Network Telemetry**: If no physical Android device is connected via ADB, the app must truthfully report offline/empty states rather than populating fabricated devices.
- **Real Operations Only**: USB ADB device discovery, live cellular telemetry queries (`dumpsys telephony.registry`, `dumpsys battery`, `getprop`), IP rotation (`cmd connectivity airplane-mode`, `svc data`), TCP Netstat connection tracking, and ICMP/TCP ping diagnostics must execute real network and hardware operations.
- **Strict Clean Architecture & CQRS**: Strict segregation across Domain, Application (Commands/Queries/Handlers via MediatR), Infrastructure (Persistence/ADB/Adapters), Proxy Engine, Service API, and Presentation Desktop.
