#!perl
use strict; use warnings;
my $ROOT = "D:/Work/CDT-320/QMC.CDT-320";
my @rows; sub row { push @rows, [@_] }
sub greps { my ($f,$p)=@_; return 0 unless -e $f; open my $fh,'<',$f or return 0; local $/; my $c=<$fh>; close $fh; return $c=~/$p/s?1:0 }

row("BUILD","QMC.CDT-320.exe", -e "$ROOT/QMC.CDT-320/bin/Debug/QMC.CDT-320.exe"?"PASS":"FAIL","");

my $lang = "$ROOT/QMC.CDT-320/Ui/Localization/Lang.cs";
my $r1 = greps($lang, qr/public const string Zh\s*=\s*"zh-CN"/) &&
        greps($lang, qr/Supported = new\[\] \{ Ko, En, Zh/) &&    # 후속 언어 추가 가능
        greps($lang, qr/RegisterChinese/) &&
        greps($lang, qr/private static void Z\(/);
row("STAGE11","Lang.cs — zh-CN 추가 + Supported + RegisterChinese + Z helper", $r1?"PASS":"FAIL", $lang);

my $r2 = greps($lang, qr/Z\("app\.title".*"CDT-320 \xe8\xae\xbe\xe5\xa4\x87"/) || # 设备
        greps($lang, qr/Z\("tab\.work".*"\xe5\xb7\xa5\xe4\xbd\x9c"/);  # 工作
row("STAGE11","Chinese key 50+ 등록 (UTF-8)", $r2?"PASS":"FAIL", $lang);

# T() fallback 로직 — current 미번역 시 영어로 fallback
my $r3 = greps($lang, qr/d\.TryGetValue\(En, out var efallback\)/);
row("STAGE11","T() — Chinese 미번역 시 English fallback", $r3?"PASS":"FAIL", $lang);

my $bar="="x110;
print "$bar\n";
printf "%-9s %-65s %-6s %s\n","CATEGORY","ITEM","RESULT","DETAIL";
print "-"x110,"\n";
my ($pass,$fail)=(0,0);
foreach my $r (@rows){ my ($c,$i,$res,$d)=@$r; printf "%-9s %-65s %-6s %s\n",$c,$i,$res,($d//""); $pass++ if $res eq "PASS"; $fail++ if $res eq "FAIL"; }
print "$bar\n";
print "TOTAL ${\scalar @rows}   PASS $pass   FAIL $fail\n";
exit ($fail>0?1:0);
