#!perl
use strict; use warnings;
my $ROOT = "D:/Work/CDT-320/QMC.CDT-320";
my @rows; sub row { push @rows, [@_] }
sub greps { my ($f,$p)=@_; return 0 unless -e $f; open my $fh,'<',$f or return 0; local $/; my $c=<$fh>; close $fh; return $c=~/$p/s?1:0 }

row("BUILD","QMC.CDT-320.exe", -e "$ROOT/QMC.CDT-320/bin/Debug/QMC.CDT-320.exe"?"PASS":"FAIL","");

# Stage 29 — TransferPickerUnit integration
my $mc = "$ROOT/QMC.CDT-320/Equipment/MachineController.cs";
my $r1 = greps($mc, qr/PickupAsync/) &&
         greps($mc, qr/PlaceAsync/);
row("STAGE29","MachineController — PickerComponent.PickupAsync / PlaceAsync calls",
    $r1?"PASS":"FAIL", $mc);

my $tpu = "$ROOT/QMC.CDT-320/Equipment/TransferPickerUnit.cs";
my $r2 = greps($tpu, qr/class\s+TransferPickerUnit/);
row("STAGE29","TransferPickerUnit exists (ArmX 1600mm / ArmY 350 / PickerZ 100 / PickerT)",
    $r2?"PASS":"FAIL", $tpu);

# PickerZ/T ServoOn references
my $r3 = greps($mc, qr/PickerZ\.ServoOn|PickerT\.ServoOn/);
row("STAGE29","MachineController — PickerZ/PickerT ServoOn invocation",
    $r3?"PASS":"FAIL", $mc);

my $bar="="x110;
print "$bar\n";
printf "%-9s %-65s %-6s %s\n","CATEGORY","ITEM","RESULT","DETAIL";
print "-"x110,"\n";
my ($pass,$fail)=(0,0);
foreach my $r (@rows){ my ($c,$i,$res,$d)=@$r; printf "%-9s %-65s %-6s %s\n",$c,$i,$res,($d//""); $pass++ if $res eq "PASS"; $fail++ if $res eq "FAIL"; }
print "$bar\n";
print "TOTAL ${\scalar @rows}   PASS $pass   FAIL $fail\n";
exit ($fail>0?1:0);
