# Vision MATCH "재시작 후 검출 실패" 근본원인 분석 (2026-06-22)

> 대상: `QMC.Vision` EjectPinFinder(및 전 finder) MATCH. 증상: 새 Train은 검출되나, 저장·재시작 후 MATCH가 실패.
> 결론: **Cognex finder와 그 OpenCv fallback이 SearchRoi를 따로 들고 있었고(불일치), 게다가 Cognex 이미지 변환이 상시 실패해 사실상 항상 fallback으로 매칭**되고 있었다.

---

## 1. 왜 지금 되는가 (근본 원인)

핵심은 두 겹이다.

**(A) Cognex는 사실상 한 번도 안 쓰이고 있었다 — 항상 OpenCv fallback.**
`CognexPatternFinder.Match`는 Cognex 패턴(`_pma`)이 학습돼 있을 때만 PatMax를 쓰고, 아니면 내부 `_fallback`(OpenCvPatternFinder)로 넘긴다. 그런데 이미지 변환 `CognexInterop.BitmapToICogImage`가 **`CogImageFileBMP` 타입을 못 찾아 예외**를 던지며 학습/매칭이 모두 실패한다(로그 546행). 따라서 `_pma=null` → **항상 OpenCv fallback**으로 매칭한다. ("Cognex Loaded" 진단은 어셈블리 로드만 의미하며, 실제 이미지 변환 단계에서 실패.)

**(B) 그 fallback의 SearchRoi가 동기화되지 않았다.**
`CognexPatternFinder`와 `_fallback`은 **각각 별도의 SearchRoi/TrainRoi 객체**를 가진다.
- 레시피 로드(`AlgorithmNode.ApplyToRuntime`)는 `CognexPatternFinder.SearchRoi`만 채운다 → 파라미터 그리드·ROI 제어·EDIT SEARCH·PRE-MATCH 로그 전부 여기(정상값)를 읽는다.
- 하지만 `_fallback.SearchRoi`는 생성자 기본값(center 320,240 · **400×300**)으로 남는다.
- **새 Train()이 됐던 이유**: `Train()`은 `_fallback.SearchRoi = SearchRoi.Clone()`로 동기화한다(CognexPatternFinder.cs:53). 그 세션 동안만 fallback ROI가 맞아 검출됨.
- **재시작 후 실패 이유**: 복원 경로 `LoadTrainImage`는 fallback ROI를 동기화하지 않았다 → fallback이 기본 400×300으로 매칭 → `search 400x300 < template 438x445` 실패.

**수정**: `CognexPatternFinder.Match`에서 fallback으로 넘기기 전에 **SearchRoi/TrainRoi도 동기화**(기존엔 AcceptThreshold/Angle*/MaxInstances만 동기화했음). 이제 fallback이 레시피 ROI로 매칭 → 재시작 후에도 검출.

```
[로그 증거]  bin/Debug/Log/Event/2026-06-22.csv
520  PRE-MATCH ... SearchRoi center(2606.5,4112) 1357x1200 | img 5120x5120 | TrainImage 438x445   ← finder ROI(정상)
521  검출 NG — search 400x300 < template 438x445 (... roi @120,90 400x300)                          ← 실제 매칭(fallback 기본값)
546  EjectPinFinder LoadTrainImage Cognex 재학습 실패 → InvalidOperationException: CogImageFileBMP 타입 못 찾음
```
PRE-MATCH(1357×1200) ≠ 실제 매칭(400×300) → 표시값과 사용값이 다른 객체였음이 명백.

---

## 2. 로그 분석 (2026-06-22.csv)

| 행 | 내용 | 의미 |
|---|---|---|
| 173~516 | `LoadTrainPattern ... 복원=True` | 패턴 PNG 복원은 **정상**(앞서 고친 깊은복사/포맷/노드로드 OK) |
| 360,380,400 | `recipe='GM1SP-...' PNG 없음 → 패턴 비움` | 그 레시피엔 eject 학습 PNG 없음(정상 동작) |
| 520,522,524 | `PRE-MATCH SearchRoi 1357x1200` | finder가 든 ROI는 **정상값** |
| 521,523,525 | `검출 NG — search 400x300` | 실제 매칭은 **fallback 기본값** → desync 확정 |
| 546,554,556,558,572,580,582,584,596,598 | `LoadTrainImage Cognex 재학습 실패 → CogImageFileBMP 타입 못찾음` | **EjectPin·BinVision Reticle/Die/Scale 전부** Cognex 실패 → 전 finder가 OpenCv fallback |

**결론**: 현재 매칭은 전부 OpenCv NCC(fallback)이고 Cognex PatMax는 동작하지 않는다. 패턴 복원·이미지(5120)·ROI(1357) 모두 정상인데, fallback ROI 미동기화 한 가지가 재시작 검출을 막았다.

---

