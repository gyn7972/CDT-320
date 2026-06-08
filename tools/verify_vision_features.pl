#!perl
# verify_vision_features.pl — 320 Vision 의 310 이식 기능 자동 검증.
# 정적 grep + 런타임 TCP 라운드트립 (PASS/FAIL 출력).

use strict;
use warnings;
use IO::Socket::INET;

use FindBin; my $ROOT = "$FindBin::Bin/..";
my $VIS_ROOT  = "$ROOT/QMC.Vision";
my $HND_ROOT  = "$ROOT/QMC.CDT-320";
my $VIS_EXE   = "$VIS_ROOT/bin/Debug/QMC.Vision.exe";
my $HND_EXE   = "$HND_ROOT/bin/Debug/QMC.CDT-320.exe";

my @rows;
sub row { push @rows, [@_]; }

# ── 0. Build ──────────────────────────────
row("BUILD",  "QMC.Vision.exe (310 이식 후 빌드)",  -e $VIS_EXE ? "PASS" : "FAIL", "");
row("BUILD",  "QMC.CDT-320.exe (Handler 빌드)",      -e $HND_EXE ? "PASS" : "FAIL", "");

# ── 1. 정적 검사 ─────────────────────────
sub greps {
    my ($file, $pat) = @_;
    return 0 unless -e $file;
    open my $fh, '<', $file or return 0;
    local $/; my $c = <$fh>; close $fh;
    return $c =~ /$pat/s ? 1 : 0;
}

my $cv = "$VIS_ROOT/Core/CameraVector.cs";
row("STATIC", "CameraVector — InvertedX/Y + IsRotated 필드", greps($cv, qr/InvertedX.*InvertedY.*IsRotated/) ? "PASS" : "FAIL", $cv);

my $vs = "$VIS_ROOT/Core/VisionScale.cs";
row("STATIC", "VisionScale.ConvertPosition — pixel→mm 변환 함수",
    greps($vs, qr/static\s+void\s+ConvertPosition/) ? "PASS" : "FAIL", $vs);

my $vc = "$VIS_ROOT/Config/VisionConfig.cs";
my $cfg_ok = greps($vc, qr/ScaleX/) && greps($vc, qr/ScaleY/) && greps($vc, qr/IsRotated/) &&
             greps($vc, qr/InvertedX/) && greps($vc, qr/InvertedY/) && greps($vc, qr/DelayBeforeGrabMs/) &&
             greps($vc, qr/SideLocation/) && greps($vc, qr/OffAfterGrabWhenAutoFocus/) &&
             greps($vc, qr/ReturnMmCoordinates/) && greps($vc, qr/DataLogPath/);
row("STATIC", "VisionConfig — 좌표/지연/측면/오토포커스/데이터로그 키 10종",
    $cfg_ok ? "PASS" : "FAIL", $vc);

my $mt = "$VIS_ROOT/Core/MaterialTracker.cs";
my $mt_ok = greps($mt, qr/class\s+DieRecord/) && greps($mt, qr/static\s+class\s+MaterialTracker/) &&
            greps($mt, qr/ApplyBottom/) && greps($mt, qr/ApplySide/) && greps($mt, qr/ApplyDieGap/);
row("STATIC", "MaterialTracker — DieRecord + ApplyBottom/Side/DieGap",
    $mt_ok ? "PASS" : "FAIL", $mt);

my $ip = "$VIS_ROOT/Core/Inspectors/InspectionParameters.cs";
my $ip_ok = greps($ip, qr/class\s+BottomInspectionParameters/) &&
            greps($ip, qr/class\s+SideInspectionParameters/) &&
            greps($ip, qr/class\s+DieGapInspectionParameters/) &&
            greps($ip, qr/class\s+DistortionParameters/) &&
            greps($ip, qr/class\s+VisionScaleParameters/) &&
            greps($ip, qr/enum\s+ChipType/) &&
            greps($ip, qr/enum\s+SideSurface/);
row("STATIC", "InspectionParameters — 5종 클래스 + ChipType + SideSurface enum",
    $ip_ok ? "PASS" : "FAIL", $ip);

