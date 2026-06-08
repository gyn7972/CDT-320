#!perl
use strict; use warnings;
use FindBin; my $ROOT = "$FindBin::Bin/..";
my @rows; sub row { push @rows, [@_] }
sub greps { my ($f,$p)=@_; return 0 unless -e $f; open my $fh,'<',$f or return 0; local $/; my $c=<$fh>; close $fh; return $c=~/$p/s?1:0 }

row("BUILD","QMC.CDT-320.exe", -e "$ROOT/QMC.CDT-320/bin/Debug/QMC.CDT-320.exe"?"PASS":"FAIL","");

# Stage 50 — Bin Barcode Reader (NullBarcodeReader adapter)
my $cdtm = "$ROOT/QMC.CDT-320/Equipment/CDT320Machine.cs";
my $r1 = greps($cdtm, qr/class\s+NullBarcodeReader/);
row("STAGE50","CDT320Machine — NullBarcodeReader adapter (IBarcodeReader)",
    $r1?"PASS":"FAIL", $cdtm);

my $r2 = greps($cdtm, qr/BinBarcodeReader/);
row("STAGE50","CDT320Machine — BinBarcodeReader integration",
    $r2?"PASS":"FAIL", $cdtm);

my $bsa = "$ROOT/QMC.CDT-320/Equipment/Vision/BarcodeSerialAdapter.cs";
my $r3 = greps($bsa, qr/class\s+BarcodeSerialAdapter/);
row("STAGE50","BarcodeSerialAdapter — for Bin Serial 6 port",
    $r3?"PASS":"FAIL", $bsa);

my $bar="="x110;
print "$bar\n";
printf "%-9s %-65s %-6s %s\n","CATEGORY","ITEM","RESULT","DETAIL";
print "-"x110,"\n";
my ($pass,$fail)=(0,0);
foreach my $r (@rows){ my ($c,$i,$res,$d)=@$r; printf "%-9s %-65s %-6s %s\n",$c,$i,$res,($d//""); $pass++ if $res eq "PASS"; $fail++ if $res eq "FAIL"; }
print "$bar\n";
print "TOTAL ${\scalar @rows}   PASS $pass   FAIL $fail\n";
exit ($fail>0?1:0);
