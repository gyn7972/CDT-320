using System;
using System.Runtime.InteropServices;

namespace QMC.Vision.Core
{
    /// <summary>
    /// CDT-310 <c>CudaWrapper</c>(MakePixelShiftImage.dll) P/Invoke 포팅 — 라인 후보 검출의 GPU 경로.
    /// 장비 PC에 CUDA DLL 이 있으면 사용하고, 없으면(<see cref="Available"/>=false) 호출측이 CPU 로 우회한다.
    /// 첫 사용 시 1회 가용성 프로브(작은 할당/해제). 실패해도 예외를 던지지 않고 false 로만 표시.
    /// </summary>
    internal static class CudaInterop
    {
        private const string DLL = "MakePixelShiftImage.dll";

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern int AllocateDeviceMemory(out IntPtr devPtr, ulong size);
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern int FreeDeviceMemory(IntPtr devPtr);
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern int CopyHostToDevice(IntPtr dst, IntPtr src, ulong size);
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern int CopyDeviceToHost(IntPtr dst, IntPtr src, ulong size);
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern int FindTopBottomLineCandidates(
            IntPtr d_input, int width, int height, byte threshold,
            int topStartY, int topEndY, int bottomStartY, int bottomEndY,
            IntPtr d_topCandidates, IntPtr d_bottomCandidates);

        // ── 선택적 확장 익스포트(장비 PC CUDA DLL 이 제공하면 자동 사용, 없으면 CPU 폴백) ──
        // 계약/시그니처는 docs/CUDA_Integration.md 참조. 이름은 Collet Finder(cf_*) 패턴을 따른다.
        // DLL 에 해당 익스포트가 없으면 EntryPointNotFoundException → 기능별 가용성 캐시가 0 으로 고정되어 이후 CPU 고정.
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern int cf_cuda_device_name(int index, byte[] buf, int bufLen);
        /// <summary>박스(정사각 SE) 그레이 모폴로지. isMax!=0 → dilate(최대), 0 → erode(최소). 성공 0.</summary>
        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern int cf_morph_box_u8(byte[] src, byte[] dst, int width, int height, int radius, int isMax);

        private static int _avail = -1;   // -1=미확인, 0=불가, 1=가능
        private static int _morph = -1;   // 박스 모폴로지 익스포트 가용성(기능별 캐시)
        private static string _devName;   // 1회 조회 캐시
        private static readonly object _lock = new object();

        /// <summary>CUDA DLL 사용 가능 여부(최초 1회 프로브). DLL 없거나 GPU 없으면 false → CPU 우회.</summary>
        public static bool Available
        {
            get
            {
                if (_avail >= 0) return _avail == 1;
                lock (_lock)
                {
                    if (_avail >= 0) return _avail == 1;
                    try
                    {
                        IntPtr p;
                        if (AllocateDeviceMemory(out p, 64) == 0 && p != IntPtr.Zero)
                        { FreeDeviceMemory(p); _avail = 1; }
                        else _avail = 0;
                    }
                    catch { _avail = 0; }   // DllNotFound/EntryPointNotFound/BadImageFormat 등 → CPU 우회
                    return _avail == 1;
                }
            }
        }

