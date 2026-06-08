#!perl
# verify_all.pl — 코어 회귀 (Vision + 자동화 존재체크).
#   Handler 계열 36개는 tools/retired/ 로 격리(은퇴 백업 AI-Handler 스캐폴딩 기준, 팀 권위 Handler 와 불일치).
#   런타임(comm/cognex_runtime)은 별도 그룹 — run_runtime.pl 참조.
use strict; use warnings; use FindBin;

# 코어 정적 베이스라인 — 현행 트리(Vision 디자이너 스윕 반영) 대상만. Handler 빌드에 비의존.
my @stages = qw(
    stage2 stage4 stage6 stage52
    stage14 stage15 stage17 stage18
);

print "=" x 110, "\n";
print "CDT-320 통합 회귀 (verify_all)\n";
print "=" x 110, "\n";
printf "%-25s %-12s %-10s %-10s\n", "STAGE", "TOTAL", "PASS", "FAIL";
print "-" x 110, "\n";

my $totalPass = 0;
my $totalFail = 0;
my $totalAll  = 0;

foreach my $s (@stages) {
    my $script = "$FindBin::Bin/verify_${s}.pl";
    if (! -e $script) {
        printf "%-25s %-12s\n", $s, "(skip — script not found)";
        next;
    }
    my $out = `perl "$script" 2>&1`;
    my ($total, $pass, $fail) = (0, 0, 0);
    if ($out =~ /TOTAL\s+(\d+)\s+PASS\s+(\d+)\s+FAIL\s+(\d+)/) {
        $total = $1; $pass = $2; $fail = $3;
    }
    $totalAll  += $total;
    $totalPass += $pass;
    $totalFail += $fail;
    printf "%-25s %-12d %-10d %-10s\n", $s, $total, $pass,
        ($fail > 0 ? "$fail ✗" : "0");
}

print "-" x 110, "\n";
printf "%-25s %-12d %-10d %-10s\n", "TOTAL", $totalAll, $totalPass,
    ($totalFail > 0 ? "$totalFail ✗" : "0 ✓");
print "=" x 110, "\n";

# Vision 정적
my $vis = "$FindBin::Bin/verify_vision_features.pl";
if (-e $vis) {
    print "\nVision (정적 only — Vision exe 미실행 시 1 SKIP):\n";
    my $vout = `perl "$vis" 2>&1 | tail -1`;
    print "  $vout";
}

exit ($totalFail > 0 ? 1 : 0);
