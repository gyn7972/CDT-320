# R1 — Handler ↔ Vision API 미러 대조표

> 생성: 2026-06-08. R1: Vision 에 ParameterGrid 계열 + NumericKeypadDialog 재구현(결정 c).
> **public API 는 Handler 와 1:1 미러** — 향후 (a) QMC.Common 공용화 시 드롭인 교체 가능.
> Handler(`QMC.CDT_320.Ui.Controls`) **무수정**(읽기만). Vision 신규 namespace = `QMC.Vision.Ui.Controls`.

## 의존성 치환 (내부 구현만; public API 영향 없음)
| Handler 의존 | Vision 대체 | 사유 |
|---|---|---|
| `QMC.Common.Logging.EventLogger.Write(...)` | `System.Diagnostics.Debug.WriteLine(...)` | 컨트롤 self-contained 유지(WinForms만) → 공용화 용이 |
| `QMC.Common.MessageDialog.Show(owner,text,cap,btn,icon)` | `System.Windows.Forms.MessageBox.Show(owner,text,cap,btn,icon)` | 동일 시그니처/반환(DialogResult); Vision 기존 관행 |

## ParameterGridItem (모델) — 1:1
- enum `ParameterGridValueType { Double, Int, Bool, Text, Selection }`
- enum `ParameterGridScope { Recipe, Setup, Config }`
- class `ParameterGridItem` props: `Key, DisplayName, Unit, ValueType, Scope, DisplayScale, Getter:Func<object>, Setter:Action<object>, Validator:Func<object,bool>, Options:List<ParameterGridOption>`
- 정적 팩토리: `Micron(name,scope,Func<double>,Action<double>)`, `Double(name,unit,scope,...)`, `Int(...)`, `Bool(name,scope,Func<bool>,Action<bool>)`, `Selection(name,unit,scope,Func<object>,Action<object>,IEnumerable<ParameterGridOption>)`, `Selection<TEnum>(name,unit,scope,Func<TEnum>,Action<TEnum>)`
- class `ParameterGridOption { string Text; object Value; }` + ctor(text,value)

## ParameterGridChangedEventArgs — 1:1
- `: EventArgs`, props `Item:ParameterGridItem`, `Scope:ParameterGridScope`, ctor(`ParameterGridItem`).

## ParameterGridControl : UserControl — 1:1 public 표면
- ctor `ParameterGridControl()`
- `event EventHandler<ParameterGridChangedEventArgs> ParameterValueChanged`
- `event EventHandler<ParameterGridChangedEventArgs> ParameterRowDoubleClicked`
- `ParameterGridItem SelectedItem { get; }`
- `void SetItems(IEnumerable<ParameterGridItem> items)`
- `void RefreshValues()`
- 동작: 4열(PARAMETER|VALUE|UNIT|SCOPE) DataGridView, Bool 토글(확인 다이얼로그), Int/Double 더블클릭→NumericKeypadDialog, Selection 콤보셀 드롭다운, scope 색/value 색 동일.
- 동적 행 채움(SetItems/RebuildRows)은 **런타임**(Code.cs). Designer IC 엔 정적 grid+4컬럼만(헬퍼호출/객체초기화자/산술 0).

## NumericKeypadDialog : Form — 1:1 public 표면
- ctor `NumericKeypadDialog(string title, string valueText, string unit)`
- `string ValueText { get; }`
- 키패드(0-9, 000, ., +/-, BS, CLR, ±1/10/100/1000, OK, Cancel). ShowDialog→DialogResult.OK 시 ValueText 유효 숫자.

## EnumPickerDialog — **불요**
ParameterGridControl 의 Selection 편집은 **그리드 내 DataGridViewComboBoxCell 드롭다운**으로 처리(ShowSelectionEditor→grid.BeginEdit). Handler 도 EnumPickerDialog 미사용(별도 용도). → R1 신규 대상 아님.

## 신규 파일 (전부 QMC.Vision/Ui/Controls/)
- ParameterGridItem.cs (모델: enums + Item + Option)
- ParameterGridChangedEventArgs.cs
- ParameterGridControl.cs + .Designer.cs
- NumericKeypadDialog.cs + .Designer.cs
