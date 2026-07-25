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
            : this(
                movingName,
                movingKey,
                targetValue,
                moveKind,
                targetName,
                requiredChecks,
                context,
                skipSharedRailXRule,
                executionMode,
                originalMoveKind,
                false)
        {
        }

        // 인터락 기준(사용자 승인 2026-07-25): 구동 중 위치 오버라이드(팔로잉 중간 세그먼트) 요청 표시.
        // 팔로잉은 존 사이의 중간 좌표로 폴링마다 재명령하므로 목표 존 판정이 Unknown이 되고,
        // 최종 목표에 대한 존 진입 조건은 팔로잉 최초 명령(AxisMove)에서 이미 1회 검증된다.
        // 이 플래그가 true면 존 판정/진입 조건은 생략하고 위치 기반 안전(Y 대향 거리, SharedRailX
        // 페어 간격, Z 상승/Reticle/Busy)만 확인한다 — 기존 Jog 경로 선례와 동일한 방식이다.
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
            MotionGuardMoveKind originalMoveKind,
            bool isPositionOverrideStep)
        {
            IsPositionOverrideStep = isPositionOverrideStep;
            MovingName = movingName ?? string.Empty;
            MovingKey = movingKey ?? string.Empty;
            TargetValue = targetValue;
            MoveKind = moveKind;
            OriginalMoveKind = originalMoveKind;
            TargetName = targetName ?? string.Empty;
            Intent = MotionGuardMoveIntent.Parse(TargetName);
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
        /// <summary>구동 중 위치 오버라이드(팔로잉 중간 세그먼트) 요청이면 true. 존 판정 생략 대상.</summary>
        public bool IsPositionOverrideStep { get; private set; }
        public MotionGuardExecutionMode ExecutionMode { get; private set; }
        public string TargetName { get; private set; }
        public MotionGuardMoveIntent Intent { get; private set; }
        public IReadOnlyList<InterlockCheckPair> RequiredChecks { get; private set; }
        public MotionGuardContext Context { get; private set; }
        public bool SkipSharedRailXRule { get; private set; }

        public bool IsManualSequenceProcess
        {
            get { return ExecutionMode == MotionGuardExecutionMode.ManualSequenceProcess; }
        }

        public bool IsSequenceProcess
        {
            get
            {
                return ExecutionMode == MotionGuardExecutionMode.AutoSequenceProcess ||
                       ExecutionMode == MotionGuardExecutionMode.ManualSequenceProcess;
            }
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
            Register(PickerZoneInterlockRules.VerifyFacingYDistanceFirst);
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
