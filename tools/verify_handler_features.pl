#!perl
# verify_handler_features.pl — 320 Handler 의 310 이식 기능 자동 검증.
# 정적 grep + 빌드 산출물 확인.

use strict;
use warnings;

use FindBin; my $ROOT = "$FindBin::Bin/..";
my $HND_ROOT = "$ROOT/QMC.CDT-320";
my $HND_EXE  = "$HND_ROOT/bin/Debug/QMC.CDT-320.exe";

my @rows;
sub row { push @rows, [@_]; }

# ── 0. Build ──
row("BUILD", "QMC.CDT-320.exe (Tier 1 추가 후 빌드)", -e $HND_EXE ? "PASS" : "FAIL", "");

# ── 1. 정적 검사 ──
sub greps {
    my ($file, $pat) = @_;
    return 0 unless -e $file;
    open my $fh, '<', $file or return 0;
    local $/; my $c = <$fh>; close $fh;
    return $c =~ /$pat/s ? 1 : 0;
}

# A. Materials
my $die = "$HND_ROOT/Equipment/Materials/Die.cs";
my $a1 = greps($die, qr/enum\s+DieResult/) &&
         greps($die, qr/List<string>\s+NGCodes/) &&
         greps($die, qr/int\s+BinCode/) &&
         greps($die, qr/class\s+ProcessInformationData/) &&
         greps($die, qr/AddNG/);
row("STATIC", "Die — Result/NGCodes/BinCode/ProcessInformation/AddNG", $a1?"PASS":"FAIL", $die);

my $tf = "$HND_ROOT/Equipment/Materials/DieTapeFrame.cs";
my $a2 = greps($tf, qr/enum\s+ProcessMode/) &&
         greps($tf, qr/enum\s+TapeFrameRole/) &&
         greps($tf, qr/enum\s+TapeFrameRotate/) &&
         greps($tf, qr/enum\s+IdentifierState/) &&
         greps($tf, qr/enum\s+DieMapGenerateState/) &&
         greps($tf, qr/RepeatabilityInformation/) &&
         greps($tf, qr/GridX/) && greps($tf, qr/PitchX/);
row("STATIC", "DieTapeFrame — Mode/Role/Rotate/State 5 enum + Grid/Pitch", $a2?"PASS":"FAIL", $tf);

my $ms = "$HND_ROOT/Equipment/Materials/MaterialStorage.cs";
my $a3 = greps($ms, qr/static\s+class\s+MaterialStorage/) &&
         greps($ms, qr/GetByObjId/) &&
         greps($ms, qr/AddDie/) && greps($ms, qr/AddFrame/);
row("STATIC", "MaterialStorage — Dies/Frames + GetByObjId", $a3?"PASS":"FAIL", $ms);

