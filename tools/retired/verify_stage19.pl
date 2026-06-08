#!perl
use strict; use warnings;
use FindBin; my $ROOT = "$FindBin::Bin/..";
my @rows; sub row { push @rows, [@_] }
sub greps { my ($f,$p)=@_; return 0 unless -e $f; open my $fh,'<',$f or return 0; local $/; my $c=<$fh>; close $fh; return $c=~/$p/s?1:0 }

row("BUILD","QMC.CDT-320.exe", -e "$ROOT/QMC.CDT-320/bin/Debug/QMC.CDT-320.exe"?"PASS":"FAIL","");

my $am = "$ROOT/QMC.CDT-320/Ui/Pages/Settings/AlarmMasterPage.cs";
my $r1 = greps($am, qr/class\s+AlarmMasterPage/) &&
        greps($am, qr/AlarmMaster\.ByCode/) &&
        greps($am, qr/Code.*Category.*Severity.*Title.*Cause.*Action/s) &&
        greps($am, qr/AlarmMaster\.Save/);
row("STAGE19","AlarmMasterPage — DataGridView + Save + 카테고리 필터", $r1?"PASS":"FAIL", $am);

my $st = "$ROOT/QMC.CDT-320/Ui/Tabs/SettingsTab.cs";
my $r2 = greps($st, qr/settings\.alarmMaster/) && greps($st, qr/new\s+AlarmMasterPage/);
row("STAGE19","SettingsTab — alarmMaster 사이드바 등록", $r2?"PASS":"FAIL", $st);

my $mgr = "$ROOT/QMC.CDT-320/Equipment/Alarms/AlarmManager.cs";
my $r3 = greps($mgr, qr/AlarmMaster\.Get\(code\)/) &&
        greps($mgr, qr/IsNullOrEmpty\(message\)/);
row("STAGE19","AlarmManager — message 비어있을 때 AlarmMaster.Get fallback", $r3?"PASS":"FAIL", $mgr);

my $lang = "$ROOT/QMC.CDT-320/Ui/Localization/Lang.cs";
my $r4 = greps($lang, qr/"settings\.alarmMaster"/);
row("STAGE19","i18n settings.alarmMaster 키", $r4?"PASS":"FAIL", $lang);

my $bar="="x110;
print "$bar\n";
printf "%-9s %-65s %-6s %s\n","CATEGORY","ITEM","RESULT","DETAIL";
print "-"x110,"\n";
my ($pass,$fail)=(0,0);
foreach my $r (@rows){ my ($c,$i,$res,$d)=@$r; printf "%-9s %-65s %-6s %s\n",$c,$i,$res,($d//""); $pass++ if $res eq "PASS"; $fail++ if $res eq "FAIL"; }
print "$bar\n";
print "TOTAL ${\scalar @rows}   PASS $pass   FAIL $fail\n";
exit ($fail>0?1:0);
