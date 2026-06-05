#!perl
# verify_stage15.pl — gui_cycle_automation.ps1 syntax/structure 검증
# 실 GUI 클릭 동작 검증은 i18n 영어 모드 + UIA 호환성 별도 라운드.
use strict; use warnings;

my $ROOT = "D:/Work/CDT-320/QMC.CDT-320";
my @rows; sub row { push @rows, [@_] }
sub greps { my ($f,$p)=@_; return 0 unless -e $f; open my $fh,'<',$f or return 0; local $/; my $c=<$fh>; close $fh; return $c=~/$p/s?1:0 }

my $ps = "$ROOT/tools/gui_cycle_automation.ps1";
my $r1 = greps($ps, qr/Add-Type -AssemblyName UIAutomationClient/) &&
        greps($ps, qr/Find-CDT320Window/) &&
        greps($ps, qr/Find-ButtonByText/) &&
        greps($ps, qr/Click-Element/) &&
        greps($ps, qr/InvokePattern/);
row("STAGE15","gui_cycle_automation.ps1 — UIA 4 함수 + InvokePattern", $r1?"PASS":"FAIL", $ps);

# ASCII only 검증 (인코딩 안전성)
my $r2 = 1;
if (-e $ps) {
    open my $fh, '<', $ps;
    my @lines = <$fh>; close $fh;
    foreach my $line (@lines) {
        # bytes > 127 (non-ASCII)
        foreach my $b (unpack("C*", $line)) {
            if ($b > 127) { $r2 = 0; last; }
        }
        last unless $r2;
    }
}
row("STAGE15","ASCII only (PowerShell 인코딩 안전)", $r2?"PASS":"FAIL", "");

# Initialize/Cycle Run/Cycle Stop 영문 패턴 매칭
my $r3 = greps($ps, qr/"INITIALIZE",\s*"Initialize"/) &&
        greps($ps, qr/"CYCLE RUN",\s*"CycleRun"/) &&
        greps($ps, qr/"CYCLE STOP",\s*"CycleStop"/);
row("STAGE15","UIA 패턴 영문 — Initialize/CycleRun/CycleStop", $r3?"PASS":"FAIL", $ps);

# 운영 안내: 한국어 i18n 모드의 버튼 매칭은 별도 라운드 (PowerShell ANSI 인코딩 + UTF-8 한국어 호환 작업 필요)
row("STAGE15","[NOTE] 실 GUI 클릭 검증 — i18n=en 으로 변경 후 또는 좌표 시뮬 도구 별도",
    "INFO", "본 라운드는 스크립트 정합성만, 실 클릭은 다음 라운드");

my $bar="="x110;
print "$bar\n";
printf "%-9s %-65s %-6s %s\n","CATEGORY","ITEM","RESULT","DETAIL";
print "-"x110,"\n";
my ($pass,$fail)=(0,0);
foreach my $r (@rows){ my ($c,$i,$res,$d)=@$r; printf "%-9s %-65s %-6s %s\n",$c,$i,$res,($d//""); $pass++ if $res eq "PASS"; $fail++ if $res eq "FAIL"; }
print "$bar\n";
print "TOTAL ${\scalar @rows}   PASS $pass   FAIL $fail\n";
exit ($fail>0?1:0);
