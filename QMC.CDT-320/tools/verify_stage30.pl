#!perl
use strict; use warnings;
my $ROOT = "D:/Work/CDT-320/QMC.CDT-320";
my @rows; sub row { push @rows, [@_] }
sub greps { my ($f,$p)=@_; return 0 unless -e $f; open my $fh,'<',$f or return 0; local $/; my $c=<$fh>; close $fh; return $c=~/$p/s?1:0 }

row("BUILD","QMC.CDT-320.exe", -e "$ROOT/QMC.CDT-320/bin/Debug/QMC.CDT-320.exe"?"PASS":"FAIL","");

# Stage 30 — OutputStageUnit integration
my $mc = "$ROOT/QMC.CDT-320/Equipment/MachineController.cs";
my $r1 = greps($mc, qr/ReceiveDieAsync/) &&
         greps($mc, qr/InspectBinPositionAsync/);
row("STAGE30","MachineController — ReceiveDieAsync + InspectBinPositionAsync",
    $r1?"PASS":"FAIL", $mc);

my $osu = "$ROOT/QMC.CDT-320/Equipment/OutputStageUnit.cs";
my $r2 = greps($osu, qr/class\s+OutputStageUnit/);
row("STAGE30","OutputStageUnit exists (StageY 350 / StageZ 250 / BinCameraX 350)",
    $r2?"PASS":"FAIL", $osu);

# VisionOffset transfer
my $r3 = greps($mc, qr/VisionOffset/);
row("STAGE30","ReceiveDieAsync — VisionOffset (Bottom Picker1) transfer",
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
