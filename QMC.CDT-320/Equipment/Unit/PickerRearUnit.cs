using QMC.CDT320.Ajin;
using QMC.CDT320.Motion.SharedRailX;
using QMC.Common;
using QMC.Common.Alarms;
using QMC.Common.IO;
using QMC.Common.Logging;
using QMC.Common.Motion;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace QMC.CDT320
{
    [DataContract]
    public sealed class PickerRearSetup : ISetupData
    {
        [DataMember] public bool IsSimulationMode { get; set; }
        [DataMember] public double InputSafetyOffset { get; set; }
        [DataMember] public double OutputSafetyOffset { get; set; }
        [DataMember] public double ArmInputPositionX { get; set; } = 300.0;
        [DataMember] public double ArmInspectionPositionX { get; set; } = 750.0;
        [DataMember] public double ArmOutputPositionX { get; set; } = 1200.0;
        [DataMember] public double ArmYPickupPosition { get; set; } = 100.0;
        [DataMember] public double ArmYAvoidPosition { get; set; } = 50.0;
        [DataMember] public double PickerPitchX { get; set; } = 50.0;
        [DataMember] public double SideVision1X { get; set; } = 850.0;
        [DataMember] public double SideVision1Y { get; set; } = 0.0;
        [DataMember] public double SideVisionY0 { get; set; } = 0.0;
    }

    [DataContract]
    public sealed class PickerRearConfig : IConfigData
    {
        [DataMember] public bool UseUnit { get; set; } = true;
        [DataMember] public PickerRunOrderMode RunOrderMode { get; set; } = PickerRunOrderMode.Descending;
        [DataMember] public bool bDryRun { get; set; }
        [DataMember] public bool[] UsePicker { get; set; } = new bool[] { true, true, true, true };
        [DataMember] public PickerAlignOffset[] Picker { get; set; } = CreatePickerOffsets();

        public bool IsSimulationMode
        {
            get { return bDryRun; }
            set { bDryRun = value; }
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext ctx)
        {
            EnsureArrays();
        }

        public void EnsureArrays()
        {
            if (UsePicker == null || UsePicker.Length < PickerRearUnit.MaxPickerCount)
            {
                bool[] next = new bool[] { true, true, true, true };
                if (UsePicker != null)
                {
                    for (int i = 0; i < Math.Min(UsePicker.Length, next.Length); i++)
                        next[i] = UsePicker[i];
                }
                UsePicker = next;
            }

            if (Picker == null || Picker.Length < PickerRearUnit.MaxPickerCount)
                Picker = CreatePickerOffsets();

            for (int i = 0; i < Picker.Length; i++)
            {
                if (Picker[i] == null)
                    Picker[i] = new PickerAlignOffset();
            }
        }

        private static PickerAlignOffset[] CreatePickerOffsets()
        {
            return new PickerAlignOffset[]
            {
                new PickerAlignOffset(),
                new PickerAlignOffset(),
                new PickerAlignOffset(),
                new PickerAlignOffset()
            };
        }
    }

    [DataContract]
    public sealed class PickerRearRecipe : IRecipeData
    {
        [DataMember] public PickerAxisPositionSet PickerX { get; set; } = new PickerAxisPositionSet();
        [DataMember] public PickerAxisPositionSet PickerY { get; set; } = new PickerAxisPositionSet();
        [DataMember] public PickerAxisPositionSet PickerT0 { get; set; } = new PickerAxisPositionSet();
        [DataMember] public PickerAxisPositionSet PickerZ0 { get; set; } = new PickerAxisPositionSet();
        [DataMember] public PickerAxisPositionSet PickerT1 { get; set; } = new PickerAxisPositionSet();
        [DataMember] public PickerAxisPositionSet PickerZ1 { get; set; } = new PickerAxisPositionSet();
        [DataMember] public PickerAxisPositionSet PickerT2 { get; set; } = new PickerAxisPositionSet();
        [DataMember] public PickerAxisPositionSet PickerZ2 { get; set; } = new PickerAxisPositionSet();
        [DataMember] public PickerAxisPositionSet PickerT3 { get; set; } = new PickerAxisPositionSet();
        [DataMember] public PickerAxisPositionSet PickerZ3 { get; set; } = new PickerAxisPositionSet();
        [DataMember] public int MoveTimeoutMs { get; set; } = 5000;
        [DataMember] public int IoTimeoutMs { get; set; } = 1000;
        [DataMember] public int BlowTimeMs { get; set; } = 100;
        [DataMember] public double ArmXVelocity { get; set; } = 2000.0;
        [DataMember] public double ArmYVelocity { get; set; } = 500.0;
        [DataMember] public double PickerZVelocity { get; set; } = 1000.0;
        [DataMember] public double PickerTVelocity { get; set; } = 1000.0;
        [DataMember] public int VacuumSettleMs { get; set; } = 50;
        [DataMember] public double PickLiftPosition { get; set; } = 2.0;
        [DataMember] public int PickLiftWaitMs { get; set; } = 50;
        [DataMember] public int PlaceDelayMs { get; set; } = 50;

        [OnDeserialized]
        private void OnDeserialized(StreamingContext ctx)
        {
            EnsurePositionObjects();
        }

        public void EnsurePositionObjects()
        {
            if (PickerX == null) PickerX = new PickerAxisPositionSet();
            if (PickerY == null) PickerY = new PickerAxisPositionSet();
            if (PickerT0 == null) PickerT0 = new PickerAxisPositionSet();
            if (PickerZ0 == null) PickerZ0 = new PickerAxisPositionSet();
            // 구버전 레시피 호환: 개별 T/Z 세트가 없으면 기존 T0/Z0 값을 복제해 동작을 보존한다.
            if (PickerT1 == null) PickerT1 = PickerT0.Clone();
            if (PickerT2 == null) PickerT2 = PickerT0.Clone();
            if (PickerT3 == null) PickerT3 = PickerT0.Clone();
            if (PickerZ1 == null) PickerZ1 = PickerZ0.Clone();
            if (PickerZ2 == null) PickerZ2 = PickerZ0.Clone();
            if (PickerZ3 == null) PickerZ3 = PickerZ0.Clone();
            PickerX.EnsureArrays();
            PickerY.EnsureArrays();
            PickerT0.EnsureArrays();
            PickerZ0.EnsureArrays();
            PickerT1.EnsureArrays();
            PickerT2.EnsureArrays();
            PickerT3.EnsureArrays();
            PickerZ1.EnsureArrays();
            PickerZ2.EnsureArrays();
            PickerZ3.EnsureArrays();
        }
    }

    public class PickerRearUnit : BaseUnit<PickerRearSetup, PickerRearConfig, PickerRearRecipe>, IUnitJogController
    {
        public const int MaxPickerCount = 4;
        public const int MaxIoPickerCount = 8;

        private readonly Dictionary<PickerAxis, BaseAxis> axes = new Dictionary<PickerAxis, BaseAxis>();
        private readonly string side;

        public BaseAxis PickerX { get; private set; }
        public BaseAxis PickerY { get; private set; }
        public BaseAxis PickerT0 { get; private set; }
        public BaseAxis PickerZ0 { get; private set; }
        public BaseAxis PickerT1 { get; private set; }
        public BaseAxis PickerZ1 { get; private set; }
        public BaseAxis PickerT2 { get; private set; }
        public BaseAxis PickerZ2 { get; private set; }
        public BaseAxis PickerT3 { get; private set; }
        public BaseAxis PickerZ3 { get; private set; }

        public BaseDigitalInput CdaTankPressureCheck { get; private set; }
        public BaseDigitalInput VacuumTankPressureCheck { get; private set; }
        public BaseDigitalInput[] FlowChecks { get; private set; }
        public BaseDigitalOutput[] Vacuums { get; private set; }
        public BaseDigitalOutput[] Blows { get; private set; }
        public BaseAxis ArmX { get { return PickerX; } }
        public BaseAxis ArmY { get { return PickerY; } }
        public BaseAxis SideVisionY { get { return PickerY; } }
        public PickerRuntimeTool[] Pickers { get; private set; }
        public int[] ColletUseCounts { get; private set; } = new int[MaxPickerCount];
        public int PickFailCount { get; private set; }
        public int PlaceFailCount { get; private set; }

        public PickerRearUnit() : base("PickerRearUnit")
        {
            side = "Rear";

            PickerX = RegisterAxis(PickerAxis.PickerX, side + "PickerX");
            PickerY = RegisterAxis(PickerAxis.PickerY, side + "PickerY");
            PickerT0 = RegisterAxis(PickerAxis.PickerT0, side + "PickerT0");
            PickerZ0 = RegisterAxis(PickerAxis.PickerZ0, side + "PickerZ0");
            PickerT1 = RegisterAxis(PickerAxis.PickerT1, side + "PickerT1");
            PickerZ1 = RegisterAxis(PickerAxis.PickerZ1, side + "PickerZ1");
            PickerT2 = RegisterAxis(PickerAxis.PickerT2, side + "PickerT2");
            PickerZ2 = RegisterAxis(PickerAxis.PickerZ2, side + "PickerZ2");
            PickerT3 = RegisterAxis(PickerAxis.PickerT3, side + "PickerT3");
            PickerZ3 = RegisterAxis(PickerAxis.PickerZ3, side + "PickerZ3");

            CdaTankPressureCheck = RegisterInput(side + "PickerCdaTankPressureCheck");
            VacuumTankPressureCheck = RegisterInput(side + "PickerVacuumTankPressureCheck");
            FlowChecks = new BaseDigitalInput[MaxIoPickerCount];
            Vacuums = new BaseDigitalOutput[MaxIoPickerCount];
            Blows = new BaseDigitalOutput[MaxIoPickerCount];

            for (int i = 0; i < MaxIoPickerCount; i++)
            {
                int catalogNo = i + 1;
                FlowChecks[i] = RegisterInput(side + "Picker" + catalogNo + "Flow");
                Vacuums[i] = RegisterOutput(BuildVacuumName(catalogNo));
                Blows[i] = RegisterOutput(BuildBlowName(catalogNo));
            }

            Pickers = CreateRuntimePickers();
        }

        private PickerRuntimeTool[] CreateRuntimePickers()
        {
            PickerRuntimeTool[] tools = new PickerRuntimeTool[MaxPickerCount];
            for (int i = 0; i < MaxPickerCount; i++)
            {
                int index = i;
                int pickerNo = i + 1;
                tools[i] = new PickerRuntimeTool(
                    GetAxis(GetPickerZAxis(index)),
                    GetAxis(GetPickerTAxis(index)),
                    () => CreateRuntimeToolSetup(index),
                    () => CreateRuntimeToolRecipe(),
                    () => PickerVacuumOn(pickerNo),
                    () => PickerVacuumOff(pickerNo),
                    () => SetPickerBlow(pickerNo, true),
                    () => PickerBlowOff(pickerNo));
            }

            return tools;
        }

        private PickerRuntimeToolSetup CreateRuntimeToolSetup(int index)
        {
            Config.EnsureArrays();
            return new PickerRuntimeToolSetup
            {
                ColletOffsetX = Config.Picker[index].AlignOffsetX,
                ColletOffsetY = Config.Picker[index].AlignOffsetY,
                PickupPosition = Recipe.PickerZ0.PickPosition,
                WaitPosition = Recipe.PickerZ0.AvoidPosition,
                PlacePosition = Recipe.PickerZ0.PlacePosition
            };
        }

        public void SetRuntimePickerZPosition(int pickerIndex, string positionName, double position)
        {
            if (string.Equals(positionName, "PickPosition", StringComparison.OrdinalIgnoreCase))
                Recipe.PickerZ0.PickPosition = position;
            else if (string.Equals(positionName, "PlacePosition", StringComparison.OrdinalIgnoreCase))
                Recipe.PickerZ0.PlacePosition = position;
            else if (string.Equals(positionName, "FocusPosition", StringComparison.OrdinalIgnoreCase))
                Recipe.PickerZ0.BottomPosition = position;
            else if (string.Equals(positionName, "WaitPosition", StringComparison.OrdinalIgnoreCase))
                Recipe.PickerZ0.AvoidPosition = position;
        }

        private PickerRuntimeToolRecipe CreateRuntimeToolRecipe()
        {
            return new PickerRuntimeToolRecipe
            {
                ZVelocity = Recipe.PickerZVelocity,
                ThetaVelocity = Recipe.PickerTVelocity,
                VacuumSettleMs = Recipe.VacuumSettleMs,
                PickLiftPosition = Recipe.PickLiftPosition,
                PickLiftWaitMs = Recipe.PickLiftWaitMs,
                PlaceDelayMs = Recipe.PlaceDelayMs
            };
        }

        public Task<Tuple<BottomVisionOffset[], SideVisionResult[]>> InspectBottomAndSideAsync(double dieSizeX, double dieSizeY)
        {
            BottomVisionOffset[] bottom = new BottomVisionOffset[MaxPickerCount];
            SideVisionResult[] sideResults = new SideVisionResult[MaxPickerCount];
            for (int i = 0; i < MaxPickerCount; i++)
            {
                bottom[i] = new BottomVisionOffset { PickerNo = i + 1, IsOk = true };
                sideResults[i] = new SideVisionResult
                {
                    PickerNo = i + 1,
                    Side1Ok = true,
                    Side2Ok = true,
                    Side3Ok = true,
                    Side4Ok = true
                };
            }

            return Task.FromResult(Tuple.Create(bottom, sideResults));
        }

        public IReadOnlyDictionary<PickerAxis, BaseAxis> Axes { get { return axes; } }

        public bool CanHandleJogAxis(BaseAxis axis)
        {
            PickerAxis pickerAxis;
            return TryResolvePickerAxis(axis, out pickerAxis);
        }

        public async Task<int> JogStepAsync(
            BaseAxis axis,
            int direction,
            JogSpeedType speedType,
            double customSpeed,
            double axisStepDistance)
        {
            PickerAxis pickerAxis;
            if (!TryResolvePickerAxis(axis, out pickerAxis))
                return -1;

            double signedDistance = (direction < 0 ? -1.0 : 1.0) * Math.Abs(axisStepDistance);
            double target = axis.ActualPosition + signedDistance;
            return await MovePickerAxis(pickerAxis, target, speedType == JogSpeedType.Fine);
        }

        public Task<int> JogContinuousAsync(
            BaseAxis axis,
            int direction,
            JogSpeedType speedType,
            double customSpeed)
        {
            PickerAxis pickerAxis;
            if (!TryResolvePickerAxis(axis, out pickerAxis))
                return Task.FromResult(-1);

            double speed = UnitJogVelocityResolver.Resolve(axis, speedType, customSpeed);
            ManualMovePickerAxisJog(pickerAxis, direction < 0 ? Direction.Minus : Direction.Plus, speed);
            return Task.FromResult(0);
        }

        public Task<int> StopJogAsync(BaseAxis axis)
        {
            PickerAxis pickerAxis;
            if (!TryResolvePickerAxis(axis, out pickerAxis))
                return Task.FromResult(-1);

            ManualStopPickerAxis(pickerAxis);
            return Task.FromResult(0);
        }

        private bool TryResolvePickerAxis(BaseAxis axis, out PickerAxis pickerAxis)
        {
            foreach (KeyValuePair<PickerAxis, BaseAxis> pair in axes)
            {
                if (ReferenceEquals(axis, pair.Value))
                {
                    pickerAxis = pair.Key;
                    return true;
                }
            }

            pickerAxis = PickerAxis.PickerX;
            return false;
        }

        public async Task<int> MovePickerAxis(PickerAxis axis, double targetPos, bool bFine = false)
        {
            try
            {
                BaseAxis item = GetAxis(axis);
                if (!CheckPickerAxisMoveReady(axis))
                    return RaisePickerAlarm("PK-MOVE-READY", axis + " is not ready to move.");

                EventLogger.Write(EventKind.Event, "QMC", "PK-MOVE", Name + " " + axis + " target=" + targetPos);
                int result = await SharedRailXMotionRuntime.MoveAxisAsync(item, targetPos, ResolveMoveVelocity(item, bFine));
                if (result != 0 || item.IsAlarm)
                    return RaisePickerAlarm("PK-MOVE", axis + " move failed. result=" + result + ", alarm=" + item.IsAlarm);

                return 0;
            }
            catch (Exception ex)
            {
                return RaisePickerAlarm("PK-MOVE-EX", axis + " move exception: " + ex.Message);
            }
        }

        public async Task<int> MovePickerAxes(Dictionary<PickerAxis, double> targets, bool bFine = false)
        {
            if (targets == null)
                return RaisePickerAlarm("PK-MOVE-TARGET", "Picker move target collection is null.");

            List<Task<int>> tasks = new List<Task<int>>();
            MoveZAxesToSafeFirst(targets, bFine, tasks);

            foreach (KeyValuePair<PickerAxis, double> pair in targets)
            {
                if (!IsZAxis(pair.Key))
                    tasks.Add(MovePickerAxis(pair.Key, pair.Value, bFine));
            }

            int[] results = await Task.WhenAll(tasks);
            for (int i = 0; i < results.Length; i++)
            {
                if (results[i] != 0)
                    return results[i];
            }
            return 0;
        }

        public Task<int> MovePickerAxisToTeachingPosition(PickerAxis axis, string positionName, bool bFine = false)
        {
            return MovePickerAxis(axis, GetPickerTeachingPosition(axis, positionName), bFine);
        }

        public Task<int> MoveToPickerAvoidPosition(bool bFine = false)
        {
            return MovePickerGroup("AvoidPosition", bFine);
        }

        public Task<int> MoveToPickerLoadPosition(bool bFine = false)
        {
            return MovePickerGroup("InputAvoidPosition", bFine);
        }

        public Task<int> MoveToPickerUnloadPosition(bool bFine = false)
        {
            return MovePickerGroup("OutputAvoidPosition", bFine);
        }

        public Task<int> MoveToPickerSafeRetreatPosition(bool bFine = false)
        {
            return MovePickerGroup("AvoidPosition", bFine);
        }

        public Task<int> MoveToPickerDiePickPosition(int pickerNo, bool bFine = false)
        {
            return MoveToDiePosition(pickerNo, "DiePickPosition", bFine);
        }

        public Task<int> MoveToPickerDieProcessPosition(int pickerNo, bool bFine = false)
        {
            return MoveToDiePosition(pickerNo, "DieBottomPosition", bFine);
        }

        public Task<int> MoveToPickerDiePlacePosition(int pickerNo, bool bFine = false)
        {
            return MoveToDiePosition(pickerNo, "DiePlacePosition", bFine);
        }

        public Task<int> MovePickerZToPickHeight(int pickerNo, bool bFine = false)
        {
            return MovePickerAxis(GetPickerZAxis(pickerNo), Recipe.PickerZ0.PickPosition, bFine);
        }

        public Task<int> MovePickerZToSafeHeight(int pickerNo, bool bFine = false)
        {
            return MovePickerAxis(GetPickerZAxis(pickerNo), Recipe.PickerZ0.AvoidPosition, bFine);
        }

        public Task<int> MovePickerTToOffset(int pickerNo, bool bFine = false)
        {
            int index = NormalizePickerIndex(pickerNo, MaxPickerCount);
            return MovePickerAxis(GetPickerTAxis(index), Recipe.PickerT0.PickPosition + Config.Picker[index].AlignOffsetT, bFine);
        }

        public bool IsPickerAxisInPosition(PickerAxis axis, double targetPos, double tolerance)
        {
            BaseAxis item = GetAxis(axis);
            return Math.Abs(item.ActualPosition - targetPos) <= tolerance && !item.IsAlarm;
        }

        public async Task<bool> WaitPickerAxisMoveDone(PickerAxis axis, int timeoutMs)
        {
            return await WaitUntilAsync(() =>
            {
                BaseAxis item = GetAxis(axis);
                return !item.IsMoving && item.IsInPosition && !item.IsAlarm;
            }, timeoutMs);
        }

        public async Task<bool> WaitPickerAxesMoveDone(IEnumerable<PickerAxis> targetAxes, int timeoutMs)
        {
            return await WaitUntilAsync(() =>
            {
                foreach (PickerAxis axis in targetAxes)
                {
                    BaseAxis item = GetAxis(axis);
                    if (item.IsMoving || !item.IsInPosition || item.IsAlarm)
                        return false;
                }
                return true;
            }, timeoutMs);
        }

        public bool IsPickerAxisInTeachingPosition(PickerAxis axis, string positionName)
        {
            BaseAxis item = GetAxis(axis);
            return IsPickerAxisInPosition(axis, GetPickerTeachingPosition(axis, positionName), item.Config.InPositionTolerance);
        }

        public Task<bool> WaitPickerAxisInTeachingPosition(PickerAxis axis, string positionName, int timeoutMs)
        {
            return WaitUntilAsync(() => IsPickerAxisInTeachingPosition(axis, positionName), timeoutMs);
        }

        public bool IsPickerInAvoidPosition()
        {
            return IsPickerGroupInPosition("AvoidPosition");
        }

        /// <summary>해당 픽커 축의 원점복귀(IsHomeDone) 완료 여부. (AVOID 홈게이트용)</summary>
        public bool IsPickerAxisHomeDone(PickerAxis axis)
        {
            BaseAxis item;
            return axes.TryGetValue(axis, out item) && item != null && item.IsHomeDone;
        }

        /// <summary>
        /// 스테이지 평면 이동 간섭 기준: Picker Z(0~3)가 모두 Avoid(상승)인지 확인한다.
        /// 막는 첫 Z축과 사유를 돌려준다(진단용). 모두 상승+정상이면 null.
        /// (X/Y/T는 보지 않음 — 픽커가 Z만 상승해 있으면 평면 이동에 간섭 없음)
        /// </summary>
        public string GetPickerZClearBlockReason()
        {
            PickerAxis[] zAxes = { PickerAxis.PickerZ0, PickerAxis.PickerZ1, PickerAxis.PickerZ2, PickerAxis.PickerZ3 };
            foreach (PickerAxis axis in zAxes)
            {
                BaseAxis item;
                if (!axes.TryGetValue(axis, out item) || item == null)
                    continue;

                string label = axis.ToString().Replace("Picker", "");
                if (item.IsAlarm)
                    return label + " Alarm";

                double target = GetPickerTeachingPosition(axis, "AvoidPosition");
                double tol = item.Config != null ? item.Config.InPositionTolerance : 0.05;
                if (System.Math.Abs(item.ActualPosition - target) > tol)
                    return label + " 안 올라감(목표 " + target.ToString("F3") + ", 현재 " + item.ActualPosition.ToString("F3") + ")";
            }

            return null;
        }

        public bool IsPickerInLoadPosition()
        {
            return IsPickerGroupInPosition("InputAvoidPosition");
        }

        public bool IsPickerInUnloadPosition()
        {
            return IsPickerGroupInPosition("OutputAvoidPosition");
        }

        public bool IsPickerInDiePickPosition(int pickerNo)
        {
            return IsPickerInDiePosition(pickerNo, "DiePickPosition");
        }

        public bool IsPickerInDieProcessPosition(int pickerNo)
        {
            return IsPickerInDiePosition(pickerNo, "DieBottomPosition");
        }

        public bool IsPickerInDiePlacePosition(int pickerNo)
        {
            return IsPickerInDiePosition(pickerNo, "DiePlacePosition");
        }

        public void TeachPickerAxisPosition(PickerAxis axis, string positionName)
        {
            SetPickerTeachingPosition(axis, positionName, GetAxis(axis).ActualPosition);
            EventLogger.Write(EventKind.Event, "QMC", "PK-TEACH", Name + " " + axis + "." + positionName + "=" + GetAxis(axis).ActualPosition);
        }

        public void TeachPickerAvoidPositions()
        {
            TeachPickerGroup("AvoidPosition");
        }

        public void TeachPickerLoadPositions()
        {
            TeachPickerGroup("InputAvoidPosition");
        }

        public void TeachPickerUnloadPositions()
        {
            TeachPickerGroup("OutputAvoidPosition");
        }

        public void TeachPickerSafeRetreatPositions()
        {
            TeachPickerGroup("AvoidPosition");
        }

        public void TeachPickerDiePickPosition(int pickerNo)
        {
            TeachPickerDiePosition(pickerNo, "DiePickPosition");
        }

        public void TeachPickerDieProcessPosition(int pickerNo)
        {
            TeachPickerDiePosition(pickerNo, "DieBottomPosition");
        }

        public void TeachPickerDiePlacePosition(int pickerNo)
        {
            TeachPickerDiePosition(pickerNo, "DiePlacePosition");
        }

        public void TeachPickerOffsetPosition(int pickerNo)
        {
            int index = NormalizePickerIndex(pickerNo, MaxPickerCount);
            Config.EnsureArrays();
            Config.Picker[index].AlignOffsetT = GetAxis(GetPickerTAxis(index)).ActualPosition - Recipe.PickerT0.PickPosition;
        }

        public double GetPickerTeachingPosition(PickerAxis axis, string positionName)
        {
            PickerAxisPositionSet set = GetPositionSet(axis);
            if (positionName == "InputAvoidPosition") return set.InputAvoidPosition;
            if (positionName == "OutputAvoidPosition") return set.OutputAvoidPosition;
            if (positionName == "AvoidPosition") return set.AvoidPosition;
            if (positionName == "PickPosition") return set.PickPosition;
            if (positionName == "BottomPosition") return set.BottomPosition;
            if (positionName == "SidePosition") return set.SidePosition;
            if (positionName == "PlacePosition") return set.PlacePosition;
            if (positionName.StartsWith("DiePickPosition", StringComparison.OrdinalIgnoreCase)) return GetIndexedPosition(set.DiePickPosition, ExtractIndex(positionName));
            if (positionName.StartsWith("DieBottomPosition", StringComparison.OrdinalIgnoreCase)) return GetIndexedPosition(set.DieBottomPosition, ExtractIndex(positionName));
            if (positionName.StartsWith("DieSidePosition", StringComparison.OrdinalIgnoreCase)) return GetIndexedPosition(set.DieSidePosition, ExtractIndex(positionName));
            if (positionName.StartsWith("DiePlacePosition", StringComparison.OrdinalIgnoreCase)) return GetIndexedPosition(set.DiePlacePosition, ExtractIndex(positionName));
            return 0.0;
        }

        public bool ValidatePickerTeachingComplete()
        {
            foreach (PickerAxis axis in axes.Keys)
            {
                if (GetPickerTeachingPosition(axis, "AvoidPosition") == 0.0)
                    return false;
            }
            return true;
        }

        public void SetPickerVacuum(int pickerNo, bool on)
        {
            try
            {
                BaseDigitalOutput output = Vacuums[NormalizePickerIndex(pickerNo, MaxIoPickerCount)];
                if (on) output.On(); else output.Off();
                EventLogger.Write(EventKind.Event, "QMC", "PK-VAC", Name + " picker=" + pickerNo + " vacuum=" + on);
            }
            catch (Exception ex)
            {
                RaisePickerAlarm("PK-VAC", "Picker vacuum output failed. picker=" + pickerNo + ", " + ex.Message);
            }
        }

        public void SetPickerBlow(int pickerNo, bool on)
        {
            try
            {
                BaseDigitalOutput output = Blows[NormalizePickerIndex(pickerNo, MaxIoPickerCount)];
                if (on) output.On(); else output.Off();
                EventLogger.Write(EventKind.Event, "QMC", "PK-BLOW", Name + " picker=" + pickerNo + " blow=" + on);
            }
            catch (Exception ex)
            {
                RaisePickerAlarm("PK-BLOW", "Picker blow output failed. picker=" + pickerNo + ", " + ex.Message);
            }
        }

        public void PickerVacuumOn(int pickerNo) { SetPickerVacuum(pickerNo, true); }
        public void PickerVacuumOff(int pickerNo) { SetPickerVacuum(pickerNo, false); }

        public async Task PickerBlowOn(int pickerNo, int timeoutMs = 0)
        {
            SetPickerBlow(pickerNo, true);
            if (timeoutMs > 0)
                await Task.Delay(timeoutMs);
        }

        public void PickerBlowOff(int pickerNo) { SetPickerBlow(pickerNo, false); }

        public bool IsPickerFlowDetected(int pickerNo, bool expected = true)
        {
            return FlowChecks[NormalizePickerIndex(pickerNo, MaxIoPickerCount)].IsOn == expected;
        }

        public bool IsPickerCdaPressureOk()
        {
            return CdaTankPressureCheck.IsOn || Config.IsSimulationMode || Setup.IsSimulationMode;
        }

        public bool IsPickerVacuumPressureOk()
        {
            return VacuumTankPressureCheck.IsOn || Config.IsSimulationMode || Setup.IsSimulationMode;
        }

        public Task<bool> WaitPickerFlowState(int pickerNo, bool expected, int timeoutMs)
        {
            return WaitUntilAsync(() => IsPickerFlowDetected(pickerNo, expected), timeoutMs);
        }

        public void ManualMovePickerAxisJog(PickerAxis axis, Direction dir, double speed)
        {
            SharedRailXMotionRuntime.MoveJogContinuous(GetAxis(axis), (int)dir, speed);
        }

        public void ManualStopPickerAxis(PickerAxis axis)
        {
            GetAxis(axis).StopJog();
        }

        public async Task<bool> PickDie(int pickerNo, int timeoutMs, bool bFine = false)
        {
            if (await MoveToPickerDiePickPosition(pickerNo, bFine) != 0)
                return false;
            PickerVacuumOn(pickerNo);
            bool ok = await WaitPickerFlowState(pickerNo, true, timeoutMs);
            if (!ok)
                RaisePickerAlarm("PK-PICK-FLOW", "Picker flow timeout after vacuum on. picker=" + pickerNo);
            return ok;
        }

        public async Task<bool> PlaceDie(int pickerNo, int timeoutMs, bool bFine = false)
        {
            if (await MoveToPickerDiePlacePosition(pickerNo, bFine) != 0)
                return false;
            PickerVacuumOff(pickerNo);
            await PickerBlowOn(pickerNo, timeoutMs > 0 ? timeoutMs : Recipe.BlowTimeMs);
            PickerBlowOff(pickerNo);
            return true;
        }

        public bool CheckPickerMoveReady()
        {
            return IsPickerCdaPressureOk();
        }

        public bool CheckPickerAxisMoveReady(PickerAxis axis)
        {
            BaseAxis item = GetAxis(axis);
            return CheckPickerMoveReady() && !item.IsAlarm;
        }

        public void StopPickerMotionAndOutputs(string reason)
        {
            foreach (BaseAxis axis in axes.Values)
                axis.StopJog();
            for (int i = 0; i < Vacuums.Length; i++)
            {
                Vacuums[i].Off();
                Blows[i].Off();
            }
            EventLogger.Write(EventKind.Event, "QMC", "PK-STOP", Name + " stopped. reason=" + reason);
        }

        public string BuildPickerAlarmMessage(StageAlarmCode code)
        {
            return side + " picker alarm: " + code;
        }

        public void RecordColletUse(int pickerNo)
        {
            int index = NormalizePickerIndex(pickerNo, MaxPickerCount);
            ColletUseCounts[index]++;
        }

        public void RecordPickFail()
        {
            PickFailCount++;
        }

        public void RecordPlaceFail()
        {
            PlaceFailCount++;
        }

        public void ResetWorkCounters()
        {
            ColletUseCounts = new int[MaxPickerCount];
            PickFailCount = 0;
            PlaceFailCount = 0;
        }

        protected BaseAxis GetAxis(PickerAxis axis)
        {
            BaseAxis item;
            if (!axes.TryGetValue(axis, out item))
                throw new ArgumentException("Unknown picker axis: " + axis);
            return item;
        }

        protected PickerAxis GetPickerZAxis(int pickerNo)
        {
            int index = NormalizePickerIndex(pickerNo, MaxPickerCount);
            if (index == 0) return PickerAxis.PickerZ0;
            if (index == 1) return PickerAxis.PickerZ1;
            if (index == 2) return PickerAxis.PickerZ2;
            return PickerAxis.PickerZ3;
        }

        protected PickerAxis GetPickerTAxis(int pickerNo)
        {
            int index = NormalizePickerIndex(pickerNo, MaxPickerCount);
            if (index == 0) return PickerAxis.PickerT0;
            if (index == 1) return PickerAxis.PickerT1;
            if (index == 2) return PickerAxis.PickerT2;
            return PickerAxis.PickerT3;
        }

        private BaseAxis RegisterAxis(PickerAxis axis, string axisName)
        {
            BaseAxis item = AjinFactory.CreateAxis(axisName);
            axes[axis] = item;
            Components.Add(item);
            return item;
        }

        private BaseDigitalInput RegisterInput(string catalogName)
        {
            BaseDigitalInput item = AjinFactory.CreateDigitalInput(AjinIoCatalog.FindInput(catalogName));
            Components.Add(item);
            return item;
        }

        private BaseDigitalOutput RegisterOutput(string catalogName)
        {
            BaseDigitalOutput item = AjinFactory.CreateDigitalOutput(AjinIoCatalog.FindOutput(catalogName));
            Components.Add(item);
            return item;
        }

        private string BuildVacuumName(int pickerNo)
        {
            return side == "Rear" ? "RearPicker" + pickerNo + "_Vacuum" : "RearPicker" + pickerNo + "Vacuum";
        }

        private string BuildBlowName(int pickerNo)
        {
            return side == "Rear" ? "RearPicker" + pickerNo + "_Blow" : "RearPicker" + pickerNo + "Blow";
        }

        private double ResolveMoveVelocity(BaseAxis axis, bool bFine)
        {
            if (bFine && axis.Config.JogFineVelocity > 0)
                return axis.Config.JogFineVelocity;
            return axis.Config.DefaultVelocity;
        }

        private Task<int> MovePickerGroup(string positionName, bool bFine)
        {
            Dictionary<PickerAxis, double> targets = new Dictionary<PickerAxis, double>();
            foreach (PickerAxis axis in axes.Keys)
                targets[axis] = GetPickerTeachingPosition(axis, positionName);
            return MovePickerAxes(targets, bFine);
        }

        private async Task<int> MoveToDiePosition(int pickerNo, string positionArrayName, bool bFine)
        {
            int index = NormalizePickerIndex(pickerNo, MaxPickerCount);
            Config.EnsureArrays();

            Dictionary<PickerAxis, double> targets = new Dictionary<PickerAxis, double>();
            targets[PickerAxis.PickerX] = GetPickerTeachingPosition(PickerAxis.PickerX, positionArrayName + "[" + index + "]") + Config.Picker[index].AlignOffsetX;
            targets[PickerAxis.PickerY] = GetPickerTeachingPosition(PickerAxis.PickerY, positionArrayName + "[" + index + "]") + Config.Picker[index].AlignOffsetY;
            targets[GetPickerTAxis(index)] = ResolveTPosition(positionArrayName) + Config.Picker[index].AlignOffsetT;
            targets[GetPickerZAxis(index)] = ResolveZPosition(positionArrayName);
            return await MovePickerAxes(targets, bFine);
        }

        private double ResolveTPosition(string positionArrayName)
        {
            if (positionArrayName == "DieBottomPosition") return Recipe.PickerT0.BottomPosition;
            if (positionArrayName == "DieSidePosition") return Recipe.PickerT0.SidePosition;
            if (positionArrayName == "DiePlacePosition") return Recipe.PickerT0.PlacePosition;
            return Recipe.PickerT0.PickPosition;
        }

        private double ResolveZPosition(string positionArrayName)
        {
            if (positionArrayName == "DieBottomPosition") return Recipe.PickerZ0.BottomPosition;
            if (positionArrayName == "DieSidePosition") return Recipe.PickerZ0.SidePosition;
            if (positionArrayName == "DiePlacePosition") return Recipe.PickerZ0.PlacePosition;
            return Recipe.PickerZ0.PickPosition;
        }

        private bool IsPickerGroupInPosition(string positionName)
        {
            foreach (PickerAxis axis in axes.Keys)
            {
                if (!IsPickerAxisInTeachingPosition(axis, positionName))
                    return false;
            }
            return true;
        }

        private bool IsPickerInDiePosition(int pickerNo, string positionArrayName)
        {
            int index = NormalizePickerIndex(pickerNo, MaxPickerCount);
            BaseAxis x = GetAxis(PickerAxis.PickerX);
            BaseAxis y = GetAxis(PickerAxis.PickerY);
            BaseAxis t = GetAxis(GetPickerTAxis(index));
            BaseAxis z = GetAxis(GetPickerZAxis(index));
            return IsPickerAxisInPosition(PickerAxis.PickerX, GetPickerTeachingPosition(PickerAxis.PickerX, positionArrayName + "[" + index + "]") + Config.Picker[index].AlignOffsetX, x.Config.InPositionTolerance)
                && IsPickerAxisInPosition(PickerAxis.PickerY, GetPickerTeachingPosition(PickerAxis.PickerY, positionArrayName + "[" + index + "]") + Config.Picker[index].AlignOffsetY, y.Config.InPositionTolerance)
                && IsPickerAxisInPosition(GetPickerTAxis(index), ResolveTPosition(positionArrayName) + Config.Picker[index].AlignOffsetT, t.Config.InPositionTolerance)
                && IsPickerAxisInPosition(GetPickerZAxis(index), ResolveZPosition(positionArrayName), z.Config.InPositionTolerance);
        }

        private void TeachPickerGroup(string positionName)
        {
            foreach (PickerAxis axis in axes.Keys)
                SetPickerTeachingPosition(axis, positionName, GetAxis(axis).ActualPosition);
        }

        private void TeachPickerDiePosition(int pickerNo, string positionArrayName)
        {
            int index = NormalizePickerIndex(pickerNo, MaxPickerCount);
            SetIndexedPosition(GetPositionSet(PickerAxis.PickerX), positionArrayName, index, PickerX.ActualPosition);
            SetIndexedPosition(GetPositionSet(PickerAxis.PickerY), positionArrayName, index, PickerY.ActualPosition);
        }

        private void SetPickerTeachingPosition(PickerAxis axis, string positionName, double position)
        {
            PickerAxisPositionSet set = GetPositionSet(axis);
            if (positionName == "InputAvoidPosition") set.InputAvoidPosition = position;
            else if (positionName == "OutputAvoidPosition") set.OutputAvoidPosition = position;
            else if (positionName == "AvoidPosition") set.AvoidPosition = position;
            else if (positionName == "PickPosition") set.PickPosition = position;
            else if (positionName == "BottomPosition") set.BottomPosition = position;
            else if (positionName == "SidePosition") set.SidePosition = position;
            else if (positionName == "PlacePosition") set.PlacePosition = position;
            else if (positionName.StartsWith("DiePickPosition", StringComparison.OrdinalIgnoreCase)) set.DiePickPosition = SetIndexedPosition(set.DiePickPosition, ExtractIndex(positionName), position);
            else if (positionName.StartsWith("DieBottomPosition", StringComparison.OrdinalIgnoreCase)) set.DieBottomPosition = SetIndexedPosition(set.DieBottomPosition, ExtractIndex(positionName), position);
            else if (positionName.StartsWith("DieSidePosition", StringComparison.OrdinalIgnoreCase)) set.DieSidePosition = SetIndexedPosition(set.DieSidePosition, ExtractIndex(positionName), position);
            else if (positionName.StartsWith("DiePlacePosition", StringComparison.OrdinalIgnoreCase)) set.DiePlacePosition = SetIndexedPosition(set.DiePlacePosition, ExtractIndex(positionName), position);
        }

        private PickerAxisPositionSet GetPositionSet(PickerAxis axis)
        {
            Recipe.EnsurePositionObjects();
            if (axis == PickerAxis.PickerX) return Recipe.PickerX;
            if (axis == PickerAxis.PickerY) return Recipe.PickerY;
            if (axis == PickerAxis.PickerT0) return Recipe.PickerT0;
            if (axis == PickerAxis.PickerT1) return Recipe.PickerT1;
            if (axis == PickerAxis.PickerT2) return Recipe.PickerT2;
            if (axis == PickerAxis.PickerT3) return Recipe.PickerT3;
            if (axis == PickerAxis.PickerZ1) return Recipe.PickerZ1;
            if (axis == PickerAxis.PickerZ2) return Recipe.PickerZ2;
            if (axis == PickerAxis.PickerZ3) return Recipe.PickerZ3;
            return Recipe.PickerZ0;
        }

        private static void SetIndexedPosition(PickerAxisPositionSet set, string positionArrayName, int index, double value)
        {
            if (positionArrayName == "DiePickPosition") set.DiePickPosition = SetIndexedPosition(set.DiePickPosition, index, value);
            else if (positionArrayName == "DieBottomPosition") set.DieBottomPosition = SetIndexedPosition(set.DieBottomPosition, index, value);
            else if (positionArrayName == "DieSidePosition") set.DieSidePosition = SetIndexedPosition(set.DieSidePosition, index, value);
            else if (positionArrayName == "DiePlacePosition") set.DiePlacePosition = SetIndexedPosition(set.DiePlacePosition, index, value);
        }

        private static double[] SetIndexedPosition(double[] source, int index, double value)
        {
            if (source == null || source.Length <= index)
                Array.Resize(ref source, index + 1);
            source[index] = value;
            return source;
        }

        private static double GetIndexedPosition(double[] source, int index)
        {
            if (source == null || index < 0 || index >= source.Length)
                return 0.0;
            return source[index];
        }

        private static int ExtractIndex(string positionName)
        {
            int open = positionName.IndexOf('[');
            int close = positionName.IndexOf(']');
            int index;
            if (open >= 0 && close > open && int.TryParse(positionName.Substring(open + 1, close - open - 1), out index))
                return index;
            return 0;
        }

        private static int NormalizePickerIndex(int pickerNo, int maxCount)
        {
            if (pickerNo >= 1 && pickerNo <= maxCount)
                return pickerNo - 1;
            if (pickerNo >= 0 && pickerNo < maxCount)
                return pickerNo;
            throw new ArgumentOutOfRangeException("pickerNo", "Picker number is out of range.");
        }

        private static bool IsZAxis(PickerAxis axis)
        {
            return axis == PickerAxis.PickerZ0 || axis == PickerAxis.PickerZ1 || axis == PickerAxis.PickerZ2 || axis == PickerAxis.PickerZ3;
        }

        private void MoveZAxesToSafeFirst(Dictionary<PickerAxis, double> targets, bool bFine, List<Task<int>> tasks)
        {
            foreach (KeyValuePair<PickerAxis, double> pair in targets)
            {
                if (IsZAxis(pair.Key) && GetAxis(pair.Key).ActualPosition != pair.Value)
                    tasks.Add(MovePickerAxis(pair.Key, Recipe.PickerZ0.AvoidPosition, bFine));
            }
        }

        private int RaisePickerAlarm(string code, string message)
        {
            EventLogger.Write(EventKind.Alarm, "QMC", code, message);
            AlarmManager.Raise(AlarmSeverity.Error, code, Name, message);
            return -1;
        }

        public Task<int> MoveRearPickerAxis(PickerAxis axis, double targetPos, bool bFine = false)
        {
            return MovePickerAxis(axis, targetPos, bFine);
        }

        public Task<int> MoveRearPickerAxes(Dictionary<PickerAxis, double> targets, bool bFine = false)
        {
            return MovePickerAxes(targets, bFine);
        }

        public Task<int> MoveRearPickerAxisToTeachingPosition(PickerAxis axis, string positionName, bool bFine = false)
        {
            return MovePickerAxisToTeachingPosition(axis, positionName, bFine);
        }

        public Task<int> MoveToRearPickerAvoidPosition(bool bFine = false)
        {
            return MoveToPickerAvoidPosition(bFine);
        }

        public Task<int> MoveToRearPickerLoadPosition(bool bFine = false)
        {
            return MoveToPickerLoadPosition(bFine);
        }

        public Task<int> MoveToRearPickerUnloadPosition(bool bFine = false)
        {
            return MoveToPickerUnloadPosition(bFine);
        }

        public Task<int> MoveToRearPickerSafeRetreatPosition(bool bFine = false)
        {
            return MoveToPickerSafeRetreatPosition(bFine);
        }

        public Task<int> MoveToRearPickerDiePickPosition(int pickerNo, bool bFine = false)
        {
            return MoveToPickerDiePickPosition(pickerNo, bFine);
        }

        public Task<int> MoveToRearPickerDieProcessPosition(int pickerNo, bool bFine = false)
        {
            return MoveToPickerDieProcessPosition(pickerNo, bFine);
        }

        public Task<int> MoveToRearPickerDiePlacePosition(int pickerNo, bool bFine = false)
        {
            return MoveToPickerDiePlacePosition(pickerNo, bFine);
        }

        public Task<int> MoveRearPickerZToPickHeight(int pickerNo, bool bFine = false)
        {
            return MovePickerZToPickHeight(pickerNo, bFine);
        }

        public Task<int> MoveRearPickerZToSafeHeight(int pickerNo, bool bFine = false)
        {
            return MovePickerZToSafeHeight(pickerNo, bFine);
        }

        public Task<int> MoveRearPickerTToOffset(int pickerNo, bool bFine = false)
        {
            return MovePickerTToOffset(pickerNo, bFine);
        }

        public bool IsRearPickerAxisInPosition(PickerAxis axis, double targetPos, double tolerance)
        {
            return IsPickerAxisInPosition(axis, targetPos, tolerance);
        }

        public Task<bool> WaitRearPickerAxisMoveDone(PickerAxis axis, int timeoutMs)
        {
            return WaitPickerAxisMoveDone(axis, timeoutMs);
        }

        public Task<bool> WaitRearPickerAxesMoveDone(IEnumerable<PickerAxis> targetAxes, int timeoutMs)
        {
            return WaitPickerAxesMoveDone(targetAxes, timeoutMs);
        }

        public bool IsRearPickerAxisInTeachingPosition(PickerAxis axis, string positionName)
        {
            return IsPickerAxisInTeachingPosition(axis, positionName);
        }

        public Task<bool> WaitRearPickerAxisInTeachingPosition(PickerAxis axis, string positionName, int timeoutMs)
        {
            return WaitPickerAxisInTeachingPosition(axis, positionName, timeoutMs);
        }

        public bool IsRearPickerInAvoidPosition()
        {
            return IsPickerInAvoidPosition();
        }

        public bool IsRearPickerInLoadPosition()
        {
            return IsPickerInLoadPosition();
        }

        public bool IsRearPickerInUnloadPosition()
        {
            return IsPickerInUnloadPosition();
        }

        public bool IsRearPickerInDiePickPosition(int pickerNo)
        {
            return IsPickerInDiePickPosition(pickerNo);
        }

        public bool IsRearPickerInDieProcessPosition(int pickerNo)
        {
            return IsPickerInDieProcessPosition(pickerNo);
        }

        public bool IsRearPickerInDiePlacePosition(int pickerNo)
        {
            return IsPickerInDiePlacePosition(pickerNo);
        }

        public void TeachRearPickerAxisPosition(PickerAxis axis, string positionName)
        {
            TeachPickerAxisPosition(axis, positionName);
        }

        public void TeachRearPickerAvoidPositions()
        {
            TeachPickerAvoidPositions();
        }

        public void TeachRearPickerLoadPositions()
        {
            TeachPickerLoadPositions();
        }

        public void TeachRearPickerUnloadPositions()
        {
            TeachPickerUnloadPositions();
        }

        public void TeachRearPickerSafeRetreatPositions()
        {
            TeachPickerSafeRetreatPositions();
        }

        public void TeachRearPickerDiePickPosition(int pickerNo)
        {
            TeachPickerDiePickPosition(pickerNo);
        }

        public void TeachRearPickerDieProcessPosition(int pickerNo)
        {
            TeachPickerDieProcessPosition(pickerNo);
        }

        public void TeachRearPickerDiePlacePosition(int pickerNo)
        {
            TeachPickerDiePlacePosition(pickerNo);
        }

        public void TeachRearPickerOffsetPosition(int pickerNo)
        {
            TeachPickerOffsetPosition(pickerNo);
        }

        public double GetRearPickerTeachingPosition(PickerAxis axis, string positionName)
        {
            return GetPickerTeachingPosition(axis, positionName);
        }

        public bool ValidateRearPickerTeachingComplete()
        {
            return ValidatePickerTeachingComplete();
        }

        private static async Task<bool> WaitUntilAsync(Func<bool> condition, int timeoutMs)
        {
            DateTime start = DateTime.UtcNow;
            while ((DateTime.UtcNow - start).TotalMilliseconds < timeoutMs)
            {
                if (condition())
                    return true;
                await Task.Delay(10);
            }
            return condition();
        }
    }

}


