#!perl
use strict; use warnings;
use FindBin; my $ROOT = "$FindBin::Bin/..";
my @rows; sub row { push @rows, [@_] }
sub greps { my ($f,$p)=@_; return 0 unless -e $f; open my $fh,'<',$f or return 0; local $/; my $c=<$fh>; close $fh; return $c=~/$p/s?1:0 }

row("BUILD","QMC.CDT-320.exe", -e "$ROOT/QMC.CDT-320/bin/Debug/QMC.CDT-320.exe"?"PASS":"FAIL","");

my $am = "$ROOT/QMC.CDT-320/Equipment/Alarms/AlarmMaster.cs";
my $r1 = greps($am, qr/TitleEn/) && greps($am, qr/CauseEn/) && greps($am, qr/ActionEn/) &&
        greps($am, qr/GetTitle\(string lang\)/);
row("STAGE23","AlarmDefinition — TitleEn/CauseEn/ActionEn + GetTitle(lang)", $r1?"PASS":"FAIL", $am);

my $r2 = greps($am, qr/TitleEn="Home search failed"/) &&
        greps($am, qr/TitleEn="Interlock blocked"/) &&
        greps($am, qr/TitleEn="Vision match failed"/) &&
        greps($am, qr/TitleEn="Pick failed"/) &&
        greps($am, qr/TitleEn="Emergency Stop"/);
row("STAGE23","핵심 5 알람 영어 번역 (HOME/INTERLOCK/VisionMatch/Pick/EMG)", $r2?"PASS":"FAIL", $am);

my $ah = "$ROOT/QMC.CDT-320/Ui/Pages/History/AlarmHistoryPage.cs";
my $r3 = greps($ah, qr/Lang\.Current/) && greps($ah, qr/GetCause\(lang\)/) && greps($ah, qr/GetAction\(lang\)/);
row("STAGE23","AlarmHistoryPage — Lang.Current 적용", $r3?"PASS":"FAIL", $ah);

my $mgr = "$ROOT/QMC.CDT-320/Equipment/Alarms/AlarmManager.cs";
my $r4 = greps($mgr, qr/Lang\.Current/) && greps($mgr, qr/def\.GetTitle\(lang\)/);
row("STAGE23","AlarmManager — message 비어있을 때 Lang.Current 활용", $r4?"PASS":"FAIL", $mgr);

my $bar="="x110;
print "$bar\n";
printf "%-9s %-65s %-6s %s\n","CATEGORY","ITEM","RESULT","DETAIL";
print "-"x110,"\n";
my ($pass,$fail)=(0,0);
foreach my $r (@rows){ my ($c,$i,$res,$d)=@$r; printf "%-9s %-65s %-6s %s\n",$c,$i,$res,($d//""); $pass++ if $res eq "PASS"; $fail++ if $res eq "FAIL"; }
print "$bar\n";
print "TOTAL ${\scalar @rows}   PASS $pass   FAIL $fail\n";
exit ($fail>0?1:0);
