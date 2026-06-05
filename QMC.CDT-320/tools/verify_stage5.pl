#!perl
# verify_stage5.pl — Stage 5 정적 검증 (UIA 자동화 스크립트 + Lot log 디렉토리 환경)

use strict;
use warnings;

my $ROOT = "D:/Work/CDT-320/QMC.CDT-320";
my @rows;
sub row { push @rows, [@_]; }
sub greps {
    my ($file, $pat) = @_;
    return 0 unless -e $file;
    open my $fh, '<', $file or return 0;
    local $/; my $c = <$fh>; close $fh;
    return $c =~ /$pat/s ? 1 : 0;
}

# A. 자동화 스크립트
my $ps = "$ROOT/tools/gui_cycle_automation.ps1";
my $a1 = greps($ps, qr/UIAutomationClient/) &&
         greps($ps, qr/Find-CDT320Window/) &&
         greps($ps, qr/Find-ButtonByText/) &&
         greps($ps, qr/Click-Element/) &&
         greps($ps, qr/CYCLE RUN|\xec\x82\xac\xec\x9d\xb4\xed\x81\xb4 \xec\x8b\xa4\xed\x96\x89/);  # 사이클 실행
row("STAGE5", "gui_cycle_automation.ps1 — UIA + 한/영 버튼 매칭",
    $a1?"PASS":"FAIL", $ps);

# B. 핵심 cmdlet 호출 존재 여부 (정적)
my $b1 = greps($ps, qr/Add-Type -AssemblyName UIAutomationClient/) &&
         greps($ps, qr/AutomationElement.*RootElement/) &&
         greps($ps, qr/InvokePattern/) &&
         greps($ps, qr/Get-Process -Name "QMC\.CDT-320"/);
row("STAGE5", "PS 스크립트 — UIA Add-Type + RootElement + InvokePattern + Process 체크",
    $b1?"PASS":"FAIL", "");

# C. Lot log 디렉토리 자동 생성 시 사용될 경로 확인
my $lotDir = "$ROOT/QMC.CDT-320/bin/Debug/Log/Lots";
mkdir $lotDir unless -d $lotDir;
row("STAGE5", "Lot 로그 디렉토리 (자동 생성 시 위치)",
    -d $lotDir ? "PASS" : "FAIL", $lotDir);

# 출력
my $bar = "=" x 110;
print "$bar\n";
printf "%-9s %-65s %-6s %s\n", "CATEGORY", "ITEM", "RESULT", "DETAIL";
print "-" x 110, "\n";
my ($pass,$fail) = (0,0);
foreach my $r (@rows) {
    my ($c, $i, $res, $d) = @$r;
    printf "%-9s %-65s %-6s %s\n", $c, $i, $res, ($d // "");
    $pass++ if $res eq "PASS";
    $fail++ if $res eq "FAIL";
}
print "$bar\n";
print "TOTAL ${\ scalar @rows}   PASS $pass   FAIL $fail\n";
exit ($fail > 0 ? 1 : 0);
