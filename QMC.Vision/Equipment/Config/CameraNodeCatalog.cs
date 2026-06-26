using System;
using System.Collections.Generic;
using System.Linq;
using QMC.Vision.Core;

namespace QMC.Vision.Config
{
    /// <summary>
    /// 카메라 제네릭 노드 파라미터 "테이블" 정의.
    /// MVS(Hikrobot) Feature Tree 에서 노출되는 GenICam 노드를 한 줄(<see cref="CameraNodeDef"/>)씩 정의한다.
    /// <para>
    /// 새 MVS 기능을 설정 화면에 추가하려면 <see cref="All"/> 에 행 1개만 추가하면 된다.
    /// (UI 그리드 자동 생성 + Binder 자동 적용. 코드 수정 불필요.)
    /// </para>
    /// <para>
    /// 주의: Exposure / Gain / FrameRate / TriggerMode / TriggerSource / PixelFormat / ROI 는
    /// 기존 고정 항목(<see cref="AlgorithmCameraBinder"/>)에서 이미 처리하므로 여기서 중복 정의하지 않는다.
    /// </para>
    /// </summary>
    public sealed class CameraNodeDef
    {
        /// <summary>GenICam 노드명(SDK SetXxxValue 키). 예: "AcquisitionBurstFrameCount".</summary>
        public string Node { get; set; }
        /// <summary>UI 표시 라벨(한글).</summary>
        public string Label { get; set; }
        /// <summary>단위 표시(없으면 빈 문자열).</summary>
        public string Unit { get; set; } = "";
        /// <summary>값 타입 — set 메서드 디스패치 기준.</summary>
        public CameraParamKind Kind { get; set; }
        /// <summary>Enum 옵션(심볼릭 노드값). Enum 타입에서만 사용.</summary>
        public string[] Options { get; set; }
        /// <summary>숫자 범위(Float/Int). UI 입력 검증용.</summary>
        public double Min { get; set; }
        public double Max { get; set; } = 1000000;
        /// <summary>기본 표시값(미저장 시 그리드 표시용). Bool 은 "True"/"False".</summary>
        public string Default { get; set; } = "";

        /// <summary>속한 그룹(섹션 구분/가독성용 — 현재 표시는 단일 그리드).</summary>
        public string Group { get; set; } = "";
    }

    /// <summary>카메라 노드 카탈로그 — 설정 화면에 노출할 MVS 파라미터 목록(행 추가로 확장).</summary>
    public static class CameraNodeCatalog
    {
        private const string GImage = "Image Format";
        private const string GAcq   = "Acquisition";
        private const string GIo     = "IO Output(Strobe)";

