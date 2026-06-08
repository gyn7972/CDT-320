#!perl
use strict; use warnings;
use FindBin; my $ROOT = "$FindBin::Bin/..";
my @rows; sub row { push @rows, [@_] }
sub greps { my ($f,$p)=@_; return 0 unless -e $f; open my $fh,'<',$f or return 0; local $/; my $c=<$fh>; close $fh; return $c=~/$p/s?1:0 }

row("BUILD","QMC.CDT-320.exe", -e "$ROOT/QMC.CDT-320/bin/Debug/QMC.CDT-320.exe"?"PASS":"FAIL","");

# Stage 45 — OperationPanelUnit (Tower Lamp + Buzzer + Op Panel buttons)
my $op = "$ROOT/QMC.CDT-320/Equipment/OperationPanelUnit.cs";
my $r1 = greps($op, qr/class\s+OperationPanelUnit/);
row("STAGE45","OperationPanelUnit.cs exists",
    $r1?"PASS":"FAIL", $op);

my $r2 = greps($op, qr/TlRed/) &&
         greps($op, qr/TlYellow/) &&
         greps($op, qr/TlGreen/) &&
         greps($op, qr/Buzzer/);
row("STAGE45","OperationPanelUnit — Tower Lamp 3 (Red/Yellow/Green) + Buzzer",
    $r2?"PASS":"FAIL", $op);

my $cdtm = "$ROOT/QMC.CDT-320/Equipment/CDT320Machine.cs";
my $r3 = greps($cdtm, qr/OperationPanel/);
row("STAGE45","CDT320Machine — OperationPanelUnit integration",
    $r3?"PASS":"FAIL", $cdtm);

my $bar="="x110;
print "$bar\n";
printf "%-9s %-65s %-6s %s\n","CATEGORY","ITEM","RESULT","DETAIL";
print "-"x110,"\n";
my ($pass,$fail)=(0,0);
foreach my $r (@rows){ my ($c,$i,$res,$d)=@$r; printf "%-9s %-65s %-6s %s\n",$c,$i,$res,($d//""); $pass++ if $res eq "PASS"; $fail++ if $res eq "FAIL"; }
print "$bar\n";
print "TOTAL ${\scalar @rows}   PASS $pass   FAIL $fail\n";
exit ($fail>0?1:0);
