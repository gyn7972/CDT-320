#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
오토포커스 FOCUS 프로토콜 TCP 테스트 (핸들러 없이 Vision 단독 검증).

핸들러가 보낼 명령을 그대로 흉내내어:
  FOCUS_START -> (Z 스윕하며 FOCUS_VAL 반복) -> FOCUS_BEST
를 보내고 Vision 응답을 출력한다. Vision UI(설정 > 오토 포커스)에
표/그래프가 실시간으로 차오르는지 함께 확인.

전제:
  - QMC.Vision.exe 실행 중(서버 모드, 해당 포트 listen).
  - Sim 모드면 Vision 이 현재 보이는(저장/시뮬) 프레임을 grab 해 채점하므로
    Z 와 무관하게 score 가 비교적 평탄할 수 있음 → 통신/누적 파이프라인 검증용.
  - 실제 초점 곡선은 핸들러가 실제 Z 를 움직일 때 형성됨.

사용 예:
  python autofocus_tcp_test.py --camera bottom --target collet --pickups 1,2,3,4
  python autofocus_tcp_test.py --camera front  --target side
  python autofocus_tcp_test.py --host 192.168.0.10 --camera back --target side
"""
import argparse
import socket
import time

# 카메라 -> (포트, 와이어 모듈명, 기본 타깃들)
CAMERA_MAP = {
    "bottom": (5101, "BottomInspection", ["collet", "die"]),
    "front":  (5105, "TopSideVision",    ["side"]),
    "back":   (5106, "BottomSideVision", ["side"]),
}


def send_line(sock, line, timeout=5.0):
    sock.sendall((line + "\n").encode("utf-8"))
    sock.settimeout(timeout)
    buf = b""
    try:
        while b"\n" not in buf:
            chunk = sock.recv(4096)
            if not chunk:
                break
            buf += chunk
    except socket.timeout:
        return "(no response / timeout)"
    return buf.decode("utf-8", "replace").strip()


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--host", default="127.0.0.1")
    ap.add_argument("--camera", choices=list(CAMERA_MAP.keys()), default="bottom")
    ap.add_argument("--target", default=None, help="collet | die | side (생략 시 카메라 기본 첫 타깃)")
    ap.add_argument("--port", type=int, default=None, help="기본 포트 오버라이드")
    ap.add_argument("--module", default=None, help="와이어 모듈명 오버라이드")
    ap.add_argument("--pickups", default=None, help="콤마구분 픽업번호. Bottom 기본 1,2,3,4 / 측면 0")
    ap.add_argument("--zstart", type=float, default=19.0)
    ap.add_argument("--zend", type=float, default=21.0)
    ap.add_argument("--zstep", type=float, default=0.1)
    ap.add_argument("--delay", type=float, default=0.05, help="VAL 간 지연(초) — UI 채워지는 것 관찰용")
    args = ap.parse_args()

    port_default, module_default, targets = CAMERA_MAP[args.camera]
    port = args.port or port_default
    module = args.module or module_default
    target = (args.target or targets[0]).upper()
    camera = args.camera.upper()

    if args.pickups is not None:
        pickups = [int(x) for x in args.pickups.split(",") if x.strip() != ""]
    elif args.camera == "bottom":
        pickups = [1, 2, 3, 4]
    else:
        pickups = [0]

    zs = []
    z = args.zstart
    while z <= args.zend + 1e-9:
        zs.append(round(z, 4))
        z += args.zstep

    print(f"연결: {args.host}:{port}  모듈={module}  카메라={camera}  타깃={target}  픽업={pickups}")
    with socket.create_connection((args.host, port), timeout=5.0) as s:
        print("PING ->", send_line(s, f"{module}|PING"))
        print("START ->", send_line(s, f"{module}|FOCUS_START|{camera}|{target}"))

        for p in pickups:
            for i, zv in enumerate(zs):
                init = "1" if i == 0 else "0"
                resp = send_line(s, f"{module}|FOCUS_VAL|{zv}|{camera}|{target}|{p}|{init}")
                if i == 0 or i == len(zs) - 1:
                    print(f"  VAL p{p} z={zv} init={init} -> {resp}")
                time.sleep(args.delay)

        print("BEST ->", send_line(s, f"{module}|FOCUS_BEST|{camera}|{target}"))


if __name__ == "__main__":
    main()