## 3. 왜 여태 못 찾았나

1. **다중 결함의 직렬 차단**: ① GDI+ 얕은복사(ICloneable.Clone) ② ToGray 8bpp LockBits ③ 회전탐색 마진 과다제외 ④ **fallback SearchRoi 불일치** — 앞 결함이 먼저 막아, 그걸 풀어야 다음이 드러나는 구조였다. 마지막 desync는 앞 3개를 다 푼 뒤에야 노출됨.
2. **표시값과 사용값의 분리**: UI(파라미터·ROI 제어·EDIT SEARCH)는 `CognexPatternFinder.SearchRoi`(정상)를 보여주는데, 매칭은 `_fallback.SearchRoi`(기본값)를 쓴다. 화면은 완벽히 정상이라 "ROI는 맞는데 왜 안 되지"로 직관이 계속 어긋남.
3. **silent catch**: Cognex 변환 실패(`catch { _trainSucceeded=false; _pma=null; }`)와 fallback 전환이 **로그 없이** 일어나, "Cognex Loaded"만 보고 Cognex를 쓰는 줄로 착각.
4. **잔상 오해**: 초기 상태(레시피 적용 전) 매칭의 400×300 에러가 누적되어, 설정 변경 후에도 같은 문구가 보여 혼선.
5. **전환점**: ① 실패 메시지에 실제 크기 표기 ② **MATCH 직전 PRE-MATCH 로그** ③ **Cognex 실패 사유 로그** — 이 세 진단을 넣고 나서야 `1357 vs 400`(desync)와 `CogImageFileBMP 못찾음`(Cognex 미사용)이 한눈에 드러났다. → 교훈: **추측 대신 실측 로그**.

---

## 4. 다른 곳 전수 점검

- **CognexInspector**: `Inspect` 진입 시 매번 `_fallback.InspectionRoi = InspectionRoi?.Clone()`(CognexInspector.cs:39)로 동기화 → **같은 버그 없음**(올바른 패턴).
- **CognexPatternFinder.Match 동기화 항목(수정 후 전수)**: `SearchRoi` ✓ `TrainRoi` ✓ `AngleEnabled/AngleToleranceDeg/AngleStepDeg` ✓ `MaxInstances` ✓ `AcceptThreshold` ✓ / `TrainImage`는 `LoadTrainImage`에서 fallback 동기화 ✓ → OpenCv fallback 입력 전부 커버.
- **Cognex 타입해석 실패 = 시스템 전역 영향**: `BitmapToICogImage`가 임시 BMP 파일 로더(`CogImageFileBMP`) 타입을 못 찾아 **모든 Cognex 변환이 실패**. 동일 함수를 쓰는 Caliper/ColorMatch 등 다른 Cognex 도구도 똑같이 fallback로 동작할 것 → 현재 "Cognex 사용"은 사실상 전무. (점검·결정 필요.)
- **재발 위험 패턴**: "표시 객체 ≠ 사용 객체"(Cognex/ fallback 이중 상태). 동기화 누락이 또 생길 수 있는 구조.

---

## 5. 권고 / 다음 단계

1. **즉시(현행)**: OpenCv fallback로 정상 검출됨. Cognex가 꼭 필요치 않으면 이대로 운용 가능.
2. **진짜 Cognex(PatMax)를 쓰려면** — `BitmapToICogImage` 개선:
   - `Cognex.VisionPro.ImageFile` 어셈블리 로드를 보장해 `CogImageFileBMP` 타입을 찾게 하거나,
   - **임시 BMP 파일 방식 대신 `CogImage8Grey`를 raw 픽셀로 직접 생성**(파일 IO·특정 타입 의존 제거 → 더 빠르고 견고). ← 권장.
   - 적용 후 로그에 `재학습 실패`가 사라지고 Cognex PatMax 경로(SearchRoi=SearchRegion, 각도=AngleStart/Extent)가 동작하는지 확인.
3. **재발 방지**:
   - Cognex 도구의 fallback 파라미터 동기화를 **공통 헬퍼 한 곳**으로 모은다(누락 방지).
   - **silent catch 금지** — 최소 `EventLogger` 기록(이번에 그 덕에 원인 발견).
   - Provider 표시에 **실효 backend(Cognex 동작/Fallback)** 를 노출해 "Loaded≠Used" 착시 제거.
4. **진단 로그 정리**: PRE-MATCH 로그·Cognex 실패 로그는 유용하므로 유지하거나, 운영 전 `#if DEBUG` 또는 토글로 두어 폭주 방지.

---

---

## 6. Cognex 미사용 확정 원인 + 조치 (추가)

`CognexInterop.BitmapToICogImage`는 **이미 내부에서 임시 BMP로 저장 후 `CogImageFileBMP`로 읽는다**(CognexInterop.cs:73-83). 실패는 파일 포맷이 아니라 **`CogImageFileBMP` 타입을 못 찾음**이었다.

