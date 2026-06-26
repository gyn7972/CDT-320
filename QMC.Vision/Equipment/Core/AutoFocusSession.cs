using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace QMC.Vision.Core
{
    /// <summary>오토포커스 대상 카메라.</summary>
    public enum FocusCamera
    {
        Bottom,
        Front,
        Back,
    }

    /// <summary>오토포커스 측정 타깃. Bottom 카메라는 Collet/Die, Front/Back 은 Side.</summary>
    public enum FocusTarget
    {
        Collet,
        Die,
        Side,
    }

    /// <summary>스캔 1점: 핸들러에서 받은 모터 Z(=그래프 X) 와 측정 Score(=그래프 Y).</summary>
    public sealed class FocusSample
    {
        public double MotorZ { get; set; }
        public double Score { get; set; }
        /// <summary>최초 Value 여부(요구사항: 최초 Value 는 점으로 표시).</summary>
        public bool IsInitial { get; set; }

        public FocusSample(double motorZ, double score, bool isInitial = false)
        {
            MotorZ = motorZ;
            Score = score;
            IsInitial = isInitial;
        }
    }

    /// <summary>
    /// 한 픽업(Pickup1~4) 또는 측면 1채널의 포커스 곡선.
    /// (motorZ, score) 점들과 best, 최초값, 표시 색을 보관.
    /// </summary>
    public sealed class FocusSeries
    {
        /// <summary>Pickup 번호(1~4). 측면(Side)은 0.</summary>
        public int PickupNo { get; private set; }

        /// <summary>그래프 색(요구사항: 빨·노·파·녹 = Pickup1~4).</summary>
        public Color Color { get; private set; }

        public List<FocusSample> Samples { get; } = new List<FocusSample>();

        public FocusSeries(int pickupNo, Color color)
        {
            PickupNo = pickupNo;
            Color = color;
        }

        /// <summary>최초 Value(점 표시 대상). 핸들러가 init 으로 지정한 샘플을 우선,
        /// 지정이 없으면 첫 샘플로 폴백. 없으면 null.</summary>
        public FocusSample Initial
        {
            get { return Samples.FirstOrDefault(s => s.IsInitial) ?? Samples.FirstOrDefault(); }
        }

        /// <summary>Best(최대 Score) 샘플. 없으면 null.</summary>
        public FocusSample Best
        {
            get
            {
                if (Samples.Count == 0) return null;
                FocusSample best = Samples[0];
                for (int i = 1; i < Samples.Count; i++)
                    if (Samples[i].Score > best.Score) best = Samples[i];
                return best;
            }
        }

        public double BestMotorZ { get { var b = Best; return b != null ? b.MotorZ : 0.0; } }
        public double BestScore { get { var b = Best; return b != null ? b.Score : 0.0; } }

        public void Add(double motorZ, double score, bool isInitial = false)
        {
            // (A)안: 핸들러가 init 으로 지정한 샘플을 최초값(점)으로 표시한다.
            // 지정이 없을 때의 점 표시는 Initial 프로퍼티가 첫 샘플로 폴백 처리.
            Samples.Add(new FocusSample(motorZ, score, isInitial));
        }

        public void Clear()
        {
            Samples.Clear();
        }
    }

    /// <summary>
    /// 한 카메라/타깃의 오토포커스 세션. Bottom-Collet / Bottom-Die 는 Pickup1~4 의 4개 시리즈,
    /// Front/Back-Side 는 1개 시리즈를 가진다.
    /// <para>
    /// 핸들러가 FOCUS_VAL 로 모터 Z 를 보낼 때마다 <see cref="AddSample"/> 로 누적되고,
    /// UI 는 BEST 표(<see cref="Series"/> 의 Best)와 4색 그래프(각 Series 의 Samples)를 그린다.
    /// </para>
    /// </summary>
    public sealed class AutoFocusSession
    {
        /// <summary>Pickup1~4 색: 빨·노·파·녹 (요구사항).</summary>
        public static readonly Color[] PickupColors =
        {
            Color.Red,        // Pickup1
            Color.Gold,       // Pickup2 (노랑 — 가독성 위해 Gold)
            Color.RoyalBlue,  // Pickup3
            Color.ForestGreen // Pickup4
        };

        public const int MaxPickup = 4;

        public FocusCamera Camera { get; private set; }
        public FocusTarget Target { get; private set; }

        /// <summary>Pickup 별(또는 Side 단일) 시리즈.</summary>
        public IReadOnlyList<FocusSeries> Series { get { return _series; } }

        private readonly List<FocusSeries> _series = new List<FocusSeries>();
        private readonly Dictionary<int, FocusSeries> _byPickup = new Dictionary<int, FocusSeries>();
        private readonly object _lock = new object();

        public AutoFocusSession(FocusCamera camera, FocusTarget target)
        {
            Camera = camera;
            Target = target;

            // 모든 카메라/타깃(콜렛·다이·앞측면·뒤측면) → Pickup1~4, 빨·노·파·녹.
            for (int i = 0; i < MaxPickup; i++)
            {
                int pickupNo = i + 1;
                FocusSeries s = new FocusSeries(pickupNo, PickupColors[i]);
                _series.Add(s);
                _byPickup[pickupNo] = s;
            }
        }

        /// <summary>
        /// 핸들러에서 수신한 (모터Z, Score) 1점 누적.
        /// </summary>
        /// <param name="pickupNo">Pickup1~4. 측면이면 0.</param>
        /// <param name="motorZ">핸들러가 TCP/IP 로 보낸 모터 Z(그래프 X).</param>
        /// <param name="score">AutoFocusCore.Score 결과(그래프 Y).</param>
        /// <param name="isInitial">최초 Value 여부(점 표시).</param>
        public void AddSample(int pickupNo, double motorZ, double score, bool isInitial = false)
        {
            lock (_lock)
            {
                FocusSeries s;
                if (!_byPickup.TryGetValue(pickupNo, out s))
                    return;
                s.Add(motorZ, score, isInitial);
            }
        }

        /// <summary>지정 Pickup 시리즈. 없으면 null.</summary>
        public FocusSeries GetSeries(int pickupNo)
        {
            lock (_lock)
            {
                FocusSeries s;
                return _byPickup.TryGetValue(pickupNo, out s) ? s : null;
            }
        }

        /// <summary>
        /// 지정 Pickup 의 샘플 복사본(스레드 안전). UI 가 TCP 누적과 동시에 곡선을 그릴 때 사용.
        /// _series 구조는 생성 후 불변이므로 시리즈 메타(PickupNo/Color)는 직접 읽고,
        /// 변하는 Samples 만 이 메서드로 락 하에 복사한다.
        /// </summary>
        public List<FocusSample> CopySamples(int pickupNo)
        {
            lock (_lock)
            {
                FocusSeries s;
                if (!_byPickup.TryGetValue(pickupNo, out s))
                    return new List<FocusSample>();
                return s.Samples.Select(x => new FocusSample(x.MotorZ, x.Score, x.IsInitial)).ToList();
            }
        }

        /// <summary>전체 또는 지정 Pickup 초기화.</summary>
        public void Reset(int pickupNo = -1)
        {
            lock (_lock)
            {
                if (pickupNo < 0)
                {
                    foreach (FocusSeries s in _series) s.Clear();
                    return;
                }
                FocusSeries one;
                if (_byPickup.TryGetValue(pickupNo, out one)) one.Clear();
            }
        }

        /// <summary>BEST 표 행 데이터. UI 바인딩용 스냅샷.</summary>
        public List<FocusBestRow> BuildBestTable()
        {
            lock (_lock)
            {
                return _series.Select(s => new FocusBestRow
                {
                    PickupNo = s.PickupNo,
                    Color = s.Color,
                    BestMotorZ = s.BestMotorZ,
                    BestScore = s.BestScore,
                    InitialMotorZ = s.Initial != null ? s.Initial.MotorZ : (double?)null,
                    InitialScore = s.Initial != null ? s.Initial.Score : (double?)null,
                    SampleCount = s.Samples.Count
                }).ToList();
            }
        }
    }

    /// <summary>BEST 표 1행.</summary>
    public sealed class FocusBestRow
    {
        public int PickupNo { get; set; }
        public Color Color { get; set; }
        public double BestMotorZ { get; set; }
        public double BestScore { get; set; }
        public double? InitialMotorZ { get; set; }
        public double? InitialScore { get; set; }
        public int SampleCount { get; set; }
    }
}
