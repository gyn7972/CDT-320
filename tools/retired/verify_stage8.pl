#!perl
use strict; use warnings;
use FindBin; my $ROOT = "$FindBin::Bin/..";
my @rows; sub row { push @rows, [@_] }
sub greps { my ($f,$p)=@_; return 0 unless -e $f; open my $fh,'<',$f or return 0; local $/; my $c=<$fh>; close $fh; return $c=~/$p/s?1:0 }

row("BUILD","QMC.CDT-320.exe", -e "$ROOT/QMC.CDT-320/bin/Debug/QMC.CDT-320.exe"?"PASS":"FAIL","");

my $f = "$ROOT/QMC.CDT-320/Equipment/Interlocks/ExtendedInterlocks2.cs";
my $r1 = greps($f, qr/class\s+LifterVsExpanderInterlock/) &&
        greps($f, qr/class\s+BarcodeVsLoaderInterlock/) &&
        greps($f, qr/class\s+SubPortVsPickerInterlock/) &&
        greps($f, qr/class\s+ColletCleanerVsPickerInterlock/) &&
        greps($f, qr/class\s+EmgStopVsAllInterlock/);
row("STAGE8","Extended2 — 5 추가 Interlock (Lifter/Barcode/SubPort/Cleaner/Emg)", $r1?"PASS":"FAIL", $f);

my $bar="="x110;
print "$bar\n";
printf "%-9s %-65s %-6s %s\n","CATEGORY","ITEM","RESULT","DETAIL";
print "-"x110,"\n";
my ($pass,$fail)=(0,0);
foreach my $r (@rows){ my ($c,$i,$res,$d)=@$r; printf "%-9s %-65s %-6s %s\n",$c,$i,$res,($d//""); $pass++ if $res eq "PASS"; $fail++ if $res eq "FAIL"; }
print "$bar\n";
print "TOTAL ${\scalar @rows}   PASS $pass   FAIL $fail\n";
exit ($fail>0?1:0);
