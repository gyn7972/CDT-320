#!perl
use strict; use warnings;
my $ROOT = "D:/Work/CDT-320/QMC.CDT-320";
my @rows; sub row { push @rows, [@_] }
sub greps { my ($f,$p)=@_; return 0 unless -e $f; open my $fh,'<',$f or return 0; local $/; my $c=<$fh>; close $fh; return $c=~/$p/s?1:0 }

row("BUILD","QMC.CDT-320.exe", -e "$ROOT/QMC.CDT-320/bin/Debug/QMC.CDT-320.exe"?"PASS":"FAIL","");

my $rs = "$ROOT/QMC.CDT-320/Equipment/Recipes/RecipeStore.cs";
my $r1 = greps($rs, qr/SMALL-DIE-0\.5mm/) &&
        greps($rs, qr/LARGE-DIE-3mm/) &&
        greps($rs, qr/SAMPLE-DEMO/) &&
        greps($rs, qr/GM1SP-T150-G300/);
row("STAGE21","RecipeStore — 4 시드 프로젝트 (Small/Large/Demo/GM1SP)", $r1?"PASS":"FAIL", $rs);

# Subset 가 시드에 포함됐는지
my $r2 = greps($rs, qr/Die\s*=\s*new DieSubset/) &&
        greps($rs, qr/Frame\s*=\s*new TapeFrameSubset/) &&
        greps($rs, qr/12inch_100x100/) &&
        greps($rs, qr/8inch_15x15/);
row("STAGE21","시드 프로젝트에 Die/Frame Subset 포함 (다양한 사이즈)", $r2?"PASS":"FAIL", $rs);

my $bar="="x110;
print "$bar\n";
printf "%-9s %-65s %-6s %s\n","CATEGORY","ITEM","RESULT","DETAIL";
print "-"x110,"\n";
my ($pass,$fail)=(0,0);
foreach my $r (@rows){ my ($c,$i,$res,$d)=@$r; printf "%-9s %-65s %-6s %s\n",$c,$i,$res,($d//""); $pass++ if $res eq "PASS"; $fail++ if $res eq "FAIL"; }
print "$bar\n";
print "TOTAL ${\scalar @rows}   PASS $pass   FAIL $fail\n";
exit ($fail>0?1:0);
