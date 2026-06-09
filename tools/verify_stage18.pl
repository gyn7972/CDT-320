#!perl
use strict; use warnings;
use FindBin; my $ROOT = "$FindBin::Bin/..";
my @rows; sub row { push @rows, [@_] }
sub greps { my ($f,$p)=@_; return 0 unless -e $f; open my $fh,'<',$f or return 0; local $/; my $c=<$fh>; close $fh; return $c=~/$p/s?1:0 }

my $rct = "$ROOT/tools/runtime_cycle_test.pl";
my $r1 = greps($rct, qr/Stage 18/) &&
        greps($rct, qr/RUN_GUI_CYCLE/) &&
        greps($rct, qr/gui_cycle_automation\.ps1/) &&
        greps($rct, qr/Lot.*ProcessedDies/) &&
        greps($rct, qr/Lot.*State/);
row("STAGE18","runtime_cycle_test.pl — Stage 18 cycle 결과 검증 통합", $r1?"PASS":"FAIL", $rct);

my $cga = "$ROOT/tools/gui_cycle_automation.ps1";
my $r2 = -e $cga ? "PASS" : "FAIL";
row("STAGE18","gui_cycle_automation.ps1 (Stage 5 의존성)", $r2, $cga);

my $bar="="x110;
print "$bar\n";
printf "%-9s %-65s %-6s %s\n","CATEGORY","ITEM","RESULT","DETAIL";
print "-"x110,"\n";
my ($pass,$fail)=(0,0);
foreach my $r (@rows){ my ($c,$i,$res,$d)=@$r; printf "%-9s %-65s %-6s %s\n",$c,$i,$res,($d//""); $pass++ if $res eq "PASS"; $fail++ if $res eq "FAIL"; }
print "$bar\n";
print "TOTAL ${\scalar @rows}   PASS $pass   FAIL $fail\n";
exit ($fail>0?1:0);
