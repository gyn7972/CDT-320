#!perl
# verify_all.pl — 모든 단계 통합 회귀
use strict; use warnings; use FindBin;

my @stages = qw(
    handler_features
    stage2 stage3 stage4 stage5 stage6 stage7 stage8
    stage9 stage10
    stage11 stage12 stage13
    stage14 stage15 stage16 stage17 stage18 stage19 stage20 stage21 stage22 stage23
    stage24 stage25
    stage26 stage27 stage28 stage29 stage30 stage31 stage32
    stage43 stage44 stage45 stage46 stage47 stage48
    stage49 stage50 stage51 stage52 stage53 stage54
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
