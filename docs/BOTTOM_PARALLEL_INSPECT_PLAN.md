# Bottom 검사 병렬화 설계 (INSPECTASYNC 그랩 + INSPECTRESULT 일괄 병렬)

목표: 핸들러가 픽커 4개를 그랩만 시켜두고(INSPECTASYNC), INSPECTRESULT 요청 시 4개 그랩을
**병렬로** 검사 처리해 1.5s → 0.6~0.7s. **310 알고리즘 코어는 불변**, GPU off/CPU 경로.

> **적용 상태(2026-07): 코드 반영 완료 — 장비 VS에서 빌드/테스트만 남음.**
> - 신규: `Equipment/Core/PendingGrabStore.cs` (+ csproj 등록)
> - 수정: `Equipment/Comm/VisionTcpServer.cs` (DoInspectAsync 그랩전용+자동트리거 / DoInspectResult 안전망 / ProcessPendingBatchParallel / ExpectedPickerCount)
> - 수정: `Equipment/Core/VisionCommandCore.cs` (InspectOnImageExplicit 오버로드 + CopyInspectorConfig)
> - 이 문서는 해당 구현의 설계 기준. 아래 §4/§5 의 ⚠ 항목을 장비에서 검증할 것.
> - **샌드박스에서 컴파일 검증 불가** — 반드시 VS 빌드로 확인.

---

## 1. 플로우

**트리거 = 방식 B (4번째 그랩이 들어오는 순간 자동 시작).**

```
핸들러: MODULE|INSPECTASYNC|inspector|chip_uid   (×4, 픽커마다)
비전  : ACK|MODULE|INSPECTASYNC|STARTED           → 그랩만 하고 이미지 보관(검사 X)
        ↳ 보관 개수가 예상 픽커 수(기본 4)에 도달하는 순간 → 그 자리에서 일괄 병렬 처리 자동 시작

핸들러: MODULE|INSPECTRESULT|inspector|chip_uid   → 폴링(처리는 이미 진행/완료)
비전  : 진행 ACK|..|0 / 완료 ACK|..|1;PASS|FAIL;x=..;y=..;t=..;score=.. / 실패 ACK|..|ERR;msg
```

- 트리거: `DoInspectAsync` 에서 그랩 보관 후 `Count>=ExpectedPickerCount` 이면 `TryBeginProcessing`(멱등) 성공 시 즉시 병렬 처리.
- 안전망: 예상 개수가 안 채워진 부분 배치라도, 이후 `INSPECTRESULT` 가 `TryBeginProcessing` 을 한 번 더 호출해 남은 보관분을 처리(둘 중 먼저 호출된 쪽만 처리).

현재 코드와의 차이 (조사 결과):
- `DoInspectAsync`가 지금은 **그랩+검사**를 한 번에 백그라운드로 함 → **그랩만**으로 분리 + 4번째에서 자동 트리거.
- `DoInspectResult`는 `AsyncMatchStore` 폴링만 함 → 폴링 유지 + 안전망 트리거만 추가.
- 결과 저장/폴링은 기존 `AsyncMatchStore`(모듈·inspector·chip_uid 키) 그대로 재사용.

---

## 2. 컴포넌트

1. **PendingGrabStore (신규)** — 그랩된 이미지를 모듈별로 보관, 일괄 처리 1회만 트리거(멱등).
2. **DoInspectAsync (수정)** — 그랩만 하고 PendingGrabStore에 보관.
3. **DoInspectResult (수정)** — 첫 호출에서 보관분을 **픽커별 독립 인스턴스**로 병렬 처리, 이후 폴링.
4. **CopyInspectorConfig (신규 헬퍼)** — 신규 인스펙터에 레시피 파라미터 복제(핵심).

---

## 3. 핵심 코드

### 3.1 PendingGrabStore (신규 파일 — 이미 생성됨)

`QMC.Vision/Equipment/Core/PendingGrabStore.cs` 로 생성 완료(+ csproj 등록). 방식 B용 API:

```csharp
int  Add(module, insp, chipUid, picker, image);  // 보관 후 현재 개수 반환(picker=0 이면 도착순서로 대체)
int  Count(module);                        // 현재 보관 개수
bool TryBeginProcessing(module);           // 멱등: 처음 한 번만 true(보관분 있을 때)
List<Item> Take(module);                   // 회수+비움(이미지 Dispose 책임 이전). Item.Picker 포함
void Clear(module);                        // 사이클 취소 시 보관분 폐기(Dispose)
```

