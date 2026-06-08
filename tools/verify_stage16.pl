#!perl
use strict; use warnings;
use FindBin; my $ROOT = "$FindBin::Bin/..";
my @rows; sub row { push @rows, [@_] }
sub greps { my ($f,$p)=@_; return 0 unless -e $f; open my $fh,'<',$f or return 0; local $/; my $c=<$fh>; close $fh; return $c=~/$p/s?1:0 }

row("BUILD","QMC.CDT-320.exe", -e "$ROOT/QMC.CDT-320/bin/Debug/QMC.CDT-320.exe"?"PASS":"FAIL","");

my $si = "$ROOT/QMC.CDT-320/Equipment/Secs/SecsItem.cs";
my $r1 = greps($si, qr/enum\s+SecsItemFormat/) &&
        greps($si, qr/class\s+SecsItem/) &&
        greps($si, qr/static\s+SecsItem\s+List/) &&
        greps($si, qr/static\s+SecsItem\s+A\(/) &&
        greps($si, qr/static\s+SecsItem\s+U[124]/) &&
        greps($si, qr/static\s+SecsItem\s+I[124]/) &&
        greps($si, qr/static\s+SecsItem\s+F[48]/) &&
        greps($si, qr/byte\[\]\s+Encode/) &&
        greps($si, qr/static\s+SecsItem\s+Decode/);
row("STAGE16","SecsItem — 9 Format + 팩토리 + Encode/Decode", $r1?"PASS":"FAIL", $si);

my $sm = "$ROOT/QMC.CDT-320/Equipment/Secs/SecsMessage.cs";
my $r2 = greps($sm, qr/SecsItem\s+Body/) &&
        greps($sm, qr/DecodeBody/);
row("STAGE16","SecsMessage — Body 프로퍼티 + DecodeBody()", $r2?"PASS":"FAIL", $sm);

my $bar="="x110;
print "$bar\n";
printf "%-9s %-65s %-6s %s\n","CATEGORY","ITEM","RESULT","DETAIL";
print "-"x110,"\n";
my ($pass,$fail)=(0,0);
foreach my $r (@rows){ my ($c,$i,$res,$d)=@$r; printf "%-9s %-65s %-6s %s\n",$c,$i,$res,($d//""); $pass++ if $res eq "PASS"; $fail++ if $res eq "FAIL"; }
print "$bar\n";
print "TOTAL ${\scalar @rows}   PASS $pass   FAIL $fail\n";
exit ($fail>0?1:0);
