$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
$root = Split-Path $PSScriptRoot -Parent
$process = Start-Process (Join-Path $root 'src/MobiFlux.Desktop/bin/Debug/net10.0-windows/MobiFlux.Desktop.exe') -PassThru
try {
    $watch = [Diagnostics.Stopwatch]::StartNew()
    $condition = [System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $process.Id)
    do {
        $window = [System.Windows.Automation.AutomationElement]::RootElement.FindFirst([System.Windows.Automation.TreeScope]::Children, $condition)
        if ($window) { break }
        if ($process.HasExited) { throw 'Desktop exited before creating window' }
        [Threading.Thread]::Sleep(200)
    } while ($watch.Elapsed.TotalSeconds -lt 15)
    if (!$window) { throw 'Desktop window missing after 15 seconds' }
    $items = $window.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::ListItem))
    $names = @($items | ForEach-Object { $_.Current.Name })
    foreach ($expected in @('Tổng quan', 'Proxy', 'Điện thoại', 'Pool và định tuyến', 'Đổi IP', 'Lịch sử kết nối', 'Mạng Windows', 'Chẩn đoán', 'Nhật ký')) {
        if ($names -notcontains $expected) { throw "Missing navigation: $expected" }
    }
    if ($names -match 'NavigationItem') { throw 'Record leaked into accessible name' }
    foreach ($item in $items) {
        $selection = [System.Windows.Automation.SelectionItemPattern]$item.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern)
        $selection.Select()
        if (!$selection.Current.IsSelected) { throw "Navigation selection failed: $($item.Current.Name)" }
    }
    $toggle = $window.FindFirst([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::NameProperty, 'Thu gọn hoặc mở rộng thanh điều hướng'))
    if (!$toggle) { throw 'Sidebar toggle missing' }
    $expandedLeft = $toggle.Current.BoundingRectangle.Left
    $invoke = [System.Windows.Automation.InvokePattern]$toggle.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
    $invoke.Invoke()
    [Threading.Thread]::Sleep(200)
    $collapsedLeft = $toggle.Current.BoundingRectangle.Left
    if ($collapsedLeft -ge $expandedLeft) { throw 'Sidebar did not collapse' }
    $invoke.Invoke()
    [Threading.Thread]::Sleep(200)
    if ([Math]::Abs($toggle.Current.BoundingRectangle.Left - $expandedLeft) -gt 1) { throw 'Sidebar did not expand' }
    Write-Output 'PASS: native startup, nine navigation selections and sidebar collapse/expand'
}
finally {
    if (!$process.HasExited) { $process.Kill(); $process.WaitForExit() }
}
