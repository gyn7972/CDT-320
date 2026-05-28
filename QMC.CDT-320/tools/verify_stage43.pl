#!perl
use strict; use warnings;
my $ROOT = "D:/Work/CDT-320/QMC.CDT-320";
my @rows; sub row { push @rows, [@_] }
sub greps { my ($f,$p)=@_; return 0 unless -e $f; open my $fh,'<',$f or return 0; local $/; my $c=<$fh>; close $fh; return $c=~/$p/s?1:0 }

row("BUILD","QMC.CDT-320.exe", -e "$ROOT/QMC.CDT-320/bin/Debug/QMC.CDT-320.exe"?"PASS":"FAIL","");

# Stage 43 — VisionHub 6 channels (5100/5101/5103/5104/5105/5106) + BarcodeSerialAdapter
my $vh = "$ROOT/QMC.CDT-320/Equipment/Vision/VisionHub.cs";
my $r1 = greps($vh, qr/Main\s*\{\s*get/) &&
         greps($vh, qr/FrontSide\s*\{\s*get/) &&
         greps($vh, qr/RearSide\s*\{\s*get/);
row("STAGE43","VisionHub — 3 new channels (Main 5104 / FrontSide 5105 / RearSide 5106)",
    $r1?"PASS":"FAIL", $vh);

my $bsa = "$ROOT/QMC.CDT-320/Equipment/Vision/BarcodeSerialAdapter.cs";
my $r2 = greps($bsa, qr/class\s+BarcodeSerialAdapter/);
row("STAGE43","BarcodeSerialAdapter.cs exists (Wafer Serial 4 / Bin Serial 6)",
    $r2?"PASS":"FAIL", $bsa);

my $appset = "$ROOT/QMC.CDT-320/Equipment/AppSettings.cs";
my $r3 = greps($appset, qr/WaferBarcodeSerialPort/) &&
         greps($appset, qr/BinBarcodeSerialPort/);
row("STAGE43","AppSettings — WaferBarcodeSerialPort / BinBarcodeSerialPort",
    $r3?"PASS":"FAIL", $appset);

my $bar="="x110;
print "$bar\n";
printf "%-9s %-65s %-6s %s\n","CATEGORY","ITEM","RESULT","DETAIL";
print "-"x110,"\n";
my ($pass,$fail)=(0,0);
foreach my $r (@rows){ my ($c,$i,$res,$d)=@$r; printf "%-9s %-65s %-6s %s\n",$c,$i,$res,($d//""); $pass++ if $res eq "PASS"; $fail++ if $res eq "FAIL"; }
print "$bar\n";
print "TOTAL ${\scalar @rows}   PASS $pass   FAIL $fail\n";
exit ($fail>0?1:0);