        /// <summary>
        /// 310 CudaWrapper.GetTopBottomLineCandidates 동등. 각 열의 상/하 에지 후보 y(-1=없음)를 GPU 로 산출.
        /// 성공 시 true + topC/botC(길이 w), 실패/미가용 시 false(호출측 CPU 폴백).
        /// </summary>
        public static bool TryGetTopBottomLineCandidates(byte[] img, int w, int h, byte threshold, double scanRate,
                                                         out int[] topC, out int[] botC)
        {
            topC = null; botC = null;
            if (!Available || img == null || w <= 0 || h <= 0) return false;

            if (scanRate < 0.3) scanRate = 0.3;
            if (scanRate > 1) scanRate = 0.99;

            IntPtr dIn = IntPtr.Zero, dTop = IntPtr.Zero, dBot = IntPtr.Zero;
            GCHandle hIn = default(GCHandle), hTop = default(GCHandle), hBot = default(GCHandle);
            try
            {
                ulong imgSize = (ulong)(w * h);
                ulong candSize = (ulong)(w * sizeof(int));
                if (AllocateDeviceMemory(out dIn, imgSize) != 0) return false;
                if (AllocateDeviceMemory(out dTop, candSize) != 0) return false;
                if (AllocateDeviceMemory(out dBot, candSize) != 0) return false;

                topC = new int[w]; botC = new int[w];
                hIn = GCHandle.Alloc(img, GCHandleType.Pinned);
                hTop = GCHandle.Alloc(topC, GCHandleType.Pinned);
                hBot = GCHandle.Alloc(botC, GCHandleType.Pinned);

                if (CopyHostToDevice(dIn, hIn.AddrOfPinnedObject(), imgSize) != 0) return false;

                int topStartY = Math.Max(2, 10);
                int topEndY = Math.Min(h - 3, (int)(h * scanRate) - 10);
                if (topEndY < topStartY) topEndY = topStartY;
                int bottomStartY = Math.Min(h - 3, h - 10);
                int bottomEndY = Math.Max(2, (int)(h * 0.7) + 10);
                if (bottomStartY < bottomEndY) bottomStartY = bottomEndY;

                int st = FindTopBottomLineCandidates(dIn, w, h, threshold,
                    topStartY, topEndY, bottomStartY, bottomEndY, dTop, dBot);
                if (st != 0) return false;

                if (CopyDeviceToHost(hTop.AddrOfPinnedObject(), dTop, candSize) != 0) return false;
                if (CopyDeviceToHost(hBot.AddrOfPinnedObject(), dBot, candSize) != 0) return false;
                return true;
            }
            catch { _avail = 0; topC = null; botC = null; return false; }   // 런타임 실패 시 이후 CPU 고정
            finally
            {
                if (hIn.IsAllocated) hIn.Free();
                if (hTop.IsAllocated) hTop.Free();
                if (hBot.IsAllocated) hBot.Free();
                if (dIn != IntPtr.Zero) try { FreeDeviceMemory(dIn); } catch { }
                if (dTop != IntPtr.Zero) try { FreeDeviceMemory(dTop); } catch { }
                if (dBot != IntPtr.Zero) try { FreeDeviceMemory(dBot); } catch { }
            }
        }

        /// <summary>CUDA 디바이스 이름(없거나 미지원 익스포트면 null). 1회 조회 후 캐시.</summary>
        public static string DeviceName
        {
            get
            {
                if (_devName != null) return _devName.Length == 0 ? null : _devName;
                if (!Available) { _devName = ""; return null; }
                try
                {
                    var buf = new byte[256];
                    if (cf_cuda_device_name(0, buf, buf.Length) == 0)
                    {
                        int n = Array.IndexOf(buf, (byte)0); if (n < 0) n = buf.Length;
                        _devName = System.Text.Encoding.ASCII.GetString(buf, 0, n).Trim();
                    }
                    else _devName = "";
                }
                catch { _devName = ""; }   // 익스포트 없음 → 이름 미표시(기능은 별도 캐시)
                return _devName.Length == 0 ? null : _devName;
            }
        }

        /// <summary>박스 모폴로지 GPU 시도. isMax=true→dilate, false→erode. 성공 시 true+dst, 미가용/실패 시 false(CPU 폴백).</summary>
        public static bool TryBoxMorph(byte[] src, int w, int h, int radius, bool isMax, out byte[] dst)
        {
            dst = null;
            if (_morph == 0 || src == null || w <= 0 || h <= 0 || !Available) return false;
            try
            {
                var o = new byte[src.Length];
                int st = cf_morph_box_u8(src, o, w, h, Math.Max(1, radius), isMax ? 1 : 0);
                if (st != 0) return false;
                _morph = 1; dst = o; return true;
            }
            catch { _morph = 0; return false; }   // EntryPointNotFound/런타임 실패 → 이후 CPU 고정
        }
    }
}
