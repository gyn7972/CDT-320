# Bottom 좌표 선응답 — 구현 체크리스트 (Stage 5)

## D-1. VisionInspector — SearchDieEnd W/H 확장
- [ ] `SearchDieEnd` 시그니처에 `double wMm, double hMm` 추가(구독자 동시 수정)
- [ ] `RaiseSearchDieEnd` 발화 지점을 vList 평균 계산 직후로 이동(여전히 CUDA 칩핑 이전)
- [ ] W/H 사본 선계산이 본류 수식과 동일(mm 변환 ÷2 + W↔H 스왑 포함)
- [ ] 본류(result.Width/Height 계산 경로)는 무변경 — diff 로 확인
- [ ] 미검출(angle NaN) 시에도 예외 없이 발화(값 판단은 구독자)
- [ ] 발화 실패가 검사 흐름에 영향 없음(기존 try/catch 유지)

## D-2. QMC.Vision — 푸시 페이로드 확장
- [ ] `BottomXytPushService.OnSearchDieEnd` 새 시그니처 수신, valid=0 이면 w/h=0
- [ ] `VisionTcpServer.PushBottomXyt` 에 wMm/hMm 추가, 페이로드 끝에 `;w=..;h=..`(F4)
- [ ] 푸시 로그에 w/h 포함
- [ ] Task.Run 비동기 송신 유지(본류 비차단)

## D-3. QMC.CDT-320 — 수신/스토어 확장
- [ ] `BottomXytPush` 에 `W`/`H`(mm) 프로퍼티 추가
- [ ] `HandleBottomXytPush` 가 `w`/`h` 키 파싱(키 없으면 0 — 구버전 호환)
- [ ] `VisionProtocolPushCommands.BottomXyt` 주석의 페이로드 형식 갱신
- [ ] 핸들러 빌드 무경고(신규 경고 없음)

## 공통 검증
- [ ] QMc.Vision.Inspector / QMC.Vision / QMC.CDT-320 3개 프로젝트 빌드 성공
- [ ] 검사 결과 값 불변: 벤치 세트(D:\BenchImages) 골든 대조 105장 전량 일치(테스터)
- [ ] 라이브 검증(장비 재시작 후): Bottom 통신 검사 1회 → 이벤트 로그의 XYT 푸시 라인에 w/h 포함 확인,
      해당 값 == 이후 INSPECTRESULT 의 W/H 와 일치 확인
- [ ] 미검출 다이(빈 촬상)에서 valid=0, w/h=0 푸시 확인

## 범위 밖(옵션 B — 별도 건)
- PickerBottomInspectionSequence 가 XYT 도착 시점에 후속 이동을 선행하는 시퀀스 재배열
