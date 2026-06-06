#!perl
use strict; use warnings;
my $ROOT = "D:/Work/CDT-320/QMC.CDT-320";
my @rows; sub row { push @rows, [@_] }
sub greps { my ($f,$p)=@_; return 0 unless -e $f; open my $fh,'<',$f or return 0; local $/; my $c=<$fh>; close $fh; return $c=~/$p/s?1:0 }

row("BUILD","QMC.CDT-320.exe", -e "$ROOT/QMC.CDT-320/bin/Debug/QMC.CDT-320.exe"?"PASS":"FAIL","");

# Stage 44 — EjectPinZ (axis 8) + SideVisionY (axis 19/20)
my $isu = "$ROOT/QMC.CDT-320/Equipment/InputStageUnit.cs";
my $r1 = greps($isu, qr/EjectPinZ/);
row("STAGE44","InputStageUnit — EjectPinZ axis (axis 8)",
    $r1?"PASS":"FAIL", $isu);

my $tpu = "$ROOT/QMC.CDT-320/Equipment/TransferPickerUnit.cs";
my $r2 = greps($tpu, qr/SideVisionY/);
row("STAGE44","TpuArmUnit — SideVisionY axis (LeftArm 19 / RightArm 20)",
    $r2?"PASS":"FAIL", $tpu);

my $sb = "$ROOT/QMC.CDT-320/Equipment/SimulatorBridge.cs";
my $r3 = greps($sb, qr/EjectPinZ.*=\s*8/s) &&
         greps($sb, qr/SideVisionY.*=\s*19/s);
row("STAGE44","SimulatorBridge — axis 8/19/20 mapping",
    $r3?"PASS":"FAIL", $sb);

my $bar="="x110;
print "$bar\n";
printf "%-9s %-65s %-6s %s\n","CATEGORY","ITEM","RESULT","DETAIL";
print "-"x110,"\n";
my ($pass,$fail)=(0,0);
foreach my $r (@rows){ my ($c,$i,$res,$d)=@$r; printf "%-9s %-65s %-6s %s\n",$c,$i,$res,($d//""); $pass++ if $res eq "PASS"; $fail++ if $res eq "FAIL"; }
print "$bar\n";
print "TOTAL ${\scalar @rows}   PASS $pass   FAIL $fail\n";
exit ($fail>0?1:0);
