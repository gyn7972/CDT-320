param([switch]$KeepArtifacts)
$ErrorActionPreference = 'Stop'
$taskRepo = Split-Path $PSScriptRoot -Parent
$taskOutput = Join-Path $taskRepo ('_build_check_handler\verify-wafer-map-dialog-' + [Guid]::NewGuid().ToString('N'))
$taskCompiler = 'C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\Roslyn\csc.exe'
if (!(Test-Path -LiteralPath $taskCompiler)) { throw "C# compiler not found: $taskCompiler" }
[IO.Directory]::CreateDirectory($taskOutput) | Out-Null
$taskPassed = $false
try {
    # 기존 페이지의 Designer와 진입 이벤트만 검증한다. 실제 SubsetPageBase/Recipe 초기화는 제외한다.
    $taskPage = [IO.File]::ReadAllText((Join-Path $taskRepo 'QMC.CDT-320\Ui\Pages\Recipe\TapeFrameSubsetPage.cs'))
    $taskStart = $taskPage.IndexOf('        private void btnPreviewWaferMap_Click(')
    $taskEnd = $taskPage.IndexOf('        private void btnImportWaferMap_Click(', $taskStart)
    if ($taskStart -lt 0 -or $taskEnd -le $taskStart) { throw 'Preview entry handler not found.' }
    $taskHandler = $taskPage.Substring($taskStart, $taskEnd - $taskStart)
    # 테스트 창은 화면에 노출하지 않고 동일한 modal 경로와 입력 전달을 확인한다.
    $taskHandler = $taskHandler.Replace('dialog.ShowDialog(this)', 'OpenHiddenDialog(dialog, this)')
    $taskProbe = @'
using System;
using System.Windows.Forms;
using QMC.CDT320.DieMaps;
using QMC.CDT_320.Ui.Dialogs;
namespace QMC.CDT_320.Ui.Pages.Recipe
{
    public partial class TapeFrameSubsetPage : UserControl
    {
        private Panel _editorPanel;
        public TapeFrameSubsetPage()
        {
            _editorPanel = new Panel { Dock = DockStyle.Fill };
            Controls.Add(_editorPanel);
            InitializeComponent();
        }
        private static void OpenHiddenDialog(WaferMapCreateDialog dialog, IWin32Window owner)
        {
            dialog.Opacity = 0;
            dialog.ShowInTaskbar = false;
            dialog.ShowDialog(owner);
        }
        private void btnLoadSpec_Click(object sender, EventArgs e) { }
        private void btnSaveSpec_Click(object sender, EventArgs e) { }
        private void OnWaferRoleChanged(object sender, EventArgs e) { }
        private void btnImportWaferMap_Click(object sender, EventArgs e) { }
        private void btnGridMapCreate_Click(object sender, EventArgs e) { }
        private void _cbEdgeSkipMode_SelectedIndexChanged(object sender, EventArgs e) { }
        private void btnEditDieSpec_Click(object sender, EventArgs e) { }
        private void btnGridCountPreview_Click(object sender, EventArgs e) { }
'@
    $taskProbe += "`r`n" + $taskHandler + "`r`n    }`r`n}`r`n"
    $taskProbePath = Join-Path $taskOutput 'WaferPageProbe.cs'
    [IO.File]::WriteAllText($taskProbePath, $taskProbe, (New-Object System.Text.UTF8Encoding($true)))
    $taskSources = @(
        'QMC.CDT-320\Equipment\DieMaps\WaferMapGeneration.cs',
        'QMC.CDT-320\Ui\Dialogs\WaferMapCreateDialog.cs',
        'QMC.CDT-320\Ui\Dialogs\WaferMapCreateDialog.Designer.cs',
        'QMC.CDT-320\Ui\Dialogs\WaferMapGenerationView.cs',
        'QMC.CDT-320\Ui\Pages\Recipe\TapeFrameSubsetPage.Designer.cs',
        'tools\verify_wafer_map_dialog.cs'
    ) | ForEach-Object { Join-Path $taskRepo $_ }
    $taskExe = Join-Path $taskOutput 'VerifyWaferMapDialog.exe'
    & $taskCompiler /nologo /langversion:7.3 /target:exe "/out:$taskExe" /r:System.dll /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll $taskSources $taskProbePath
    if ($LASTEXITCODE -ne 0) { throw 'Standalone dialog compilation failed.' }
    & $taskExe $taskOutput
    if ($LASTEXITCODE -ne 0) { throw "Dialog verification failed. Artifacts: $taskOutput" }
    $taskPassed = $true
}
finally {
    if ($taskPassed -and !$KeepArtifacts) {
        $taskBoundary = [IO.Path]::GetFullPath((Join-Path $taskRepo '_build_check_handler')) + '\'
        $taskResolved = [IO.Path]::GetFullPath($taskOutput)
        if (!$taskResolved.StartsWith($taskBoundary, [StringComparison]::OrdinalIgnoreCase)) { throw 'Cleanup boundary mismatch.' }
        Remove-Item -LiteralPath $taskResolved -Recurse -Force
    }
    else { Write-Output "Artifacts: $taskOutput" }
}
