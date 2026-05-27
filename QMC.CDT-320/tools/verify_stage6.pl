#!perl
use strict; use warnings;
my $ROOT = "D:/Work/CDT-320/QMC.CDT-320";
my @rows; sub row { push @rows, [@_] }
sub greps { my ($f,$p)=@_; return 0 unless -e $f; open my $fh,'<',$f or return 0; local $/; my $c=<$fh>; close $fh; return $c=~/$p/s?1:0 }

row("BUILD","QMC.Vision.exe", -e "$ROOT/QMC.Vision/bin/Debug/QMC.Vision.exe"?"PASS":"FAIL","");

my $i = "$ROOT/QMC.Vision/Core/IEdgeFinder.cs";
my $r1 = greps($i, qr/interface\s+IEdgeFinder/) &&
        greps($i, qr/interface\s+IHistogramAnalyzer/) &&
        greps($i, qr/interface\s+IColorMatcher/) &&
        greps($i, qr/class\s+EdgeMeasurement/) &&
        greps($i, qr/class\s+HistogramResult/) &&
        greps($i, qr/class\s+ColorMatchResult/);
row("STAGE6","IEdgeFinder + IHistogramAnalyzer + IColorMatcher + 3 result class", $r1?"PASS":"FAIL", $i);

my $cal = "$ROOT/QMC.Vision/Backends/Cognex/CognexCaliper.cs";
my $r2 = greps($cal, qr/class\s+CognexCaliper\s*:\s*IEdgeFinder/) &&
        greps($cal, qr/Cognex\.VisionPro\.Caliper\.CogCaliperTool/) &&
        greps($cal, qr/MeasureFallback/);
row("STAGE6","CognexCaliper — CogCaliperTool 동적 + fallback", $r2?"PASS":"FAIL", $cal);

my $h = "$ROOT/QMC.Vision/Backends/Cognex/CognexHistogram.cs";
my $r3 = greps($h, qr/class\s+CognexHistogram\s*:\s*IHistogramAnalyzer/) &&
        greps($h, qr/CogHistogramTool/) &&
        greps($h, qr/AnalyzeFallback/);
row("STAGE6","CognexHistogram — CogHistogramTool 동적 + fallback", $r3?"PASS":"FAIL", $h);

my $cm = "$ROOT/QMC.Vision/Backends/Cognex/CognexColorMatch.cs";
my $r4 = greps($cm, qr/class\s+CognexColorMatch\s*:\s*IColorMatcher/) &&
        greps($cm, qr/CogColorMatchTool/) &&
        greps($cm, qr/MatchFallback/);
row("STAGE6","CognexColorMatch — CogColorMatchTool 동적 + RGB fallback", $r4?"PASS":"FAIL", $cm);

my $bar="="x110;
print "$bar\n";
printf "%-9s %-65s %-6s %s\n","CATEGORY","ITEM","RESULT","DETAIL";
print "-"x110,"\n";
my ($pass,$fail)=(0,0);
foreach my $r (@rows){ my ($c,$i,$res,$d)=@$r; printf "%-9s %-65s %-6s %s\n",$c,$i,$res,($d//""); $pass++ if $res eq "PASS"; $fail++ if $res eq "FAIL"; }
print "$bar\n";
print "TOTAL ${\scalar @rows}   PASS $pass   FAIL $fail\n";
exit ($fail>0?1:0);
