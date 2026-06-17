#!perl
# verify_binmap.pl — 원형 GOOD/NG 빈맵 기능(에디터 재사용 + 런타임 일원화) 정적 검증.
use strict; use warnings;
use FindBin; my $ROOT = "$FindBin::Bin/..";
my @rows; sub row { push @rows, [@_] }
sub slurp { my ($f)=@_; return "" unless -e $f; open my $fh,'<',$f or return ""; local $/; my $c=<$fh>; close $fh; return $c }
sub has  { my ($f,$p)=@_; return slurp($f)=~/$p/s?1:0 }
sub lacks{ my ($f,$p)=@_; return -e $f ? (slurp($f)=~/$p/s?0:1) : 1 }

my $store = "$ROOT/QMC.CDT-320/Equipment/Recipes/RecipeStore.cs";
my $paths = "$ROOT/QMC.CDT-320/Equipment/Recipes/RecipeMapPaths.cs";
my $csproj= "$ROOT/QMC.CDT-320/QMC.CDT-320.csproj";
my $page  = "$ROOT/QMC.CDT-320/Ui/Pages/Recipe/MapCreatePage.cs";
my $dsgn  = "$ROOT/QMC.CDT-320/Ui/Pages/Recipe/MapCreatePage.Designer.cs";
my $tab   = "$ROOT/QMC.CDT-320/Ui/Tabs/RecipeTab.cs";
my $lang  = "$ROOT/QMC.CDT-320/Ui/Localization/Lang.cs";
my $outpg = "$ROOT/QMC.CDT-320/Ui/Pages/Work/OutputStageMapTransferPage.cs";
my $mss   = "$ROOT/QMC.CDT-320/Equipment/Materials/MaterialStateService.cs";
my $unit  = "$ROOT/QMC.CDT-320/Equipment/Unit/OutputStageUnit.cs";
my $orcp  = "$ROOT/QMC.CDT-320/Ui/Pages/Recipe/OutputStageRecipePage.cs";

# 1) 레시피 스키마 — GOOD/NG 빈맵 파일명 필드 + 레거시 폴백 유지
row("SCHEMA","RecipeProject GoodBinDieMapFileName/NgBinDieMapFileName + OutputDieMapFileName(legacy)",
    (has($store,qr/GoodBinDieMapFileName/) && has($store,qr/NgBinDieMapFileName/) && has($store,qr/OutputDieMapFileName/))?"PASS":"FAIL", $store);

# 2) 공용 경로 헬퍼 — 종류 enum + 핵심 API + 접미사 + csproj 등록
row("PATHS","RecipeMapPaths: RecipeMapKind(Input/GoodBin/NgBin) + ResolveConfigured/SetConfiguredFileName/FileSuffix",
    (has($paths,qr/enum\s+RecipeMapKind/) && has($paths,qr/GoodBin/) && has($paths,qr/NgBin/) &&
     has($paths,qr/ResolveConfigured/) && has($paths,qr/SetConfiguredFileName/) &&
     has($paths,qr/GoodBinDieMap/) && has($paths,qr/NgBinDieMap/))?"PASS":"FAIL", $paths);
row("PATHS","csproj Compile에 RecipeMapPaths.cs 등록",
    has($csproj,qr/Equipment\\Recipes\\RecipeMapPaths\.cs/)?"PASS":"FAIL", $csproj);

# 3) MapCreatePage 3-모드 + OK/NG 토글 + 원형 고정 + 직사각 분기 제거
row("EDITOR","MapCreatePage 3-모드(MapEditorMode/CurrentMapKind) + binMapCreate 진입",
    (has($page,qr/enum\s+MapEditorMode/) && has($page,qr/CurrentMapKind/) && has($page,qr/recipe\.binMapCreate/))?"PASS":"FAIL", $page);
row("EDITOR","MapCreatePage GOOD/NG 토글(ConfigureBinSideToggle/OnBinSideChanged/rbBinGood/rbBinNg)",
    (has($page,qr/ConfigureBinSideToggle/) && has($page,qr/OnBinSideChanged/) &&
     has($page,qr/rbBinGood/) && has($page,qr/rbBinNg/))?"PASS":"FAIL", $page);