        // 동일 모델 4대(Wafer/Bin/FrontSide/RearSide) 공통 — 값만 모듈별 Config 로 저장된다.
        public static readonly IReadOnlyList<CameraNodeDef> All = new List<CameraNodeDef>
        {
            // ── Image Format Control ──────────────────────────────
            new CameraNodeDef { Group = GImage, Node = "ReverseX", Label = "이미지 반전 X(카메라)", Kind = CameraParamKind.Bool, Default = "False" },
            new CameraNodeDef { Group = GImage, Node = "ReverseY", Label = "이미지 반전 Y(카메라)", Kind = CameraParamKind.Bool, Default = "False" },
            new CameraNodeDef { Group = GImage, Node = "BinningHorizontal", Label = "Binning 가로", Kind = CameraParamKind.Int, Min = 1, Max = 16, Default = "1" },
            new CameraNodeDef { Group = GImage, Node = "BinningVertical",   Label = "Binning 세로", Kind = CameraParamKind.Int, Min = 1, Max = 16, Default = "1" },
            new CameraNodeDef { Group = GImage, Node = "DecimationHorizontal", Label = "Decimation 가로", Kind = CameraParamKind.Int, Min = 1, Max = 16, Default = "1" },
            new CameraNodeDef { Group = GImage, Node = "DecimationVertical",   Label = "Decimation 세로", Kind = CameraParamKind.Int, Min = 1, Max = 16, Default = "1" },
            new CameraNodeDef { Group = GImage, Node = "TestPattern", Label = "테스트 패턴", Kind = CameraParamKind.Enum,
                Options = new[] { "Off", "GrayGradient", "VerticalColorBar", "HorizontalColorBar" }, Default = "Off" },

            // ── Acquisition Control ───────────────────────────────
            new CameraNodeDef { Group = GAcq, Node = "AcquisitionMode", Label = "취득 모드", Kind = CameraParamKind.Enum,
                Options = new[] { "Continuous", "SingleFrame", "MultiFrame" }, Default = "Continuous" },
            new CameraNodeDef { Group = GAcq, Node = "AcquisitionBurstFrameCount", Label = "버스트 프레임 수", Kind = CameraParamKind.Int, Min = 1, Max = 1023, Default = "1" },
            new CameraNodeDef { Group = GAcq, Node = "AcquisitionFrameRateEnable", Label = "프레임레이트 제어 사용", Kind = CameraParamKind.Bool, Default = "False" },
            new CameraNodeDef { Group = GAcq, Node = "TriggerDelay", Label = "트리거 지연", Unit = "μs", Kind = CameraParamKind.Float, Min = 0, Max = 10000000, Default = "0" },
            new CameraNodeDef { Group = GAcq, Node = "TriggerCacheEnable", Label = "트리거 캐시 사용", Kind = CameraParamKind.Bool, Default = "False" },
            new CameraNodeDef { Group = GAcq, Node = "ExposureMode", Label = "노출 모드", Kind = CameraParamKind.Enum,
                Options = new[] { "Timed", "TriggerWidth" }, Default = "Timed" },
            new CameraNodeDef { Group = GAcq, Node = "ExposureAuto", Label = "자동 노출", Kind = CameraParamKind.Enum,
                Options = new[] { "Off", "Once", "Continuous" }, Default = "Off" },
            new CameraNodeDef { Group = GAcq, Node = "HDREnable", Label = "HDR 사용", Kind = CameraParamKind.Bool, Default = "False" },
            new CameraNodeDef { Group = GAcq, Node = "HDRSelector", Label = "HDR 선택", Kind = CameraParamKind.Int, Min = 0, Max = 1, Default = "0" },
            new CameraNodeDef { Group = GAcq, Node = "HDRShutter", Label = "HDR 셔터", Unit = "μs", Kind = CameraParamKind.Float, Min = 0, Max = 1000000, Default = "0" },

            // ── Trigger ▸ IO Output (Strobe) ──────────────────────
            // LineSelector 가 먼저 적용돼야 이후 Line* 설정이 해당 라인에 반영된다(카탈로그 순서 = 적용 순서).
            new CameraNodeDef { Group = GIo, Node = "LineSelector", Label = "라인 선택", Kind = CameraParamKind.Enum,
                Options = new[] { "Line0", "Line1", "Line2", "Line3" }, Default = "Line1" },
            new CameraNodeDef { Group = GIo, Node = "LineMode", Label = "라인 모드", Kind = CameraParamKind.Enum,
                Options = new[] { "Input", "Strobe" }, Default = "Strobe" },
            new CameraNodeDef { Group = GIo, Node = "LineSource", Label = "라인 소스", Kind = CameraParamKind.Enum,
                Options = new[] { "ExposureStartActive", "AcquisitionStartActive", "FrameBurstActive", "SoftTriggerActive" }, Default = "ExposureStartActive" },
            new CameraNodeDef { Group = GIo, Node = "StrobeEnable", Label = "스트로브 사용", Kind = CameraParamKind.Bool, Default = "True" },
            new CameraNodeDef { Group = GIo, Node = "StrobeLineDuration", Label = "스트로브 지속", Unit = "μs", Kind = CameraParamKind.Float, Min = 0, Max = 10000000, Default = "0" },
            new CameraNodeDef { Group = GIo, Node = "StrobeLineDelay", Label = "스트로브 지연", Unit = "μs", Kind = CameraParamKind.Float, Min = 0, Max = 10000000, Default = "0" },
            new CameraNodeDef { Group = GIo, Node = "StrobeLinePreDelay", Label = "스트로브 선지연", Unit = "μs", Kind = CameraParamKind.Float, Min = 0, Max = 10000000, Default = "0" },
        };

        /// <summary>노드명으로 정의 조회 — 없으면 null.</summary>
        public static CameraNodeDef Find(string node)
        {
            if (string.IsNullOrEmpty(node)) return null;
            return All.FirstOrDefault(d => string.Equals(d.Node, node, StringComparison.OrdinalIgnoreCase));
        }
    }
}
