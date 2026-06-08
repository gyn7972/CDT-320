#!perl
use strict; use warnings;
use FindBin; my $ROOT = "$FindBin::Bin/..";
my @rows; sub row { push @rows, [@_] }
sub greps { my ($f,$p)=@_; return 0 unless -e $f; open my $fh,'<',$f or return 0; local $/; my $c=<$fh>; close $fh; return $c=~/$p/s?1:0 }

row("BUILD","QMC.CDT-320.exe", -e "$ROOT/QMC.CDT-320/bin/Debug/QMC.CDT-320.exe"?"PASS":"FAIL","");

# Stage 10 — Static Audit (audit_memory.pl) + USER_GUIDE
my $audit_m = "$ROOT/tools/audit_memory.pl";
row("STAGE10","audit_memory.pl exists (Bitmap leak detection)",
    (-e $audit_m)?"PASS":"FAIL", $audit_m);

# audit_memory.pl content sanity — Bitmap or Dispose pattern
my $r = greps($audit_m, qr/Bitmap|Dispose/);
row("STAGE10","audit_memory.pl scans Bitmap/Dispose patterns",
    $r?"PASS":"FAIL", $audit_m);

# audit_threading.pl content sanity — ConcurrentDictionary/lock/volatile
my $audit_t = "$ROOT/tools/audit_threading.pl";
my $r2 = greps($audit_t, qr/ConcurrentDictionary|lock|volatile/);
row("STAGE10","audit_threading.pl scans concurrency primitives",
    $r2?"PASS":"FAIL", $audit_t);

my $bar="="x110;
print "$bar\n";
printf "%-9s %-65s %-6s %s\n","CATEGORY","ITEM","RESULT","DETAIL";
print "-"x110,"\n";
my ($pass,$fail)=(0,0);
foreach my $r (@rows){ my ($c,$i,$res,$d)=@$r; printf "%-9s %-65s %-6s %s\n",$c,$i,$res,($d//""); $pass++ if $res eq "PASS"; $fail++ if $res eq "FAIL"; }
print "$bar\n";
print "TOTAL ${\scalar @rows}   PASS $pass   FAIL $fail\n";
exit ($fail>0?1:0);
