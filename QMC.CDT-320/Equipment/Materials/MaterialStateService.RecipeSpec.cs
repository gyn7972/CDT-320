using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using QMC.Common;
using QMC.CDT320.Bin;
using QMC.CDT320.DieMaps;
using QMC.CDT320.Lots;
using QMC.CDT320.Recipes;
using QMC.CDT320.Sequencing;

namespace QMC.CDT320.Materials
{
    // MaterialStateService partial: Recipe TapeFrame/Die spec 해석·동기화 (원본 1972-2169)
    public static partial class MaterialStateService
    {
        public static string ResolveRecipeTapeFrameSpecName(int inchSelect)
        {
            var project = RecipeStore.LoadLastOrDefault();
            if (project == null)
                return ResolveDefaultTapeFrameSpecName(inchSelect);

            var frame = project.InputFrame ?? project.Frame;
            if (frame == null)
                return ResolveDefaultTapeFrameSpecName(inchSelect);

            string specName = string.IsNullOrWhiteSpace(frame.FrameSpecName)
                ? ResolveDefaultTapeFrameSpecName(inchSelect)
                : frame.FrameSpecName.Trim();

            EnsureTapeFrameSpecFromFrame(project, frame, specName, "");
            return specName;
        }

        public static string ResolveInputTapeFrameSpecName(int inchSelect)
        {
            string specName = ResolveRecipeTapeFrameSpecName(inchSelect);
            if (!string.IsNullOrWhiteSpace(specName))
                return NormalizeInputTapeFrameSpecName(specName);

            return NormalizeInputTapeFrameSpecName(ResolveDefaultTapeFrameSpecName(inchSelect));
        }

        public static string NormalizeInputTapeFrameSpecName(string specName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(specName))
                    return "";

                string trimmed = specName.Trim();
                if (trimmed.IndexOf("Output", StringComparison.OrdinalIgnoreCase) < 0)
                    return trimmed;

                string candidateName = ReplaceIgnoreCase(trimmed, "Output", "Input");
                TapeFrameSpec candidate = MaterialSpecs.FindFrame(candidateName);
                if (candidate == null)
                    return trimmed;

                TapeFrameSpec current = MaterialSpecs.FindFrame(trimmed);
                if (current != null && !IsCompatibleTapeFrameSpec(current, candidate))
                    return trimmed;

                Log.Write("Main", "SYSTEM", "MaterialStateService",
                    "Input tape frame spec normalized. requested=" + trimmed +
                    ", normalized=" + candidate.Name + " - Ok");
                return candidate.Name;
            }
            catch
            {
                return string.IsNullOrWhiteSpace(specName) ? "" : specName.Trim();
            }
            finally
            {
            }
        }

        private static string ReplaceIgnoreCase(string source, string oldValue, string newValue)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(oldValue))
                return source;

            int index = source.IndexOf(oldValue, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
                return source;

            return source.Substring(0, index) + newValue + source.Substring(index + oldValue.Length);
        }

        private static bool IsCompatibleTapeFrameSpec(TapeFrameSpec a, TapeFrameSpec b)
        {
            if (a == null || b == null)
                return false;

            return a.DieMapX == b.DieMapX &&
                   a.DieMapY == b.DieMapY &&
                   Math.Abs(a.PitchX - b.PitchX) <= 0.000001 &&
                   Math.Abs(a.PitchY - b.PitchY) <= 0.000001 &&
                   Math.Abs(a.OuterDiameterMm - b.OuterDiameterMm) <= 0.001;
        }

        private static double ResolveProcessTestThetaAlignOffset(QMC.CDT320.InputStageUnit inputStage)
        {
            try
            {
                double offset = ProcessTestThetaAlignOffsetDeg;
                if (inputStage != null)
                {
                    double limit = inputStage.ResolveWaferAlignThetaCorrectionLimit();
                    if (limit > InputStageThetaOffsetReadyEpsilon && offset > limit)
                        offset = (limit + InputStageThetaOffsetReadyEpsilon) * 0.5;
                }

                return Math.Abs(offset) > InputStageThetaOffsetReadyEpsilon
                    ? offset
                    : InputStageThetaOffsetReadyEpsilon * 10.0;
            }
            catch
            {
                return ProcessTestThetaAlignOffsetDeg;
            }
            finally
            {
            }
        }

        public static int ResolveWaferSizeInch(int inchSelect)
        {
            switch (inchSelect)
            {
                // 0 또는 8은 8인치로 해석
                case 0:
                case 8:
                    return 8;
                // 1 또는 12는 12인치로 해석
                case 1:
                case 12:
                    return 12;
                default:
                    return inchSelect > 0 ? inchSelect : 8;
            }
        }

        public static string SyncRecipeTapeFrameSpec(RecipeProject project)
        {
            try
            {
                if (project == null)
                    return "";

                TapeFrameSubset frame = project.InputFrame ?? project.Frame;
                if (frame == null)
                    return "";

                string specName = string.IsNullOrWhiteSpace(frame.FrameSpecName)
                    ? ResolveDefaultTapeFrameSpecName(0)
                    : frame.FrameSpecName.Trim();

                // 현재 기준: Input/Output wafer spec을 각각 MaterialSpecs에 동기화한다.
                EnsureTapeFrameSpecFromFrame(project, frame, specName, project.InputDieMapFileName);
                if (project.OutputFrame != null && !string.IsNullOrWhiteSpace(project.OutputFrame.FrameSpecName))
                    EnsureTapeFrameSpecFromFrame(project, project.OutputFrame, project.OutputFrame.FrameSpecName.Trim(), project.GoodBinDieMapFileName);
                string legacySpecName = project.Frame != null ? (project.Frame.FrameSpecName ?? "").Trim() : "";
                // 구형 Frame 별칭을 마지막에 저장하면 동일 이름 Input/Output의 물리 사양과 MapFileName을 덮어쓴다.
                // 이미 역할별로 동기화한 이름은 유지하고, 별도 이름인 구형 사양만 기존처럼 동기화한다.
                if (project.Frame != null && !ReferenceEquals(project.Frame, frame) && !string.IsNullOrWhiteSpace(legacySpecName) &&
                    !string.Equals(legacySpecName, specName, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(legacySpecName, project.OutputFrame != null ? (project.OutputFrame.FrameSpecName ?? "").Trim() : "", StringComparison.OrdinalIgnoreCase))
                    EnsureTapeFrameSpecFromFrame(project, project.Frame, legacySpecName, "");
                return specName;
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialSpecSync", "Recipe tape frame spec sync failed: " + ex.Message + " - Failed");
                return "";
            }
            finally
            {
            }
        }

        public static string ResolveRecipeDieSpecName()
        {
            var project = RecipeStore.LoadLastOrDefault();
            if (project == null || project.Die == null)
                return "Default";

            string specName = string.IsNullOrWhiteSpace(project.Die.DieSpecName)
                ? "Default"
                : project.Die.DieSpecName.Trim();

            EnsureDieSpecFromRecipe(project, specName);
            return specName;
        }

        public static string SyncRecipeDieSpec(RecipeProject project)
        {
            try
            {
                if (project == null || project.Die == null)
                    return "";

                string specName = string.IsNullOrWhiteSpace(project.Die.DieSpecName)
                    ? "Default"
                    : project.Die.DieSpecName.Trim();

                EnsureDieSpecFromRecipe(project, specName);
                return specName;
            }
            catch (Exception ex)
            {
                Log.Write("Main", "SYSTEM", "MaterialSpecSync", "Recipe die spec sync failed: " + ex.Message + " - Failed");
                return "";
            }
            finally
            {
            }
        }

    }
}