**Sim==Real 배선(적용됨):** INSPECTASYNC 포맷 = `MODULE|INSPECTASYNC|inspector|picker_id|chip_uid` (picker_id 가 chip_uid **앞**).
구형 `inspector|chip_uid`(picker 생략)도 호환. `VisionTcpServer.DoInspectAsync` 가 parts≥5면 [3]=picker/[4]=chip_uid, parts==4면 [3]=chip_uid.
`ProcessPendingBatchParallel` 이 `it.Picker` 사용. INSPECTRESULT 는 `inspector|chip_uid` 유지(결과 키=chip_uid).
Sim 셀프런(`ToolSequence` Bottom 분기)이 픽커 1~4를 INSPECTASYNC(그랩만)로 연속 전송 후 INSPECTRESULT 폴링(150ms) — 실제 핸들러 플로우와 동일.

- 방식 B 트리거: `Add` 후 `Count>=ExpectedPickerCount` && `TryBeginProcessing` → 즉시 병렬 처리.
- 안전망: `INSPECTRESULT` 에서도 `TryBeginProcessing` 호출(먼저 성공한 쪽만 처리, 멱등).

### 3.2 DoInspectAsync — 그랩만 (VisionTcpServer.cs 수정)

```csharp
private string DoInspectAsync(IVisionModule m, string[] parts)
{
    string insp    = parts.Length > 2 ? parts[2] : "";
    string chipUid = parts.Length > 3 ? parts[3] : "";
    if (string.IsNullOrEmpty(insp)) return "fail:no inspector";
    if (!m.Inspectors.TryGetValue(insp, out var ins)) return "fail:inspector not found";

    if (VisionCommandCore.IsInspectionSkipped(m, insp))   // 검사 OFF → 그랩 없이 즉시 PASS
    {
        ModuleResultStore.Record(m.Name, insp, true, "inspection=skip");
        AsyncMatchStore.Complete(m.Name, insp, chipUid, "PASS;inspection=skip");
        return "STARTED";
    }

    AsyncMatchStore.Start(m.Name, insp, chipUid);   // Running
    System.Threading.Tasks.Task.Run(() =>           // 그랩만(ACK STARTED는 1단계에서 이미 전송)
    {
        try
        {
            var g = m.GrabForTool(insp);
            if (g == null || !g.IsSuccess) { AsyncMatchStore.Fail(m.Name, insp, chipUid, g?.ErrorMessage ?? "grab"); g?.Dispose(); return; }
            // GrabResult.Dispose가 Image를 해제하므로 사본을 보관.
            Bitmap keep; using (g) { keep = new Bitmap(g.Image); }
            int n = PendingGrabStore.Add(m.Name, insp, chipUid, keep);   // 검사 안 함 — 보관만

            // ★ 방식 B: 예상 픽커 수에 도달하면 그 자리에서 일괄 병렬 처리 자동 시작(멱등).
            if (n >= ExpectedPickerCount(m) && PendingGrabStore.TryBeginProcessing(m.Name))
                ProcessPendingBatchParallel(m, insp);
        }
        catch (Exception ex) { AsyncMatchStore.Fail(m.Name, insp, chipUid, ex.Message); }
    });
    return "STARTED";
}

/// <summary>예상 픽커 수 — 한 번의 Bottom 검사 배치에서 그랩되는 픽커 개수.
/// 우선순위: (구현 시) 머신/설정 상수 → 없으면 기존 관례(picker %4)에 맞춰 기본 4.</summary>
private static int ExpectedPickerCount(IVisionModule m) => 4;   // TODO: 머신 config 로 승격
```

### 3.3 DoInspectResult — 첫 호출에서 일괄 병렬 처리 트리거 (VisionTcpServer.cs 수정)