원인 확정: `CognexBackend._dllSpec`는 Core/PMAlign/Blob/Caliper/**ImageProcessing**만 로드하고, `CogImageFileBMP`가 든 **`Cognex.VisionPro.ImageFile.dll`은 로드 목록에 없었다**. (백엔드 버전 문자열의 "Im…"은 ImageProcessing.) → `BitmapToICogImage`가 타입 못 찾음 → 모든 Cognex 변환/학습 실패 → 상시 OpenCv fallback.

조치(적용): `_dllSpec`와 `LoadedAssemblies`에 **`Cognex.VisionPro.ImageFile.dll`("ImageFile") 추가**. 이제 `CogImageFileBMP` 타입을 찾아 Cognex PatMax가 동작한다. (해당 DLL이 설치 폴더에 있어야 하며, 없으면 `required:false`라 무해하게 fallback 유지.)

→ "Cognex이면 BMP로 저장"이 아니라, **임시 BMP는 이미 저장 중이었고 그것을 읽을 ImageFile 어셈블리 로드가 빠졌던 것**.

---

## 7. Cognex PatMax 검출 실패 → 해결 (Origin) ★ 최종

ImageFile.dll 로드 후 Cognex 변환·재학습은 성공했으나(`Cognex 재학습 OK`), **PatMax 매칭이 found=0**으로 검출을 못 했다(이때 fallback-on-empty 안전망으로 검출은 유지). 진단 결과 원인은 **재학습 시 `Pattern.Origin` 미설정**.

- `Train()`은 학습영역 중심으로 `Origin`을 설정한다(CognexPatternFinder.cs:74).
- 복원 경로 `LoadTrainImage`는 Origin을 빠뜨려, PatMax 모델 기준점이 어긋나 매칭이 성립하지 않았다.

**조치**: `LoadTrainImage` 재학습에 `Origin = 패턴 중심` 설정(Train()과 parity). → **검출 OK, score 0.992, 각도 -7° 검출**(회전까지). 진짜 Cognex PatMax 동작 확정(`MATCH → Cognex PatMax 사용 (found=1)`).

부가:
- **fallback-on-empty 안전망**: Cognex PatMax가 0개면 OpenCv fallback로 재시도(둘 다 같은 SearchRoi/TrainImage) → Cognex가 특정 케이스 실패해도 검출 보장.
- 진단 로그 추가: `PRE-MATCH`(SearchRoi/이미지/패턴 크기), `Engine`(어느 엔진·found 수·best score), `Cognex`(재학습 성공/실패 사유). 운영 전 토글/정리 권장.

---

## 8. 전체 수정 체인 (요약)

| # | 결함 | 조치 | 파일 |
|---|---|---|---|
| 1 | 학습 패턴 복원 시 `ICloneable.Clone()` 얕은복사 → 스트림 해제 후 GDI+ 오류 | `new Bitmap(src)` 깊은복사 | OpenCv/Sim/Cognex PatternFinder |
| 2 | `ToGray` 가 8bpp 인덱스(그레이 PNG)를 `LockBits(24bpp)` → GDI+ 오류 | 32bppArgb DrawImage 정규화 | OpenCvPatternFinder |
| 3 | 회전탐색 마진이 가장자리 타깃 제외 | 마진 제거 + 경계 클램프 | OpenCvPatternFinder |
| 4 | Cognex finder/fallback **SearchRoi 별도 객체** — 재시작 시 fallback ROI 미동기화(기본 400×300) | Match 에서 SearchRoi/TrainRoi 동기화 | CognexPatternFinder |
| 5 | `Cognex.VisionPro.ImageFile.dll` 미로드 → `CogImageFileBMP` 타입 못찾음 → 변환 상시 실패 | 로드 목록에 ImageFile 추가 | CognexBackend |
| 6 | 재학습 시 `Pattern.Origin` 미설정 → PatMax 검출 실패 | LoadTrainImage 에 Origin 설정 | CognexPatternFinder |
| — | 학습 패턴 복원이 UI 페이지에만 있어 런타임/TCP 미복원 | 노드(`FinderAlgorithm`) LoadRecipe/SaveRecipe 로 영속화 이관 | AlgorithmNode |

재발 방지: ① Cognex 도구의 fallback 동기화·Origin 설정을 공통 헬퍼로 일원화 ② silent catch 금지(로그 의무) ③ Provider 표시에 실효 backend 노출.

---

### 한 줄 요약
"표시 객체 ≠ 사용 객체"(Cognex/ fallback 이중 상태) + Cognex 어셈블리/Origin 누락이 겹쳐, 화면은 정상인데 매칭만 틀렸다. **SearchRoi 동기화 + ImageFile 로드 + 재학습 Origin** 세 줄로 Cognex PatMax 정상화(0.992 검출).