row("EDITOR","MapCreatePage 직사각 생성(CreateRectMapFromRecipe) 제거",
    lacks($page,qr/CreateRectMapFromRecipe/)?"PASS":"FAIL", $page);
row("EDITOR","MapCreatePage 경로 헬퍼는 RecipeMapPaths 위임(중복 제거)",
    (has($page,qr/RecipeMapPaths\.ResolveConfigured/) && has($page,qr/RecipeMapPaths\.SetConfiguredFileName/))?"PASS":"FAIL", $page);

# 4) Designer — 독립 라디오 그룹(binSidePanel) + 두 라디오
row("DESIGNER","binSidePanel(독립 그룹) + rbBinGood/rbBinNg 선언/배치",
    (has($dsgn,qr/binSidePanel/) && has($dsgn,qr/this\.rbBinGood/) && has($dsgn,qr/this\.rbBinNg/) &&
     has($dsgn,qr/binSidePanel\.Controls\.Add\(this\.rbBinGood\)/))?"PASS":"FAIL", $dsgn);

# 5) 사이드바 + i18n
row("NAV","RecipeTab 출력 버튼 → recipe.binMapCreate",
    has($tab,qr/recipe\.binMapCreate/)?"PASS":"FAIL", $tab);
row("NAV","Lang recipe.binMapCreate 등록",
    has($lang,qr/recipe\.binMapCreate/)?"PASS":"FAIL", $lang);

# 6) OutputStageMapTransferPage — 원형 빈맵 로드 + 직사각 코드 제거
row("OUTPAGE","LoadRecipeBinMap(원형) + RecipeMapPaths 사용",
    (has($outpg,qr/LoadRecipeBinMap/) && has($outpg,qr/RecipeMapPaths\.ResolveConfigured/))?"PASS":"FAIL", $outpg);
row("OUTPAGE","직사각 코드 제거(BuildBinTrayMap/CreateBinDieMap/PersistBinDieMapToMaterialState)",
    (lacks($outpg,qr/BuildBinTrayMap/) && lacks($outpg,qr/CreateBinDieMap/) &&
     lacks($outpg,qr/PersistBinDieMapToMaterialState/))?"PASS":"FAIL", $outpg);

# 7) MaterialStateService — 자동사이클 원형 빈맵 소스화
row("RUNTIME","MaterialStateService LoadRecipeBinMap + Initialize/Reserve가 사용",
    (has($mss,qr/private\s+static\s+DieMap\s+LoadRecipeBinMap/) &&
     has($mss,qr/DieMap\s+binMap\s*=\s*LoadRecipeBinMap\(side\)/))?"PASS":"FAIL", $mss);
row("RUNTIME","MaterialStateService 직사각 빌더(BuildBinDieMapFromWafer/ResolveBinDies) 제거",
    (lacks($mss,qr/BuildBinDieMapFromWafer/) && lacks($mss,qr/ResolveBinDies/))?"PASS":"FAIL", $mss);

# 8) 직사각 BinTray 레시피/UI 잔재 제거
row("CLEANUP","OutputStageUnit BinTrayLayout/GoodBinTray/NgBinTray 제거",
    (lacks($unit,qr/class\s+BinTrayLayout/) && lacks($unit,qr/GoodBinTray/) && lacks($unit,qr/NgBinTray/))?"PASS":"FAIL", $unit);
row("CLEANUP","OutputStageRecipePage AddBinTrayGroup 제거",
    lacks($orcp,qr/AddBinTrayGroup/)?"PASS":"FAIL", $orcp);

my $bar="="x110;
print "$bar\n";
printf "%-9s %-72s %-6s\n","CATEGORY","ITEM","RESULT";
print "-"x110,"\n";
my ($pass,$fail)=(0,0);
foreach my $r (@rows){ my ($c,$i,$res,$d)=@$r; printf "%-9s %-72s %-6s\n",$c,$i,$res; $pass++ if $res eq "PASS"; $fail++ if $res eq "FAIL"; }
print "$bar\n";
print "TOTAL ${\scalar @rows}   PASS $pass   FAIL $fail\n";
exit ($fail>0?1:0);
