#!perl
use strict; use warnings;
my $ROOT = "D:/Work/CDT-320/QMC.CDT-320";
my @rows; sub row { push @rows, [@_] }
sub greps { my ($f,$p)=@_; return 0 unless -e $f; open my $fh,'<',$f or return 0; local $/; my $c=<$fh>; close $fh; return $c=~/$p/s?1:0 }

row("BUILD","QMC.CDT-320.exe", -e "$ROOT/QMC.CDT-320/bin/Debug/QMC.CDT-320.exe"?"PASS":"FAIL","");

my $ah = "$ROOT/QMC.CDT-320/Ui/Pages/History/AlarmHistoryPage.cs";
my $r1 = greps($ah, qr/class\s+AlarmHistoryPage/) &&
        greps($ah, qr/AlarmManager\.History/) &&
        greps($ah, qr/AlarmMaster\.Get/) &&
        greps($ah, qr/AlarmManager\.AlarmRaised \+= OnRaise/) &&
        greps($ah, qr/AlarmManager\.ClearAll/);
row("STAGE20","AlarmHistoryPage — History+Master+이벤트구독+ClearAll", $r1?"PASS":"FAIL", $ah);

my $ht = "$ROOT/QMC.CDT-320/Ui/Tabs/HistoryTab.cs";
my $r2 = greps($ht, qr/new\s+AlarmHistoryPage/);
row("STAGE20","HistoryTab — hist.alarm 새 페이지 연결", $r2?"PASS":"FAIL", $ht);

my $bar="="x110;
print "$bar\n";
printf "%-9s %-65s %-6s %s\n","CATEGORY","ITEM","RESULT","DETAIL";
print "-"x110,"\n";
my ($pass,$fail)=(0,0);
foreach my $r (@rows){ my ($c,$i,$res,$d)=@$r; printf "%-9s %-65s %-6s %s\n",$c,$i,$res,($d//""); $pass++ if $res eq "PASS"; $fail++ if $res eq "FAIL"; }
print "$bar\n";
print "TOTAL ${\scalar @rows}   PASS $pass   FAIL $fail\n";
exit ($fail>0?1:0);
