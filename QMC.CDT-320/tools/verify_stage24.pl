#!perl
# verify_stage24.pl — Stage 24: --auto-cycle 명령행 옵션 + Lot JSON 결과 자동 검증
use strict; use warnings;

my $ROOT = "D:/Work/CDT-320/QMC.CDT-320";
my @rows; sub row { push @rows, [@_] }
sub greps { my ($f,$p)=@_; return 0 unless -e $f; open my $fh,'<',$f or return 0; local $/; my $c=<$fh>; close $fh; return $c=~/$p/s?1:0 }

my $prog = "$ROOT/QMC.CDT-320/Program.cs";
my $r1 = greps($prog, qr/AutoCycleCount/) &&
        greps($prog, qr/--auto-cycle/) &&
        greps($prog, qr/static void Main\(string\[\] args\)/);
row("STAGE24","Program.cs — --auto-cycle N CLI 인수", $r1?"PASS":"FAIL", $prog);

my $f1 = "$ROOT/QMC.CDT-320/Form1.cs";
my $r2 = greps($f1, qr/Program\.AutoCycleCount/) &&
        greps($f1, qr/Controller\.InitAsync/) &&
        greps($f1, qr/Controller\.CycleRunAsync\(n\)/);
row("STAGE24","Form1 — auto-cycle Init+CycleRun+Close", $r2?"PASS":"FAIL", $f1);

# 빌드 산출물
row("BUILD","QMC.CDT-320.exe", -e "$ROOT/QMC.CDT-320/bin/Debug/QMC.CDT-320.exe"?"PASS":"FAIL","");

# Lot JSON 결과 검증 (auto-cycle 실행 후 생성된 파일이 있는지)
my $lotDir = "$ROOT/QMC.CDT-320/bin/Debug/Log/Lots";
my @lots = ();
if (-d $lotDir) {
    opendir(my $dh, $lotDir);
    while (my $f = readdir $dh) {
        push @lots, "$lotDir/$f" if $f =~ /\.json$/;
    }
    closedir $dh;
}
row("STAGE24","Lot JSON 파일 1개 이상 (auto-cycle 후)",
    @lots > 0 ? "PASS" : "FAIL", "count=".scalar(@lots));

if (@lots > 0) {
    my $latest = (sort { -M $a <=> -M $b } @lots)[0];
    open my $fh, '<', $latest; local $/; my $c = <$fh>; close $fh;
    my $procOk = $c =~ /"ProcessedDies":(\d+)/ && $1 > 0;
    my $stateOk = $c =~ /"State":\d+/;
    my $binOk = $c =~ /"BinDistribution":\[/;
    row("STAGE24","Lot.ProcessedDies > 0 + State + BinDistribution",
        ($procOk && $stateOk && $binOk) ? "PASS" : "FAIL",
        $c =~ /"ProcessedDies":(\d+).*"GoodCount":(\d+).*"NgCount":(\d+)/s
            ? "processed=$1 good=$2 ng=$3" : "");
}

my $bar="="x110;
print "$bar\n";
printf "%-9s %-65s %-6s %s\n","CATEGORY","ITEM","RESULT","DETAIL";
print "-"x110,"\n";
my ($pass,$fail)=(0,0);
foreach my $r (@rows){ my ($c,$i,$res,$d)=@$r; printf "%-9s %-65s %-6s %s\n",$c,$i,$res,($d//""); $pass++ if $res eq "PASS"; $fail++ if $res eq "FAIL"; }
print "$bar\n";
print "TOTAL ${\scalar @rows}   PASS $pass   FAIL $fail\n";
exit ($fail>0?1:0);
