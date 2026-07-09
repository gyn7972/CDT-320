# CDT-320 구현 체크리스트 — 칩데이터·차트 리밋 공통화 (Stage 5, 확정본)

근거: [04_design.md](04_design.md) — 범위 Bottom만 / 모듈 우선 / 기준값±공차 입력

## 공통 헬퍼 (CommonChipSpec)
- [ ] `QMC.Vision/Equipment/Core/CommonChipSpec.cs` 신규 — TryGetCommonWidth/Height, ApplyToBottom, PushBottomChartLimits
- [ ] 공통값 유효 조건: 축별 Lower > 0 && Upper > Lower (공차 0 = 비활성)
- [ ] ApplyToBottom: 모듈값(해당 축 상·하한 중 하나라도 >0) 우선, 둘 다 0인 축만 공통 폴백
- [ ] 공통 폴백 발생 시 EventLogger(Event, "VISION", "ChipSpec") 1줄 기록
- [ ] try/catch — 조회/적용 실패가 레시피 적용을 깨지 않음

## R-001 — AlgorithmNode 스펙 적용
- [ ] BottomInspector 분기의 ChipLower/UpperSpecLimit 직접 대입(242–243행)을 CommonChipSpec.ApplyToBottom 호출로 대체
- [ ] 다른 필드(Threshold, ChippingDepth 등) 적용 동작 불변

## R-002 — 차트 리밋 유도
- [ ] ② 차트 push 블록에서 Bottom 만 CommonChipSpec.PushBottomChartLimits 로 대체
- [ ] 모듈 Chart1/2 리밋이 설정된 축은 모듈값 그대로 push (모듈 우선)
- [ ] Side/Bin 차트 push 경로 변경 없음

## R-003 — 공통 그리드(기준값±공차) + 즉시 반영
- [ ] BuildCommonGrid: "Chip W/H Lower·Upper" 4칸 제거, "Chip Width ±"/"Chip Height ±" 공차 2칸 추가
- [ ] 기준값 변경 시 현재 공차 유지한 채 Lower/Upper 재계산
- [ ] 공차 변경 시 기준값 중심으로 Lower/Upper 재계산(Lower는 0 미만 클램프)
- [ ] OnCommonSaveClick: 저장 후 BottomInspection 노드에 ApplyToBottom + PushBottomChartLimits 재실행
- [ ] 운영뷰(Bottom 뷰어) 차트 빨간 점선이 공통값으로 갱신됨

## 공통
- [ ] UTF-8 저장, 기존 코드 스타일(주석 밀도·명명) 준수
- [ ] 별도 OutDir 컴파일 검증 통과(장비 프로그램 종료 금지 — 메모리 규칙)
- [ ] 기존 레시피 JSON 로드 호환(신규 저장 필드 없음)
