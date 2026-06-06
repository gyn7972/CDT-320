#!perl
# runtime_cycle_test.pl — Stage 4 런타임 환경 검증 (사이클 자체는 사용자 GUI 동작 필요)
# 본 스크립트는 다음을 자동 검증:
#   1) Handler/Vision 빌드 산출물 존재
#   2) 필요한 Config 파일 존재 또는 자동 생성 가능
#   3) Log 디렉토리 쓰기 가능
#   4) Handler exe 8초 안정 기동
#   5) Vision exe 8초 안정 기동 + Wafer PING 응답

use strict;
use warnings;
use IO::Socket::INET;

my $ROOT     = "D:/Work/CDT-320/QMC.CDT-320";
my $HND_EXE  = "$ROOT/QMC.CDT-320/bin/Debug/QMC.CDT-320.exe";
my $VIS_EXE  = "$ROOT/QMC.Vision/bin/Debug/QMC.Vision.exe";
my $HND_BIN  = "$ROOT/QMC.CDT-320/bin/Debug";
my $VIS_BIN  = "$ROOT/QMC.Vision/bin/Debug";

my @rows;
sub row { push @rows, [@_]; }

# 1) 빌드 산출물
row("BUILD",  "QMC.CDT-320.exe 존재",  -e $HND_EXE ? "PASS" : "FAIL", $HND_EXE);
row("BUILD",  "QMC.Vision.exe 존재",   -e $VIS_EXE ? "PASS" : "FAIL", $VIS_EXE);

# 2) Config 디렉토리
foreach my $f (qw(Config Recipes Log)) {
    my $p = "$HND_BIN/$f";
    if (! -d $p) { mkdir $p }
    row("ENV", "Handler $f/ 디렉토리", -d $p ? "PASS" : "FAIL", $p);
}
foreach my $f (qw(Config Recipes Log)) {
    my $p = "$VIS_BIN/$f";
    if (! -d $p) { mkdir $p }
    row("ENV", "Vision $f/ 디렉토리",  -d $p ? "PASS" : "FAIL", $p);
}

# 3) Log 쓰기 가능 여부 (probe)
sub probe_write {
    my ($dir) = @_;
    return 0 unless -d $dir;
    my $tmp = "$dir/_probe.tmp";
    open(my $fh, ">", $tmp) or return 0;
    print $fh "ok"; close $fh;
    unlink $tmp;
    return 1;
}
row("ENV", "Handler bin/Debug/Log 쓰기 가능",
    probe_write("$HND_BIN/Log") ? "PASS" : "FAIL", "$HND_BIN/Log");
row("ENV", "Vision bin/Debug/Log 쓰기 가능",
    probe_write("$VIS_BIN/Log")  ? "PASS" : "FAIL", "$VIS_BIN/Log");

# 4) Handler 기동 안정성 (이미 실행 중이면 죽이고 재기동)
sub kill_proc {
    my ($name) = @_;
    system("taskkill /F /IM $name 2>NUL >NUL");
}
kill_proc("QMC.CDT-320.exe");
kill_proc("QMC.Vision.exe");
sleep 2;

# 5) Vision 먼저 실행 (Handler 가 Vision 자동 연결)
print "Launching Vision... ";
system("start \"\" \"$VIS_EXE\"");
sleep 6;
print "Handler... ";
system("start \"\" \"$HND_EXE\"");
sleep 8;

sub proc_running {
    my ($name) = @_;
    # Windows tasklist 출력이 한국어 + 인코딩 문제로 unreliable.
    # PowerShell Get-Process 사용 — name 에서 .exe 제거해서 매칭.
    my $base = $name; $base =~ s/\.exe$//i;
    # PowerShell 따옴표 escape
    my $cmd = qq(powershell -NoProfile -Command "if (Get-Process -Name '$base' -ErrorAction SilentlyContinue) { 'YES' } else { 'NO' }");
    my $out = `$cmd 2>NUL`;
    return $out =~ /YES/;
}

row("RUNTIME", "Vision exe 8초 후 실행 중",
    proc_running("QMC.Vision.exe") ? "PASS" : "FAIL", "");
row("RUNTIME", "Handler exe 8초 후 실행 중",
    proc_running("QMC.CDT-320.exe") ? "PASS" : "FAIL", "");

# 6) Wafer PING
sub req {
    my ($p, $line) = @_;
    my $s = IO::Socket::INET->new(PeerAddr=>"127.0.0.1", PeerPort=>$p, Proto=>"tcp", Timeout=>3) or return undef;
    $s->autoflush(1); print $s "$line\n";
    my $r;
    eval { local $SIG{ALRM}=sub{die "tmo\n"}; alarm 4;
        while (defined(my $ln=<$s>)) { chomp $ln; next if $ln=~/^EPD\|/||$ln=~/^ARM\|/||$ln eq ""; $r=$ln; last; }
        alarm 0;
    };
    close $s; return $r;
}
my $ping = req(5100, "WaferVision|PING");
row("RUNTIME", "Vision Wafer PING ACK",
    (defined $ping && $ping =~ /^ACK\|/) ? "PASS" : "FAIL", $ping // "(no resp)");

# ── (Stage 18) GUI 자동화 + Cycle 결과 검증 ──
# 환경 변수 RUN_GUI_CYCLE=1 일 때만 실행 (시간 ~30초 소요)
my $runGui = $ENV{RUN_GUI_CYCLE} // "0";
if ($runGui eq "1") {
    print "[Stage 18] GUI Cycle 자동화 + 결과 검증...\n";

    # gui_cycle_automation.ps1 호출
    my $ps_script = "$ROOT/tools/gui_cycle_automation.ps1";
    my $cmd = qq(powershell -NoProfile -ExecutionPolicy Bypass -File "$ps_script" -WaitSeconds 25);
    system($cmd);

    sleep 5;  # Lot JSON 저장 여유

    # Lot log 확인
    my $lotDir = "$HND_BIN/Log/Lots";
    my @lotFiles = ();
    if (-d $lotDir) {
        opendir(my $dh, $lotDir);
        while (my $f = readdir $dh) {
            push @lotFiles, "$lotDir/$f" if $f =~ /\.json$/;
        }
        closedir $dh;
    }
    row("STAGE18", "Cycle 후 Lot JSON 1개 이상 생성",
        scalar(@lotFiles) > 0 ? "PASS" : "FAIL",
        "count=" . scalar(@lotFiles));

    # 가장 최신 Lot 검증
    if (@lotFiles) {
        my $latest = (sort { -M $a <=> -M $b } @lotFiles)[0];
        open my $lf, '<', $latest;
        local $/; my $content = <$lf>; close $lf;

        my $hasProcessed = $content =~ /"ProcessedDies":\s*(\d+)/;
        my $proc = $1 || 0;
        row("STAGE18", "Lot.ProcessedDies > 0",
            ($hasProcessed && $proc > 0) ? "PASS" : "FAIL", "ProcessedDies=$proc");

        my $hasState = $content =~ /"State":\s*"(Completed|Aborted|Running)"/;
        row("STAGE18", "Lot.State 설정됨",
            $hasState ? "PASS" : "FAIL", "state=" . ($1 // "?"));
    }
}

# 7) Cleanup
kill_proc("QMC.CDT-320.exe");
kill_proc("QMC.Vision.exe");

# 출력
my $bar = "=" x 110; my $sep = "-" x 110;
print "\n$bar\n";
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
