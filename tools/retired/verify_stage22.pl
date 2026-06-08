#!perl
use strict; use warnings;
use FindBin; my $ROOT = "$FindBin::Bin/..";
my @rows; sub row { push @rows, [@_] }
sub greps { my ($f,$p)=@_; return 0 unless -e $f; open my $fh,'<',$f or return 0; local $/; my $c=<$fh>; close $fh; return $c=~/$p/s?1:0 }

row("BUILD","QMC.CDT-320.exe", -e "$ROOT/QMC.CDT-320/bin/Debug/QMC.CDT-320.exe"?"PASS":"FAIL","");

my $g = "$ROOT/QMC.CDT-320/Equipment/DieMaps/DieMapGenerator.cs";
my $r1 = greps($g, qr/static\s+DieMap\s+LoadCsv/) &&
        greps($g, qr/static\s+DieMap\s+Load\(/);
row("STAGE22","DieMapGenerator — LoadCsv + Load(auto-detect)", $r1?"PASS":"FAIL", $g);

# DieMapPage 의 LOAD 버튼이 .csv 도 받게 확장됐는지 확인 (현재는 JSON 만)
my $dp = "$ROOT/QMC.CDT-320/Ui/Pages/Material/DieMapPage.cs";
my $r2 = greps($dp, qr/DieMapGenerator\.Load(?:Csv|\()/);
row("STAGE22","DieMapPage — Load 확장 (CSV 호환)", $r2?"PASS":"INFO", $dp);

my $bar="="x110;
print "$bar\n";
printf "%-9s %-65s %-6s %s\n","CATEGORY","ITEM","RESULT","DETAIL";
print "-"x110,"\n";
my ($pass,$fail)=(0,0);
foreach my $r (@rows){ my ($c,$i,$res,$d)=@$r; printf "%-9s %-65s %-6s %s\n",$c,$i,$res,($d//""); $pass++ if $res eq "PASS"; $fail++ if $res eq "FAIL"; }
print "$bar\n";
print "TOTAL ${\scalar @rows}   PASS $pass   FAIL $fail\n";
exit ($fail>0?1:0);
