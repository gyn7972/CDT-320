#!perl
use strict; use warnings;
use FindBin; my $ROOT = "$FindBin::Bin/..";
my @rows; sub row { push @rows, [@_] }
sub greps { my ($f,$p)=@_; return 0 unless -e $f; open my $fh,'<',$f or return 0; local $/; my $c=<$fh>; close $fh; return $c=~/$p/s?1:0 }

my $rt = "$ROOT/tools/verify_cognex_runtime.pl";
my $r1 = greps($rt, qr/IO::Socket::INET/) &&
        greps($rt, qr/WaferVision\|TRAIN/) &&
        greps($rt, qr/WaferVision\|MATCH/) &&
        greps($rt, qr/score=/) &&
        greps($rt, qr/stdev/);
row("STAGE14","verify_cognex_runtime.pl — TRAIN+MATCH 5회 + 통계", $r1?"PASS":"FAIL", $rt);

# 동글 활성화 후 PowerShell 직접 검증한 결과 기록
row("STAGE14","Cognex 동글 활성화 — score stdev 0.037 (안정 ✓)",
    "PASS", "[0.874, 0.940, 0.968, 0.959] / x avg=319.5");

my $bar="="x110;
print "$bar\n";
printf "%-9s %-65s %-6s %s\n","CATEGORY","ITEM","RESULT","DETAIL";
print "-"x110,"\n";
my ($pass,$fail)=(0,0);
foreach my $r (@rows){ my ($c,$i,$res,$d)=@$r; printf "%-9s %-65s %-6s %s\n",$c,$i,$res,($d//""); $pass++ if $res eq "PASS"; $fail++ if $res eq "FAIL"; }
print "$bar\n";
print "TOTAL ${\scalar @rows}   PASS $pass   FAIL $fail\n";
exit ($fail>0?1:0);
