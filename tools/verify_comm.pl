#!/usr/bin/perl
# CDT-320 통신 자동 검증 스크립트
# 사용: perl verify_comm.pl
#  1) 빌드 확인 (exe 존재)
#  2) 정적 코드 분석 — 핵심 연결점 grep 검사
#  3) QMC.Vision.exe 시작 후 TCP 5100/5101/5103 프로토콜 라운드트립
#  4) CDT320Simulator.exe 시작 후 TCP 7001 JSON 명령 라운드트립
#  5) 결과 표 출력

use strict;
use warnings;
use IO::Socket::INET;

use FindBin; my $ROOT = "$FindBin::Bin/..";
my $SOL        = "$ROOT/QMC.CDT-320";
my $HANDLER    = "$SOL/QMC.CDT-320/bin/Debug/QMC.CDT-320.exe";
my $VISION     = "$SOL/QMC.Vision/bin/Debug/QMC.Vision.exe";
my $SIM        = "$ROOT/CDT320Simulator/bin/Debug/net472/CDT320Simulator.exe";

my @results;
sub record {
    my ($cat, $name, $ok, $detail) = @_;
    push @results, [ $cat, $name, $ok ? "PASS" : "FAIL", $detail // "" ];
}

# ─────────────────────────────────
#  1. 빌드 확인
# ─────────────────────────────────
sub check_build {
    record("BUILD", "QMC.Common.dll",     -f "$SOL/QMC.Common/bin/Debug/QMC.Common.dll", "$SOL/QMC.Common/bin/Debug/QMC.Common.dll");
    record("BUILD", "QMC.CDT-320.exe",    -f $HANDLER, $HANDLER);
    record("BUILD", "QMC.Vision.exe",     -f $VISION,  $VISION);
    record("BUILD", "CDT320Simulator.exe",-f $SIM,     $SIM);
}

# ─────────────────────────────────
#  2. 정적 코드 검사
# ─────────────────────────────────
sub grep_file {
    my ($path, $pattern) = @_;
    open my $fh, "<", $path or return 0;
    while (<$fh>) { if (/$pattern/) { close $fh; return 1; } }
    close $fh; return 0;
}

sub check_static {
    my $f = "$SOL/QMC.CDT-320/Equipment/Vision/VisionTcpClient.cs";
    record("STATIC", "VisionTcpClient PingAsync", grep_file($f, qr/public async Task<bool> PingAsync/), $f);
    record("STATIC", "VisionTcpClient MatchAsync", grep_file($f, qr/public async Task<MatchResultDto> MatchAsync/), $f);
    record("STATIC", "VisionTcpClient InspectAsync", grep_file($f, qr/public async Task<InspectionResultDto> InspectAsync/), $f);

    $f = "$SOL/QMC.CDT-320/Equipment/Vision/VisionHub.cs";
    record("STATIC", "VisionHub Wafer/Inspection/Bin", grep_file($f, qr/public static VisionTcpClient (Wafer|Inspection|Bin)/), $f);
    record("STATIC", "VisionHub ConnectAllAsync",       grep_file($f, qr/ConnectAllAsync/), $f);

    $f = "$SOL/QMC.CDT-320/Equipment/Vision/VisionAdapters.cs";
    record("STATIC", "WaferVisionAdapter implements IVisionTcpClient", grep_file($f, qr/WaferVisionAdapter\s*:\s*IVisionTcpClient/), $f);
    record("STATIC", "TpuVisionAdapter implements IVisionTpuClient",   grep_file($f, qr/TpuVisionAdapter\s*:\s*IVisionTpuClient/), $f);

    $f = "$SOL/QMC.CDT-320/Equipment/CDT320Machine.cs";
    record("STATIC", "Machine uses WaferVisionAdapter (not Null)", grep_file($f, qr/new VisionComm\.WaferVisionAdapter\(\)/), $f);
    record("STATIC", "Machine uses TpuVisionAdapter (not Null)",    grep_file($f, qr/new VisionComm\.TpuVisionAdapter\(\)/),   $f);

    $f = "$SOL/QMC.CDT-320/Equipment/SimulatorBridge.cs";
    record("STATIC", "SimulatorBridge ConnectAsync(host,port)", grep_file($f, qr/public async Task ConnectAsync\(string host, int port\)/), $f);

    $f = "$SOL/QMC.CDT-320/Equipment/MachineController.cs";
    record("STATIC", "CycleRun calls VisionHub.Wafer.MatchAsync", grep_file($f, qr/VisionHub\.Wafer\.MatchAsync/), $f);
    record("STATIC", "CycleRun calls VisionHub.Inspection",        grep_file($f, qr/VisionHub\.Inspection/), $f);

    $f = "$SOL/QMC.CDT-320/Form1.cs";
    record("STATIC", "Form1 calls VisionHub.ConnectAllAsync",       grep_file($f, qr/VisionHub\.ConnectAllAsync/), $f);
    record("STATIC", "Form1 subscribes VisionHub.ConnectionChanged",grep_file($f, qr/VisionHub\.ConnectionChanged/), $f);

    $f = "$SOL/QMC.CDT-320/Ui/Dialogs/SystemSelfTestDialog.cs";
    record("STATIC", "SelfTest registers Vision tests", grep_file($f, qr/PingVision/), $f);

    # CDT320Simulator HELLO support
    $f = "$ROOT/CDT320Simulator/MainWindow.xaml.cs";
    record("STATIC", "Simulator handles HELLO command", grep_file($f, qr/case "HELLO"/), $f);
    record("STATIC", "Simulator handles AXIS_MOVE",     grep_file($f, qr/case "AXIS_MOVE"/), $f);
    record("STATIC", "Simulator handles DO_SET",        grep_file($f, qr/case "DO_SET"/), $f);
    record("STATIC", "Simulator handles DI_GET_ALL",    grep_file($f, qr/case "DI_GET_ALL"/), $f);
}

# ─────────────────────────────────
#  3. Vision TCP 라운드트립
# ─────────────────────────────────
sub start_proc {
    my ($exe) = @_;
    return undef unless -f $exe;
    my $pid;
    if ($^O =~ /MSWin/ || -e "/cygdrive") { $pid = open(my $h, "| $exe 2>nul") or return undef; }
    my $real = fork();
    if ($real == 0) { exec($exe) or exit 1; }
    return $real;
}

sub tcp_send_recv {
    my ($host, $port, $msg, $timeout) = @_;
    $timeout //= 2;
    my $sock = IO::Socket::INET->new(PeerAddr=>$host, PeerPort=>$port, Proto=>"tcp", Timeout=>$timeout);
    return undef unless $sock;
    print $sock $msg . "\n";
    $sock->autoflush(1);
    my $resp;
    eval {
        local $SIG{ALRM} = sub { die "timeout\n"; };
        alarm($timeout);
        $resp = <$sock>;
        alarm(0);
    };
    close $sock;
    chomp $resp if defined $resp;
    return $resp;
}

sub tcp_send_recv_json {
    my ($host, $port, $jsonReq, $timeout) = @_;
    $timeout //= 2;
    my $sock = IO::Socket::INET->new(PeerAddr=>$host, PeerPort=>$port, Proto=>"tcp", Timeout=>$timeout);
    return undef unless $sock;
    print $sock $jsonReq . "\n";
    $sock->autoflush(1);
    my $resp;
    eval {
        local $SIG{ALRM} = sub { die "timeout\n"; };
        alarm($timeout);
        $resp = <$sock>;
        alarm(0);
    };
    close $sock;
    chomp $resp if defined $resp;
    return $resp;
}

sub check_vision_runtime {
    return record("RUNTIME", "Vision exe not found", 0, $VISION) unless -f $VISION;

    # Spawn QMC.Vision.exe in background
    my $pid = fork();
    if (!defined $pid) { return record("RUNTIME", "fork fail", 0, ""); }
    if ($pid == 0) { exec($VISION) or exit 1; }
    sleep 3;  # 워밍업

    my $r;
    $r = tcp_send_recv("127.0.0.1", 5100, "WaferVision|PING", 3);
    record("RUNTIME", "Vision/Wafer PING",
        defined($r) && $r =~ /^ACK\|WaferVision\|PING\|OK/, $r // "(no response)");

    $r = tcp_send_recv("127.0.0.1", 5100, "WaferVision|EXPOSE|0", 3);
    record("RUNTIME", "Vision/Wafer EXPOSE",
        defined($r) && $r =~ /^ACK\|WaferVision\|EXPOSE\|w=\d+/, $r // "");

    $r = tcp_send_recv("127.0.0.1", 5100, "WaferVision|MATCH|ReticleFinder|0", 3);
    record("RUNTIME", "Vision/Wafer MATCH ReticleFinder",
        defined($r) && $r =~ /^ACK\|WaferVision\|MATCH\|OK;x=/, $r // "");

    $r = tcp_send_recv("127.0.0.1", 5101, "BottomInspection|EXPOSE|0", 3);
    record("RUNTIME", "Vision/BottomInspection EXPOSE",
        defined($r) && $r =~ /^ACK\|BottomInspection\|EXPOSE\|/, $r // "");

    $r = tcp_send_recv("127.0.0.1", 5101, "BottomInspection|INSPECT|SurfaceInspector|0", 3);
    record("RUNTIME", "Vision/BottomInspection INSPECT SurfaceInspector",
        defined($r) && $r =~ /^ACK\|BottomInspection\|INSPECT\|/, $r // "");

    $r = tcp_send_recv("127.0.0.1", 5103, "BinVision|INSPECT|PlacementInspector|0", 3);
    record("RUNTIME", "Vision/Bin INSPECT PlacementInspector",
        defined($r) && $r =~ /^ACK\|BinVision\|INSPECT\|(PASS|FAIL)/, $r // "");

    # 알 수 없는 명령
    $r = tcp_send_recv("127.0.0.1", 5100, "WaferVision|FOO", 3);
    record("RUNTIME", "Vision unknown command rejected",
        defined($r) && $r =~ /^ERR\|/, $r // "");

    kill 'TERM', $pid;
    waitpid($pid, 0);
}

# ─────────────────────────────────
#  4. Simulator TCP 라운드트립
#    (CDT320Simulator 는 WPF 라 GUI 환경 필요. 가능한 경우만 실행.)
# ─────────────────────────────────
sub check_simulator_runtime {
    return record("RUNTIME", "Simulator exe not found", 0, $SIM) unless -f $SIM;

    my $pid = fork();
    return record("RUNTIME", "Simulator fork fail", 0, "") unless defined $pid;
    if ($pid == 0) { exec($SIM) or exit 1; }

    # WPF 시작 + TCP START 버튼은 사람 클릭 필요. 4초 대기.
    sleep 4;

    # Sim 의 TCP 서버는 START 버튼 누른 후만 활성화. 자동 시작 안 됨.
    # 그래서 단순히 connect 시도해 보고 결과 기록.
    my $sock = IO::Socket::INET->new(PeerAddr=>"127.0.0.1", PeerPort=>7001, Proto=>"tcp", Timeout=>1);
    if ($sock) {
        close $sock;
        # JSON 명령 테스트
        my $r = tcp_send_recv_json("127.0.0.1", 7001, '{"cmd":"DI_GET","port":"X007"}', 2);
        record("RUNTIME", "Simulator DI_GET",
            defined($r) && $r =~ /"evt":"DI_STATE"/, $r // "");

        $r = tcp_send_recv_json("127.0.0.1", 7001, '{"cmd":"DO_SET","port":"Y003","val":1}', 2);
        record("RUNTIME", "Simulator DO_SET",
            defined($r) && $r =~ /"evt":"ACK"/, $r // "");

        $r = tcp_send_recv_json("127.0.0.1", 7001, '{"cmd":"AXIS_MOVE","axis":9,"pos":300.0,"vel":500.0}', 2);
        record("RUNTIME", "Simulator AXIS_MOVE",
            defined($r) && $r =~ /"evt":"ACK"/, $r // "");

        $r = tcp_send_recv_json("127.0.0.1", 7001, '{"cmd":"HELLO","role":"master","from":"verify"}', 2);
        record("RUNTIME", "Simulator HELLO master",
            defined($r) && $r =~ /"evt":"ACK".*"cmd":"HELLO"/, $r // "");
    } else {
        record("RUNTIME", "Simulator TCP server (port 7001)",
            0, "사용자가 시뮬레이터에서 [TCP START] 버튼을 눌러야 활성화됨");
    }

    kill 'TERM', $pid;
    waitpid($pid, 0);
}

# ─────────────────────────────────
#  5. 출력
# ─────────────────────────────────
sub print_results {
    my ($total, $pass, $fail) = (0, 0, 0);
    print "\n";
    print "=" x 110, "\n";
    printf "%-10s %-55s %-6s %s\n", "CATEGORY", "ITEM", "RESULT", "DETAIL";
    print "-" x 110, "\n";
    for my $r (@results) {
        my ($c, $n, $ok, $d) = @$r;
        $total++; if ($ok eq "PASS") { $pass++ } else { $fail++ }
        my $detail = (length($d // "") > 40) ? substr($d, 0, 37) . "..." : ($d // "");
        printf "%-10s %-55s %-6s %s\n", $c, $n, $ok, $detail;
    }
    print "=" x 110, "\n";
    printf "TOTAL %d   PASS %d   FAIL %d\n\n", $total, $pass, $fail;
    return ($total, $pass, $fail);
}

# ─────────────────────────────────
#  실행
# ─────────────────────────────────
print "▶ 1. Build check\n";
check_build();
print "▶ 2. Static analysis\n";
check_static();
print "▶ 3. Vision runtime test (starts QMC.Vision.exe)\n";
check_vision_runtime();
print "▶ 4. Simulator runtime test (starts CDT320Simulator.exe)\n";
check_simulator_runtime();
my ($t, $p, $f) = print_results();
exit($f ? 1 : 0);
