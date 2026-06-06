#!perl
use strict; use warnings;
my $ROOT = "D:/Work/CDT-320/QMC.CDT-320";
my @rows; sub row { push @rows, [@_] }
sub greps { my ($f,$p)=@_; return 0 unless -e $f; open my $fh,'<',$f or return 0; local $/; my $c=<$fh>; close $fh; return $c=~/$p/s?1:0 }

row("BUILD","QMC.CDT-320.exe", -e "$ROOT/QMC.CDT-320/bin/Debug/QMC.CDT-320.exe"?"PASS":"FAIL","");

# Stage 48 — PostPnpTransferUnit (Post PNP Transfer Tool)
my $pp = "$ROOT/QMC.CDT-320/Equipment/PostPnpTransferUnit.cs";
my $r1 = greps($pp, qr/class\s+PostPnpTransferUnit/);
row("STAGE48","PostPnpTransferUnit.cs exists",
    $r1?"PASS":"FAIL", $pp);

my $cdtm = "$ROOT/QMC.CDT-320/Equipment/CDT320Machine.cs";
my $r2 = greps($cdtm, qr/PostPnp/);
row("STAGE48","CDT320Machine — PostPnp integration",
    $r2?"PASS":"FAIL", $cdtm);

my $bar="="x110;
print "$bar\n";
printf "%-9s %-65s %-6s %s\n","CATEGORY","ITEM","RESULT","DETAIL";
print "-"x110,"\n";
my ($pass,$fail)=(0,0);
foreach my $r (@rows){ my ($c,$i,$res,$d)=@$r; printf "%-9s %-65s %-6s %s\n",$c,$i,$res,($d//""); $pass++ if $res eq "PASS"; $fail++ if $res eq "FAIL"; }
print "$bar\n";
print "TOTAL ${\scalar @rows}   PASS $pass   FAIL $fail\n";
exit ($fail>0?1:0);
