#!perl
use strict; use warnings;
my $ROOT = "D:/Work/CDT-320/QMC.CDT-320";
my @rows; sub row { push @rows, [@_] }
sub greps { my ($f,$p)=@_; return 0 unless -e $f; open my $fh,'<',$f or return 0; local $/; my $c=<$fh>; close $fh; return $c=~/$p/s?1:0 }

row("BUILD","QMC.CDT-320.exe", -e "$ROOT/QMC.CDT-320/bin/Debug/QMC.CDT-320.exe"?"PASS":"FAIL","");

# Stage 51 — Inspection Subset (Bottom + TopSide + BottomSide)
my $rs = "$ROOT/QMC.CDT-320/Equipment/Recipes/RecipeStore.cs";
my $r1 = greps($rs, qr/class\s+InspectionSubset/);
row("STAGE51","RecipeStore — InspectionSubset class",
    $r1?"PASS":"FAIL", $rs);

my $r2 = greps($rs, qr/BottomInsp/) &&
         greps($rs, qr/TopSideInsp/) &&
         greps($rs, qr/BottomSideInsp/);
row("STAGE51","RecipeProject — BottomInsp + TopSideInsp + BottomSideInsp",
    $r2?"PASS":"FAIL", $rs);

my $bar="="x110;
print "$bar\n";
printf "%-9s %-65s %-6s %s\n","CATEGORY","ITEM","RESULT","DETAIL";
print "-"x110,"\n";
my ($pass,$fail)=(0,0);
foreach my $r (@rows){ my ($c,$i,$res,$d)=@$r; printf "%-9s %-65s %-6s %s\n",$c,$i,$res,($d//""); $pass++ if $res eq "PASS"; $fail++ if $res eq "FAIL"; }
print "$bar\n";
print "TOTAL ${\scalar @rows}   PASS $pass   FAIL $fail\n";
exit ($fail>0?1:0);