```csharp
private string DoInspectResult(IVisionModule m, string[] parts)
{
    string insp    = parts.Length > 2 ? parts[2] : "";
    string chipUid = parts.Length > 3 ? parts[3] : "";

    // 안전망(방식 B 보완): 4개가 다 안 채워진 부분 배치라도 여기서 한 번 더 트리거(멱등).
    // 정상 흐름에서는 이미 4번째 그랩에서 처리가 시작돼 있으므로 여기선 false 가 되고 폴링만 한다.
    if (PendingGrabStore.TryBeginProcessing(m.Name))
        ProcessPendingBatchParallel(m, insp);

    var st = AsyncMatchStore.TryGet(m.Name, insp, chipUid, out string payload);
    switch (st)
    {
        case AsyncMatchStore.State.Done:    return "1;" + payload;
        case AsyncMatchStore.State.Error:   return "ERR;" + payload;
        default:                            return "0";   // 진행중/미완
    }
}

/// <summary>보관분 전체를 픽커별 '독립 인스턴스'로 병렬 검사(진짜 병렬 — 공유 _libInspector 락 회피).
/// 310 코어(CDTInspector.BottomInspect)는 그대로 호출. 결과는 chip_uid별 AsyncMatchStore에 저장.</summary>
private void ProcessPendingBatchParallel(IVisionModule m, string insp)
{
    var items = PendingGrabStore.Take(m.Name);
    var cfg = _cfg;
    System.Threading.Tasks.Task.Run(() =>
    {
        System.Threading.Tasks.Parallel.ForEach(items, it =>
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                // 픽커마다 새 인스펙터 인스턴스 → 각자 _libInspector/FindChippingNForeign/Last* 보유(레이스 없음)
                if (!DomainInspectorFactory.TryCreate(insp, out var ins))
                { AsyncMatchStore.Fail(m.Name, insp, it.ChipUid, "inspector create fail"); return; }

                UnitContext.ApplyScale(ins, m.ScaleX, m.ScaleY);
                CopyInspectorConfig(m.Inspectors[insp], ins);          // ★ 레시피 파라미터 복제(3.4)

                // 결과 기록/오버레이는 '픽커별 컨텍스트'가 필요 → 명시적 컨텍스트 오버로드 사용(3.5)
                string res = VisionCommandCore.InspectOnImageExplicit(
                    m, cfg, insp, ins, it.Image, it.ChipUid, /*picker*/PickerOf(it.ChipUid), /*ch*/-1, /*ix*/0, /*iy*/0);

                sw.Stop();
                if (res != null && (res.StartsWith("PASS") || res.StartsWith("FAIL")))
                    AsyncMatchStore.Complete(m.Name, insp, it.ChipUid, res + ";t=" + sw.ElapsedMilliseconds);
                else
                    AsyncMatchStore.Fail(m.Name, insp, it.ChipUid, res ?? "no result");
            }
            catch (Exception ex) { AsyncMatchStore.Fail(m.Name, insp, it.ChipUid, ex.Message); }
            finally { try { it.Image?.Dispose(); } catch { } }
        });
    });
}
```

### 3.4 CopyInspectorConfig — 레시피 파라미터 복제 (핵심)

신규 인스펙터는 **기본값**이라, 모듈의 설정된 인스펙터에서 파라미터를 복사해야 한다.
공개 read/write 프로퍼티만 반사 복사(`Last*`는 private set이라 자동 제외, `InspectionRoi`는 참조 복사=검사 중 읽기전용이라 안전).

```csharp
private static void CopyInspectorConfig(IInspector src, IInspector dst)
{
    if (src == null || dst == null || src.GetType() != dst.GetType()) return;
    foreach (var p in src.GetType().GetProperties(
                 System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
    {
        if (!p.CanRead || !p.CanWrite) continue;   // 계산/출력 프로퍼티(private set) 제외
        try { p.SetValue(dst, p.GetValue(src)); } catch { }
    }
}
```

### 3.5 InspectOnImageExplicit — 픽커별 컨텍스트 (VisionCommandCore 소폭 리팩터)

현재 `InspectOnImage`는 `GetInspectCtx(m.Name)`(모듈 공유 컨텍스트)를 읽어 픽커/ix/iy를 붙인다.
병렬에서는 픽커별로 달라야 하므로, 컨텍스트를 **인자로 받는** 오버로드가 필요하다.
기존 `InspectOnImage`는 `InspectOnImageExplicit(...)`를 `GetInspectCtx(m.Name)` 값으로 호출하도록 위임 →
동기 경로 무변경, 병렬 경로만 명시적 컨텍스트 사용.

```csharp
// 기존 시그니처 유지(위임)
public static string InspectOnImage(IVisionModule m, VisionSettings cfg, string inspId,
                                    IInspector ins, Bitmap image, string chipUid)
{
    var ctx = GetInspectCtx(m?.Name);
    return InspectOnImageExplicit(m, cfg, inspId, ins, image, chipUid, ctx.Picker, ctx.Channel, ctx.IndexX, ctx.IndexY);
}

// 신규: 컨텍스트를 인자로(병렬용). 본문은 기존 InspectOnImage 그대로 옮기되,
//       GetInspectCtx(m.Name) 대신 인자 picker/channel/ix/iy 사용.
public static string InspectOnImageExplicit(IVisionModule m, VisionSettings cfg, string inspId,
                                            IInspector ins, Bitmap image, string chipUid,
                                            int picker, int channel, int ix, int iy)
{ /* 기존 InspectOnImage 본문 + ctx 인자화 */ }
```

---

## 4. 스레드 안전성 체크리스트