my $vm = "$VIS_ROOT/Modules/VisionModule.cs";
my $vm_ok = greps($vm, qr/event\s+Action<string>\s+ExposureDone/) &&
            greps($vm, qr/event\s+Action<string,\s*string>\s+Alarmed/) &&
            greps($vm, qr/Calibrate\s*\(/) &&
            greps($vm, qr/MeasureRotationalCenter\s*\(/) &&
            greps($vm, qr/LearnDistortion\s*\(/) &&
            greps($vm, qr/MeasureFocus\s*\(/);
row("STATIC", "VisionModule — EPD/ARM 이벤트 + 4 신규 메서드 (Calibrate/RotCenter/Distort/Focus)",
    $vm_ok ? "PASS" : "FAIL", $vm);

my $ts = "$VIS_ROOT/Comm/VisionTcpServer.cs";
my $ts_ok = greps($ts, qr/case\s+"SCALE"/) &&
            greps($ts, qr/case\s+"ROT_CENTER"/) &&
            greps($ts, qr/case\s+"DISTORT"/) &&
            greps($ts, qr/case\s+"CAM_SWITCH"/) &&
            greps($ts, qr/case\s+"FOCUS_VAL"/) &&
            greps($ts, qr/Broadcast\s*\(/) &&
            greps($ts, qr/OnExposureDone/) &&
            greps($ts, qr/OnAlarmed/);
row("STATIC", "VisionTcpServer — 5 신규 명령 + Broadcast + EPD/ARM 핸들러",
    $ts_ok ? "PASS" : "FAIL", $ts);

my $is = "$VIS_ROOT/Core/ImageLogSaver.cs";
row("STATIC", "ImageLogSaver — chipUid 디렉토리 PNG 저장",
    greps($is, qr/static\s+void\s+Save/) && greps($is, qr/Manual/) ? "PASS" : "FAIL", $is);

my $ds = "$VIS_ROOT/Core/DataLogSaver.cs";
my $ds_ok = greps($ds, qr/SaveIfDieGapComplete/) &&
            greps($ds, qr/Material_ID/) &&
            greps($ds, qr/Back_Chipping_Top_Size/) &&
            greps($ds, qr/Side_Chipping_Length/) &&
            greps($ds, qr/Post_Place_Gap_UpperLimit/);
row("STATIC", "DataLogSaver — 30+ 칼럼 일자별 CSV", $ds_ok ? "PASS" : "FAIL", $ds);

my $vc2 = "$HND_ROOT/Equipment/Vision/VisionTcpClient.cs";
my $vc2_ok = greps($vc2, qr/event\s+Action<string>\s+ExposureDone/) &&
             greps($vc2, qr/event\s+Action<string,\s*string>\s+Alarmed/) &&
             greps($vc2, qr/StartsWith\("EPD\|"\)/) &&
             greps($vc2, qr/StartsWith\("ARM\|"\)/);
row("STATIC", "Handler VisionTcpClient — EPD/ARM 비동기 이벤트 수신",
    $vc2_ok ? "PASS" : "FAIL", $vc2);

# ── 2. 런타임 검사 (Vision exe 가 띄워져 있어야 함) ────
sub req {
    my ($host, $port, $line, $timeout) = @_;
    $timeout //= 3;
    my $sock = IO::Socket::INET->new(
        PeerAddr => $host, PeerPort => $port,
        Proto => 'tcp', Timeout => $timeout);
    return undef unless $sock;
    $sock->autoflush(1);
    print $sock "$line\n";
    my $r;
    eval {
        local $SIG{ALRM} = sub { die "timeout\n" };
        alarm $timeout;
        # EPD|...  / ARM|...  비동기 푸시는 건너뜀.  ACK|.. 또는 ERR|.. 만 응답으로 간주.
        while (defined(my $ln = <$sock>)) {
            chomp $ln;
            next if $ln =~ /^EPD\|/ || $ln =~ /^ARM\|/ || $ln eq "";
            $r = $ln;
            last;
        }
        alarm 0;
    };
    close $sock;
    return $r;
}

my $running = 0;
{
    my $r = req("127.0.0.1", 5100, "WaferVision|PING");
    $running = (defined $r && $r =~ /^ACK\|/) ? 1 : 0;
}

if (!$running) {
    row("RUNTIME", "Vision exe 실행 (PING)", "SKIP", "QMC.Vision.exe 실행되어 있지 않음 — 런타임 검사 건너뜀");
} else {
    my @runtime_tests = (
        ["WaferVision|PING",                                 qr/^ACK\|.*OK/,       "PING"],
        ["WaferVision|EXPOSE|0",                             qr/^ACK\|.*w=/,        "EXPOSE"],
        ["WaferVision|MATCH|ReticleFinder|0",                qr/^ACK\|.*OK/,        "MATCH"],
        ["WaferVision|SCALE|1.0|1.0",                        qr/^ACK\|.*scaleX=/,   "SCALE"],
        ["WaferVision|ROT_CENTER",                           qr/^ACK\|.*OK/,        "ROT_CENTER"],
        ["WaferVision|DISTORT",                              qr/^ACK\|/,            "DISTORT"],
        ["WaferVision|CAM_SWITCH|toolA|true",                qr/^ACK\|.*tool=/,     "CAM_SWITCH"],
        ["WaferVision|FOCUS_VAL",                            qr/^ACK\|.*OK;Left top/, "FOCUS_VAL"],
        ["BinVision|INSPECT|PlacementInspector|TestUid001",  qr/^ACK\|.*PASS/,      "INSPECT (Bin) + Material 누적"],
    );
    foreach my $t (@runtime_tests) {
        my $r = req("127.0.0.1", 5100, $t->[0]);
        # MATCH/SCALE/ROT_CENTER/DISTORT/CAM_SWITCH/FOCUS_VAL 은 wafer 포트 5100
        # INSPECT (Bin) 은 5103
        if ($t->[2] eq "INSPECT (Bin) + Material 누적") {
            $r = req("127.0.0.1", 5103, $t->[0]);
        }
        my $ok = (defined $r && $r =~ $t->[1]) ? "PASS" : "FAIL";
        row("RUNTIME", "Vision/Wafer $t->[2]", $ok, $r // "(no response)");
    }
}

# ── 3. 출력 ────────────────────────────────
my $bar = "=" x 110;
my $sep = "-" x 110;
print "$bar\n";
printf "%-9s %-65s %-6s %s\n", "CATEGORY", "ITEM", "RESULT", "DETAIL";
print "$sep\n";
my ($pass, $fail, $skip) = (0,0,0);
foreach my $r (@rows) {
    my ($c, $i, $res, $d) = @$r;
    printf "%-9s %-65s %-6s %s\n", $c, $i, $res, ($d // "");
    $pass++ if $res eq "PASS";
    $fail++ if $res eq "FAIL";
    $skip++ if $res eq "SKIP";
}
print "$bar\n";
my $total = scalar @rows;
print "TOTAL $total   PASS $pass   SKIP $skip   FAIL $fail\n";
exit ($fail > 0 ? 1 : 0);
