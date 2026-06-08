#!perl
# run_runtime.pl — 런타임 verify 그룹 (옵트인). 코어 베이스라인(verify_all.pl)에서 분리됨.
#   이 그룹은 실행 환경이 필요하다:
#     - verify_comm.pl          : QMC.Vision.exe 를 spawn 하고 TCP 5100/5101/5103 라운드트립 검사
#     - verify_cognex_runtime.pl: QMC.Vision 프로세스 가동 여부 검사
#   따라서 CI/정적 베이스라인에 넣지 않고, 장비/실행 환경에서 수동(옵트인)으로만 돌린다.
use strict; use warnings; use FindBin;

my @runtime = qw( comm cognex_runtime );

print "=" x 90, "\n";
print "CDT-320 런타임 verify 그룹 (옵트인 — Vision.exe 실행/spawn 필요)\n";
print "=" x 90, "\n";

my $fail = 0;
foreach my $s (@runtime) {
    my $script = "$FindBin::Bin/verify_${s}.pl";
    if (! -e $script) { printf "%-22s (skip — script not found)\n", $s; next; }
    print "\n▶ verify_${s}.pl\n", "-" x 90, "\n";
    my $out = `perl "$script" 2>&1`;
    print $out;
    $fail = 1 if $? != 0;
}
print "\n", "=" x 90, "\n";
print $fail ? "RUNTIME 그룹: 일부 FAIL/환경미충족\n" : "RUNTIME 그룹: OK\n";
exit($fail ? 1 : 0);
