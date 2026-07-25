using System;
using System.Runtime.Serialization;

namespace QMC.CDT320.Calibration
{
    /// <summary>콜렛 클리닝 검사 결과.</summary>
    public enum ColletCleaningResult
    {
        None,
        Ok,
        Ng,
        ReplaceRequired,
        Skipped
    }

    /// <summary>자동 콜렛 클리닝 공정 횟수 트리거의 계수 단위.</summary>
    public enum ColletCleaningProcessCountUnit
    {
        Die,
        Wafer
    }

    /// <summary>
    /// 콜렛 클리닝 설정.
    /// 클리닝은 NG Stage의 Bin 다이맵 셀을 눌러 콜렛 하단을 닦아내고, 이후 다이 검사존에서 콜렛 검사를 수행한다.
    /// 수동(ColletCleaningControlDialog)과 자동(트리거) 실행이 동일한 설정을 공유한다.
    /// </summary>
    [DataContract]
    public sealed class ColletCleaningSettings
    {
        public const double DefaultCleanVelocity = 5.0;
        public const double DefaultCleanAcceleration = 50.0;
        public const double DefaultCleanDeceleration = 50.0;
        public const int DefaultArriveDwellMs = 200;
        public const int DefaultCleanPressCount = 3;
        public const double DefaultRepeatLiftHeight = 1.0;
        public const double DefaultMaxExtraPressDepth = 0.5;
        public const int DefaultMaxRetryCount = 2;
        public const int DefaultWaferExchangeInterval = 1;
        public const int DefaultProcessCountInterval = 1000;
        public const int DefaultMoveTimeoutMs = 30000;

        // 대상 선택: Front/Rear × Collet 1~4. 인덱스는 콜렛 번호 - 1.
        [DataMember] public bool[] UseFrontCollet { get; set; } = new[] { true, true, true, true };
        [DataMember] public bool[] UseRearCollet { get; set; } = new[] { true, true, true, true };

        // 클리닝 Z 동작 조건.
        [DataMember] public double CleanVelocity { get; set; } = DefaultCleanVelocity;
        [DataMember] public double CleanAcceleration { get; set; } = DefaultCleanAcceleration;
        [DataMember] public double CleanDeceleration { get; set; } = DefaultCleanDeceleration;

        /// <summary>
        /// 접촉 Z 사용자 보정. 부호 규약(사용자 확정 2026-07-26):
        /// + 값이면 Z축이 상승(덜 누름), - 값이면 Z축이 더 하강(더 누름).
        /// </summary>
        [DataMember] public double ContactZUserOffset { get; set; }

        /// <summary>
        /// 과압 방지 한계. 계산된 접촉 Z보다 추가로 하강할 수 있는 최대량(mm, 양수).
        /// ContactZUserOffset이 음수로 이 값을 넘어가면 실행을 차단한다.
        /// </summary>
        [DataMember] public double MaxExtraPressDepth { get; set; } = DefaultMaxExtraPressDepth;

        [DataMember] public int ArriveDwellMs { get; set; } = DefaultArriveDwellMs;
        [DataMember] public int CleanPressCount { get; set; } = DefaultCleanPressCount;

        /// <summary>누름 반복 사이에 다시 올라가는 높이(mm, 양수).</summary>
        [DataMember] public double RepeatLiftHeight { get; set; } = DefaultRepeatLiftHeight;

        [DataMember] public int MoveTimeoutMs { get; set; } = DefaultMoveTimeoutMs;

        // 티칭 위치에서 제외할 높이 성분.
        // Place 티칭 Z는 다이/림/필름 높이만큼 이미 올라가 있으므로, 클리닝 접촉 Z는 이 값들을 빼서 더 내려간다.
        [DataMember] public double DieHeight { get; set; }
        [DataMember] public double RimHeight { get; set; }
        [DataMember] public double FilmHeight { get; set; }

        /// <summary>검사 NG 시 클린 -> 검사를 다시 반복할 최대 횟수. 소진되면 콜렛 교체 알람.</summary>
        [DataMember] public int MaxRetryCount { get; set; } = DefaultMaxRetryCount;

        /// <summary>
        /// true이면 클리닝에 사용한 다이맵 셀에도 생산 NG die 배치를 허용한다.
        /// false이면 해당 셀을 배치 대상에서 제외한다.
        /// </summary>
        [DataMember] public bool AllowPlaceOnCleanedCell { get; set; } = true;

        /// <summary>콜렛 교체 알람 발생 시 해당 픽커만 생산에서 제외하고 나머지로 계속 운전할지 여부.</summary>
        [DataMember] public bool DisablePickerOnReplaceAlarm { get; set; }

