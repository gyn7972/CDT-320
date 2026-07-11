using System;
using QMC.CDT320;
using QMC.Common.Logging;

namespace QMC.CDT320.Recipes
{
    public sealed class RecipeColletZApplyResult
    {
        public bool Applied { get; set; }
        public double EffectiveOffsetMm { get; set; }
        public double DeltaMm { get; set; }
        public int UpdatedCount { get; set; }
        public string Message { get; set; }
    }

    public static class RecipeColletZApplyService
    {
        private const double ToleranceMm = 0.000001;
        private const int PickerCount = 4;

        public static bool Apply(RecipeProject project, CDT320_Machine machine, out RecipeColletZApplyResult result)
        {
            result = new RecipeColletZApplyResult();
            if (project == null)
            {
                result.Message = "Project is null.";
                return false;
            }

            if (project.ColletZ == null)
                project.ColletZ = new ColletZConfigSubset();
            project.ColletZ.Ensure();

            double effectiveOffset = project.ColletZ.ResolveEffectiveOffsetMm();
            double previousOffset = project.ColletZ.LastAppliedOffsetMm;
            double delta = effectiveOffset - previousOffset;

            result.EffectiveOffsetMm = effectiveOffset;
            result.DeltaMm = delta;

            if (Math.Abs(delta) <= ToleranceMm)
            {
                result.Applied = false;
                result.Message = "Collet Z offset already applied. offset=" + F(effectiveOffset);
                return true;
            }

            if (machine == null)
            {
                result.Message = "Machine is null.";
                return false;
            }

            int updated = 0;
            if (machine.PickerFrontUnit != null && machine.PickerFrontUnit.Recipe != null)
                updated += ApplyFront(machine.PickerFrontUnit, delta);
            if (machine.PickerRearUnit != null && machine.PickerRearUnit.Recipe != null)
                updated += ApplyRear(machine.PickerRearUnit, delta);

            project.ColletZ.LastAppliedOffsetMm = effectiveOffset;
            result.Applied = true;
            result.UpdatedCount = updated;
            result.Message =
                "Collet Z offset applied. type=" + project.ColletZ.ColletType +
                ", previous=" + F(previousOffset) +
                ", effective=" + F(effectiveOffset) +
                ", delta=" + F(delta) +
                ", updated=" + updated;

            EventLogger.Write(EventKind.Event, "DATA", "PROJECT-COLLET-Z-APPLY",
                "project=" + project.FileName + ", " + result.Message);
            return true;
        }

        private static int ApplyFront(PickerFrontUnit unit, double delta)
        {
            int updated = 0;
            unit.Recipe.EnsurePositionObjects();
            updated += ApplySet(unit.Recipe.PickerZ0, delta);
            updated += ApplySet(unit.Recipe.PickerZ1, delta);
            updated += ApplySet(unit.Recipe.PickerZ2, delta);
            updated += ApplySet(unit.Recipe.PickerZ3, delta);
            return updated;
        }

        private static int ApplyRear(PickerRearUnit unit, double delta)
        {
            int updated = 0;
            unit.Recipe.EnsurePositionObjects();
            updated += ApplySet(unit.Recipe.PickerZ0, delta);
            updated += ApplySet(unit.Recipe.PickerZ1, delta);
            updated += ApplySet(unit.Recipe.PickerZ2, delta);
            updated += ApplySet(unit.Recipe.PickerZ3, delta);
            return updated;
        }

        private static int ApplySet(PickerAxisPositionSet set, double delta)
        {
            if (set == null)
                return 0;

            int updated = 0;
            set.PickPosition += delta; updated++;
            set.BottomPosition += delta; updated++;
            set.SidePosition += delta; updated++;
            set.PlacePosition += delta; updated++;

            updated += ApplyArray(set.DiePickPosition, delta);
            updated += ApplyArray(set.DieBottomPosition, delta);
            updated += ApplyArray(set.DieSidePosition, delta);
            updated += ApplyArray(set.DiePlacePosition, delta);
            return updated;
        }

        private static int ApplyArray(double[] values, double delta)
        {
            if (values == null)
                return 0;

            for (int i = 0; i < values.Length && i < PickerCount; i++)
                values[i] += delta;
            return Math.Min(values.Length, PickerCount);
        }

        private static string F(double value)
        {
            return value.ToString("F6");
        }
    }
}
