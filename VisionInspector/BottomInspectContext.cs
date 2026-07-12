using System;
using System.Runtime.InteropServices;

namespace QMC.Vision.Inspector
{
    /// <summary>
    /// Bottom 검사 1건이 빌려 쓰는 host 스크래치 묶음(2026-07-12) — 매 호출 반복되던 대형 LOH 할당을
    /// 재사용으로 없앤다. 검사 진입 시 <see cref="BottomScratchPool.Rent"/> 로 하나 빌리고, 끝날 때
    /// Lease.Dispose 로 반납한다(콜렛 8개 동시 = 풀 8개).
    /// <para>1단계 A(2026-07-12): 2배 업스케일 ROI 버퍼(<see cref="EnsureUpscaled"/>) 만 담는다.
    /// 나머지 대형 버퍼(전치 밴드/칩핑 마스크/링크 마스크)는 이미 <see cref="BufferPool"/> 로 풀링됨.</para>
    /// <para>안전 계약: 재사용 버퍼는 소비 전 전량 덮어써지는 용도(GPU D2H 가 outSize 전체 기록)로만 쓰며,
    /// 검사가 끝날 때까지(finally 반납) 다른 검사가 같은 컨텍스트를 쓰지 않으므로 결과에 영향이 없다.</para>
    /// </summary>
    public sealed class BottomInspectContext
    {
        // 2배 업스케일 ROI 버퍼(byte[,]) + 영속 핀. (h,w) 가 바뀔 때만 재할당·재핀 → 정상 운전 시 할당 0회.
        // LOH(>85KB)라 장기 핀도 압축 방해가 없다(LOH 미압축).
        private byte[,] _upscaled;
        private GCHandle _upscaledPin;
        private int _upH, _upW;

        /// <summary>2배 업스케일 ROI 버퍼를 (h,w) 크기로 보장하고 고정 포인터를 돌려준다.
        /// 크기가 같으면 기존 버퍼·핀 그대로 재사용, 다르면 그 버퍼만 재할당+재핀.</summary>
        public byte[,] EnsureUpscaled(int h, int w, out IntPtr pinnedPtr)
        {
            if (_upscaled == null || _upH != h || _upW != w)
            {
                if (_upscaledPin.IsAllocated) _upscaledPin.Free();
                _upscaled = new byte[h, w];
                _upscaledPin = GCHandle.Alloc(_upscaled, GCHandleType.Pinned);
                _upH = h;
                _upW = w;
            }
            pinnedPtr = _upscaledPin.AddrOfPinnedObject();
            return _upscaled;
        }

        /// <summary>보유 핀 해제(프로세스 종료 시 풀이 호출). 이후 EnsureUpscaled 재호출 시 재할당된다.</summary>
        internal void FreePins()
        {
            if (_upscaledPin.IsAllocated) _upscaledPin.Free();
            _upscaled = null;
            _upH = 0;
            _upW = 0;
        }
    }
}
