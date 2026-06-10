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

# B. 파라미터 영속화 — BaseUnit Composite 일원화(구 ParameterStore/② InspectionParameters 일체 제거)
my $an  = "$VIS_ROOT/Modules/AlgorithmNode.cs";
my $ad  = "$VIS_ROOT/Modules/AlgorithmData.cs";
my $ivm = "$VIS_ROOT/Modules/IVisionModule.cs";
my $vtp = "$VIS_ROOT/Ui/Pages/VisionTargetPage.cs";
my $itp = "$VIS_ROOT/Ui/Pages/InspectorTargetPage.cs";
# 구 ParameterStore 일체 제거 + 노드 Apply/Collect
my $b1 = (! -e "$VIS_ROOT/Core/Parameters") &&
         (! -e "$VIS_ROOT/Core/Inspectors/InspectionParameters.cs") &&
         (! -e "$VIS_ROOT/Config/ParameterStoreBootstrap.cs") &&
         greps($an, qr/ApplyToRuntime/) && greps($an, qr/CollectFromRuntime/);
row("STAGE2", "BaseUnit 노드 Apply/Collect + 구 ParameterStore 제거", $b1?"PASS":"FAIL", $an);

# 알고리즘 노드 데이터(POCO) — FinderAlgoRecipe/InspectorAlgoRecipe + Finder/InspectorAlgorithm
my $b2 = greps($ad, qr/class\s+FinderAlgoRecipe/) && greps($ad, qr/SearchRoi/) && greps($ad, qr/AcceptThreshold/) &&
         greps($ad, qr/class\s+InspectorAlgoRecipe/) && greps($ad, qr/InspectionRoi/) &&
         greps($an, qr/class\s+FinderAlgorithm/) && greps($an, qr/class\s+InspectorAlgorithm/);
row("STAGE2", "알고리즘 노드 데이터(FinderAlgo/InspectorAlgo Recipe)", $b2?"PASS":"FAIL", $ad);

# 타깃 페이지 → 노드 API(GetAlgorithm/SaveRecipe), 구 ParameterStore 미참조
my $b3 = greps($ivm, qr/GetAlgorithm/) &&
         greps($vtp, qr/SaveRecipe/) && greps($itp, qr/SaveRecipe/) &&
         !greps($vtp, qr/ParameterStoreHost/);
row("STAGE2", "타깃 페이지 → BaseUnit 노드(GetAlgorithm/SaveRecipe)", $b3?"PASS":"FAIL", $vtp);

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
         greps($proj, qr/Modules\\AlgorithmNode\.cs/) &&
         greps($proj, qr/Modules\\AlgorithmData\.cs/) &&
         greps($proj, qr/ZoomDialog\.cs/) &&
         !greps($proj, qr/Core\\Parameters\\ParameterStore\.cs/) &&
         !greps($proj, qr/ParameterEditorBase\.cs/) &&
         greps($proj, qr/System\.Windows\.Forms\.DataVisualization/);
row("STAGE2", "csproj: SPC/BaseUnit(AlgorithmNode/Data)/Zoom 등록 + ParameterStore·에디터 제거", $d2?"PASS":"FAIL", $proj);

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
