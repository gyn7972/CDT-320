# CDT-320 사용자 가이드

## 1. 빠른 시작 (Quick Start)

### 1단계 — 프로그램 실행 순서
1. **CDT320Simulator.exe** 실행 → 메인 윈도우의 **[TCP START]** 버튼 클릭 (포트 7001)
2. **QMC.Vision.exe** 실행 → 자동으로 5100/5101/5103 listen 시작
3. **QMC.CDT-320.exe** (메인 핸들러) 실행 → 헤더 우측 "VIS ●" 녹색 확인 (Vision 자동 연결)

### 2단계 — 사용자 로그인
- 우하단 **USER** 탭 → "engineer" 클릭 → PIN 입력 (기본: `1234`) → 로그인
- 권한 레벨:
  - **None** — 화면 보기만
  - **Operator** — Init / Start / CycleRun
  - **Engineer** — Recipe 편집 + Vision Align
  - **Maintenance** — IO / Motion 직접 조작
  - **Admin** — 사용자 관리 + 시스템 설정

### 3단계 — 첫 번째 사이클
1. **WORK** 탭 → 좌측 사이드바
2. **초기화** 버튼 → 모든 축 HOME 검색 (~30초)
3. **CYCLE RUN** 버튼 → 자동으로 10 다이 처리
4. 헤더 중앙 큰 글자 = 현재 상태 (READY / CYCLING / ALARM ...)

## 2. 메인 화면 구성

### 헤더 (상단 70px)
```
[QMC 로고 + CDT-320]   [큰 상태 글자]   [USER (level)]   [시간]
```

### 상태바 (오렌지, 30px)
```
[Map]  [Project]  [Barcode/VIS ●]  [Bin]  [Vision]  [Pick]  [Reference]
```

### 메인 영역 (탭 별 50+ 페이지)
- **Work** — 운전 / 정지 / 사이클 + 비전 정렬 + 다이맵
- **Work Info** — 모듈 별 실시간 정보 + Active Lot
- **History** — 알람 / 이벤트 / 작업 시간
- **Recipe** — 프로젝트 + 5 Subset (Die/Frame/Load/Unload/Module) + Vision recipes
- **Settings** — Motion / IO / 시뮬레이터 / 비전 / 자가진단 / 원격 뷰어
- **User** — 로그인 / 사용자 관리

### 하단 네비 (검정, 80px)
```
[WORK] [WORK INFO] [HISTORY] [RECIPE] [SETTINGS] [USER]    [EXIT]
```

## 3. 자가 진단 실행

1. **Settings** 탭 → 좌측 하단 "자가진단" 버튼
2. 다이얼로그 → **RUN** 클릭
3. 14 항목 자동 검증:
   - AppSettings / AjinConfig / AXL library / Machine tree
   - Simulator TCP / Vision×3 / Event log / Recipe dir
   - **BinCodeMap** / **DieMap generator** / **JobQueue** / **InterlockRegistry** / **AlignmentSolver**
4. 모두 OK (녹색) 면 정상.

## 4. Vision 정렬 (3-point Align)

1. WORK 탭 → "비전 얼라인" 클릭
2. 3 기준점 입력 (모터 좌표):
   - TopLeft, TopRight, BottomLeft
3. **ALIGN** 버튼 → 비전이 3 회 매칭 → CoordinateMap 자동 갱신 + JSON 저장

## 5. 다이 맵 시각화

1. WORK 탭 → "다이 맵" 클릭
2. 격자 입력 (예: 50×50, pitch 1mm)
3. **GENERATE** → 화면에 격자 표시
4. **FILL DEMO RESULTS** → 랜덤 결과로 색칠 (테스트 용)
5. **SAVE CSV+JSON** → `Log/DieMap/yyyy-MM-dd/` 에 저장

## 6. Bin Code 매핑 편집

1. RECIPE 탭 → "빈 코드 매핑" 클릭
2. 좌측 그리드 — NG Code → Bin Number (행 우선순위)
3. 우측 그리드 — Bin 번호 → Color (셀 더블클릭으로 색상 선택)
4. 우하단 **Test mapping** — NG Codes (CSV) 입력 후 **Test** 클릭 → bin + 색상 미리보기
5. **SAVE** 클릭 → `Config/bin_codes.json` 저장

## 7. Recipe Subset 편집

1. RECIPE 탭 → "다이 사양" / "웨이퍼 사양" / "로드 웨이퍼" / "언로드 웨이퍼" 4 페이지
2. 각 페이지 상단 — 현재 프로젝트 (AppSettings.LastProject) 자동 로드
3. 값 수정 → **SAVE** 클릭 → 같은 프로젝트의 Subset 부분만 갱신

## 8. SECS Host 통신

### 시뮬 모드 (line protocol)
- SecsHost.Start() 호출 (자동 또는 코드)
- 클라이언트 (테스트 용) — `telnet localhost 5000` 후:
  ```
  PING                    → PONG
  RC|ProceedWithTapeFrame|<objId>  → RCACK|ProceedWithTapeFrame|0
  ```

### HSMS 모드 (SEMI E37)
- SecsHost.UseHsms = true 설정
- 호스트에서 4-byte length prefix + SECS-II 메시지 송신
- AlarmManager 가 알람 발생 시 자동 S5F1 송신

## 9. Active Lot 모니터링

1. **WORK INFO** 탭 → "현재 LOT" 클릭
2. 표시 정보:
   - LotID / Recipe / State / Started time
   - Processed / Total
   - Good / NG count
   - Yield %
   - Bin 분포 막대 차트 (BinCodeMap 색상)
3. 1초 주기로 자동 갱신

## 10. Remote Viewer (원격 모니터링)

1. **Settings** 탭 → "원격 뷰어" 클릭
2. 포트 입력 (기본 5099) → **Start** 클릭
3. Handler 가 1초 주기로 화면 캡처 → TCP 5099 로 base64 송신
4. 외부 PC 에서 `telnet localhost 5099` 접속하면 `FRAME|<base64>` 라인 수신
5. 자체 미리보기 — 다이얼로그 내 PictureBox 에 같은 화면 표시

## 11. 자동 검증 스크립트

PowerShell 또는 Bash 에서:
```bash
perl tools/verify_handler_features.pl  # 25 항목
perl tools/verify_vision_features.pl   # 21 항목
perl tools/verify_stage2.pl            # SPC + 5 Editors + Zoom
perl tools/runtime_cycle_test.pl       # 환경 + 기동 안정성
```

UI Automation:
```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/gui_cycle_automation.ps1
```

## 12. 문제 해결

### Vision 연결 안 됨 (헤더 "VIS ○" 회색)
1. Vision exe 실행 중인지 확인
2. Settings → 비전 링크 → Host/Port 확인 (기본 127.0.0.1:5100)
3. 방화벽 5100/5101/5103 허용

### 사이클 실행 시 Interlock 차단
1. History → Alarm 에서 차단 사유 확인 (예: "Picker in stage workspace + Z down")
2. Maintenance 탭 → 수동 모션으로 안전 위치로 이동 후 재시도

### Cognex VisionPro 미작동
1. Vision exe 의 Configuration 페이지 → "Cognex VisionPro diagnostics" 패널
2. **Refresh** 클릭 → 어셈블리 로드 상태 확인
3. **Run test** 클릭 → 실 Train+Match 시도 → 실패 시 fallback 사실 표시

### Lot 데이터 누락
1. 사이클 실행 후 `bin/Debug/Log/Lots/yyyyMMdd_LOT-*.json` 확인
2. WorkInfo → 현재 LOT 페이지에서 실시간 상태
