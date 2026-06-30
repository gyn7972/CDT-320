using System;
using System.Collections.Generic;
using QMC.Common.IO;
using QMC.Common.Motion;

namespace QMC.CDT320.Interlocks
{
    public delegate bool MotionGuardRule(MotionGuardRuleContext request, out string reason);

    public sealed class MotionGuardRuleContext
    {
        public MotionGuardRuleContext(
            string movingName,
            string movingKey,
            double targetValue,
            MotionGuardMoveKind moveKind,
            string targetName,
            IReadOnlyList<InterlockCheckPair> requiredChecks,
            MotionGuardContext context)
            : this(movingName, movingKey, targetValue, moveKind, targetName, requiredChecks, context, false)
        {
        }

        public MotionGuardRuleContext(
            string movingName,
            string movingKey,
            double targetValue,
            MotionGuardMoveKind moveKind,
            string targetName,
            IReadOnlyList<InterlockCheckPair> requiredChecks,
            MotionGuardContext context,
            bool skipSharedRailXRule)
            : this(
                movingName,
                movingKey,
                targetValue,
                moveKind,
                targetName,
                requiredChecks,
                context,
                skipSharedRailXRule,
                MotionGuardExecutionMode.Default,
                moveKind)
        {
        }

        public MotionGuardRuleContext(
            string movingName,
            string movingKey,
            double targetValue,
            MotionGuardMoveKind moveKind,
            string targetName,
            IReadOnlyList<InterlockCheckPair> requiredChecks,
            MotionGuardContext context,
            bool skipSharedRailXRule,
            MotionGuardExecutionMode executionMode,
            MotionGuardMoveKind originalMoveKind)
        {
            MovingName = movingName ?? string.Empty;
            MovingKey = movingKey ?? string.Empty;
            TargetValue = targetValue;
            MoveKind = moveKind;
            OriginalMoveKind = originalMoveKind;
            TargetName = targetName ?? string.Empty;
            RequiredChecks = requiredChecks ?? new List<InterlockCheckPair>();
            Context = context;
            SkipSharedRailXRule = skipSharedRailXRule;
            ExecutionMode = executionMode;
        }

        public string MovingName { get; private set; }
        public string MovingKey { get; private set; }
        public double TargetValue { get; private set; }
        public MotionGuardMoveKind MoveKind { get; private set; }
        public MotionGuardMoveKind OriginalMoveKind { get; private set; }
        public MotionGuardExecutionMode ExecutionMode { get; private set; }
        public string TargetName { get; private set; }
        public IReadOnlyList<InterlockCheckPair> RequiredChecks { get; private set; }
        public MotionGuardContext Context { get; private set; }
        public bool SkipSharedRailXRule { get; private set; }

        public bool IsManualSequenceProcess
        {
            get { return ExecutionMode == MotionGuardExecutionMode.ManualSequenceProcess; }
        }

        public CDT320_Machine Machine
        {
            get { return Context != null ? Context.Machine : null; }
        }

        public BaseAxis GetAxis(string name)
        {
            if (Context == null || Context.Axes == null)
                return null;

            BaseAxis axis;
            return Context.Axes.TryGetValue(InterlockCheckMatrix.NormalizeName(name), out axis) ? axis : null;
        }

        public BaseCylinder GetCylinder(string name)
        {
            if (Context == null || Context.Cylinders == null)
                return null;

            BaseCylinder cylinder;
            return Context.Cylinders.TryGetValue(InterlockCheckMatrix.NormalizeName(name), out cylinder) ? cylinder : null;
        }
    }

    public static class MotionGuardRuleRegistry
    {
        private static readonly object Sync = new object();
        private static readonly List<MotionGuardRule> Rules = new List<MotionGuardRule>();

        static MotionGuardRuleRegistry()
        {
            Register(SharedRailXInterlockRules.Verify);
            Register(InputCassetteInterlockRules.Verify);
            Register(InputFeederInterlockRules.Verify);
            Register(InputStageInterlockRules.Verify);
            Register(VisionInterlockRules.Verify);
            Register(PickerFrontInterlockRules.Verify);
            Register(PickerRearInterlockRules.Verify);
            Register(OutputStageInterlockRules.Verify);
            Register(OutputFeederInterlockRules.Verify);
            Register(OutputCassetteInterlockRules.Verify);
        }

        public static void Register(MotionGuardRule rule)
        {
            if (rule == null)
                return;

            lock (Sync)
            {
                if (!Rules.Contains(rule))
                    Rules.Add(rule);
            }
        }

        public static bool Verify(MotionGuardRuleContext request, out string reason)
        {
            reason = string.Empty;

            if (request == null)
                return MotionGuardRuleHelpers.BlockUnsupportedMoveKind(request, out reason);

            if (!MotionGuardRuleHelpers.IsKnownMoveKind(request.MoveKind))
                return MotionGuardRuleHelpers.BlockUnsupportedMoveKind(request, out reason);

            MotionGuardRule[] snapshot;
            lock (Sync)
                snapshot = Rules.ToArray();

            foreach (MotionGuardRule rule in snapshot)
            {
                if (!rule(request, out reason))
                    return false;
            }

            reason = string.Empty;
            return true;
        }
    }
}
