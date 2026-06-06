using System;
using System.Runtime.Serialization;

namespace QMC.CDT320.Jobs
{
    /// <summary>Job 상태.</summary>
    [DataContract]
    public enum JobState
    {
        [EnumMember] Ready,
        [EnumMember] Running,
        [EnumMember] Done,
        [EnumMember] Failed,
        [EnumMember] Cancelled,
    }

    /// <summary>Job 종류 (310 의 Job 클래스 군 단순화).</summary>
    [DataContract]
    public enum JobType
    {
        [EnumMember] Pick,
        [EnumMember] Place,
        [EnumMember] Inspect,
        [EnumMember] Align,
        [EnumMember] Scan,
        [EnumMember] MapGenerate,
        [EnumMember] CleanCollet,
        [EnumMember] Custom,
    }

    /// <summary>Job 우선순위.</summary>
    [DataContract]
    public enum JobPriority
    {
        [EnumMember] Low,
        [EnumMember] Normal,
        [EnumMember] High,
        [EnumMember] Critical,
    }

    /// <summary>다이 1개의 작업 단위 (310 의 DieJobOrder 단순화).</summary>
    [DataContract]
    public class JobOrder
    {
        [DataMember] public string      Uid       { get; set; } = Guid.NewGuid().ToString("N").Substring(0, 12);
        [DataMember] public JobType     Type      { get; set; }
        [DataMember] public JobState    State     { get; set; } = JobState.Ready;
        [DataMember] public JobPriority Priority  { get; set; } = JobPriority.Normal;

        /// <summary>대상 다이 Uid (있을 때).</summary>
        [DataMember] public string DieUid     { get; set; } = "";
        /// <summary>대상 TapeFrame ObjId (있을 때).</summary>
        [DataMember] public string FrameObjId { get; set; } = "";

        [DataMember] public DateTime  CreatedAt   { get; set; } = DateTime.Now;
        [DataMember] public DateTime? StartedAt   { get; set; }
        [DataMember] public DateTime? FinishedAt  { get; set; }

        [DataMember] public string ErrorReason   { get; set; } = "";
        [DataMember] public string Note          { get; set; } = "";

        public TimeSpan Duration =>
            (FinishedAt ?? DateTime.Now) - (StartedAt ?? CreatedAt);

        public override string ToString()
            => $"Job[{Uid}] {Type} {State} die={DieUid} frame={FrameObjId} dur={Duration.TotalSeconds:F2}s {ErrorReason}";
    }
}
