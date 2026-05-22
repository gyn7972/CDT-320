#!perl
use strict; use warnings;
my $ROOT = "D:/Work/CDT-320/QMC.CDT-320";
my @rows; sub row { push @rows, [@_] }
sub greps { my ($f,$p)=@_; return 0 unless -e $f; open my $fh,'<',$f or return 0; local $/; my $c=<$fh>; close $fh; return $c=~/$p/s?1:0 }

row("BUILD","QMC.CDT-320.exe", -e "$ROOT/QMC.CDT-320/bin/Debug/QMC.CDT-320.exe"?"PASS":"FAIL","");

my $msg = "$ROOT/QMC.CDT-320/Equipment/Secs/SecsMessage.cs";
my $r1 = greps($msg, qr/SecsMessage\s+S1F14/) &&
        greps($msg, qr/SecsMessage\s+S2F41/) &&
        greps($msg, qr/SecsMessage\s+S2F42/) &&
        greps($msg, qr/SecsMessage\s+S5F1/) &&
        greps($msg, qr/SecsMessage\s+S5F2/) &&
        greps($msg, qr/SecsMessage\s+S5F3/) &&
        greps($msg, qr/SecsMessage\s+S6F11/) &&
        greps($msg, qr/SecsMessage\s+S6F12/) &&
        greps($msg, qr/SecsMessage\s+S7F3/) &&
        greps($msg, qr/SecsMessage\s+S7F4/) &&
        greps($msg, qr/SecsMessage\s+S9F1/) &&
        greps($msg, qr/SecsMessage\s+S9F3/) &&
        greps($msg, qr/SecsMessage\s+S9F5/);
row("STAGE7","SecsMessage — 13 헬퍼 메서드 (S1/2/5/6/7/9 Stream)", $r1?"PASS":"FAIL", $msg);

my $sh = "$ROOT/QMC.CDT-320/Equipment/Secs/SecsHost.cs";
my $r2 = greps($sh, qr/AlarmManager\.AlarmRaised \+= OnAlarmRaised/) &&
        greps($sh, qr/private void OnAlarmRaised/) &&
        greps($sh, qr/RaiseEvent.*AlarmPosted/);
row("STAGE7","SecsHost — AlarmManager.AlarmRaised 자동 구독 → S5F1/Event broadcast", $r2?"PASS":"FAIL", $sh);

my $bar="="x110;
print "$bar\n";
printf "%-9s %-65s %-6s %s\n","CATEGORY","ITEM","RESULT","DETAIL";
print "-"x110,"\n";
my ($pass,$fail)=(0,0);
foreach my $r (@rows){ my ($c,$i,$res,$d)=@$r; printf "%-9s %-65s %-6s %s\n",$c,$i,$res,($d//""); $pass++ if $res eq "PASS"; $fail++ if $res eq "FAIL"; }
print "$bar\n";
print "TOTAL ${\scalar @rows}   PASS $pass   FAIL $fail\n";
exit ($fail>0?1:0);
