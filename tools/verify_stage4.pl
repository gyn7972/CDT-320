#!perl
# verify_stage4.pl — Stage 4 정적 검증 (RemoteViewerDialog + ActiveLotPage + SecsHost-HSMS + Cognex test btn)

use strict;
use warnings;

use FindBin; my $ROOT = "$FindBin::Bin/..";
my $HND_ROOT = "$ROOT/QMC.CDT-320";
my $VIS_ROOT = "$ROOT/QMC.Vision";
my $HND_EXE  = "$HND_ROOT/bin/Debug/QMC.CDT-320.exe";
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
row("BUILD", "QMC.CDT-320.exe (Stage 4 후)", -e $HND_EXE ? "PASS" : "FAIL", "");
row("BUILD", "QMC.Vision.exe  (Stage 4 후)", -e $VIS_EXE ? "PASS" : "FAIL", "");

# A. RemoteViewerDialog
my $rvd = "$HND_ROOT/Ui/Dialogs/RemoteViewerDialog.cs";
my $a1 = greps($rvd, qr/class\s+RemoteViewerDialog/) &&
         greps($rvd, qr/RemoteViewer\s+_viewer/) &&
         greps($rvd, qr/PictureBox\s+_preview/) &&
         greps($rvd, qr/_previewTimer/);
row("STAGE4", "RemoteViewerDialog — 포트 입력 + Start/Stop + 미리보기 타이머",
    $a1?"PASS":"FAIL", $rvd);

my $st = "$HND_ROOT/Ui/Tabs/SettingsTab.cs";
my $a2 = greps($st, qr/settings\.remoteViewer/) && greps($st, qr/new\s+Dialogs\.RemoteViewerDialog/);
row("STAGE4", "SettingsTab — Remote Viewer 사이드바 등록", $a2?"PASS":"FAIL", $st);

# B. ActiveLotPage
my $alp = "$HND_ROOT/Ui/Pages/WorkInfo/ActiveLotPage.cs";
my $b1 = greps($alp, qr/class\s+ActiveLotPage/) &&
         greps($alp, qr/LotStorage\.ActiveLotChanged/) &&
         greps($alp, qr/BinDistribution/) &&
         greps($alp, qr/YieldPercent/);
row("STAGE4", "ActiveLotPage — Lot 통계 + Bin 분포 막대 + 이벤트 구독",
    $b1?"PASS":"FAIL", $alp);

my $wit = "$HND_ROOT/Ui/Tabs/WorkInfoTab.cs";
my $b2 = greps($wit, qr/wi\.activeLot/) && greps($wit, qr/new\s+ActiveLotPage/);
row("STAGE4", "WorkInfoTab — Active Lot 사이드바 등록", $b2?"PASS":"FAIL", $wit);

# C. SecsHost ↔ HsmsConnection 통합
my $sh = "$HND_ROOT/Equipment/Secs/SecsHost.cs";
my $c1 = greps($sh, qr/UseHsms/) &&
         greps($sh, qr/HandleHsmsClient/) &&
         greps($sh, qr/HandleHsmsMessage/) &&
         greps($sh, qr/SecsMessage\.Parse/) &&
         greps($sh, qr/SecsMessage\.S6F11/);
row("STAGE4", "SecsHost — UseHsms 모드 + HSMS handler + S6F11",
    $c1?"PASS":"FAIL", $sh);

# D. Cognex 진단 버튼
my $cfg = "$VIS_ROOT/Ui/Pages/ConfigurationPage.cs";
my $d1 = greps($cfg, qr/RunCognexTest/) &&
         greps($cfg, qr/SimCamera\(.*Sim\/Test/) &&
         greps($cfg, qr/CreatePatternFinder/);
row("STAGE4", "ConfigurationPage — Run Cognex test 버튼 + 실호출",
    $d1?"PASS":"FAIL", $cfg);

# E. runtime_cycle_test.pl
my $rct = "$ROOT/tools/runtime_cycle_test.pl";
my $e1 = greps($rct, qr/proc_running/) && greps($rct, qr/probe_write/) && greps($rct, qr/WaferVision.PING/);
row("STAGE4", "runtime_cycle_test.pl — 환경 + 기동 안정성 검증 스크립트",
    $e1?"PASS":"FAIL", $rct);

# F. csproj 등록
my $proj = "$HND_ROOT/QMC.CDT-320.csproj";
my $f1 = greps($proj, qr/RemoteViewerDialog\.cs/) &&
         greps($proj, qr/ActiveLotPage\.cs/);
row("STAGE4", "Handler csproj — RemoteViewerDialog + ActiveLotPage 등록",
    $f1?"PASS":"FAIL", $proj);

# G. i18n
my $lang = "$HND_ROOT/Ui/Localization/Lang.cs";
my $g1 = greps($lang, qr/"settings\.remoteViewer"/) &&
         greps($lang, qr/"wi\.activeLot"/);
row("STAGE4", "i18n — settings.remoteViewer + wi.activeLot 키",
    $g1?"PASS":"FAIL", $lang);

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
