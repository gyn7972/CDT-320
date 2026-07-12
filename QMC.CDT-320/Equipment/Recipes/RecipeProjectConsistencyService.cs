using System;

namespace QMC.CDT320.Recipes
{
    /// <summary>
    /// Recipe 내부의 중복 사양을 정리한다. 제품 Die 크기/두께의 기준은 RecipeProject.Die 하나뿐이다.
    /// 기존 Recipe 로드만으로 파일을 저장하지 않으며, 명시적인 저장/맵 생성 시점에만 미러 값을 갱신한다.
    /// </summary>
    public static class RecipeProjectConsistencyService
    {
        public static RecipeProject EnsureStructure(RecipeProject project)
        {
            try
            {
                if (project == null)
                    return null;

                if (project.Die == null)
                    project.Die = CreateLegacyCompatibleDie(project);
                if (project.Frame == null)
                    project.Frame = new TapeFrameSubset();
                if (project.InputFrame == null)
                    project.InputFrame = CloneFrame(project.Frame);
                if (project.OutputFrame == null)
                    project.OutputFrame = CloneFrame(project.Frame);

                // 일부 필드만 있던 구형 Project도 정상 Frame/legacy 두께를 1x1 기본값으로 덮지 않는다.
                if (project.Die.WidthMm <= 0.0)
                    project.Die.WidthMm = ResolveFrameDieSize(project, true);
                if (project.Die.HeightMm <= 0.0)
                    project.Die.HeightMm = ResolveFrameDieSize(project, false);
                if (project.Die.ThicknessMm <= 0.0)
                    project.Die.ThicknessMm = ResolveLegacyThickness(project);
                if (string.IsNullOrWhiteSpace(project.Die.DieSpecName))
                    project.Die.DieSpecName = "LegacyRecipeDie";

                return project;
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        public static void SynchronizeDieSpecification(RecipeProject project)
        {
            try
            {
                EnsureStructure(project);
                if (project == null || project.Die == null)
                    return;

                double width = ResolvePositive(project.Die.WidthMm, 1.0);
                double height = ResolvePositive(project.Die.HeightMm, 1.0);
                double thickness = ResolvePositive(project.Die.ThicknessMm, 0.1);

                project.Die.WidthMm = width;
                project.Die.HeightMm = height;
                project.Die.ThicknessMm = thickness;

                MirrorDieSize(project.Frame, width, height);
                MirrorDieSize(project.InputFrame, width, height);
                MirrorDieSize(project.OutputFrame, width, height);

                // Legacy ChipThickness는 um 단위 호환 필드다. Master/ColletZ 보정 두께는 의미가 달라 갱신하지 않는다.
                project.ChipThickness = thickness * 1000.0;
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        public static TapeFrameSubset CloneFrame(TapeFrameSubset source)
        {
            try
            {
                if (source == null)
                    return new TapeFrameSubset();

                return new TapeFrameSubset
                {
                    FrameSpecName = source.FrameSpecName,
                    DieMapX = source.DieMapX,
                    DieMapY = source.DieMapY,
                    PitchX = source.PitchX,
                    PitchY = source.PitchY,
                    DieSizeX = source.DieSizeX,
                    DieSizeY = source.DieSizeY,
                    Rotate = source.Rotate,
                    OuterDiameterMm = source.OuterDiameterMm,
                    EdgeSkipMode = source.EdgeSkipMode,
                    SideEdgeSkip = source.SideEdgeSkip,
                    TopBottomEdgeSkip = source.TopBottomEdgeSkip,
                    SideEdgeSkipMm = source.SideEdgeSkipMm,
                    TopBottomEdgeSkipMm = source.TopBottomEdgeSkipMm
                };
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private static void MirrorDieSize(TapeFrameSubset frame, double width, double height)
        {
            if (frame == null)
                return;

            frame.DieSizeX = width;
            frame.DieSizeY = height;
        }

        private static double ResolvePositive(double value, double fallback)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value) && value > 0.0
                ? value
                : fallback;
        }

        private static DieSubset CreateLegacyCompatibleDie(RecipeProject project)
        {
            return new DieSubset
            {
                DieSpecName = "LegacyRecipeDie",
                WidthMm = ResolveFrameDieSize(project, true),
                HeightMm = ResolveFrameDieSize(project, false),
                ThicknessMm = ResolveLegacyThickness(project)
            };
        }

        private static double ResolveFrameDieSize(RecipeProject project, bool xAxis)
        {
            TapeFrameSubset[] candidates =
            {
                project != null ? project.InputFrame : null,
                project != null ? project.OutputFrame : null,
                project != null ? project.Frame : null
            };

            foreach (TapeFrameSubset frame in candidates)
            {
                double value = frame == null ? 0.0 : (xAxis ? frame.DieSizeX : frame.DieSizeY);
                if (!double.IsNaN(value) && !double.IsInfinity(value) && value > 0.0)
                    return value;
            }

            return 1.0;
        }

        private static double ResolveLegacyThickness(RecipeProject project)
        {
            double legacyMicrometer = project != null ? project.ChipThickness : 0.0;
            return !double.IsNaN(legacyMicrometer) && !double.IsInfinity(legacyMicrometer) && legacyMicrometer > 0.0
                ? legacyMicrometer / 1000.0
                : 0.1;
        }
    }
}
