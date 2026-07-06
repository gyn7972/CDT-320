using System;
using System.Collections.Generic;

namespace QMC.CDT320.VisionComm
{
    /// <summary>Bottom 외곽 종료(EventSearchDieEnd) XYT 푸시 1건 —
    /// "XYT|MODULE|fb|collet|die_index|x=..;y=..;t=..;ix=..;iy=..;valid=0|1" 파싱 결과(키=die_index, 2026-07-06).</summary>
    public sealed class BottomXytPush
    {
        /// <summary>0=Front / 1=Back(Rear).</summary>
        public int Fb { get; set; }
        /// <summary>콜렛 1~4.</summary>
        public int Collet { get; set; }
        /// <summary>결과 매칭 키 = die_index(픽업 순서 1-base, 문자열 원문 보존 — 구 chip_uid 자리).</summary>
        public string DieIndex { get; set; }
        /// <summary>중심 X(px, Vision 최종 result.Offset 규약).</summary>
        public double X { get; set; }
        /// <summary>중심 Y(px).</summary>
        public double Y { get; set; }
        /// <summary>각도 T(deg). 미검출 시 0 송신 정책(valid=0 로 구분).</summary>
        public double T { get; set; }
        public int IndexX { get; set; }
        public int IndexY { get; set; }
        public DateTime ReceivedAt { get; set; } = DateTime.Now;

        /// <summary>외곽 검출 유효 여부 — 와이어 valid=0/1 기준(미검출 시 x/y/t=0 으로 송신되고 valid=0).
        /// 구형 페이로드(valid 키 없음)는 수신부가 T 의 NaN 여부로 설정한다. 미검출이어도 진행(정지 아님)이 정책.</summary>
        public bool IsValid { get; set; } = true;
    }

    /// <summary>
    /// Vision → 핸들러 XYT 푸시 스토어 — (fb, collet) 최신 1건 + die_index 별 최신 1건 보관.
    /// <para>Bottom 외곽이 확정되는 즉시(칩핑/이물 검사 완료 전) 푸시가 도착하므로,
    /// Side 공정은 Bottom 결과 폴링을 기다리지 않고 해당 콜렛 다이의 X/Y/T 를 조회할 수 있다.</para>
    /// 기록은 <see cref="VisionTcpClient"/> 수신 루프가 수행(구독 배선 불필요). 스레드 세이프.
    /// </summary>
    public static class BottomXytStore
    {
        private static readonly object _lock = new object();
        private static readonly Dictionary<int, BottomXytPush> _byCollet = new Dictionary<int, BottomXytPush>();          // key = fb*4+collet
        private static readonly Dictionary<string, BottomXytPush> _byUid = new Dictionary<string, BottomXytPush>(StringComparer.OrdinalIgnoreCase);

        /// <summary>XYT 1건 기록(수신 루프 전용). 같은 (fb,collet)/uid 는 최신으로 덮어쓴다.</summary>
        public static void Record(BottomXytPush push)
        {
            if (push == null)
                return;
            lock (_lock)
            {
                if (push.Fb >= 0 && push.Collet >= 1 && push.Collet <= 4)
                    _byCollet[push.Fb * 4 + push.Collet] = push;
                if (!string.IsNullOrEmpty(push.DieIndex))
                    _byUid[push.DieIndex] = push;
            }
        }

        /// <summary>(fb, collet) 최신 XYT 조회. fb=0(Front)/1(Back), collet=1~4.</summary>
        public static bool TryGet(int fb, int collet, out BottomXytPush push)
        {
            lock (_lock)
                return _byCollet.TryGetValue(fb * 4 + collet, out push) && push != null;
        }

        /// <summary>die_index(결과 매칭 키) 최신 XYT 조회 — 구 chip_uid 키 폐기(2026-07-06).</summary>
        public static bool TryGetByDie(int dieIndex, out BottomXytPush push)
        {
            return TryGetByDie(dieIndex.ToString(), out push);
        }

        /// <summary>die_index 문자열 키 최신 XYT 조회.</summary>
        public static bool TryGetByDie(string dieIndex, out BottomXytPush push)
        {
            push = null;
            if (string.IsNullOrEmpty(dieIndex))
                return false;
            lock (_lock)
                return _byUid.TryGetValue(dieIndex, out push) && push != null;
        }

        /// <summary>랏/웨이퍼 경계 등에서 초기화.</summary>
        public static void Clear()
        {
            lock (_lock)
            {
                _byCollet.Clear();
                _byUid.Clear();
            }
        }
    }
}
