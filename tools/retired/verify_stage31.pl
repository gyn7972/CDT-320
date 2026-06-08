#!perl
use strict; use warnings;
use FindBin; my $ROOT = "$FindBin::Bin/..";
my @rows; sub row { push @rows, [@_] }
sub greps { my ($f,$p)=@_; return 0 unless -e $f; open my $fh,'<',$f or return 0; local $/; my $c=<$fh>; close $fh; return $c=~/$p/s?1:0 }

row("BUILD","QMC.CDT-320.exe", -e "$ROOT/QMC.CDT-320/bin/Debug/QMC.CDT-320.exe"?"PASS":"FAIL","");

# Stage 31 — VisionInspection integration
my $mc = "$ROOT/QMC.CDT-320/Equipment/MachineController.cs";
my $r1 = greps($mc, qr/InspectBottomVisionAsync/) &&
         greps($mc, qr/InspectSideVisionAsync/);
row("STAGE31","MachineController — InspectBottomVisionAsync + InspectSideVisionAsync",
    $r1?"PASS":"FAIL", $mc);

my $vh = "$ROOT/QMC.CDT-320/Equipment/Vision/VisionHub.cs";
my $r2 = greps($vh, qr/class\s+VisionHub/) &&
         greps($vh, qr/Inspection/);
row("STAGE31","VisionHub.Inspection channel exists (sim fallback when not connected)",
    $r2?"PASS":"FAIL", $vh);

my $tpu = "$ROOT/QMC.CDT-320/Equipment/TransferPickerUnit.cs";
my $r3 = greps($tpu, qr/InspectBottomVisionAsync|InspectSideVisionAsync/);
row("STAGE31","TpuArmUnit — InspectBottom/Side VisionAsync methods",
    $r3?"PASS":"FAIL", $tpu);

my $bar="="x110;
print "$bar\n";
printf "%-9s %-65s %-6s %s\n","CATEGORY","ITEM","RESULT","DETAIL";
print "-"x110,"\n";
my ($pass,$fail)=(0,0);
foreach my $r (@rows){ my ($c,$i,$res,$d)=@$r; printf "%-9s %-65s %-6s %s\n",$c,$i,$res,($d//""); $pass++ if $res eq "PASS"; $fail++ if $res eq "FAIL"; }
print "$bar\n";
print "TOTAL ${\scalar @rows}   PASS $pass   FAIL $fail\n";
exit ($fail>0?1:0);
