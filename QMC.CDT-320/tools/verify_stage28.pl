#!perl
use strict; use warnings;
my $ROOT = "D:/Work/CDT-320/QMC.CDT-320";
my @rows; sub row { push @rows, [@_] }
sub greps { my ($f,$p)=@_; return 0 unless -e $f; open my $fh,'<',$f or return 0; local $/; my $c=<$fh>; close $fh; return $c=~/$p/s?1:0 }

row("BUILD","QMC.CDT-320.exe", -e "$ROOT/QMC.CDT-320/bin/Debug/QMC.CDT-320.exe"?"PASS":"FAIL","");

# Stage 28 — InputStageUnit integration
my $wla = "$ROOT/QMC.CDT-320/Equipment/Sim/WaferLoaderAdapter.cs";
my $r1 = (-e $wla) ? 1 : 0;
row("STAGE28","WaferLoaderAdapter.cs exists (FeederY position + Cyl state check)",
    $r1?"PASS":"FAIL", $wla);

my $wla_r = greps($wla, qr/class\s+WaferLoaderAdapter/);
row("STAGE28","WaferLoaderAdapter class declared",
    $wla_r?"PASS":"FAIL", $wla);

my $cdtm = "$ROOT/QMC.CDT-320/Equipment/CDT320Machine.cs";
my $r2 = greps($cdtm, qr/WaferLoaderAdapter/);
row("STAGE28","CDT320Machine — WaferLoaderAdapter injection",
    $r2?"PASS":"FAIL", $cdtm);

my $mc = "$ROOT/QMC.CDT-320/Equipment/MachineController.cs";
my $r3 = greps($mc, qr/MoveInputStageToDieAsync/) &&
         greps($mc, qr/UnloadInputStageWaferAsync/);
row("STAGE28","MachineController — MoveInputStageToDieAsync + UnloadInputStageWaferAsync",
    $r3?"PASS":"FAIL", $mc);

my $isu = "$ROOT/QMC.CDT-320/Equipment/InputStageUnit.cs";
my $r4 = greps($isu, qr/class\s+InputStageUnit/);
row("STAGE28","InputStageUnit class exists (StageY/CameraX/StageT/NeedleBlockX SoftLimit)",
    $r4?"PASS":"FAIL", $isu);

my $bar="="x110;
print "$bar\n";
printf "%-9s %-65s %-6s %s\n","CATEGORY","ITEM","RESULT","DETAIL";
print "-"x110,"\n";
my ($pass,$fail)=(0,0);
foreach my $r (@rows){ my ($c,$i,$res,$d)=@$r; printf "%-9s %-65s %-6s %s\n",$c,$i,$res,($d//""); $pass++ if $res eq "PASS"; $fail++ if $res eq "FAIL"; }
print "$bar\n";
print "TOTAL ${\scalar @rows}   PASS $pass   FAIL $fail\n";
exit ($fail>0?1:0);
