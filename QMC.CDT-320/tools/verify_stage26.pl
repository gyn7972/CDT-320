#!perl
use strict; use warnings;
my $ROOT = "D:/Work/CDT-320/QMC.CDT-320";
my @rows; sub row { push @rows, [@_] }
sub greps { my ($f,$p)=@_; return 0 unless -e $f; open my $fh,'<',$f or return 0; local $/; my $c=<$fh>; close $fh; return $c=~/$p/s?1:0 }

row("BUILD","QMC.CDT-320.exe", -e "$ROOT/QMC.CDT-320/bin/Debug/QMC.CDT-320.exe"?"PASS":"FAIL","");

# Stage 26 — UI Polish + LotPort 5 items
my $mc = "$ROOT/QMC.CDT-320/Equipment/MachineController.cs";
my $r1 = greps($mc, qr/LoadNextWaferAsync/) &&
         greps($mc, qr/RetractCurrentWaferAsync/);
row("STAGE26","MachineController — LoadNextWaferAsync / RetractCurrentWaferAsync",
    $r1?"PASS":"FAIL", $mc);

my $icp = "$ROOT/QMC.CDT-320/Ui/Pages/WorkInfo/InputCassettePage.cs";
my $r2 = greps($icp, qr/class\s+InputCassettePage/);
row("STAGE26","InputCassettePage exists (Lot Port 7 buttons + slot LED)",
    $r2?"PASS":"FAIL", $icp);

my $sim = "$ROOT/QMC.CDT-320/Equipment/Sim/SimCassetteDriver.cs";
my $r3 = greps($sim, qr/class\s+SimCassetteDriver/);
row("STAGE26","SimCassetteDriver — DI mapping (X060~X063)",
    $r3?"PASS":"FAIL", $sim);

my $bar="="x110;
print "$bar\n";
printf "%-9s %-65s %-6s %s\n","CATEGORY","ITEM","RESULT","DETAIL";
print "-"x110,"\n";
my ($pass,$fail)=(0,0);
foreach my $r (@rows){ my ($c,$i,$res,$d)=@$r; printf "%-9s %-65s %-6s %s\n",$c,$i,$res,($d//""); $pass++ if $res eq "PASS"; $fail++ if $res eq "FAIL"; }
print "$bar\n";
print "TOTAL ${\scalar @rows}   PASS $pass   FAIL $fail\n";
exit ($fail>0?1:0);
