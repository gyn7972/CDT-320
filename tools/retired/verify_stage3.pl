#!perl
# verify_stage3.pl — Stage 3 정적 검증 (RemoteViewer + IonizerSensor + Reject + Lot + 5 Interlock + HSMS)

use strict;
use warnings;

use FindBin; my $ROOT = "$FindBin::Bin/..";
my $HND_ROOT = "$ROOT/QMC.CDT-320";
my $HND_EXE  = "$HND_ROOT/bin/Debug/QMC.CDT-320.exe";

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
row("BUILD", "QMC.CDT-320.exe (Stage 3 후)", -e $HND_EXE ? "PASS" : "FAIL", "");

# A. RemoteViewer
my $rv = "$HND_ROOT/Equipment/Remote/RemoteViewer.cs";
my $a1 = greps($rv, qr/class\s+RemoteViewer/) &&
         greps($rv, qr/AcceptLoop/) && greps($rv, qr/CaptureLoop/) &&
         greps($rv, qr/DrawToBitmap/);
row("STAGE3", "RemoteViewer — TCP listener + 화면 캡처 + base64 송신", $a1?"PASS":"FAIL", $rv);

# B. IonizerSensor
my $is = "$HND_ROOT/Equipment/Sensors/IonizerSensor.cs";
my $b1 = greps($is, qr/class\s+IonizerSensor/) &&
         greps($is, qr/AlarmManager\.Raise.*IONIZER/) &&
         greps($is, qr/PollIntervalMs/);
row("STAGE3", "IonizerSensor — DI polling + 알람 발생기", $b1?"PASS":"FAIL", $is);

# C. SubPortMaterialRejector
my $rej = "$HND_ROOT/Equipment/Bin/SubPortMaterialRejector.cs";
my $c1 = greps($rej, qr/class\s+RejectConfig/) &&
         greps($rej, qr/static\s+class\s+SubPortMaterialRejector/) &&
         greps($rej, qr/ShouldReject/) &&
         greps($rej, qr/reject_config\.json/);
row("STAGE3", "SubPortMaterialRejector — BinCode 거부 분리 + JSON", $c1?"PASS":"FAIL", $rej);

# D. Lot
my $lot   = "$HND_ROOT/Equipment/Lots/Lot.cs";
my $lotst = "$HND_ROOT/Equipment/Lots/LotStorage.cs";
my $d1 = greps($lot, qr/enum\s+LotState/) &&
         greps($lot, qr/class\s+Lot\b/) &&
         greps($lot, qr/RecordDie/) &&
         greps($lot, qr/YieldPercent/) &&
         greps($lotst, qr/static\s+class\s+LotStorage/) &&
         greps($lotst, qr/OpenLot/) && greps($lotst, qr/CloseLot/);
row("STAGE3", "Lot + LotStorage (Open/Close/RecordDie + JSON 저장)", $d1?"PASS":"FAIL", $lotst);

my $mc = "$HND_ROOT/Equipment/MachineController.cs";
my $d2 = greps($mc, qr/LotStorage\.OpenLot/) &&
         greps($mc, qr/LotStorage\.CloseLot/) &&
         greps($mc, qr/LotStorage\.ActiveLot.*RecordDie/) &&
         greps($mc, qr/SubPortMaterialRejector\.ShouldReject/);
row("STAGE3", "MachineController — Lot lifecycle + Reject 통합", $d2?"PASS":"FAIL", $mc);

# E. 5 Interlock 추가
my $ext = "$HND_ROOT/Equipment/Interlocks/ExtendedInterlocks.cs";
my $e1 = greps($ext, qr/class\s+EjectVsStageInterlock/) &&
         greps($ext, qr/class\s+LoaderVsStageInterlock/) &&
         greps($ext, qr/class\s+UnloaderVsStageInterlock/) &&
         greps($ext, qr/class\s+EjectVsPickerInterlock/) &&
         greps($ext, qr/class\s+BinGuideVsPickerInterlock/);
row("STAGE3", "Extended 5 Interlock (Eject/Loader/Unloader/EjectPicker/BinGuide)",
    $e1?"PASS":"FAIL", $ext);

# F. SECS HSMS
my $hsms = "$HND_ROOT/Equipment/Secs/HsmsConnection.cs";
my $msg  = "$HND_ROOT/Equipment/Secs/SecsMessage.cs";
my $f1 = greps($hsms, qr/class\s+HsmsConnection/) &&
         greps($hsms, qr/4-byte/) &&
         greps($hsms, qr/ReadExactly/) &&
         greps($msg, qr/class\s+SecsMessage/) &&
         greps($msg, qr/ToBytes/) && greps($msg, qr/static\s+SecsMessage\s+Parse/) &&
         greps($msg, qr/S6F11/);
row("STAGE3", "SECS HSMS — Connection + Message (S/F + System bytes)", $f1?"PASS":"FAIL", $hsms);

# csproj 등록
my $proj = "$HND_ROOT/QMC.CDT-320.csproj";
my $g1 = greps($proj, qr/RemoteViewer\.cs/) &&
         greps($proj, qr/IonizerSensor\.cs/) &&
         greps($proj, qr/SubPortMaterialRejector\.cs/) &&
         greps($proj, qr/Equipment\\Lots\\Lot\.cs/) &&
         greps($proj, qr/LotStorage\.cs/) &&
         greps($proj, qr/ExtendedInterlocks\.cs/) &&
         greps($proj, qr/HsmsConnection\.cs/) &&
         greps($proj, qr/SecsMessage\.cs/);
row("STAGE3", "csproj — Stage 3 9 신규 .cs 등록", $g1?"PASS":"FAIL", $proj);

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
