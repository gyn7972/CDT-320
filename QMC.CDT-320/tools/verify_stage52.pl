#!perl
use strict; use warnings;
my $ROOT = "D:/Work/CDT-320/QMC.CDT-320";
my @rows; sub row { push @rows, [@_] }
sub greps { my ($f,$p)=@_; return 0 unless -e $f; open my $fh,'<',$f or return 0; local $/; my $c=<$fh>; close $fh; return $c=~/$p/s?1:0 }

row("BUILD","QMC.CDT-320.exe", -e "$ROOT/QMC.CDT-320/bin/Debug/QMC.CDT-320.exe"?"PASS":"FAIL","");

# Stage 52 — FrontSide / RearSide Inspection Modules in QMC.Vision
my $tsi = "$ROOT/QMC.Vision/Modules/FrontSideInspectionModule.cs";
my $r1 = greps($tsi, qr/class\s+FrontSideInspectionModule/);
row("STAGE52","FrontSideInspectionModule.cs exists (port 5105)",
    $r1?"PASS":"FAIL", $tsi);

my $bsi = "$ROOT/QMC.Vision/Modules/RearSideInspectionModule.cs";
my $r2 = greps($bsi, qr/class\s+RearSideInspectionModule/);
row("STAGE52","RearSideInspectionModule.cs exists (port 5106)",
    $r2?"PASS":"FAIL", $bsi);

my $vform = "$ROOT/QMC.Vision/Form1.cs";
my $r3 = greps($vform, qr/FrontSide|RearSide/);
row("STAGE52","QMC.Vision Form1 — FrontSide/RearSide modules wired (TCP server)",
    $r3?"PASS":"FAIL", $vform);

my $bar="="x110;
print "$bar\n";
printf "%-9s %-65s %-6s %s\n","CATEGORY","ITEM","RESULT","DETAIL";
print "-"x110,"\n";
my ($pass,$fail)=(0,0);
foreach my $r (@rows){ my ($c,$i,$res,$d)=@$r; printf "%-9s %-65s %-6s %s\n",$c,$i,$res,($d//""); $pass++ if $res eq "PASS"; $fail++ if $res eq "FAIL"; }
print "$bar\n";
print "TOTAL ${\scalar @rows}   PASS $pass   FAIL $fail\n";
exit ($fail>0?1:0);
