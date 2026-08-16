using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using QMC.Common.Data.Store;

namespace QMC.CDT320.Sequencing
{
    /// <summary>
    /// Place 런타임 오프셋 필터 상태 영속화 문서.
    /// 사용 유무(UsePlaceRuntimeOffset), fc(cutoff)와 (side, pickerNo) 8세트의 X/Y/T 필터값,
    /// 마지막 갱신 시각을 저장한다.
    /// </summary>
    [DataContract]
    internal sealed class PlaceRuntimeOffsetDocument
    {
        // 필터 한계 기본값 — 구버전 파일(필드 부재 → 0)은 Normalize가 이 값으로 복원한다.
        public const double DefaultOutlierLimitXyMm = 1.0;
        public const double DefaultOutlierLimitTDeg = 1.0;
        public const double DefaultClampLimitXyMm = 0.5;
        public const double DefaultClampLimitTDeg = 1.0;

        [DataMember(Order = 0)] public bool UsePlaceRuntimeOffset { get; set; }
        [DataMember(Order = 1)] public double CutoffFrequency { get; set; }
        [DataMember(Order = 2)] public double OutlierLimitXyMm { get; set; }
        [DataMember(Order = 3)] public double OutlierLimitTDeg { get; set; }
        [DataMember(Order = 4)] public double ClampLimitXyMm { get; set; }
        [DataMember(Order = 5)] public double ClampLimitTDeg { get; set; }
        [DataMember(Order = 6)] public List<PlaceRuntimeOffsetRow> Filters { get; set; }

        public PlaceRuntimeOffsetDocument()
        {
            UsePlaceRuntimeOffset = false;
            CutoffFrequency = 0.1;
            OutlierLimitXyMm = DefaultOutlierLimitXyMm;
            OutlierLimitTDeg = DefaultOutlierLimitTDeg;
            ClampLimitXyMm = DefaultClampLimitXyMm;
            ClampLimitTDeg = DefaultClampLimitTDeg;
            Filters = new List<PlaceRuntimeOffsetRow>();
        }
    }

    /// <summary>(side, pickerNo) 1세트의 필터 상태 행.</summary>
    [DataContract]
    internal sealed class PlaceRuntimeOffsetRow
    {
        [DataMember(Order = 0)] public string Side { get; set; }
        [DataMember(Order = 1)] public int PickerNo { get; set; }
        [DataMember(Order = 2)] public double FilteredX { get; set; }
        [DataMember(Order = 3)] public double FilteredY { get; set; }
        [DataMember(Order = 4)] public double FilteredT { get; set; }
        [DataMember(Order = 5)] public string LastUpdated { get; set; }
    }

    /// <summary>
    /// Place 런타임 오프셋 JSON 스토어.
    /// 기존 설정 스토어 패턴(Config 디렉터리, DataContractJson 로드, JsonPrettySerializer 저장,
    /// 실패 시 기본값/로그 관례)을 따른다.
    /// </summary>
    internal static class PlaceRuntimeOffsetStore
    {
        public static string Path_
        {
            get
            {
                return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "place_runtime_offset.json");
            }
        }

        /// <summary>저장 파일을 로드한다. 없거나 실패하면 기본값(fc=0.1, 전부 0)을 반환한다.</summary>
        public static PlaceRuntimeOffsetDocument Load()
        {
            try
            {
                if (!File.Exists(Path_))
                    return new PlaceRuntimeOffsetDocument();

                PlaceRuntimeOffsetDocument document;
                using (FileStream fs = File.OpenRead(Path_))
                {
                    var serializer = new DataContractJsonSerializer(typeof(PlaceRuntimeOffsetDocument));
                    document = serializer.ReadObject(fs) as PlaceRuntimeOffsetDocument;
                }

                return Normalize(document);
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "PlaceRuntimeOffsetStore",
                    "Place 런타임 오프셋 저장 파일 로드에 실패해 기본값을 사용합니다. error=" + ex.Message + " - Failed");
                return new PlaceRuntimeOffsetDocument();
            }
            finally
            {
            }
        }

        /// <summary>필터 상태를 저장한다. 실패해도 예외를 전파하지 않고 로그와 실패 결과를 반환한다.</summary>
        public static bool Save(PlaceRuntimeOffsetDocument document)
        {
            try
            {
                document = Normalize(document);
                string dir = Path.GetDirectoryName(Path_);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                using (FileStream fs = File.Create(Path_))
                {
                    JsonPrettySerializer.WriteObject(fs, typeof(PlaceRuntimeOffsetDocument), document);
                }
                return true;
            }
            catch (Exception ex)
            {
                QMC.Common.Log.Write("Main", "SYSTEM", "PlaceRuntimeOffsetStore",
                    "Place 런타임 오프셋 저장에 실패했습니다. error=" + ex.Message + " - Failed");
                return false;
            }
            finally
            {
            }
        }

        private static PlaceRuntimeOffsetDocument Normalize(PlaceRuntimeOffsetDocument document)
        {
            if (document == null)
                document = new PlaceRuntimeOffsetDocument();
            if (document.CutoffFrequency <= 0.0)
                document.CutoffFrequency = 0.1;
            document.OutlierLimitXyMm = NormalizeLimit(
                document.OutlierLimitXyMm, PlaceRuntimeOffsetDocument.DefaultOutlierLimitXyMm);
            document.OutlierLimitTDeg = NormalizeLimit(
                document.OutlierLimitTDeg, PlaceRuntimeOffsetDocument.DefaultOutlierLimitTDeg);
            document.ClampLimitXyMm = NormalizeLimit(
                document.ClampLimitXyMm, PlaceRuntimeOffsetDocument.DefaultClampLimitXyMm);
            document.ClampLimitTDeg = NormalizeLimit(
                document.ClampLimitTDeg, PlaceRuntimeOffsetDocument.DefaultClampLimitTDeg);
            if (document.Filters == null)
                document.Filters = new List<PlaceRuntimeOffsetRow>();
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
