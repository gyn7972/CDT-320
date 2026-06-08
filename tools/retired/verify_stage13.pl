#!perl
use strict; use warnings;
use FindBin; my $ROOT = "$FindBin::Bin/..";
my @rows; sub row { push @rows, [@_] }
sub greps { my ($f,$p)=@_; return 0 unless -e $f; open my $fh,'<',$f or return 0; local $/; my $c=<$fh>; close $fh; return $c=~/$p/s?1:0 }

row("BUILD","QMC.CDT-320.exe", -e "$ROOT/QMC.CDT-320/bin/Debug/QMC.CDT-320.exe"?"PASS":"FAIL","");

my $mc = "$ROOT/QMC.CDT-320/Equipment/MachineController.cs";
my $r1 = greps($mc, qr/public bool DryRun/) &&
        greps($mc, qr/public bool StepRun/) &&
        greps($mc, qr/StepRunGate/) &&
        greps($mc, qr/ApplyRecipeMode/);
row("STAGE13","MachineController — DryRun/StepRun + Gate event + ApplyRecipeMode", $r1?"PASS":"FAIL", $mc);

my $r2 = greps($mc, qr/if \(DryRun\)/) &&
        greps($mc, qr/\[DRYRUN\]/);
row("STAGE13","MoveAxisAsync — DryRun 모드 시 실 모션 skip", $r2?"PASS":"FAIL", $mc);

my $r3 = greps($mc, qr/if \(StepRun && StepRunGate != null\)/) &&
        greps($mc, qr/\[STEPRUN\]/);
row("STAGE13","DoOneDieAsync — StepRun gate 콜백 (사용자 확인)", $r3?"PASS":"FAIL", $mc);

my $bar="="x110;
print "$bar\n";
printf "%-9s %-65s %-6s %s\n","CATEGORY","ITEM","RESULT","DETAIL";
print "-"x110,"\n";
my ($pass,$fail)=(0,0);
foreach my $r (@rows){ my ($c,$i,$res,$d)=@$r; printf "%-9s %-65s %-6s %s\n",$c,$i,$res,($d//""); $pass++ if $res eq "PASS"; $fail++ if $res eq "FAIL"; }
print "$bar\n";
print "TOTAL ${\scalar @rows}   PASS $pass   FAIL $fail\n";
exit ($fail>0?1:0);