        // 자동 실행 트리거. 각 조건은 독립적으로 사용 유/무를 가진다.
        [DataMember] public bool UseTriggerOnWaferExchange { get; set; }
        [DataMember] public int WaferExchangeInterval { get; set; } = DefaultWaferExchangeInterval;

        [DataMember] public bool UseTriggerOnProcessCount { get; set; }
        [DataMember] public int ProcessCountInterval { get; set; } = DefaultProcessCountInterval;
        [DataMember] public ColletCleaningProcessCountUnit ProcessCountUnit { get; set; } = ColletCleaningProcessCountUnit.Die;

        [DataMember] public bool UseTriggerOnAutoStart { get; set; }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext ctx)
        {
            EnsureObjects();
        }

        public void EnsureObjects()
        {
            UseFrontCollet = EnsureColletFlags(UseFrontCollet);
            UseRearCollet = EnsureColletFlags(UseRearCollet);

            if (CleanVelocity <= 0.0 || double.IsNaN(CleanVelocity))
                CleanVelocity = DefaultCleanVelocity;
            if (CleanAcceleration <= 0.0 || double.IsNaN(CleanAcceleration))
                CleanAcceleration = DefaultCleanAcceleration;
            if (CleanDeceleration <= 0.0 || double.IsNaN(CleanDeceleration))
                CleanDeceleration = DefaultCleanDeceleration;
            if (double.IsNaN(ContactZUserOffset))
                ContactZUserOffset = 0.0;
            if (MaxExtraPressDepth < 0.0 || double.IsNaN(MaxExtraPressDepth))
                MaxExtraPressDepth = DefaultMaxExtraPressDepth;
            if (ArriveDwellMs < 0)
                ArriveDwellMs = DefaultArriveDwellMs;
            if (CleanPressCount <= 0)
                CleanPressCount = DefaultCleanPressCount;
            if (RepeatLiftHeight <= 0.0 || double.IsNaN(RepeatLiftHeight))
                RepeatLiftHeight = DefaultRepeatLiftHeight;
            if (MoveTimeoutMs <= 0)
                MoveTimeoutMs = DefaultMoveTimeoutMs;
            if (double.IsNaN(DieHeight))
                DieHeight = 0.0;
            if (double.IsNaN(RimHeight))
                RimHeight = 0.0;
            if (double.IsNaN(FilmHeight))
                FilmHeight = 0.0;
            if (MaxRetryCount < 0)
                MaxRetryCount = DefaultMaxRetryCount;
            if (WaferExchangeInterval <= 0)
                WaferExchangeInterval = DefaultWaferExchangeInterval;
            if (ProcessCountInterval <= 0)
                ProcessCountInterval = DefaultProcessCountInterval;
        }

        private static bool[] EnsureColletFlags(bool[] source)
        {
            var result = new bool[4];
            if (source == null)
            {
                for (int i = 0; i < result.Length; i++)
                    result[i] = true;
                return result;
            }

            for (int i = 0; i < result.Length; i++)
                result[i] = i < source.Length && source[i];
            return result;
        }

        /// <summary>colletNo(1~4) 선택 여부.</summary>
        public bool IsColletSelected(VisionFocusPickerSide side, int colletNo)
        {
            EnsureObjects();
            int index = colletNo - 1;
            if (index < 0 || index > 3)
                return false;

            return side == VisionFocusPickerSide.Front ? UseFrontCollet[index] : UseRearCollet[index];
        }

        public void SetColletSelected(VisionFocusPickerSide side, int colletNo, bool selected)
        {
            EnsureObjects();
            int index = colletNo - 1;
            if (index < 0 || index > 3)
                return;

            if (side == VisionFocusPickerSide.Front)
                UseFrontCollet[index] = selected;
            else
                UseRearCollet[index] = selected;
        }

        public bool HasAnySelection()
        {
            EnsureObjects();
            for (int i = 0; i < 4; i++)
            {
                if (UseFrontCollet[i] || UseRearCollet[i])
                    return true;
            }

            return false;
        }

