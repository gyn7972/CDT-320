using System;
using VI = global::QMC.Vision.Inspector;   // 원본 검사 라이브러리(QMc.Vision.Inspector) 별칭

namespace QMC.Vision.Core
{
    /// <summary>
    /// Bottom 외곽 종료(EventSearchDieEnd) → 핸들러 XYT 비동기 푸시 브리지.
    /// <para>흐름: <c>CDTInspector.BottomInspect</c> 가 외곽(패턴) 확정 즉시 <c>SearchDieEnd</c> 를
    /// '검사 스레드에서 동기' 발화 → 이 서비스가 스레드 로컬 컨텍스트(모듈/전역픽커/uid — 검사 직전
    /// <see cref="SetContext"/> 로 주입)와 결합해 백그라운드로
    /// "XYT|MODULE|fb|collet|chip_uid|x=..;y=..;t=..;ix=..;iy=.." 를 푸시한다.</para>
    /// <para>목적: Side 공정이 Bottom 검사(칩핑/이물 CUDA 포함) 완료를 기다리지 않고
    /// 해당 콜렛 다이의 X/Y/T 를 즉시 사용. t=NaN 이면 외곽 미검출(수신측 판단).</para>
    /// <para>컨텍스트가 없거나 전역 픽커(1~8)가 아니면(식별 불가 수동 테스트 등) 푸시하지 않는다.
    /// 푸시 실패는 검사 흐름에 영향을 주지 않는다(로그만).</para>
    /// </summary>
    public static class BottomXytPushService
    {
        // 검사 실행 스레드별 식별 컨텍스트 — SearchDieEnd 가 같은 스레드에서 동기 발화되므로 안전.
        [ThreadStatic] private static string _module;
        [ThreadStatic] private static int _picker;    // 전역 픽커 1~8(0=미지정)
        [ThreadStatic] private static string _chipUid;
        [ThreadStatic] private static int _indexX;
        [ThreadStatic] private static int _indexY;

        static BottomXytPushService()
        {
            VI.CDTInspector.SearchDieEnd += OnSearchDieEnd;
        }

        /// <summary>검사(ins.Inspect) 직전 호출 — 현재 스레드의 식별 컨텍스트 설정.</summary>
        public static void SetContext(string module, int picker, int indexX, int indexY, string chipUid)
        {
            _module = module;
            _picker = picker;
            _indexX = indexX;
            _indexY = indexY;
            _chipUid = chipUid;
        }

        /// <summary>검사 종료(finally) 시 호출 — 컨텍스트 해제.</summary>
        public static void ClearContext()
        {
            _module = null;
            _picker = 0;
            _indexX = 0;
            _indexY = 0;
            _chipUid = null;
        }

        private static void OnSearchDieEnd(float x, float y, double angleDeg, int libIx, int libIy)
        {
            try
            {
                // 스레드 로컬 캡처(백그라운드 발송 전에 지역 변수로 고정).
                string module = _module;
                int picker = _picker;
                string uid = _chipUid ?? "";
                int ix = _indexX != 0 || _indexY != 0 ? _indexX : libIx;
                int iy = _indexX != 0 || _indexY != 0 ? _indexY : libIy;

                if (string.IsNullOrEmpty(module)) return;                       // 식별 불가(구형 수동 경로) — 푸시 생략
                int fb = ColletAddress.FbOf(picker);
                int collet = ColletAddress.ColletOf(picker);
                if (fb < 0 || collet <= 0) return;                              // 전역 픽커 아님 — 푸시 생략

                // 미검출 정책(2026-07-04 확정): 각도 NaN(외곽 미검출)이면 x/y/t 전부 0 으로 보내고
                // valid=0 표시 — 핸들러/Side 는 정지하지 않고 진행한다.
                bool valid = !double.IsNaN(angleDeg) && !float.IsNaN(x) && !float.IsNaN(y);
                double px = valid ? x : 0.0;
                double py = valid ? y : 0.0;
                double pt = valid ? angleDeg : 0.0;

                System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {
                        bool sent = QMC.Vision.Comm.VisionTcpServer.PushBottomXyt(module, fb, collet, uid, px, py, pt, ix, iy, valid);
                        QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Event, "VISION", "BottomXyt",
                            module + " XYT 푸시 " + (sent ? "송신" : "생략(연결/서버 없음)") +
                            " — fb=" + fb + ", collet=" + collet + ", uid=" + uid +
                            ", x=" + px.ToString("F3") + ", y=" + py.ToString("F3") + ", t=" + pt.ToString("F4") +
                            ", ix=" + ix + ", iy=" + iy + ", valid=" + (valid ? "1" : "0(미검출→0 송신)"));
                    }
                    catch (Exception exSend)
                    {
                        QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Event, "VISION", "BottomXyt",
                            module + " XYT 푸시 실패(검사 흐름 영향 없음): " + exSend.Message);
                    }
                });
            }
            catch (Exception ex)
            {
                try
                {
                    QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Event, "VISION", "BottomXyt",
                        "SearchDieEnd 처리 실패(검사 흐름 영향 없음): " + ex.Message);
                }
                catch { /* 로그 실패는 무시 — 검사 흐름 보호 */ }
            }
            finally
            {
            }
        }
    }
}
