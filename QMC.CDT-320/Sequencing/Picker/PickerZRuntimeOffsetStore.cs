using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using QMC.Common.Data.Store;

namespace QMC.CDT320.Sequencing
{
    /// <summary>
    /// Picker Z 런타임 오프셋 필터 상태 영속화 문서.
    /// 사용 유무(UsePickerZRuntimeOffset), fc(cutoff), 한계 2종(Z 단채널)과
    /// (side, pickerNo) 8세트의 Z 필터값, 마지막 갱신 시각을 저장한다.
    /// </summary>
    [DataContract]
    internal sealed class PickerZRuntimeOffsetDocument
    {
        // 기본값(팀장님 확정 2026-08-16): fc 0.1, 이상치 0.5mm,
        // 클램프 ±0.3mm — Z는 충돌 리스크가 있어 XY(±0.5)보다 타이트하게. Enable 기본 OFF.
        // 구버전 파일(필드 부재 → 0)은 Normalize가 기본값으로 복원한다.
        public const double DefaultOutlierLimitMm = 0.5;
        public const double DefaultClampLimitMm = 0.3;

        [DataMember(Order = 0)] public bool UsePickerZRuntimeOffset { get; set; }
        [DataMember(Order = 1)] public double CutoffFrequency { get; set; }
        [DataMember(Order = 2)] public double OutlierLimitMm { get; set; }
        [DataMember(Order = 3)] public double ClampLimitMm { get; set; }
        [DataMember(Order = 4)] public List<PickerZRuntimeOffsetRow> Filters { get; set; }

        public PickerZRuntimeOffsetDocument()
        {
            UsePickerZRuntimeOffset = false;
            CutoffFrequency = 0.1;
            OutlierLimitMm = DefaultOutlierLimitMm;
            ClampLimitMm = DefaultClampLimitMm;
            Filters = new List<PickerZRuntimeOffsetRow>();
        }
    }

    /// <summary>(side, pickerNo) 1세트의 Z 필터 상태 행.</summary>
    [DataContract]
    internal sealed class PickerZRuntimeOffsetRow
    {
        [DataMember(Order = 0)] public string Side { get; set; }
        [DataMember(Order = 1)] public int PickerNo { get; set; }
        [DataMember(Order = 2)] public double FilteredZ { get; set; }
        [DataMember(Order = 3)] public string LastUpdated { get; set; }
    }

    /// <summary>
    /// (Side, PickerNo) 1행의 Z 필터 현재값 스냅샷 — 표시 전용.
    /// Z의 영구 보정은 BottomToPickMm 레시피 영역이라 기구 이관 대상이 아니다.
    /// </summary>
    internal sealed class PickerZRuntimeOffsetSnapshot
    {
        public PickerZRuntimeOffsetSnapshot(
            PickerSequenceSide side,
            int pickerNo,
            double z,
            DateTime lastUpdated)
        {
            Side = side;
            PickerNo = pickerNo;
            Z = z;
            LastUpdated = lastUpdated;
        }

        public PickerSequenceSide Side { get; private set; }
        public int PickerNo { get; private set; }
        public double Z { get; private set; }
        public DateTime LastUpdated { get; private set; }

        public bool HasSample { get { return LastUpdated != DateTime.MinValue; } }
    }

    /// <summary>
    /// Picker Z 런타임 오프셋 JSON 스토어.
    /// 기존 설정 스토어 패턴(Config 디렉터리, DataContractJson 로드, JsonPrettySerializer 저장,
    /// 실패 시 기본값/로그 관례)을 따른다.
    /// </summary>
    internal static class PickerZRuntimeOffsetStore
    {
        public static string Path_
        {
            get
            {
                return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "pickerz_runtime_offset.json");
            }
        }

        /// <summary>저장 파일을 로드한다. 없거나 실패하면 기본값(Disable, fc=0.1, 전부 0)을 반환한다.</summary>
        public static PickerZRuntimeOffsetDocument Load()
        {
            try
            {
                if (!File.Exists(Path_))
                    return new PickerZRuntimeOffsetDocument();

                PickerZRuntimeOffsetDocument document;
                using (FileStream fs = File.OpenRead(Path_))
                {
                    var serializer = new DataContractJsonSerializer(typeof(PickerZRuntimeOffsetDocument));
                    document = serializer.ReadObject(fs) as PickerZRuntimeOffsetDocument;
                }

                return Normalize(document);
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "PickerZRuntimeOffsetStore",
                    "PickerZ 런타임 오프셋 저장 파일 로드에 실패해 기본값을 사용합니다. error=" + ex.Message + " - Failed");
                return new PickerZRuntimeOffsetDocument();
            }
            finally
            {
            }
        }

        /// <summary>필터 상태를 저장한다. 실패해도 예외를 전파하지 않고 로그만 남긴다.</summary>
        public static void Save(PickerZRuntimeOffsetDocument document)
        {
            try
            {
                document = Normalize(document);
                string dir = Path.GetDirectoryName(Path_);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                using (FileStream fs = File.Create(Path_))
                {
                    JsonPrettySerializer.WriteObject(fs, typeof(PickerZRuntimeOffsetDocument), document);
                }
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "PickerZRuntimeOffsetStore",
                    "PickerZ 런타임 오프셋 저장에 실패했습니다. error=" + ex.Message + " - Failed");
            }
            finally
            {
            }
        }

        private static PickerZRuntimeOffsetDocument Normalize(PickerZRuntimeOffsetDocument document)
        {
            if (document == null)
                document = new PickerZRuntimeOffsetDocument();
            if (document.CutoffFrequency <= 0.0)
                document.CutoffFrequency = 0.1;
            document.OutlierLimitMm = NormalizeLimit(
                document.OutlierLimitMm, PickerZRuntimeOffsetDocument.DefaultOutlierLimitMm);
            document.ClampLimitMm = NormalizeLimit(
                document.ClampLimitMm, PickerZRuntimeOffsetDocument.DefaultClampLimitMm);
            if (document.Filters == null)
                document.Filters = new List<PickerZRuntimeOffsetRow>();
            return document;
        }

        // 구버전 파일에는 한계 필드가 없어 0으로 로드된다 — 0 이하·NaN은 기본값으로 복원한다.
        private static double NormalizeLimit(double value, double fallback)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0.0)
                return fallback;
            return value;
        }
    }
}