- **픽커별 인스펙터 인스턴스**(DomainInspectorFactory) → `_libInspector`/`FindChippingNForeign`/`Last*` 를 각자 보유. ✔ 진짜 병렬.
- CDTInspector의 `static CodaLock` 구간은 카운터(SaveIndex/Defact) 등 짧은 부분만 → 무거운 검사(FindChipOutline/InspectChipping)는 `lock(this)`(인스턴스별)라 병렬 유지. ✔
- 결과 저장은 `AsyncMatchStore`(내부 락) + `InspectionResultStore`/`ModuleResultStore` — 병렬 기록이 스레드 세이프인지 확인 필요. 아니면 기록만 결과 취합 후 순차. ⚠ 확인 항목.
- `CopyInspectorConfig` 반사 복사가 모든 파라미터를 정확히 옮기는지(특히 `InspectionRoi`, PixelSize, Chip/Foreign 임계) 1픽커 순차 vs 병렬 결과 동일성으로 검증. ⚠

## 5. 검증

1. 병렬 결과 = 순차 결과 **동일**(무회귀). 310 코어 불변이라 값 동일해야 정상.
2. 4픽커 wall-time 측정(목표 0.6~0.7s). 코어 수 부족 시 단축폭 제한.
3. 그랩 이미지 사본 Dispose 누수 없는지(각 Item.Image 처리 후 Dispose).
4. 와이어 포맷: `t=`/`score=` 추가는 응답 payload 확장. 핸들러 파서와 합의된 필드만.

## 6. 불변(건드리지 않음)

- CDT-310 알고리즘 코어(CDTInspector.BottomInspect, FindChippingNForeign, ContaminationDetector).
- 기존 동기 INSPECT / InspectOnImage 동작(오버로드 위임으로 무변경).
- 통신 와이어 포맷의 기존 필드(추가 필드만 합의 하에).
```

## 7. 실기 전환 규약 (2026-07-02 확정)

**와이어 포맷(확장)**: `MODULE|INSPECTASYNC|inspector|picker_id|chip_uid[|die_index]`
- chip_uid 가 숫자면 그 값이 곧 die_index(픽업 순서 1-base) — Sim/단순 핸들러는 3필드만 전송.
- die_index 로 Vision 이 활성 레시피 칩위치(InputDieMap/웨이퍼사양+Pickup, `PickupOrderResolver`)에서 셀을 매칭해 Bottom 맵에 그린다. 좌표(ix/iy)는 전송하지 않는다.

**INSPECTRESULT = 대기형 응답**: 요청 1회 → 서버가 완료까지 대기(최대 6s, 클라이언트 타임아웃은 그보다 길게 — 기본 10s) → `1;PASS|FAIL;...` 데이터 1회. 만료 시 "0"(재요청 폴백).

**모션 안전 — 핸들러는 ACK 가 아니라 EPD 기준으로 이동**:
- INSPECTASYNC 의 ACK(STARTED)는 '그랩 전' 선응답. ACK 만 보고 픽커를 빼면 촬상 전 이동이 된다.
- Vision 은 그랩(촬상) 완료 시 `EPD|MODULE` 푸시 → 핸들러 시퀀스는 EPD 수신 후 다음 위치로 이동.
- 핸들러 API: `VisionTcpClient.WaitExposureDoneAsync()` — InspectAsyncStartAsync 호출 '전'에 Task 를 만들어 두고 시작 후 await.

**그랩 직렬화**: 서버(`VisionTcpServer._grabGate`)가 백그라운드 그랩을 요청 순서대로 직렬화 — 실기 카메라 동시 그랩 방지 + 픽커 순서 보존.

**이미지 소유권**: 그랩 이미지는 사본 없이 `GrabResult.DetachImage()` 로 이전(고해상도 복제 제거). 뷰어 프레임은 TapFrame 이 자체 클론.
## 8. 전 모듈 확장 (2026-07-02)

- 엔진을 `AsyncInspectCore`(QMC.Vision.Core)로 분리 — TCP 서버와 일반 시퀀서(Direct 디스패처)가 공유. TCP시뮬/일반 시퀀서 동작 동일.
- 적용 범위: Bottom(픽커 4장) + **Bin(픽커 4장, x/y 오프셋 보존)** + **Side 앞/뒤(채널 0°/90° 2장, die 당 결과 1회 합산 판정)**.
- 와이어 확장: `MODULE|INSPECTASYNC|inspector|picker|chip_uid[|die_index[|channel]]` — channel 은 Side 전용(생략 시 -1).
- 같은 chip_uid 그룹은 전 항목 완료 후 1회 Complete(모두 PASS 여야 PASS, t=최대). 실행 오류는 ERR.
- 모니터링: InspectOnImageExplicit 의 모드별 Record 로 작업화면(Side/Die gap 뷰어, Bottom 창) 자동 연동.