        public ColletCleaningSettings Clone()
        {
            try
            {
                EnsureObjects();
                return new ColletCleaningSettings
                {
                    UseFrontCollet = (bool[])UseFrontCollet.Clone(),
                    UseRearCollet = (bool[])UseRearCollet.Clone(),
                    CleanVelocity = CleanVelocity,
                    CleanAcceleration = CleanAcceleration,
                    CleanDeceleration = CleanDeceleration,
                    ContactZUserOffset = ContactZUserOffset,
                    MaxExtraPressDepth = MaxExtraPressDepth,
                    ArriveDwellMs = ArriveDwellMs,
                    CleanPressCount = CleanPressCount,
                    RepeatLiftHeight = RepeatLiftHeight,
                    MoveTimeoutMs = MoveTimeoutMs,
                    DieHeight = DieHeight,
                    RimHeight = RimHeight,
                    FilmHeight = FilmHeight,
                    MaxRetryCount = MaxRetryCount,
                    AllowPlaceOnCleanedCell = AllowPlaceOnCleanedCell,
                    DisablePickerOnReplaceAlarm = DisablePickerOnReplaceAlarm,
                    UseTriggerOnWaferExchange = UseTriggerOnWaferExchange,
                    WaferExchangeInterval = WaferExchangeInterval,
                    UseTriggerOnProcessCount = UseTriggerOnProcessCount,
                    ProcessCountInterval = ProcessCountInterval,
                    ProcessCountUnit = ProcessCountUnit,
                    UseTriggerOnAutoStart = UseTriggerOnAutoStart
                };
            }
            catch
            {
                return new ColletCleaningSettings();
            }
            finally
            {
            }
        }
    }

    /// <summary>픽커 1개(콜렛 1개)의 클리닝 이력.</summary>
    [DataContract]
    public sealed class ColletCleaningHistoryRecord
    {
        private static readonly DateTime SafeUnsetDateTime = new DateTime(2000, 1, 1);

        [DataMember] public DateTime LastCleanedAt { get; set; }
        [DataMember] public int TotalCleanCount { get; set; }
        [DataMember] public ColletCleaningResult LastResult { get; set; } = ColletCleaningResult.None;
        [DataMember] public int LastRetryUsed { get; set; }
        [DataMember] public string LastMessage { get; set; } = string.Empty;

        public bool HasHistory
        {
            get { return TotalCleanCount > 0 && LastCleanedAt > SafeUnsetDateTime; }
        }

        public void EnsureObjects()
        {
            if (TotalCleanCount < 0)
                TotalCleanCount = 0;
            if (LastRetryUsed < 0)
                LastRetryUsed = 0;
            if (LastMessage == null)
                LastMessage = string.Empty;
            if (LastCleanedAt <= DateTime.MinValue.AddDays(1) || LastCleanedAt >= DateTime.MaxValue.AddDays(-1))
                LastCleanedAt = SafeUnsetDateTime;
        }

        public ColletCleaningHistoryRecord Clone()
        {
            return new ColletCleaningHistoryRecord
            {
                LastCleanedAt = LastCleanedAt,
                TotalCleanCount = TotalCleanCount,
                LastResult = LastResult,
                LastRetryUsed = LastRetryUsed,
                LastMessage = LastMessage
            };
        }
    }

    /// <summary>Front/Rear × Collet 1~4 클리닝 이력 모음. 작업 정보 픽커 페이지에 표시한다.</summary>
    [DataContract]
    public sealed class ColletCleaningHistory
    {
        [DataMember] public ColletCleaningHistoryRecord[] Front { get; set; }
        [DataMember] public ColletCleaningHistoryRecord[] Rear { get; set; }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext ctx)
        {
            EnsureObjects();
        }

        public void EnsureObjects()
        {
            Front = EnsureRecords(Front);
            Rear = EnsureRecords(Rear);
        }

        private static ColletCleaningHistoryRecord[] EnsureRecords(ColletCleaningHistoryRecord[] source)
        {
            var result = new ColletCleaningHistoryRecord[4];
            for (int i = 0; i < result.Length; i++)
            {
                ColletCleaningHistoryRecord record = source != null && i < source.Length ? source[i] : null;
                if (record == null)
                    record = new ColletCleaningHistoryRecord();
                record.EnsureObjects();
                result[i] = record;
            }

            return result;
        }

        /// <summary>colletNo(1~4) 이력을 가져온다. 범위를 벗어나면 null.</summary>
        public ColletCleaningHistoryRecord Get(VisionFocusPickerSide side, int colletNo)
        {
            EnsureObjects();
            int index = colletNo - 1;
            if (index < 0 || index > 3)
                return null;

            return side == VisionFocusPickerSide.Front ? Front[index] : Rear[index];
        }

        public void Update(
            VisionFocusPickerSide side,
            int colletNo,
            ColletCleaningResult result,
            int retryUsed,
            string message)
        {
            ColletCleaningHistoryRecord record = Get(side, colletNo);
            if (record == null)
                return;

            record.LastCleanedAt = DateTime.Now;
            record.TotalCleanCount = record.TotalCleanCount + 1;
            record.LastResult = result;
            record.LastRetryUsed = retryUsed;
            record.LastMessage = message ?? string.Empty;
        }
    }
}
