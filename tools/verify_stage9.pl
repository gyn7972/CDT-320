#!perl
use strict; use warnings;
my $ROOT = "D:/Work/CDT-320/QMC.CDT-320";
my @rows; sub row { push @rows, [@_] }
sub greps { my ($f,$p)=@_; return 0 unless -e $f; open my $fh,'<',$f or return 0; local $/; my $c=<$fh>; close $fh; return $c=~/$p/s?1:0 }

row("BUILD","QMC.CDT-320.exe", -e "$ROOT/QMC.CDT-320/bin/Debug/QMC.CDT-320.exe"?"PASS":"FAIL","");

# Stage 9 — Documentation outputs (README + ARCHITECTURE)
my $readme = "$ROOT/README.md";
row("STAGE9","README.md exists (system overview doc)", (-e $readme)?"PASS":"FAIL", $readme);

my $arch = "$ROOT/ARCHITECTURE.md";
row("STAGE9","ARCHITECTURE.md exists (component + comm diagram)", (-e $arch)?"PASS":"FAIL", $arch);

# audit_threading.pl exists (Stage 9 deliverable)
my $audit_t = "$ROOT/tools/audit_threading.pl";
row("STAGE9","audit_threading.pl exists (ConcurrentDictionary/lock/volatile audit)",
    (-e $audit_t)?"PASS":"FAIL", $audit_t);

my $bar="="x110;
print "$bar\n";
printf "%-9s %-65s %-6s %s\n","CATEGORY","ITEM","RESULT","DETAIL";
print "-"x110,"\n";
my ($pass,$fail)=(0,0);
foreach my $r (@rows){ my ($c,$i,$res,$d)=@$r; printf "%-9s %-65s %-6s %s\n",$c,$i,$res,($d//""); $pass++ if $res eq "PASS"; $fail++ if $res eq "FAIL"; }
print "$bar\n";
print "TOTAL ${\scalar @rows}   PASS $pass   FAIL $fail\n";
exit ($fail>0?1:0);
