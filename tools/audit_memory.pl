#!perl
# audit_memory.pl — 정적 IDisposable 누락 감지
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

my $totalUsing       = 0;
my $totalDispose     = 0;
my $totalIDisposable = 0;
my $totalNew_Bitmap  = 0;
my $totalNew_Stream  = 0;
my $totalNew_TcpClient = 0;
my $missing = 0;

foreach my $f (@cs) {
    open my $fh, '<', $f or next;
    local $/; my $c = <$fh>; close $fh;
    $totalUsing       += () = $c =~ /\busing\s*\(/g;
    $totalDispose     += () = $c =~ /\.Dispose\s*\(/g;
    $totalIDisposable += $c =~ /:\s*IDisposable/ ? 1 : 0;
    $totalNew_Bitmap  += () = $c =~ /new\s+Bitmap\s*\(/g;
    $totalNew_Stream  += () = $c =~ /new\s+(?:MemoryStream|FileStream|StreamWriter|StreamReader)\s*\(/g;
    $totalNew_TcpClient += () = $c =~ /new\s+TcpClient\s*\(/g;

    # Risk: Bitmap 인스턴스화 후 using/Dispose 없음
    if ($c =~ /new\s+Bitmap\s*\(/ && $c !~ /Bitmap.*\.Dispose|using\s*\(\s*var\s+\w+\s*=\s*new\s+Bitmap/s) {
        $missing++;
    }
}

row("MEM", "using() 블록 사용 횟수",       "INFO", $totalUsing);
row("MEM", "Dispose() 명시 호출 횟수",     "INFO", $totalDispose);
row("MEM", "IDisposable 구현 클래스 수",   "INFO", $totalIDisposable);
row("MEM", "new Bitmap() 횟수",            "INFO", $totalNew_Bitmap);
row("MEM", "new Stream/Writer/Reader 횟수","INFO", $totalNew_Stream);
row("MEM", "new TcpClient 횟수",           "INFO", $totalNew_TcpClient);
row("MEM", "Bitmap 비-Dispose 가능성 파일", $missing > 5 ? "WARN" : "OK", $missing);

my $bar="="x110;
print "$bar\n";
print "Memory Audit (정적 grep)\n";
print "$bar\n";
foreach my $r (@rows) {
    my ($c, $i, $res, $d) = @$r;
    printf "%-9s %-65s %-6s %s\n", $c, $i, $res, ($d // "");
}
print "$bar\n";
print "Files scanned: ${\ scalar @cs}\n";
