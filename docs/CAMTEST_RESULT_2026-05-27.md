# 카메라 연결 + 그랩 테스트 결과 — 2026-05-27

- **브랜치**: `stage-cam-conn-test`
- **추가 파일**: `QMC.Vision\Tools\CameraConnectTest.cs`, `QMC.Vision\Program.cs` 분기, `QMC.Vision.csproj` Compile 등록
- **테스트 PC**: 작업 PC (사용자 운영 PC)
- **빌드 결과**: warning 0 (기존 1건 무관 `HikGigECamera._handle`), error 0

---

## 환경

| 항목 | 결과 |
|---|---|
| HikMvsDll.IsLoaded | **true** (DLL 자동 배치 후) |
| HikMvsDll.Version | **HIK MVS 4.8.0.3** |
| exe 폴더 SDK DLL | **2 종 배치됨** (자동 복사) |
| .NET wrapper | `C:\Program Files (x86)\MVS\Development\DotNet\AnyCpu\net40\MvCameraControl.Net.dll` (203,264 bytes) → `bin\Debug\` |
| 네이티브 | `C:\Program Files (x86)\Common Files\MVS\Runtime\Win64_x64\MvCameraControl.dll` (1,564,288 bytes) → `bin\Debug\` |

> ⚠ DLL 은 `bin/` 아래 배치되어 `.gitignore` 에 의해 git 추적 대상 외. 빌드 산출물 정리 시 재배치 필요. 사후 배포 시엔 자동 복사 스크립트 또는 csproj `<Content CopyToOutputDirectory>` 등록 검토.

---

## Enumerate 결과

```
SDK: LOADED
VERSION: HIK MVS 4.8.0.3
DEVICES: 0
NO_DEVICE: GigE 카메라 미발견 (결선/IP/방화벽 확인)
```

| 항목 | 값 |
|---|---|
| 발견 장치 수 | **0** |
| 종료 코드 | **3** (NO_DEVICE — CLI 도구의 정지 조건) |

---

## 연결 / 그랩

| 항목 | 결과 |
|---|---|
| CreateById 결과 타입 | n/a (Enumerate 0개로 미진입) |
| Open | n/a |
| Grab | n/a |
| 저장 경로 | n/a |
| **최종 판정** | **FAIL(exit code 3 — NO_DEVICE)** |

**원인 후보 (사용자 확인 필요)**:
1. GigE 카메라 LAN 결선 / 전원
2. 네트워크 인터페이스 IP 설정 — 카메라와 동일 서브넷이어야 함 (예: 카메라 192.168.1.x / NIC 192.168.1.x)
3. Windows 방화벽 — MVS Bonjour / GVCP UDP 차단 시 enum 안 됨
4. MVS Client GUI (`MVS.exe`) 에서 먼저 발견되는지 확인

---

## 빌드 / 회귀

| 항목 | 결과 |
|---|---|
| dotnet build (QMC.Vision) | warning 0 (기존 1건 무관 무시) / error 0 |
| verify_all.ps1 | **TOTAL 57 / PASS 57 / FAIL 0** (회귀 없음) |
| 시퀀스 문서 04~08 | 본 작업 영향 없음 (CLI 도구 추가만) |

---

## CLI 도구 동작 검증

본 작업 핵심 — `--cam-test` 분기와 exit code 매핑이 정확히 동작함을 확인.

| 시도 | SDK 상태 | exit | 출력 |
|---|---|---|---|
| 1차 (DLL 미배치) | `IsLoaded=false` | **2** | `SDK: NOT_LOADED` + `HINT:` 안내 |
| 2차 (DLL 배치 후) | `IsLoaded=true` (4.8.0.3) | **3** | `DEVICES: 0` + `NO_DEVICE:` |

→ 정지 조건 매핑 정확. Sim fallback 가드(`cam is SimCamera` → exit 4) 도 코드상 활성 (3차 시도 미진입).

---

## 잔존 이슈 / 다음 제안

1. **카메라 결선 확인** — 사용자 측 GigE 카메라 LAN/IP/방화벽 점검
2. MVS Client (HIK 공식 GUI) 로 enum 가능 여부 우선 확인 권장
3. enum 정상화 후 본 CLI 도구 재실행:
   ```
   D:\Work\CDT-320\QMC.CDT-320\QMC.Vision\bin\Debug\QMC.Vision.exe --cam-test
   ```
   예상 결과 (PASS 시):
   ```
   SDK: LOADED
   VERSION: HIK MVS 4.8.0.3
   DEVICES: 1
   DEVICE[0]: Id=192.168.x.x IP=... Vendor=HIKVISION ...
   CREATE_TYPE: HikGigECamera
   OPEN: OK
   GRAB: OK W=... H=...
   INTEGRITY: OK min=... max=...
   SAVE: D:\Work\CDT-320\QMC.CDT-320\QMC.Vision\bin\Debug\Log\CamTest\camtest_192.168.x.x_20260527_HHMMSS.png
   PASS id=... ip=... WxH=...x... saved=...
   ```
4. (별도) MVS SDK DLL 의 자동 배치 — csproj `<None Include CopyToOutput>` 또는 빌드 후 이벤트 추가 검토
