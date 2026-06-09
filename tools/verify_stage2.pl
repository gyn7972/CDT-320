#!perl
# verify_stage2.pl — Stage 2 정적 검증 (SPC + 5 ParameterEditor + ZoomDialog + Cognex 진단)

use strict;
use warnings;

use FindBin; my $ROOT = "$FindBin::Bin/..";
my $VIS_ROOT = "$ROOT/QMC.Vision";
my $VIS_EXE  = "$VIS_ROOT/bin/Debug/QMC.Vision.exe";

my @rows;
sub row { push @rows, [@_]; }

sub greps {
    my ($file, $pat) = @_;
    return 0 unless -e $file;
    open my $fh, '<', $file or return 0;
    local $/; my $c = <$fh>; close $fh;
    return $c =~ /$pat/s ? 1 : 0;
}

# Build
row("BUILD", "QMC.Vision.exe (Stage 2 후)", -e $VIS_EXE ? "PASS" : "FAIL", "");

# A. SPC
my $spc = "$VIS_ROOT/Ui/Pages/SpcChartPage.cs";
my $a1 = greps($spc, qr/class\s+SpcChartPage/) &&
         greps($spc, qr/System\.Windows\.Forms\.DataVisualization\.Charting/) &&
         greps($spc, qr/Series.*ChartType\s*=\s*SeriesChartType\.Line/) &&
         greps($spc, qr/avg.*stdev.*max.*min/);
row("STAGE2", "SpcChartPage — Chart 컨트롤 + Avg/Stdev/Max/Min 통계", $a1?"PASS":"FAIL", $spc);

# Stage~97 Vision 디자이너 스윕: MaintenancePage 제거 → SPC/Editor 진입은 RecipePage 로 이동.
my $mp = "$VIS_ROOT/Ui/Pages/RecipePage.cs";
my $a2 = greps($mp, qr/SpcChartPage/) && greps($mp, qr/ShowSpc/);
row("STAGE2", "RecipePage — SPC 진입 + ShowSpc()", $a2?"PASS":"FAIL", $mp);

# B. ② 파라미터 — P4 에디터 흡수(ParameterEditor 5종/Base/Host 제거 → 통일 그리드)
my $base = "$VIS_ROOT/Ui/Editors/ParameterEditorBase.cs";
my $host = "$VIS_ROOT/Ui/Editors/ParameterEditorHost.cs";
my $reg  = "$VIS_ROOT/Core/Parameters/InspectionParamRegistry.cs";
my $pgi  = "$VIS_ROOT/Ui/Controls/ParameterGridItem.cs";
my $sp   = "$VIS_ROOT/Ui/Pages/SettingsPage.cs";
# 에디터 Base/Host 제거됨 + FromDescriptor 어댑터 + 레지스트리 존재
my $b1 = (! -e $base) && (! -e $host) &&
         greps($pgi, qr/FromDescriptor/) &&
         greps($reg, qr/class\s+InspectionParamRegistry/);
row("STAGE2", "② 에디터 흡수: Base/Host 제거 + FromDescriptor + 레지스트리", $b1?"PASS":"FAIL", $pgi);

# ② 5종 = InspectionParameters POCO IParameterProvider + 레지스트리 5 매핑
my @tools = qw(BottomInspection SideInspection DieGapInspection Distortion VisionScale);
my $b2_pass = greps("$VIS_ROOT/Core/Inspectors/InspectionParameters.cs", qr/IParameterProvider/) ? 1 : 0;
foreach my $t (@tools) { $b2_pass = 0 unless greps($reg, qr/"\Q$t\E"/); }
row("STAGE2", "② 5종 디스크립터 + 레지스트리 매핑(Bottom/Side/DieGap/Distortion/Scale)", $b2_pass?"PASS":"FAIL", $reg);

# SettingsPage → 통일 그리드(에디터 대체): 레지스트리+FromDescriptor 사용, InspectionEditorFactory 미사용
my $b3 = greps($sp, qr/InspectionParamRegistry/) && greps($sp, qr/FromDescriptor/) &&
         !greps($sp, qr/InspectionEditorFactory/);
row("STAGE2", "SettingsPage → 통일 그리드(② 에디터 흡수)", $b3?"PASS":"FAIL", $sp);

# C. ZoomDialog — 디자이너 스윕: MouseWheel 배선은 .Designer.cs, 줌로직/핸들러는 .cs.
my $zd  = "$VIS_ROOT/Ui/Dialogs/ZoomDialog.cs";
my $zdd = "$VIS_ROOT/Ui/Dialogs/ZoomDialog.Designer.cs";
my $c1 = greps($zd, qr/class\s+ZoomDialog\s*:\s*Form/) &&
         greps($zdd, qr/MouseWheel/) &&
         greps($zd, qr/_zoom\s*=\s*Math\.Max.*_zoom\s*\*\s*delta/s) &&
         greps($zd, qr/DoubleClick|OnWheel/);
row("STAGE2", "ZoomDialog — 휠 줌(Designer 배선) + 줌로직 + 1:1", $c1?"PASS":"FAIL", $zd);

my $cv = "$VIS_ROOT/Ui/Controls/CameraView.cs";
my $c2 = greps($cv, qr/DoubleClick.*ZoomDialog/s);
row("STAGE2", "CameraView — 더블클릭 → ZoomDialog", $c2?"PASS":"FAIL", $cv);

# D. Cognex 진단
my $cfg = "$VIS_ROOT/Ui/Pages/ConfigurationPage.cs";
# 디자이너 스윕: 패널 라벨 문구 변경 → 기능 심볼(ProbeCognex/CognexBackend/CognexLoaded)로 검사.
my $d1 = greps($cfg, qr/ProbeCognex/) &&
         greps($cfg, qr/CognexBackend/) &&
         greps($cfg, qr/CognexLoaded/);
row("STAGE2", "ConfigurationPage — Cognex 진단(ProbeCognex/Backend/Loaded)", $d1?"PASS":"FAIL", $cfg);

# csproj 등록 확인
my $proj = "$VIS_ROOT/QMC.Vision.csproj";
my $d2 = greps($proj, qr/SpcChartPage\.cs/) &&
         greps($proj, qr/Core\\Parameters\\ParameterStore\.cs/) &&
         greps($proj, qr/ZoomDialog\.cs/) &&
         !greps($proj, qr/ParameterEditorBase\.cs/) &&
         greps($proj, qr/System\.Windows\.Forms\.DataVisualization/);
row("STAGE2", "csproj: SPC/ParameterStore/Zoom 등록 + 에디터 제거 + DataVisualization", $d2?"PASS":"FAIL", $proj);

# 출력
my $bar = "=" x 110; my $sep = "-" x 110;
print "$bar\n";
printf "%-9s %-65s %-6s %s\n", "CATEGORY", "ITEM", "RESULT", "DETAIL";
print "$sep\n";
my ($pass,$fail) = (0,0);
foreach my $r (@rows) {
    my ($c, $i, $res, $d) = @$r;
    printf "%-9s %-65s %-6s %s\n", $c, $i, $res, ($d // "");
    $pass++ if $res eq "PASS";
    $fail++ if $res eq "FAIL";
}
print "$bar\n";
my $total = scalar @rows;
print "TOTAL $total   PASS $pass   FAIL $fail\n";
exit ($fail > 0 ? 1 : 0);
