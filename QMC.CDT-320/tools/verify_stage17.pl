#!perl
use strict; use warnings;
my $ROOT = "D:/Work/CDT-320/QMC.CDT-320";
my @rows; sub row { push @rows, [@_] }
sub greps { my ($f,$p)=@_; return 0 unless -e $f; open my $fh,'<',$f or return 0; local $/; my $c=<$fh>; close $fh; return $c=~/$p/s?1:0 }

my $rvc = "$ROOT/tools/remote_viewer_client.ps1";
my $r1 = greps($rvc, qr/System\.Windows\.Forms/) &&
        greps($rvc, qr/PictureBox/) &&
        greps($rvc, qr/TcpClient/) &&
        greps($rvc, qr/FRAME\\\|/) &&
        greps($rvc, qr/FromBase64String/);
row("STAGE17","remote_viewer_client.ps1 — Form + PictureBox + TCP + FRAME\\| 파싱", $r1?"PASS":"FAIL", $rvc);

my $bar="="x110;
print "$bar\n";
printf "%-9s %-65s %-6s %s\n","CATEGORY","ITEM","RESULT","DETAIL";
print "-"x110,"\n";
my ($pass,$fail)=(0,0);
foreach my $r (@rows){ my ($c,$i,$res,$d)=@$r; printf "%-9s %-65s %-6s %s\n",$c,$i,$res,($d//""); $pass++ if $res eq "PASS"; $fail++ if $res eq "FAIL"; }
print "$bar\n";
print "TOTAL ${\scalar @rows}   PASS $pass   FAIL $fail\n";
exit ($fail>0?1:0);
