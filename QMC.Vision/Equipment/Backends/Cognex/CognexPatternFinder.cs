using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using QMC.Vision.Core;

namespace QMC.Vision.Backends.Cognex
{
    /// <summary>
    /// Cognex CogPMAlignTool 기반 Pattern Finder.
    /// 동적 reflection 으로 호출 — 빌드 시 Cognex 어셈블리 의존 없음.
    /// 미로드/실패 시 OpenCvPatternFinder(BasicSad) 로 자동 fallback.
    /// </summary>
    public class CognexPatternFinder : IPatternFinder
    {
        public string Id { get; }
        public Roi SearchRoi { get; set; }
        public Roi TrainRoi  { get; set; }
        public Bitmap TrainImage { get; private set; }
        public double AcceptThreshold { get; set; } = 0.7;
        public int    MaxInstances    { get; set; } = 1;
        public bool   AngleEnabled      { get; set; } = false;
        public double AngleToleranceDeg { get; set; } = 10.0;
        public double AngleStepDeg      { get; set; } = 1.0;
        public bool   PreferNearestCenter { get; set; } = false;

        private readonly CognexBackend _be;
        private readonly OpenCv.OpenCvPatternFinder _fallback;

        // CogPMAlignTool 인스턴스 (dynamic). null = 미학습.
        private dynamic _pma;
        private bool    _trainSucceeded;

        public CognexPatternFinder(string id, CognexBackend be)
        {
            Id = id; _be = be;
            _fallback = new OpenCv.OpenCvPatternFinder(id, new OpenCv.OpenCvBackend());
            SearchRoi = new Roi { Name = id + ".Search", CenterX = 320, CenterY = 240, Width = 400, Height = 300 };
            TrainRoi  = new Roi { Name = id + ".Train",  CenterX = 320, CenterY = 240, Width = 100, Height = 100 };
        }

