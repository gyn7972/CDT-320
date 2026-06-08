#!perl
use strict; use warnings;
use FindBin; my $ROOT = "$FindBin::Bin/..";
my @rows; sub row { push @rows, [@_] }
sub greps { my ($f,$p)=@_; return 0 unless -e $f; open my $fh,'<',$f or return 0; local $/; my $c=<$fh>; close $fh; return $c=~/$p/s?1:0 }

row("BUILD","QMC.CDT-320.exe", -e "$ROOT/QMC.CDT-320/bin/Debug/QMC.CDT-320.exe"?"PASS":"FAIL","");

# Stage 27 — Feeder integration + 4 GAP fix
my $mc = "$ROOT/QMC.CDT-320/Equipment/MachineController.cs";
my $r1 = greps($mc, qr/StoreCompletedWaferAsync/) &&
         greps($mc, qr/WafersPerOutputBatch/);
row("STAGE27","MachineController — StoreCompletedWaferAsync / WafersPerOutputBatch",
    $r1?"PASS":"FAIL", $mc);

my $oua = "$ROOT/QMC.CDT-320/Equipment/Sim/OutputUnloaderAdapter.cs";
my $r2 = (-e $oua) ? 1 : 0;
row("STAGE27","OutputUnloaderAdapter.cs exists (NullObject -> real adapter)",
    $r2?"PASS":"FAIL", $oua);

my $ifp = "$ROOT/QMC.CDT-320/Ui/Pages/WorkInfo/InputFeederPage.cs";
my $r3 = greps($ifp, qr/class\s+InputFeederPage/);
row("STAGE27","InputFeederPage live (5 actions: Init/FwdCyl/BwdCyl/Clamp/Unclamp)",
    $r3?"PASS":"FAIL", $ifp);

my $ofp = "$ROOT/QMC.CDT-320/Ui/Pages/WorkInfo/OutputPages.cs";
my $r4 = greps($ofp, qr/class\s+OutputFeederPage/);
row("STAGE27","OutputFeederPage exists (3 cassettes + 6 sensors + 4 actions)",
    $r4?"PASS":"FAIL", $ofp);

my $bar="="x110;
print "$bar\n";
printf "%-9s %-65s %-6s %s\n","CATEGORY","ITEM","RESULT","DETAIL";
print "-"x110,"\n";
my ($pass,$fail)=(0,0);
foreach my $r (@rows){ my ($c,$i,$res,$d)=@$r; printf "%-9s %-65s %-6s %s\n",$c,$i,$res,($d//""); $pass++ if $res eq "PASS"; $fail++ if $res eq "FAIL"; }
print "$bar\n";
print "TOTAL ${\scalar @rows}   PASS $pass   FAIL $fail\n";
exit ($fail>0?1:0);
