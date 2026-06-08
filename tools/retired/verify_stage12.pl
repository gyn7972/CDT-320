#!perl
use strict; use warnings;
use FindBin; my $ROOT = "$FindBin::Bin/..";
my @rows; sub row { push @rows, [@_] }
sub greps { my ($f,$p)=@_; return 0 unless -e $f; open my $fh,'<',$f or return 0; local $/; my $c=<$fh>; close $fh; return $c=~/$p/s?1:0 }

row("BUILD","QMC.CDT-320.exe", -e "$ROOT/QMC.CDT-320/bin/Debug/QMC.CDT-320.exe"?"PASS":"FAIL","");

# 일본어
my $lang = "$ROOT/QMC.CDT-320/Ui/Localization/Lang.cs";
my $r1 = greps($lang, qr/public const string Ja\s*=\s*"ja"/) &&
        greps($lang, qr/RegisterJapanese/) &&
        greps($lang, qr/private static void J\(/) &&
        greps($lang, qr/Supported = new\[\] \{ Ko, En, Zh, Ja \}/);
row("STAGE12","Lang.cs — ja 추가 (Supported + Register + J helper)", $r1?"PASS":"FAIL", $lang);

# 알람 마스터
my $am = "$ROOT/QMC.CDT-320/Equipment/Alarms/AlarmMaster.cs";
my $r2 = greps($am, qr/enum\s+AlarmCategory/) &&
        greps($am, qr/class\s+AlarmDefinition/) &&
        greps($am, qr/static\s+class\s+AlarmMaster/) &&
        greps($am, qr/CreateDefaults/) &&
        greps($am, qr/alarm_master\.json/);
row("STAGE12","AlarmMaster — Category enum + Definition + JSON", $r2?"PASS":"FAIL", $am);

# 기본 정의 16+ 종
my $r3 = (() = `grep -c "new AlarmDefinition" "$am"` =~ /(\d+)/);
my $count = $1 // 0;
row("STAGE12","AlarmMaster 기본 정의 16+ 종", $count >= 16 ? "PASS" : "FAIL", "count=$count");

my $bar="="x110;
print "$bar\n";
printf "%-9s %-65s %-6s %s\n","CATEGORY","ITEM","RESULT","DETAIL";
print "-"x110,"\n";
my ($pass,$fail)=(0,0);
foreach my $r (@rows){ my ($c,$i,$res,$d)=@$r; printf "%-9s %-65s %-6s %s\n",$c,$i,$res,($d//""); $pass++ if $res eq "PASS"; $fail++ if $res eq "FAIL"; }
print "$bar\n";
print "TOTAL ${\scalar @rows}   PASS $pass   FAIL $fail\n";
exit ($fail>0?1:0);
