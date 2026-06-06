#!perl
# audit_threading.pl — 정적 grep 기반 thread safety audit
use strict; use warnings;
my $ROOT = "D:/Work/CDT-320/QMC.CDT-320";
my @rows; sub row { push @rows, [@_] }

sub scan_dir {
    my ($d) = @_;
    my @files;
    opendir(my $dh, $d) or return;
    while (my $f = readdir $dh) {
        next if $f =~ /^\./;
        my $p = "$d/$f";
        if (-d $p) { push @files, scan_dir($p); }
        elsif ($f =~ /\.cs$/i && $f !~ /Designer/) { push @files, $p; }
    }
    closedir $dh;
    return @files;
}

my @cs;
push @cs, scan_dir("$ROOT/QMC.CDT-320");
push @cs, scan_dir("$ROOT/QMC.Vision");
push @cs, scan_dir("$ROOT/QMC.Common");

my %stats;
foreach my $f (@cs) {
    open my $fh, '<', $f or next;
    local $/; my $c = <$fh>; close $fh;
    $stats{ConcurrentDictionary}++ if $c =~ /ConcurrentDictionary/;
    $stats{ConcurrentQueue}++       if $c =~ /ConcurrentQueue/;
    $stats{lock_keyword}            += () = $c =~ /\block\s*\(/g;
    $stats{Task_Run}                += () = $c =~ /Task\.Run/g;
    $stats{InvokeRequired}          += () = $c =~ /InvokeRequired/g;
    $stats{BeginInvoke}             += () = $c =~ /BeginInvoke/g;
    $stats{volatile_keyword}        += () = $c =~ /\bvolatile\b/g;

    # Risk: shared mutable State 없이 lock 없는 곳
    $stats{static_field_no_concurrent} += 1
        if $c =~ /private\s+static\s+(?:readonly\s+)?(?:List|Dictionary|HashSet|Queue|Stack)<[^>]+>\s+\w+\s*=/
           && $c !~ /Concurrent\w+/;
}

row("AUDIT", "ConcurrentDictionary 사용 파일 수",
    "INFO", $stats{ConcurrentDictionary} || 0);
row("AUDIT", "ConcurrentQueue 사용 파일 수",
    "INFO", $stats{ConcurrentQueue} || 0);
row("AUDIT", "lock(){} 키워드 사용 횟수",
    "INFO", $stats{lock_keyword} || 0);
row("AUDIT", "Task.Run 사용 횟수",
    "INFO", $stats{Task_Run} || 0);
row("AUDIT", "InvokeRequired 체크 횟수 (UI thread marshal)",
    "INFO", $stats{InvokeRequired} || 0);
row("AUDIT", "BeginInvoke 사용 횟수",
    "INFO", $stats{BeginInvoke} || 0);
row("AUDIT", "volatile 키워드 사용 횟수",
    "INFO", $stats{volatile_keyword} || 0);
row("AUDIT", "static List/Dictionary/HashSet 비-Concurrent 변수 (잠재 위험)",
    ($stats{static_field_no_concurrent} || 0) > 5 ? "WARN" : "OK",
    $stats{static_field_no_concurrent} || 0);

my $bar="="x110;
print "$bar\n";
print "Threading Audit (정적 grep)\n";
print "$bar\n";
foreach my $r (@rows) {
    my ($c, $i, $res, $d) = @$r;
    printf "%-9s %-65s %-6s %s\n", $c, $i, $res, ($d // "");
}
print "$bar\n";
print "Files scanned: ${\ scalar @cs}\n";
