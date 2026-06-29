using System;
using System.Collections.Generic;
using System.Diagnostics;
using QMC.Vision.Config;

namespace QMC.Vision.Core
{
    /// <summary>
    /// 오토포커스 ROI(카메라×타깃별 4개) 셋업값 접근 계층.
    /// <para>
    /// 데이터 SSOT 는 <see cref="VisionSettings.AutoFocusRois"/>(vision.json). 비전 [설정 &gt; 오토 포커스]
    /// 에서만 드래그로 지정하며, 변경 시 <see cref="VisionConfigStore.Save"/> 로 영속화한다.
    /// (세션 데이터의 <see cref="AutoFocusStore"/> 와 동일한 정적 스토어 패턴.)
    /// </para>
    /// </summary>
    public static class AutoFocusRoiStore
    {
        /// <summary>한 (카메라,타깃)당 ROI 개수.</summary>
        public const int RoiCount = 4;

        private static readonly object _lock = new object();

        // ── Public Methods ──

        /// <summary>지정 (카메라,타깃)의 ROI1~4 사본. 길이 <see cref="RoiCount"/> 고정, 미설정 항목은 null.</summary>
        public static Roi[] GetRois(FocusCamera camera, FocusTarget target)
        {
            Roi[] result = new Roi[RoiCount];
            try
            {
                lock (_lock)
                {
                    AutoFocusRoiSet set = FindSet(camera, target, false);
                    if (set != null && set.Rois != null)
                        for (int i = 0; i < RoiCount && i < set.Rois.Length; i++)
                            result[i] = set.Rois[i] != null ? set.Rois[i].Clone() : null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[AutoFocusRoiStore] GetRois 실패: " + ex.Message);
            }
            return result;
        }

        /// <summary>지정 인덱스(0~3) ROI 사본. 없으면 null.</summary>
        public static Roi GetRoi(FocusCamera camera, FocusTarget target, int index)
        {
            if (index < 0 || index >= RoiCount) return null;
            Roi[] rois = GetRois(camera, target);
            return rois[index];
        }

        /// <summary>지정 인덱스(0~3) ROI 설정 + 즉시 저장. 성공 시 true.</summary>
        public static bool SetRoi(FocusCamera camera, FocusTarget target, int index, Roi roi)
        {
            if (index < 0 || index >= RoiCount) return false;
            try
            {
                lock (_lock)
                {
                    AutoFocusRoiSet set = FindSet(camera, target, true);
                    if (set.Rois == null || set.Rois.Length < RoiCount)
                        set.Rois = NormalizeArray(set.Rois);
                    set.Rois[index] = roi != null ? roi.Clone() : null;
                    if (set.Rois[index] != null) set.Rois[index].Name = "ROI" + (index + 1);
                }
                VisionConfigStore.Save();
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[AutoFocusRoiStore] SetRoi 실패: " + ex.Message);
                return false;
            }
        }

        /// <summary>지정 (카메라,타깃)의 ROI 전체 초기화 + 즉시 저장. 성공 시 true.</summary>
        public static bool ClearRois(FocusCamera camera, FocusTarget target)
        {
            try
            {
                lock (_lock)
                {
                    AutoFocusRoiSet set = FindSet(camera, target, false);
                    if (set != null) set.Rois = new Roi[RoiCount];
                }
                VisionConfigStore.Save();
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[AutoFocusRoiStore] ClearRois 실패: " + ex.Message);
                return false;
            }
        }

        // ── Private Methods ──

        /// <summary>(카메라,타깃) 세트 조회. <paramref name="create"/> 면 없을 때 생성해 리스트에 추가.</summary>
        private static AutoFocusRoiSet FindSet(FocusCamera camera, FocusTarget target, bool create)
        {
            VisionSettings cfg = VisionConfigStore.Current;
            if (cfg.AutoFocusRois == null) cfg.AutoFocusRois = new List<AutoFocusRoiSet>();

            string camKey = CameraKey(camera);
            string tgtKey = TargetKey(target);
            foreach (AutoFocusRoiSet s in cfg.AutoFocusRois)
                if (string.Equals(s.Camera, camKey, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(s.Target, tgtKey, StringComparison.OrdinalIgnoreCase))
                    return s;

            if (!create) return null;
            AutoFocusRoiSet created = new AutoFocusRoiSet { Camera = camKey, Target = tgtKey, Rois = new Roi[RoiCount] };
            cfg.AutoFocusRois.Add(created);
            return created;
        }

        /// <summary>길이 <see cref="RoiCount"/> 배열로 정규화(기존 값 보존).</summary>
        private static Roi[] NormalizeArray(Roi[] src)
        {
            Roi[] dst = new Roi[RoiCount];
            if (src != null)
                for (int i = 0; i < RoiCount && i < src.Length; i++) dst[i] = src[i];
            return dst;
        }

        private static string CameraKey(FocusCamera camera)
        {
            return camera.ToString().ToUpperInvariant();
        }

        private static string TargetKey(FocusTarget target)
        {
            return target.ToString().ToUpperInvariant();
        }
    }
}