        public void Train(Bitmap image)
        {
            if (image == null) return;
            var rect = TrainRoi.BoundingBox;
            rect.Intersect(new Rectangle(0, 0, image.Width, image.Height));
            if (rect.Width <= 0 || rect.Height <= 0) return;
            TrainImage?.Dispose();
            TrainImage = image.Clone(rect, image.PixelFormat);

            // OpenCv fallback 도 항상 학습 (Cognex 실패시 안전망)
            _fallback.TrainRoi  = TrainRoi.Clone();
            _fallback.SearchRoi = SearchRoi.Clone();
            _fallback.Train(image);

            _trainSucceeded = false;
            if (!_be.CognexLoaded) return;

            try
            {
                var asms = _be.LoadedAssemblies;
                var pmaType = CognexInterop.GetType("Cognex.VisionPro.PMAlign.CogPMAlignTool", asms);
                if (pmaType == null) return;

                _pma = Activator.CreateInstance(pmaType);
                dynamic cogImage = CognexInterop.BitmapToICogImage(image, asms);
                dynamic trainRegion = CognexInterop.NewRectangle(rect.X, rect.Y, rect.Width, rect.Height, asms);

                // Pattern.TrainImage / TrainRegion 설정
                dynamic pattern = _pma.Pattern;
                pattern.TrainImage  = cogImage;
                pattern.TrainRegion = trainRegion;
                // Origin = 학습 영역 중심(이미지 좌표). 결과 Pose.Translation 이 '찾은 위치'의 절대 이미지 좌표가 되도록.
                // (CogRectangle 0×0 은 "Width>0" 예외 → CogTransform2DLinear 평행이동으로 지정한다.)
                try {
                    CognexInterop.TrySet(pattern, "Origin",
                        CognexInterop.NewTransform2D(rect.X + rect.Width / 2.0, rect.Y + rect.Height / 2.0, asms));
                } catch (Exception oex) {
                    try { QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Event, "VISION", "Cognex",
                        Id + " Origin 설정 실패(좌표 절대화 불가): " + oex.Message); } catch { }
                }
                pattern.Train();
                _trainSucceeded = true;
            }
            catch (Exception ex)
            {
                // 학습 실패 사유를 로그로 남겨 진단(이전엔 조용히 삼킴). 라이선스/동글, 이미지 포맷, 영역/Origin 등.
                _trainSucceeded = false;
                _pma            = null;
                try { QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Alarm, "VISION", "Cognex",
                    Id + " Cognex 학습(Train) 실패: " + ex.GetType().Name + ": " + ex.Message
                    + " | trainRoi=" + rect.Width + "x" + rect.Height + " imgFmt=" + image.PixelFormat); } catch { }
            }
        }

        public void LoadTrainImage(Bitmap pattern)
        {
            TrainImage?.Dispose();
            // 깊은 복사 — ICloneable.Clone() 의 지연 공유(스트림 해제 후 GDI+ 오류) 회피.
            TrainImage = pattern != null ? new Bitmap(pattern) : null;   // null = 학습 패턴 제거
            _fallback.LoadTrainImage(pattern);   // OpenCV fallback 도 동기화
            if (pattern == null) { _trainSucceeded = false; _pma = null; return; }

            _trainSucceeded = false;
            if (!_be.CognexLoaded) return;
            try
            {
                var asms = _be.LoadedAssemblies;
                var pmaType = CognexInterop.GetType("Cognex.VisionPro.PMAlign.CogPMAlignTool", asms);
                if (pmaType == null) return;
                _pma = Activator.CreateInstance(pmaType);
                dynamic cogImage = CognexInterop.BitmapToICogImage(pattern, asms);
                dynamic region   = CognexInterop.NewRectangle(0, 0, pattern.Width, pattern.Height, asms);
                dynamic patt = _pma.Pattern;
                patt.TrainImage  = cogImage;
                patt.TrainRegion = region;   // 저장 패턴 전체 영역 학습
                // Origin = 패턴(저장 크롭) 중심. 결과 Pose.Translation 이 절대 이미지 좌표가 되도록(Train() 과 parity).
                try {
                    CognexInterop.TrySet(patt, "Origin",
                        CognexInterop.NewTransform2D(pattern.Width / 2.0, pattern.Height / 2.0, asms));
                } catch (Exception oex) {
                    try { QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Event, "VISION", "Cognex",
                        Id + " LoadTrainImage Origin 설정 실패: " + oex.Message); } catch { }
                }
                patt.Train();
                _trainSucceeded = true;
                try { QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Event, "VISION", "Cognex",
                    Id + " Cognex 재학습 OK: pattern " + pattern.Width + "x" + pattern.Height); } catch { }
            }
            catch (Exception ex)
            {
                // 재학습 실패 → Cognex 미사용, fallback(OpenCv) 으로 매칭. 사유를 Log 에 남겨 진단.
                _trainSucceeded = false; _pma = null;
                try { QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Event, "VISION", "Cognex",
                    Id + " LoadTrainImage Cognex 재학습 실패 → fallback: " + ex.GetType().Name + ": " + ex.Message); } catch { }
            }
        }

        public MatchResult Match(Bitmap image)
        {
            // Cognex 학습 성공 시에만 Cognex 사용, 그 외엔 fallback
            if (image == null) return MatchResult.Fail(Id, "no image");
            // OpenCv fallback 으로 매칭 파라미터 동기화(Cognex 미로드/미학습 시 fallback 이 동일 설정으로 매칭).
            // ★ SearchRoi/TrainRoi 도 반드시 동기화 — 레시피 로드는 CognexPatternFinder.SearchRoi 만 채우고
            //    fallback.SearchRoi 는 기본값(400x300)으로 남아, 누락 시 fallback 이 엉뚱한 ROI 로 매칭(재시작 후 검출 실패).
            _fallback.SearchRoi         = SearchRoi;
            _fallback.TrainRoi          = TrainRoi;
            _fallback.AngleEnabled      = AngleEnabled;
            _fallback.AngleToleranceDeg = AngleToleranceDeg;
            _fallback.AngleStepDeg      = AngleStepDeg;
            _fallback.MaxInstances      = MaxInstances;
            _fallback.AcceptThreshold   = AcceptThreshold;
            _fallback.PreferNearestCenter = PreferNearestCenter;
            // Provider=Cognex 선택 시에는 OpenCv 로 대체하지 않고 Cognex 설정/결과 그대로 진행한다.
            //  - VisionPro 미로드: Cognex 자체가 불가 → 명확히 실패(설치/DLL 경로 확인). (예전엔 조용히 OpenCv 전환)
            //  - 미학습(_pma==null): 즉시 실패(티칭 필요) — 전영역 OpenCv 탐색으로 인한 지연/오검 방지.
            if (!_be.CognexLoaded)
            {
                try { QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Alarm, "VISION", "Engine",
                    Id + " MATCH 실패 → Cognex(VisionPro) 미로드. Provider=Cognex 이므로 OpenCv 로 대체하지 않음(설치/경로 확인)."); } catch { }
                return MatchResult.Fail(Id, "Cognex(VisionPro) 미로드 — 설치/DLL 경로 확인");
            }
            if (!_trainSucceeded || _pma == null)
            {
                try { QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Event, "VISION", "Engine",
                    Id + " MATCH 실패 → 미학습(티칭 필요). Provider=Cognex 라 OpenCv fallback 안 함."); } catch { }
                return MatchResult.Fail(Id, "미학습 — 티칭(Train) 필요");
            }

            try
            {
                var asms = _be.LoadedAssemblies;
                dynamic cogImage = CognexInterop.BitmapToICogImage(image, asms);
                _pma.InputImage = cogImage;

                var s = SearchRoi.BoundingBox;
                s.Intersect(new Rectangle(0, 0, image.Width, image.Height));
                dynamic searchRegion = CognexInterop.NewRectangle(s.X, s.Y, s.Width, s.Height, asms);
                _pma.SearchRegion = searchRegion;
                CognexInterop.TrySet(_pma, "RunParams",  _pma.RunParams);  // no-op (속성 접근만)

                // RunParams.AcceptThreshold / NumToFind 설정
                try { _pma.RunParams.AcceptThreshold = AcceptThreshold; } catch { }
                try { _pma.RunParams.ApproximateNumberToFind = PreferNearestCenter ? Math.Max(MaxInstances, 16) : MaxInstances; } catch { }
                // 회전 탐색 — PatMax 는 AngleStart/AngleExtent(라디안). AngleEnabled 시 [-tol,+tol] 탐색.
                try
                {
                    if (AngleEnabled && AngleToleranceDeg > 0.0)
                    {
                        double tolRad = AngleToleranceDeg * Math.PI / 180.0;
                        _pma.RunParams.AngleStart  = -tolRad;
                        _pma.RunParams.AngleExtent = 2.0 * tolRad;
                    }
                    else
                    {
                        _pma.RunParams.AngleStart  = 0.0;
                        _pma.RunParams.AngleExtent = 0.0;
                    }
                }
                catch { }

                _pma.Run();

                var result = new MatchResult { RoiName = Id, Success = true };
                dynamic results = _pma.Results;
                int idx = 0;
                if (results is IEnumerable enumer)
                {
                    foreach (dynamic r in enumer)
                    {
                        // GetPose() / Score / TranslationX / TranslationY / Rotation
                        double cx = 0, cy = 0, ang = 0, score = 0;
                        try
                        {
                            dynamic pose = r.GetPose();
                            cx = (double)pose.TranslationX;
                            cy = (double)pose.TranslationY;
                            ang = (double)pose.Rotation * (180.0 / Math.PI);   // rad → deg
                        }
                        catch
                        {
                            cx = (double)CognexInterop.TryGet(r, "TranslationX", 0.0);
                            cy = (double)CognexInterop.TryGet(r, "TranslationY", 0.0);
                            ang = (double)CognexInterop.TryGet(r, "Rotation", 0.0) * (180.0 / Math.PI);
                        }
                        try { score = (double)r.Score; }
                        catch { score = (double)CognexInterop.TryGet(r, "Score", 0.0); }

                        result.Instances.Add(new MatchInstance
                        {
                            Index    = idx++,
                            CenterX  = cx,
                            CenterY  = cy,
                            AngleDeg = ang,
                            Score    = score
                        });
                    }
                }

                // 센터 최근접: 결과 중 이미지 센터에 가장 가까운 1개만 남긴다(웨이퍼 정렬).
                if (PreferNearestCenter && result.Instances.Count > 1)
                {
                    double ccx = image.Width / 2.0, ccy = image.Height / 2.0;
                    MatchInstance pick = null; double bestD = double.MaxValue;
                    foreach (var m in result.Instances)
                    {
                        double dx = m.CenterX - ccx, dy = m.CenterY - ccy, d = dx * dx + dy * dy;
                        if (d < bestD) { bestD = d; pick = m; }
                    }
                    if (pick != null) { pick.Index = 0; result.Instances.Clear(); result.Instances.Add(pick); }
                }

                if (result.Instances.Count == 0)
                {
                    // 진단: 임계 0.3 으로 재실행해 PatMax 가 후보(점수)를 찾기는 하는지 확인.
                    //   후보=0 이면 학습/이미지 변환 문제, 후보>0·best<thr 이면 임계/패턴 변별 문제.
                    int diagN = 0; double diagBest = 0;
                    try
                    {
                        _pma.RunParams.AcceptThreshold = 0.30;
                        _pma.Run();
                        if (_pma.Results is IEnumerable de)
                            foreach (dynamic dr in de)
                            {
                                diagN++;
                                double sc = 0; try { sc = (double)dr.Score; } catch { }
                                if (sc > diagBest) diagBest = sc;
                            }
                    }
                    catch { }
                    try { QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Event, "VISION", "Engine",
                        Id + " MATCH → Cognex PatMax found=0 (search @" + s.X + "," + s.Y + " " + s.Width + "x" + s.Height +
                        ", thr=" + AcceptThreshold.ToString("F2") + " | 진단 thr0.3: 후보=" + diagN + " best=" + diagBest.ToString("F3") +
                        ") → 미검출(no match), Provider=Cognex 라 fallback 안 함"); } catch { }
                    return result;
                }
                try { QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Event, "VISION", "Engine",
                    Id + " MATCH → Cognex PatMax 사용 (found=" + result.Instances.Count + ")"); } catch { }
                return result;
            }
            catch (Exception ex)
            {
                // 런타임 실패(라이선스/동글 미체결 등) — Provider=Cognex 이므로 OpenCv 로 대체하지 않고 실패 반환.
                try { QMC.Common.Logging.EventLogger.Write(QMC.Common.Logging.EventKind.Alarm, "VISION", "Engine",
                    Id + " MATCH Cognex 런타임 실패: " + ex.Message); } catch { }
                return MatchResult.Fail(Id, "Cognex 실행 실패: " + ex.Message);
            }
        }

    }
}
