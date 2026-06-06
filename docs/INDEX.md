# CDT-320 docs 폴더 인덱스

**작성일**: 2026-05-04
**대상 폴더**: `D:\Work\CDT-320\docs\`
**총 파일 수**: 20 (Markdown 8 + PowerPoint 8 — 백업 포함)

---

## 1. 핵심 문서 (코드 분석 / 설계)

| # | 파일 | 종류 | 한 줄 요약 |
|---|---|---|---|
| 1 | `ARCHITECTURE_EXPORT.md` | MD (1,027줄) | CDT-320 솔루션 4개 프로젝트 코드 전체 분석 — 클래스/인터페이스/Sim Axis/Recipe/알람/이벤트/인터락/UI/mismatch 카탈로그 (v2, 2026-04-30) |

## 2. 시퀀스 상세 분석 (Stage 58 기준 코드 ↔ 문서)

| # | 파일 | 종류 | 한 줄 요약 |
|---|---|---|---|
| 2 | `04_Feeder_시퀀스_상세분석.md` | MD (559줄) | InputLoader / OutputUnloader 두 피더의 4단계 시퀀스 + Pickup/Place 헬퍼 + Good1→Good2 자동전환 분석 |
| 3 | `05_InputStage_시퀀스_상세분석.md` | MD (343줄) | InputStage (StageY/T, ExpanderZ, CameraX, NeedleBlockX/Z) Load/Vision Align/MultiScan/Unload 5단계 + 12개 알람 발생점 |
| 4 | `06_TransferPicker_시퀀스_상세분석.md` | MD (408줄) | TPU 2-Arm × 4-Picker × 2-Vision 구조 + PickerComponent 메서드 6종(라인번호) + Bottom/Side 4면 검사 (v1.1) |
| 5 | `07_OutputStage_시퀀스_상세분석.md` | MD (259줄) | OutputStage (Good/Ng StageModule × 2 + BinCameraX) Receive/Place/안착검사 + 좌우 충돌 회피 인터락 |
| 6 | `08_OutputUnloader_적재_시퀀스.md` | MD (320줄) | OutputUnloader (NG/Good1/Good2 × 25 슬롯 = 75) `StoreCompletedWaferAsync` 흐름 + 만석 자동 처리 |

## 3. 설정 / GUI 가이드 (Stage 59)

| # | 파일 | 종류 | 한 줄 요약 |
|---|---|---|---|
| 7 | `10_설정_페이지_가이드.md` | MD (225줄) | 신규 4개 설정 페이지(Position Teaching / Axis / Camera / Light Setup) + IO 매핑 편집 사용법 |
| 8 | `11_설정_편집_고급기능.md` | MD (154줄) | Stage 59 Round 11~14 — PositionTeaching ApplyToSetup 19개 매핑 확장 + AxisSetup APPLY 확장 (DefaultVelocity/Stroke 리플렉션) |

## 4. 작업 로그 (야간 무인 작업 결과)

| # | 파일 | 종류 | 한 줄 요약 |
|---|---|---|---|
| 9 | `MISMATCH_RESOLUTION_LOG.md` | MD (150줄) | 시퀀스 문서 04~08 ↔ 코드 mismatch 5개 sub-agent 병렬 보완 — 1,460→1,889줄 (+429), "Mismatch 보완" 마킹 18곳 |
| 10 | `ALARM_CODES_APPLIED.md` | MD (100줄) | AlarmManager.Raise 일괄 적용 (R4~R6) — AlarmMaster 신규 15개 등록 + InputStage 13곳 / OutputStage·Unloader Raise 추가 |
| 11 | `UI_BUTTON_AUDIT_REPORT.md` | MD (63줄) | `--audit-all` CLI 로 전 페이지 dead button 식별 + UiClickAuditor placeholder 자동 부착 (StageRecipePage 35/35 등) |
| 12 | `PPT_TABLE_FILL_LOG.md` | MD (257줄) | 02/03 PPT 빈 표를 채우기 위한 Stage 1~58 데이터 수집 (R8 — R9 PPT 편집의 입력 소스) |

## 5. PowerPoint 산출물 (ASE 납품용)

| # | 파일 | 종류 | 한 줄 요약 |
|---|---|---|---|
| 13 | `01_CDT320_설계도.pptx` | PPT (15 슬라이드) | System Design — 매뉴얼 3종(CDT-300/310) 호환, ASE 제출용 시스템 아키텍처 설계서 (2026-04-29) |
| 14 | `02_CDT320_개발계획서.pptx` | PPT (12 슬라이드, **파일 손상**) | Development Plan — 32 Stage 종합 개발 계획서. **현재 파일은 EOCD 누락으로 zip 파싱 불가** → `.bak2` 사용 권장 |
| 15 | `03_CDT320_체크리스트.pptx` | PPT (11 슬라이드, **파일 손상**) | Verification Checklist — Stage 1~54 verify_all.pl 매트릭스. **현재 파일은 zip 손상** → `.bak2` 사용 권장 |
| 16 | `09_시퀀스_분석.pptx` | PPT (11 슬라이드) | Sequence Detailed Analysis — InputLoader/InputStage/TPU/OutputStage/OutputUnloader 시퀀스 요약 (docs 04~08 참조) |

## 6. 백업 파일

| # | 파일 | 종류 | 한 줄 요약 |
|---|---|---|---|
| 17 | `02_CDT320_개발계획서.bak.pptx` | PPT 백업 (12 슬라이드) | 02 PPT 의 4-30 11:00 시점 백업 (개발계획서 R8 작업 직전 스냅샷) |
| 18 | `02_CDT320_개발계획서.bak2.pptx` | PPT 백업 (12 슬라이드) | 02 PPT 의 5-04 12:11 시점 백업 — **현재 정상 파싱 가능한 유일본** |
| 19 | `03_CDT320_체크리스트.bak.pptx` | PPT 백업 (11 슬라이드) | 03 PPT 의 4-30 11:00 시점 백업 (체크리스트 작업 직전 스냅샷) |
| 20 | `03_CDT320_체크리스트.bak2.pptx` | PPT 백업 (11 슬라이드) | 03 PPT 의 5-04 12:11 시점 백업 — **현재 정상 파싱 가능한 유일본** |

---

## 비고

- **파일 손상 경고**: `02_CDT320_개발계획서.pptx` 와 `03_CDT320_체크리스트.pptx` 는 file 명령상 OOXML 로 인식되나 zip Central Directory 가 손상돼 Python/unzip 모두 추출 실패. `.bak2` 가 가장 최근의 정상 파일.
- **수정일 기준 최신 작업**: 2026-05-04 의 PPT_TABLE_FILL_LOG.md 와 .bak2 PPT 가 가장 최근 변경됨.
- **시퀀스 문서 04~08** 은 모두 Stage 58 코드 기준이며 06번만 v1.1 (mismatch 보완 반영).
- 본 인덱스는 각 파일의 헤더/목차/슬라이드 1~3 텍스트를 읽어 작성됨. 세부 내용은 해당 파일 직접 열람 필요.
