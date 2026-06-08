#!perl
# verify_cognex_runtime.pl — Cognex 동글 활성화 후 실 검증
# Vision exe 가동 → TRAIN/MATCH 5회 반복 → 안정성 측정

use strict; use warnings;
use IO::Socket::INET;

use FindBin; my $ROOT = "$FindBin::Bin/..";
my $VIS_EXE  = "$ROOT/QMC.Vision/bin/Debug/QMC.Vision.exe";
my @rows;
sub row { push @rows, [@_] }

# 1) Vision exe 기동 확인
my $running = 0;
my $check = `powershell -NoProfile -Command "if (Get-Process -Name 'QMC.Vision' -ErrorAction SilentlyContinue) { 'YES' } else { 'NO' }" 2>NUL`;
$running = $check =~ /YES/;

if (!$running) {
    print "Vision exe launching...\n";
    system("start \"\" \"$VIS_EXE\"");
    sleep 6;
    $check = `powershell -NoProfile -Command "if (Get-Process -Name 'QMC.Vision' -ErrorAction SilentlyContinue) { 'YES' } else { 'NO' }" 2>NUL`;
    $running = $check =~ /YES/;
}
row("ENV", "Vision exe 실행 중", $running ? "PASS" : "FAIL", "");
unless ($running) {
    print "Vision exe 미실행 — 종료\n";
    exit 1;
}

# 2) PING
sub req {
    my ($p, $line, $timeout) = @_; $timeout //= 4;
    my $s = IO::Socket::INET->new(PeerAddr=>"127.0.0.1", PeerPort=>$p, Proto=>"tcp", Timeout=>3) or return undef;
    $s->autoflush(1); print $s "$line\n";
    my $r;
    eval { local $SIG{ALRM}=sub{die "tmo\n"}; alarm $timeout;
        while (defined(my $ln=<$s>)) { chomp $ln; next if $ln=~/^EPD\|/||$ln=~/^ARM\|/||$ln eq ""; $r=$ln; last; }
        alarm 0;
    };
    close $s; return $r;
}

my $ping = req(5100, "WaferVision|PING");
row("RUNTIME", "Vision Wafer PING ACK",
    (defined $ping && $ping =~ /^ACK\|.*OK/) ? "PASS" : "FAIL", $ping // "");

# 3) TRAIN
my $train = req(5100, "WaferVision|TRAIN|ReticleFinder");
row("RUNTIME", "Cognex TRAIN ReticleFinder ACK",
    (defined $train && $train =~ /^ACK\|.*OK/) ? "PASS" : "FAIL", $train // "");

# 4) MATCH 5회 — score 분산 측정
my @scores;
my @xs;
my @ys;
for (my $i = 0; $i < 5; $i++) {
    my $r = req(5100, "WaferVision|MATCH|ReticleFinder|TestUid$i");
    if (defined $r && $r =~ /score=([\d\.]+)/) {
        push @scores, $1 + 0;
    }
    if (defined $r && $r =~ /x=([\d\.\-]+);y=([\d\.\-]+)/) {
        push @xs, $1 + 0;
        push @ys, $2 + 0;
    }
}

row("RUNTIME", "MATCH 5회 응답 받음",
    (scalar @scores == 5) ? "PASS" : "FAIL", "responses=" . scalar @scores);

if (@scores >= 5) {
    my $avg = 0; $avg += $_ for @scores; $avg /= scalar @scores;
    my $var = 0; $var += ($_ - $avg) ** 2 for @scores; $var /= scalar @scores;
    my $stdev = sqrt($var);
    row("RUNTIME", "MATCH score 평균 ≥ 0.7", $avg >= 0.7 ? "PASS" : "FAIL", sprintf("avg=%.3f", $avg));
    row("RUNTIME", "MATCH score 표준편차 (Cognex 안정 기대)",
        "INFO", sprintf("stdev=%.4f scores=[%s]", $stdev, join(",", map { sprintf "%.3f", $_ } @scores)));

    if (@xs >= 5) {
        my $xavg = 0; $xavg += $_ for @xs; $xavg /= scalar @xs;
        my $yavg = 0; $yavg += $_ for @ys; $yavg /= scalar @ys;
        # 이미지 중앙 (320,240) 부근
        my $xnear = abs($xavg - 320) < 50;
        my $ynear = abs($yavg - 240) < 50;
        row("RUNTIME", "MATCH 좌표 이미지 중앙 근처",
            ($xnear && $ynear) ? "PASS" : "FAIL", sprintf("avg=(%.1f,%.1f)", $xavg, $yavg));
    }
}

# 출력
my $bar="="x110;
print "$bar\n";
printf "%-9s %-65s %-6s %s\n","CATEGORY","ITEM","RESULT","DETAIL";
print "-"x110,"\n";
my ($pass,$fail)=(0,0);
foreach my $r (@rows){ my ($c,$i,$res,$d)=@$r; printf "%-9s %-65s %-6s %s\n",$c,$i,$res,($d//""); $pass++ if $res eq "PASS"; $fail++ if $res eq "FAIL"; }
print "$bar\n";
print "TOTAL ${\scalar @rows}   PASS $pass   FAIL $fail\n";
exit ($fail>0?1:0);