# B. BinCodeMap
my $bcm = "$HND_ROOT/Equipment/Bin/BinCodeMap.cs";
my $b1 = greps($bcm, qr/ConvertToBinCode\s*\(\s*Die/) &&
         greps($bcm, qr/ConvertToBinCodeColor/) &&
         greps($bcm, qr/CreateDefault/) &&
         greps($bcm, qr/bin_codes\.json/);
row("STATIC", "BinCodeMap — ConvertToBinCode/Color + JSON 영속", $b1?"PASS":"FAIL", $bcm);

# C. DieMap
my $dm  = "$HND_ROOT/Equipment/DieMaps/DieMap.cs";
my $dmg = "$HND_ROOT/Equipment/DieMaps/DieMapGenerator.cs";
my $c1  = greps($dm, qr/class\s+DieMapEntry/) &&
          greps($dm, qr/class\s+DieMap/) &&
          greps($dm, qr/GetCell\s*\(/) &&
          greps($dmg, qr/static\s+DieMap\s+Generate\s*\(/) &&
          greps($dmg, qr/ApplyRotation/) &&
          greps($dmg, qr/SaveCsv/) && greps($dmg, qr/LoadJson/) &&
          greps($dmg, qr/SaveToOutput/);
row("STATIC", "DieMap + Generator (Generate/Rotation/SaveCsv/LoadJson/Output)", $c1?"PASS":"FAIL", $dmg);

my $dmv = "$HND_ROOT/Ui/Controls/DieMapView.cs";
my $c2 = greps($dmv, qr/class\s+DieMapView/) &&
         greps($dmv, qr/event\s+Action<DieMapEntry>\s+CellClicked/) &&
         greps($dmv, qr/HitTest/) &&
         greps($dmv, qr/DrawLegend/);
row("STATIC", "DieMapView UserControl — paint + hover + click", $c2?"PASS":"FAIL", $dmv);

# D. Jobs
my $jo = "$HND_ROOT/Equipment/Jobs/JobOrder.cs";
my $jq = "$HND_ROOT/Equipment/Jobs/JobQueue.cs";
my $d1 = greps($jo, qr/enum\s+JobState/) &&
         greps($jo, qr/enum\s+JobType/) &&
         greps($jo, qr/enum\s+JobPriority/) &&
         greps($jo, qr/class\s+JobOrder/) &&
         greps($jq, qr/static\s+class\s+JobQueue/) &&
         greps($jq, qr/Enqueue|TryDequeue|MarkDone|MarkFailed/);
row("STATIC", "Job 시스템 — JobOrder + JobQueue + 라이프사이클", $d1?"PASS":"FAIL", $jq);

# E. Interlocks
my $mi = "$HND_ROOT/Equipment/Interlocks/MotionInterlock.cs";
my $si = "$HND_ROOT/Equipment/Interlocks/StandardInterlocks.cs";
my $e1 = greps($mi, qr/abstract\s+class\s+MotionInterlock/) &&
         greps($mi, qr/static\s+class\s+InterlockRegistry/) &&
         greps($si, qr/PickerVsStageInterlock/) &&
         greps($si, qr/PickerVsPickerInterlock/) &&
         greps($si, qr/VisionVsPickerInterlock/) &&
         greps($si, qr/StageVsEjectInterlock/) &&
         greps($si, qr/StageLifterInterlock/);
row("STATIC", "Interlock 5종 + Registry", $e1?"PASS":"FAIL", $si);

# MachineController 통합
my $mc = "$HND_ROOT/Equipment/MachineController.cs";
my $f1 = greps($mc, qr/JobQueue\.Enqueue/) &&
         greps($mc, qr/JobQueue\.MarkDone|JobQueue\.MarkFailed/) &&
         greps($mc, qr/BinCodeMap\.ConvertToBinCode/) &&
         greps($mc, qr/MaterialStorage\.AddDie/) &&
         greps($mc, qr/die\.AddNG/);
row("STATIC", "MachineController 통합 (Job/Bin/Materials)", $f1?"PASS":"FAIL", $mc);

# H. MaterialSpecs
my $msp = "$HND_ROOT/Equipment/Materials/MaterialSpecs.cs";
my $h1 = greps($msp, qr/class\s+DieSpec/) &&
         greps($msp, qr/class\s+TapeFrameSpec/) &&
         greps($msp, qr/static\s+class\s+MaterialSpecs/) &&
         greps($msp, qr/material_specs\.json/);
row("STATIC", "MaterialSpecs — DieSpec + TapeFrameSpec + JSON 영속",
    $h1?"PASS":"FAIL", $msp);

# F1-3. AlignmentSolver + CoordinateMap + AlignWaferAsync
my $cm = "$HND_ROOT/Equipment/Vision/CoordinateMap.cs";
my $as = "$HND_ROOT/Equipment/Vision/AlignmentSolver.cs";
my $f2 = greps($cm, qr/class\s+CoordinateMap/) &&
         greps($cm, qr/ApplyToMotor/) && greps($cm, qr/ApplyToPixel/) &&
         greps($cm, qr/static\s+class\s+CoordinateMapStore/) &&
         greps($cm, qr/coord_map\.json/) &&
         greps($as, qr/static\s+class\s+AlignmentSolver/) &&
         greps($as, qr/Solve3Point/) &&
         greps($as, qr/ExtractRotationScale/) &&
         greps($mc, qr/AlignWaferAsync/);
row("STATIC", "Vision Alignment — Solver + CoordinateMap + AlignWaferAsync",
    $f2?"PASS":"FAIL", $as);

# G1. PickRetry
my $g1 = greps($mc, qr/MaxRetries/) &&
         greps($mc, qr/while\s*\(\s*attempt\s*<\s*MaxRetries/) &&
         greps($mc, qr/RetryCount/);
row("STATIC", "PickRetry — DoOneDieAsync 내부 재시도 루프", $g1?"PASS":"FAIL", $mc);

# I1. DieMapSaver
my $dms = "$HND_ROOT/Equipment/DieMaps/DieMapSaver.cs";
my $i1 = greps($dms, qr/static\s+class\s+DieMapSaver/) &&
         greps($dms, qr/SaveCycleResult/) &&
         greps($dms, qr/AppendLotSummary/) &&
         greps($dms, qr/lot_summary\.csv/);
row("STATIC", "DieMapSaver — Cycle 결과 + Lot 통계 CSV", $i1?"PASS":"FAIL", $dms);

# J1. RecipeStore Subset
my $rs = "$HND_ROOT/Equipment/Recipes/RecipeStore.cs";
my $j1 = greps($rs, qr/class\s+DieSubset/) &&
         greps($rs, qr/class\s+TapeFrameSubset/) &&
         greps($rs, qr/class\s+LoadTapeFrameSubset/) &&
         greps($rs, qr/class\s+UnloadTapeFrameSubset/) &&
         greps($rs, qr/class\s+ModuleSubset/);
row("STATIC", "RecipeStore Union — Die/Frame/Load/Unload/Module Subset 5종",
    $j1?"PASS":"FAIL", $rs);

# K1-4. SECS/GEM
my $secs = "$HND_ROOT/Equipment/Secs/SecsHost.cs";
my $k1 = greps($secs, qr/class\s+SecsHost/) &&
         greps($secs, qr/ProceedWithTapeFrame/) &&
         greps($secs, qr/StoppedWithTapeFrame/) &&
         greps($secs, qr/ProceedWithMap/) &&
         greps($secs, qr/StoppedWithMap/) &&
         greps($secs, qr/RaiseAlarmPosted/) &&
         greps($secs, qr/RaiseMaterialChanged/) &&
         greps($secs, qr/RaiseJobOrderStateChanged/) &&
         greps($secs, qr/SerializeZipBase64/) &&
         greps($secs, qr/DeserializeZipBase64/);
row("STATIC", "SECS/GEM — RemoteCommand 4 + EventReport 3 + ZipRecipe",
    $k1?"PASS":"FAIL", $secs);

# E7. MachineController.MoveAxisAsync hook
my $e7 = greps($mc, qr/Task<bool>\s+MoveAxisAsync/) &&
         greps($mc, qr/InterlockRegistry\.VerifyMove/) &&
         greps($mc, qr/await\s+MoveAxisAsync\s*\(\s*front\.ArmX/);
row("STATIC", "MachineController.MoveAxisAsync — Interlock hook + DoOneDie 적용",
    $e7?"PASS":"FAIL", $mc);

# UI Pages
my $bp = "$HND_ROOT/Ui/Pages/Material/MaterialBinPage.cs";
my $dp = "$HND_ROOT/Ui/Pages/Material/DieMapPage.cs";
my $b2 = greps($bp, qr/class\s+MaterialBinPage/) &&
         greps($bp, qr/_gridCodes/) && greps($bp, qr/_gridColors/) &&
         greps($bp, qr/CommitGridsToData/);
row("STATIC", "MaterialBinPage — bin 매핑 편집 GUI (NG↔bin↔color)",
    $b2?"PASS":"FAIL", $bp);
my $c4 = greps($dp, qr/class\s+DieMapPage/) &&
         greps($dp, qr/DieMapView/) &&
         greps($dp, qr/DoGenerate/) &&
         greps($dp, qr/DoFillDemo/);
row("STATIC", "DieMapPage — DieMapView + Generate/Demo/Load/Save",
    $c4?"PASS":"FAIL", $dp);

# ── Stage 1 신규 검증 ──
my $wt = "$HND_ROOT/Ui/Tabs/WorkTab.cs";
my $rt = "$HND_ROOT/Ui/Tabs/RecipeTab.cs";
my $s1a1 = greps($wt, qr/work\.dieMap.*new\s+DieMapPage/) ||
           (greps($wt, qr/work\.dieMap/) && greps($wt, qr/new\s+DieMapPage/));
row("STAGE1", "WorkTab — work.dieMap → DieMapPage 등록",
    $s1a1?"PASS":"FAIL", $wt);

my $s1a2 = greps($rt, qr/recipe\.binCode/) && greps($rt, qr/new\s+MaterialBinPage/) &&
           greps($rt, qr/recipe\.dieSubset/) && greps($rt, qr/new\s+DieSubsetPage/) &&
           greps($rt, qr/recipe\.tapeFrameSubset/) && greps($rt, qr/new\s+TapeFrameSubsetPage/) &&
           greps($rt, qr/recipe\.loadFrame/) && greps($rt, qr/new\s+LoadTapeFrameSubsetPage/) &&
           greps($rt, qr/recipe\.unloadFrame/) && greps($rt, qr/new\s+UnloadTapeFrameSubsetPage/);
row("STAGE1", "RecipeTab — 5 신규 사이드바 등록 (Bin/Die/Frame/Load/Unload)",
    $s1a2?"PASS":"FAIL", $rt);

my $sst = "$HND_ROOT/Ui/Dialogs/SystemSelfTestDialog.cs";
my $s1a3 = greps($sst, qr/"BinCodeMap"/) &&
           greps($sst, qr/"DieMap generator"/) &&
           greps($sst, qr/"JobQueue"/) &&
           greps($sst, qr/"InterlockRegistry"/) &&
           greps($sst, qr/"AlignmentSolver \(3pt\)"/);
row("STAGE1", "SystemSelfTestDialog — 5 신규 진단 항목 (총 14)",
    $s1a3?"PASS":"FAIL", $sst);

my $sb = "$HND_ROOT/Ui/Pages/Recipe/SubsetPageBase.cs";
my $s1b1 = greps($sb, qr/abstract\s+class\s+SubsetPageBase/) &&
           greps($sb, qr/abstract\s+void\s+BuildEditor/) &&
           greps($sb, qr/abstract\s+void\s+LoadFromRecipe/) &&
           greps($sb, qr/abstract\s+void\s+SaveToRecipe/);
row("STAGE1", "SubsetPageBase — 공통 베이스 (Load/Save 자동)",
    $s1b1?"PASS":"FAIL", $sb);

my $s1b2 = (-e "$HND_ROOT/Ui/Pages/Recipe/DieSubsetPage.cs") &&
           (-e "$HND_ROOT/Ui/Pages/Recipe/TapeFrameSubsetPage.cs") &&
           (-e "$HND_ROOT/Ui/Pages/Recipe/LoadTapeFrameSubsetPage.cs") &&
           (-e "$HND_ROOT/Ui/Pages/Recipe/UnloadTapeFrameSubsetPage.cs");
row("STAGE1", "4 Subset 편집 페이지 (Die/Frame/Load/Unload) 파일 존재",
    $s1b2?"PASS":"FAIL", "");

my $lang = "$HND_ROOT/Ui/Localization/Lang.cs";
my $s1c = greps($lang, qr/"recipe\.binCode"/) &&
          greps($lang, qr/"recipe\.dieSubset"/) &&
          greps($lang, qr/"recipe\.tapeFrameSubset"/) &&
          greps($lang, qr/"recipe\.loadFrame"/) &&
          greps($lang, qr/"recipe\.unloadFrame"/) &&
          greps($lang, qr/"work\.dieMap"/);
row("STAGE1", "i18n 키 6 신규 (recipe.binCode/dieSubset/.../work.dieMap)",
    $s1c?"PASS":"FAIL", $lang);

# 출력
my $bar = "=" x 110; my $sep = "-" x 110;
print "$bar\n";
printf "%-9s %-65s %-6s %s\n", "CATEGORY", "ITEM", "RESULT", "DETAIL";
print "$sep\n";
my ($pass, $fail) = (0,0);
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
