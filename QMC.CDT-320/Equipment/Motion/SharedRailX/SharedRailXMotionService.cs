using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using QMC.CDT320.Interlocks;
using QMC.Common.Alarms;
using QMC.Common.Motion;

namespace QMC.CDT320.Motion.SharedRailX
{
    public sealed class SharedRailXMotionService
    {
        private static readonly object _bottomBypassLogLock = new object();
        private static readonly Dictionary<string, DateTime> _bottomBypassLogUtc = new Dictionary<string, DateTime>();

        private readonly CDT320_Machine _machine;
        private readonly SharedRailXConfig _config;
        private readonly SharedRailXCollisionValidator _validator;

        public SharedRailXMotionService(CDT320_Machine machine)
            : this(machine, SharedRailXConfig.CreateDefault())
        {
        }

        public SharedRailXMotionService(CDT320_Machine machine, SharedRailXConfig config)
        {
            _machine = machine ?? throw new ArgumentNullException(nameof(machine));
            _config = config ?? SharedRailXConfig.CreateDefault();
            _validator = new SharedRailXCollisionValidator(_config);
        }

        public SharedRailXConfig Config { get { return _config; } }

        public IReadOnlyList<SharedRailXAxisSetting> GetAxisSettings()
        {
            var list = new List<SharedRailXAxisSetting>();
            Add(list, SharedRailXAxis.InputVisionX, _machine.InputStageUnit != null ? _machine.InputStageUnit.CameraX : null);
            Add(list, SharedRailXAxis.FrontPickerX, _machine.PickerFrontUnit != null ? _machine.PickerFrontUnit.PickerX : null);
            Add(list, SharedRailXAxis.RearPickerX, _machine.PickerRearUnit != null ? _machine.PickerRearUnit.PickerX : null);
            Add(list, SharedRailXAxis.OutputVisionX, _machine.OutputStageUnit != null ? _machine.OutputStageUnit.OutputCameraX : null);
            return list;
        }

        public bool IsSharedRailAxis(BaseAxis axis)
        {
            return TryResolve(axis, out _);
        }

        public bool TryResolve(BaseAxis axis, out SharedRailXAxis railAxis)
        {
            railAxis = SharedRailXAxis.InputVisionX;
            if (axis == null)
                return false;

            foreach (SharedRailXAxisSetting setting in GetAxisSettings())
            {
                if (ReferenceEquals(setting.Axis, axis))
                {
                    railAxis = setting.RailAxis;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 두 공유 레일 축의 지정 위치 조합이 페어 간격식으로 SafetyDistance를 만족하는지 판정한다.
        /// 인터락 제3 분기("간격 충족 시 진입 허용" — 사용자 승인 2026-07-24)에서 사용한다.
        /// R5: 요구 거리는 페어 SafetyDistance(없으면 축 Max)만 사용하며 RetreatExtra는 절대 더하지 않는다.
        /// 페어 미설정/축 미해석 시 false(fail-closed).
        /// </summary>
        public bool IsPairClearanceSatisfied(
            BaseAxis axisA,
            double axisAPosition,
            BaseAxis axisB,
            double axisBPosition,
            out string detail)
        {
            detail = string.Empty;

            SharedRailXAxis railA;
            SharedRailXAxis railB;
            if (!TryResolve(axisA, out railA) || !TryResolve(axisB, out railB))
            {
                detail = "공유 레일 축을 확인할 수 없습니다.";
                return false;
            }

            SharedRailXAxisPair pair;
            if (_config == null || !_config.TryGetCollisionPair(railA, railB, out pair) || !pair.HasClearanceRule)
            {
                detail = "충돌 Pair 설정이 없습니다. pair=" + railA + "<->" + railB;
                return false;
            }

            double aPos = pair.AxisA == railA ? axisAPosition : axisBPosition;
            double bPos = pair.AxisA == railA ? axisBPosition : axisAPosition;
            double clearance = CalculatePairClearance(
                pair.HomeClearance,
                pair.AxisATowardSign,
                aPos,
                pair.AxisBTowardSign,
                bPos);

            double required = pair.SafetyDistance.HasValue
                ? pair.SafetyDistance.Value
                : ResolvePairFallbackSafetyDistance(railA, railB);

            detail = "pair=" + railA + "<->" + railB +
                     ", a=" + axisAPosition.ToString("F6") +
                     ", b=" + axisBPosition.ToString("F6") +
                     ", clearance=" + clearance.ToString("F6") +
                     ", required=" + required.ToString("F6");
            return clearance + 0.000001 >= required;
        }

        private double ResolvePairFallbackSafetyDistance(SharedRailXAxis railA, SharedRailXAxis railB)
        {
            double safetyA = _config != null ? _config.DefaultSafetyDistance : 10.0;
            double safetyB = safetyA;
            foreach (SharedRailXAxisSetting setting in GetAxisSettings())
            {
                if (setting == null)
                    continue;
                if (setting.RailAxis == railA)
                    safetyA = setting.SafetyDistance;
                else if (setting.RailAxis == railB)
                    safetyB = setting.SafetyDistance;
            }

            return Math.Max(safetyA, safetyB);
        }

        public bool VerifySingleAxisMove(BaseAxis axis, double targetPosition, out string reason)
        {
            reason = string.Empty;
            SharedRailXAxis railAxis;
            if (!TryResolve(axis, out railAxis))
                return true;

            var plan = new SharedRailXMovePlan
            {
                Name = "SingleAxisGuard",
                Velocity = axis.Config != null ? axis.Config.DefaultVelocity : 0.0
            };
            plan.Add(railAxis, targetPosition);
            IReadOnlyList<SharedRailXAxisSetting> settings = GetAxisSettings();
            SharedRailXConfig effectiveConfig = ResolveEffectiveConfig(plan, settings);
            SharedRailXValidationResult result = new SharedRailXCollisionValidator(effectiveConfig).Validate(settings, plan);
            reason = result.Reason;
            return result.Allowed;
        }

        public bool TryResolveNearestVisionRetreatTarget(
            BaseAxis visionAxis,
            double fullAvoidPosition,
            double maximumPickerEntryPosition,
            IDictionary<SharedRailXAxis, IList<double>> plannedAxisPositions,
            double additionalClearance,
            out double retreatTarget,
            out string detail)
        {
            retreatTarget = fullAvoidPosition;
            detail = string.Empty;

            SharedRailXAxis visionRailAxis;
            if (!TryResolve(visionAxis, out visionRailAxis) ||
                (visionRailAxis != SharedRailXAxis.InputVisionX &&
                 visionRailAxis != SharedRailXAxis.OutputVisionX))
            {
                detail = "VisionX 공유 레일 축을 확인할 수 없어 전체 Avoid를 사용합니다.";
                return false;
            }

            IReadOnlyList<SharedRailXAxisSetting> settings = GetAxisSettings();
            var settingMap = settings
                .Where(x => x != null && x.Axis != null)
                .ToDictionary(x => x.RailAxis);
            var obstaclePositions = new Dictionary<SharedRailXAxis, List<double>>();

            if (_config == null || _config.CollisionPairs == null || _config.CollisionPairs.Count == 0)
            {
                detail = "공유 레일 충돌 Pair 설정이 없어 전체 Avoid를 사용합니다. axis=" + visionRailAxis;
                return false;
            }

            foreach (SharedRailXAxisPair pair in _config.CollisionPairs)
            {
                if (pair.AxisA != visionRailAxis && pair.AxisB != visionRailAxis)
                    continue;

                SharedRailXAxis otherAxis = pair.AxisA == visionRailAxis ? pair.AxisB : pair.AxisA;
                SharedRailXAxisSetting otherSetting;
                if (!settingMap.TryGetValue(otherAxis, out otherSetting))
                {
                    detail = "공유 레일 상대 축을 찾을 수 없어 전체 Avoid를 사용합니다. axis=" + otherAxis;
                    return false;
                }

                List<double> positions;
                if (!obstaclePositions.TryGetValue(otherAxis, out positions))
                {
                    positions = new List<double>();
                    obstaclePositions[otherAxis] = positions;
                }

                positions.Add(otherSetting.Axis.ActualPosition);
                positions.Add(otherSetting.Axis.CommandPosition);

                IList<double> planned;
                if (plannedAxisPositions != null &&
                    plannedAxisPositions.TryGetValue(otherAxis, out planned) &&
                    planned != null)
                {
                    for (int i = 0; i < planned.Count; i++)
                        positions.Add(planned[i]);
                }
            }

            if (obstaclePositions.Count == 0)
            {
                detail = "VisionX-PickerX 충돌 Pair가 없어 전체 Avoid를 사용합니다. axis=" + visionRailAxis;
                return false;
            }

            double preferred = Math.Min(visionAxis.ActualPosition, maximumPickerEntryPosition);
            string preferredReason;
            if (IsVisionRetreatTargetSafe(
                visionRailAxis,
                preferred,
                settingMap,
                obstaclePositions,
                additionalClearance,
                out preferredReason))
            {
                retreatTarget = preferred;
                detail = "현재 위치에서 가장 가까운 안전 위치를 사용합니다. target=" + retreatTarget.ToString("F6") +
                         ", fullAvoid=" + fullAvoidPosition.ToString("F6") +
                         ", entryLimit=" + maximumPickerEntryPosition.ToString("F6") +
                         ", " + preferredReason;
                return true;
            }

            string avoidReason;
            if (!IsVisionRetreatTargetSafe(
                visionRailAxis,
                fullAvoidPosition,
                settingMap,
                obstaclePositions,
                additionalClearance,
                out avoidReason))
            {
                detail = "전체 Avoid도 예정 PickerX 경로 안전거리를 만족하지 못합니다. preferredBlocked=" +
                         preferredReason + ", avoidBlocked=" + avoidReason;
                return false;
            }

            double allowedRatio = 0.0;
            double blockedRatio = 1.0;
            string boundaryReason = avoidReason;
            for (int i = 0; i < 48; i++)
            {
                double ratio = (allowedRatio + blockedRatio) * 0.5;
                double probe = fullAvoidPosition + ((preferred - fullAvoidPosition) * ratio);
                string probeReason;
                if (IsVisionRetreatTargetSafe(
                    visionRailAxis,
                    probe,
                    settingMap,
                    obstaclePositions,
                    additionalClearance,
                    out probeReason))
                {
                    allowedRatio = ratio;
                    boundaryReason = probeReason;
                }
                else
                {
                    blockedRatio = ratio;
                }
            }

            retreatTarget = fullAvoidPosition + ((preferred - fullAvoidPosition) * allowedRatio);
            if (retreatTarget > maximumPickerEntryPosition + 0.000001)
            {
                detail = "계산된 최소 회피 위치가 Picker 진입 허용 상한보다 커서 전체 Avoid를 사용합니다. " +
                         "calculated=" + retreatTarget.ToString("F6") +
                         ", entryLimit=" + maximumPickerEntryPosition.ToString("F6") +
                         ", fullAvoid=" + fullAvoidPosition.ToString("F6") +
                         ", " + boundaryReason;
                retreatTarget = fullAvoidPosition;
                return false;
            }

            detail = "예정 PickerX 경로 기준 최소 회피 위치를 계산했습니다. target=" + retreatTarget.ToString("F6") +
                     ", preferred=" + preferred.ToString("F6") +
                     ", fullAvoid=" + fullAvoidPosition.ToString("F6") +
                     ", additionalClearance=" + Math.Max(0.0, additionalClearance).ToString("F3") +
                     ", " + boundaryReason;
            return true;
        }

        /// <summary>
        /// 부호 인지(direction-aware) 최소 회피 계산의 순수 코어(닫힌 수식) — 하네스 검증용 public static.
        /// 제약: 비전부호×비전위치 ≤ bound, bound_i = HomeClearance − 피커부호×장애물_i − (Safety+Extra).
        /// boundMin = Min(bound_i), retreatTarget = 비전부호가 +면 boundMin, −면 −boundMin.
        /// </summary>
        public static bool TryComputeMinimalVisionRetreatTarget(
            int visionTowardSign,
            int pickerTowardSign,
            double homeClearance,
            double safetyDistance,
            double extraClearance,
            IList<double> obstaclePositions,
            out double retreatTarget,
            out double boundMin)
        {
            retreatTarget = 0.0;
            boundMin = double.MaxValue;
            if (visionTowardSign == 0 || pickerTowardSign == 0 ||
                obstaclePositions == null || obstaclePositions.Count == 0)
                return false;

            double required = safetyDistance + Math.Max(0.0, extraClearance);
            for (int i = 0; i < obstaclePositions.Count; i++)
            {
                double bound = homeClearance - (pickerTowardSign * obstaclePositions[i]) - required;
                if (bound < boundMin)
                    boundMin = bound;
            }

            retreatTarget = visionTowardSign > 0 ? boundMin : -boundMin;
            return true;
        }

        /// <summary>
        /// R2: 부호 인지 최소 회피 목표 계산(닫힌 수식). 촬영이 끝난 비전 축이 전체 Avoid까지 가지 않고
        /// "피커 최대 진입 위치 + (페어 SafetyDistance + extraClearance)"까지만 회피하도록 목표를 구한다.
        /// 기존 TryResolveNearestVisionRetreatTarget(-0.1 상한·이진 탐색·InputVisionX 전제)은 비Conti 경로
        /// 보존을 위해 무수정으로 두고 별도 신설했다 — OutputVisionX(회피 방향 +→− 반대)에서도 동작한다.
        /// R5(절대 금지): extraClearance는 이 "회피 목표 계산"에만 존재한다. 피커/비전 진입 허용 인터락
        /// (VerifySingleAxisMove, SharedRailXCollisionValidator, MotionGuardRuntime 등)의 요구거리는
        /// 기존 SafetyDistance 그대로이며 Extra를 더하지 않는다.
        /// </summary>
        public bool TryResolveMinimalVisionRetreatTarget(
            BaseAxis visionAxis,
            double fullAvoidPosition,
            IDictionary<SharedRailXAxis, IList<double>> plannedAxisPositions,
            double extraClearance,
            out double retreatTarget,
            out string detail)
        {
            retreatTarget = fullAvoidPosition;
            detail = string.Empty;

            SharedRailXAxis visionRailAxis;
            if (!TryResolve(visionAxis, out visionRailAxis) ||
                (visionRailAxis != SharedRailXAxis.InputVisionX &&
                 visionRailAxis != SharedRailXAxis.OutputVisionX))
            {
                detail = "VisionX 공유 레일 축을 확인할 수 없어 전체 Avoid를 사용합니다.";
                return false;
            }

            IReadOnlyList<SharedRailXAxisSetting> settings = GetAxisSettings();
            var settingMap = settings
                .Where(x => x != null && x.Axis != null)
                .ToDictionary(x => x.RailAxis);
            var obstaclePositions = new Dictionary<SharedRailXAxis, List<double>>();

            if (_config == null || _config.CollisionPairs == null || _config.CollisionPairs.Count == 0)
            {
                detail = "공유 레일 충돌 Pair 설정이 없어 전체 Avoid를 사용합니다. axis=" + visionRailAxis;
                return false;
            }

            SharedRailXAxisSetting visionSetting;
            if (!settingMap.TryGetValue(visionRailAxis, out visionSetting))
            {
                detail = "공유 레일 비전 축 설정을 찾을 수 없어 전체 Avoid를 사용합니다. axis=" + visionRailAxis;
                return false;
            }

            // 기존 함수(121~152행)와 동일한 장애물 수집: 상대 축 Actual/Command + planned 목록.
            foreach (SharedRailXAxisPair pair in _config.CollisionPairs)
            {
                if (pair.AxisA != visionRailAxis && pair.AxisB != visionRailAxis)
                    continue;

                SharedRailXAxis otherAxis = pair.AxisA == visionRailAxis ? pair.AxisB : pair.AxisA;
                SharedRailXAxisSetting otherSetting;
                if (!settingMap.TryGetValue(otherAxis, out otherSetting))
                {
                    detail = "공유 레일 상대 축을 찾을 수 없어 전체 Avoid를 사용합니다. axis=" + otherAxis;
                    return false;
                }

                List<double> positions;
                if (!obstaclePositions.TryGetValue(otherAxis, out positions))
                {
                    positions = new List<double>();
                    obstaclePositions[otherAxis] = positions;
                }

                positions.Add(otherSetting.Axis.ActualPosition);
                positions.Add(otherSetting.Axis.CommandPosition);

                IList<double> planned;
                if (plannedAxisPositions != null &&
                    plannedAxisPositions.TryGetValue(otherAxis, out planned) &&
                    planned != null)
                {
                    for (int i = 0; i < planned.Count; i++)
                        positions.Add(planned[i]);
                }
            }

            if (obstaclePositions.Count == 0)
            {
                detail = "VisionX-PickerX 충돌 Pair가 없어 전체 Avoid를 사용합니다. axis=" + visionRailAxis;
                return false;
            }

            // 닫힌 수식: 각 제약을 비전부호 곱 형태로 정규화해 위치 공간의 상/하한으로 누적한다.
            //   비전부호 +1 페어: pos ≤ bound(상한),  비전부호 −1 페어: pos ≥ −bound(하한).
            // 페어 간 부호가 달라도 상/하한 구간 [lower, upper]로 동일하게 처리하고,
            // 구간이 비면(상충) 전체 Avoid 폴백. 회피 방향(canonical)은 첫 유효 페어 부호를 따른다.
            int canonicalVisionSign = 0;
            double upperBound = double.MaxValue;   // pos ≤ upperBound
            double lowerBound = double.MinValue;   // pos ≥ lowerBound
            double obstacleMin = double.MaxValue;
            double obstacleMax = double.MinValue;
            double requiredMax = 0.0;
            foreach (SharedRailXAxisPair pair in _config.CollisionPairs)
            {
                if (pair.AxisA != visionRailAxis && pair.AxisB != visionRailAxis)
                    continue;
                if (!pair.HasClearanceRule)
                {
                    detail = "충돌 Pair 안전거리 수식이 없어 전체 Avoid를 사용합니다. pair=" +
                             pair.AxisA + "<->" + pair.AxisB;
                    return false;
                }

                int visionSign = pair.AxisA == visionRailAxis ? pair.AxisATowardSign : pair.AxisBTowardSign;
                int pickerSign = pair.AxisA == visionRailAxis ? pair.AxisBTowardSign : pair.AxisATowardSign;
                if (canonicalVisionSign == 0)
                    canonicalVisionSign = visionSign;

                SharedRailXAxis otherAxis = pair.AxisA == visionRailAxis ? pair.AxisB : pair.AxisA;
                SharedRailXAxisSetting otherSetting = settingMap[otherAxis];
                List<double> positions = obstaclePositions[otherAxis];
                double safety = pair.SafetyDistance.HasValue
                    ? pair.SafetyDistance.Value
                    : Math.Max(visionSetting.SafetyDistance, otherSetting.SafetyDistance);

                double pairTarget;
                double pairBoundMin;
                if (!TryComputeMinimalVisionRetreatTarget(
                    visionSign,
                    pickerSign,
                    pair.HomeClearance,
                    safety,
                    extraClearance,
                    positions,
                    out pairTarget,
                    out pairBoundMin))
                    continue;

                // 정규화: visionSign×pos ≤ pairBoundMin → 상한 또는 하한으로 반영.
                if (visionSign > 0)
                {
                    if (pairBoundMin < upperBound)
                        upperBound = pairBoundMin;
                }
                else
                {
                    if (-pairBoundMin > lowerBound)
                        lowerBound = -pairBoundMin;
                }

                double required = safety + Math.Max(0.0, extraClearance);
                if (required > requiredMax)
                    requiredMax = required;
                for (int i = 0; i < positions.Count; i++)
                {
                    if (positions[i] < obstacleMin)
                        obstacleMin = positions[i];
                    if (positions[i] > obstacleMax)
                        obstacleMax = positions[i];
                }
            }

            if (canonicalVisionSign == 0 ||
                (upperBound == double.MaxValue && lowerBound == double.MinValue))
            {
                detail = "VisionX-PickerX 충돌 Pair가 없어 전체 Avoid를 사용합니다. axis=" + visionRailAxis;
                return false;
            }

            // 구간 상충(혼합 부호 제약이 서로 배타적)이면 안전하게 전체 Avoid 폴백.
            if (lowerBound > upperBound + 0.000001)
            {
                detail = "페어 제약 구간이 상충하여 전체 Avoid를 사용합니다. lower=" + lowerBound.ToString("F6") +
                         ", upper=" + upperBound.ToString("F6") + ", axis=" + visionRailAxis;
                retreatTarget = fullAvoidPosition;
                return true;
            }

            // 최소 회피 목표 = 회피 방향 기준으로 진입측에 가장 가까운 허용 경계.
            //   canonical +1(회피=pos 감소): 목표 = upperBound,  canonical −1(회피=pos 증가): 목표 = lowerBound.
            // (구간 [lower, upper]는 위에서 상충 검사 완료 — 목표는 항상 구간 안의 경계값이다.)
            double computed = canonicalVisionSign > 0 ? upperBound : lowerBound;
            double boundMin = canonicalVisionSign > 0 ? upperBound : -lowerBound;

            // 전체 Avoid보다 더 물러나야 하는 값이면(회피 방향으로 fullAvoid 초과) fullAvoid 사용.
            // s-공간(s = 비전부호×위치)에서 회피는 s 감소 방향이며 제약은 s ≤ boundMin.
            bool clampedToFullAvoid = false;
            double sFullAvoid = canonicalVisionSign * fullAvoidPosition;
            if (boundMin < sFullAvoid)
            {
                retreatTarget = fullAvoidPosition;
                clampedToFullAvoid = true;
            }
            else
            {
                retreatTarget = computed;
            }

            // 수정(사용자 지시 2026-07-24): 비전이 이미 목표보다 회피 방향으로 더 물러나 정지해 있으면
            // 진입 방향으로 전진시키지 않고 현재 위치를 유지한다 (호출부의 이동 생략 관례로 무이동).
            bool heldAtCurrent = false;
            if (!clampedToFullAvoid && !visionAxis.IsMoving)
            {
                double sActual = canonicalVisionSign * visionAxis.ActualPosition;
                double sTarget = canonicalVisionSign * retreatTarget;
                if (sActual <= sTarget + 0.000001)
                {
                    retreatTarget = visionAxis.ActualPosition;
                    heldAtCurrent = true;
                }
            }

            // 소프트리밋 클램프 (리밋 밖 목표 방지).
            bool clampedToSoftLimit = false;
            if (!clampedToFullAvoid &&
                visionAxis.Setup != null && visionAxis.Setup.SoftLimitEnabled)
            {
                if (retreatTarget > visionAxis.Setup.SoftLimitPlus)
                {
                    retreatTarget = visionAxis.Setup.SoftLimitPlus;
                    clampedToSoftLimit = true;
                }
                else if (retreatTarget < visionAxis.Setup.SoftLimitMinus)
                {
                    retreatTarget = visionAxis.Setup.SoftLimitMinus;
                    clampedToSoftLimit = true;
                }
            }

            // belt-and-braces: 기존 안전 판정(IsVisionRetreatTargetSafe)에 Extra를 additionalClearance로
            // 넣어 재검증 — 불일치하면 전체 Avoid 폴백 + 로그. (이 재검증은 회피 "목표" 검증이며,
            // 피커 진입 인터락과 무관하다 — R5.)
            if (!clampedToFullAvoid)
            {
                string safeReason;
                if (!IsVisionRetreatTargetSafe(
                    visionRailAxis,
                    retreatTarget,
                    settingMap,
                    obstaclePositions,
                    Math.Max(0.0, extraClearance),
                    out safeReason))
                {
                    QMC.Common.Log.Write("SharedRailX",
                        "최소 회피 재검증 불일치로 전체 Avoid를 사용합니다. axis=" + visionRailAxis +
                        ", calculated=" + retreatTarget.ToString("F6") +
                        ", boundMin=" + boundMin.ToString("F6") +
                        ", softLimitClamped=" + clampedToSoftLimit +
                        ", " + safeReason);
                    detail = "최소 회피 재검증 불일치로 전체 Avoid를 사용합니다. calculated=" +
                             retreatTarget.ToString("F6") + ", " + safeReason;
                    retreatTarget = fullAvoidPosition;
                    return true;
                }
            }

            detail = "부호 인지 최소 회피 위치를 계산했습니다. target=" + retreatTarget.ToString("F6") +
                     ", boundMin=" + boundMin.ToString("F6") +
                     ", visionSign=" + canonicalVisionSign +
                     ", obstacleMin=" + obstacleMin.ToString("F6") +
                     ", obstacleMax=" + obstacleMax.ToString("F6") +
                     ", requiredMax=" + requiredMax.ToString("F6") +
                     ", extra=" + Math.Max(0.0, extraClearance).ToString("F3") +
                     ", fullAvoid=" + fullAvoidPosition.ToString("F6") +
                     (clampedToFullAvoid ? ", clamp=fullAvoid" : clampedToSoftLimit ? ", clamp=softLimit" : "") +
                     (heldAtCurrent ? ", hold=current(전진 금지 - B안)" : "");
            return true;
        }

        private bool IsVisionRetreatTargetSafe(
            SharedRailXAxis visionRailAxis,
            double visionPosition,
            IDictionary<SharedRailXAxis, SharedRailXAxisSetting> settingMap,
            IDictionary<SharedRailXAxis, List<double>> obstaclePositions,
            double additionalClearance,
            out string reason)
        {
            reason = "clear";
            foreach (SharedRailXAxisPair pair in _config.CollisionPairs)
            {
                if (pair.AxisA != visionRailAxis && pair.AxisB != visionRailAxis)
                    continue;
                if (!pair.HasClearanceRule)
                {
                    reason = "충돌 Pair 안전거리 수식이 없습니다. pair=" + pair.AxisA + "<->" + pair.AxisB;
                    return false;
                }

                SharedRailXAxis otherAxis = pair.AxisA == visionRailAxis ? pair.AxisB : pair.AxisA;
                List<double> positions;
                SharedRailXAxisSetting visionSetting;
                SharedRailXAxisSetting otherSetting;
                if (!obstaclePositions.TryGetValue(otherAxis, out positions) || positions.Count == 0 ||
                    !settingMap.TryGetValue(visionRailAxis, out visionSetting) ||
                    !settingMap.TryGetValue(otherAxis, out otherSetting))
                {
                    reason = "충돌 Pair 축 상태가 없습니다. pair=" + pair.AxisA + "<->" + pair.AxisB;
                    return false;
                }

                double required = (pair.SafetyDistance.HasValue
                    ? pair.SafetyDistance.Value
                    : Math.Max(visionSetting.SafetyDistance, otherSetting.SafetyDistance)) +
                    Math.Max(0.0, additionalClearance);

                for (int i = 0; i < positions.Count; i++)
                {
                    double axisA = pair.AxisA == visionRailAxis ? visionPosition : positions[i];
                    double axisB = pair.AxisB == visionRailAxis ? visionPosition : positions[i];
                    double clearance = CalculatePairClearance(
                        pair.HomeClearance,
                        pair.AxisATowardSign,
                        axisA,
                        pair.AxisBTowardSign,
                        axisB);
                    if (clearance + 0.000001 < required)
                    {
                        reason = "안전거리 부족. pair=" + pair.AxisA + "<->" + pair.AxisB +
                                 ", vision=" + visionPosition.ToString("F6") +
                                 ", other=" + positions[i].ToString("F6") +
                                 ", clearance=" + clearance.ToString("F6") +
                                 ", required=" + required.ToString("F6");
                        return false;
                    }
                }
            }

            return true;
        }

        public bool VerifyJogMove(BaseAxis axis, int direction, out string reason)
        {
            reason = string.Empty;
            SharedRailXAxis railAxis;
            if (!TryResolve(axis, out railAxis))
                return true;

            SharedRailXValidationResult result = ValidateJogCurrentDistance(GetAxisSettings(), railAxis, direction, false);
            reason = result.Reason;
            return result.Allowed;
        }

        public bool VerifyJogCurrentDistance(BaseAxis axis, int direction, out string reason)
        {
            reason = string.Empty;
            SharedRailXAxis railAxis;
            if (!TryResolve(axis, out railAxis))
                return true;

            SharedRailXValidationResult result = ValidateJogCurrentDistance(GetAxisSettings(), railAxis, direction, true);
            reason = result.Reason;
            return result.Allowed;
        }

        public async Task<int> MoveAsync(SharedRailXMovePlan plan)
        {
            if (plan == null)
                return RaiseBlocked("SharedRailX move plan is null.");

            IReadOnlyList<SharedRailXAxisSetting> settings = GetAxisSettings();
            SharedRailXValidationResult motionGuard = VerifyMotionGuardTargets(settings, plan);
            if (!motionGuard.Allowed)
                return RaiseBlocked(motionGuard.Reason);

            SharedRailXConfig effectiveConfig = ResolveEffectiveConfig(plan, settings);
            SharedRailXValidationResult validation = new SharedRailXCollisionValidator(effectiveConfig).Validate(settings, plan);
            if (!validation.Allowed)
                return RaiseBlocked(validation.Reason);

            List<SharedRailXTarget> targets = plan.Targets.ToList();
            if (targets.Count == 0)
                return RaiseBlocked("SharedRailX move target is empty.");

            SharedRailXAutoMoveGuard moveGuard = StartAutoMoveGuard(plan, targets, settings, effectiveConfig);
            try
            {
                int result = await DispatchMoveAsync(plan, targets, settings).ConfigureAwait(false);
                if (moveGuard != null && moveGuard.Blocked)
                    return -11;

                return result;
            }
            finally
            {
                if (moveGuard != null)
                    moveGuard.Dispose();
            }
        }

        private SharedRailXValidationResult VerifyMotionGuardTargets(
            IReadOnlyList<SharedRailXAxisSetting> settings,
            SharedRailXMovePlan plan)
        {
            if (settings == null)
                return SharedRailXValidationResult.Block("SharedRailX settings are empty.");
            if (plan == null || plan.Targets == null)
                return SharedRailXValidationResult.Block("SharedRailX move plan is null.");

            SharedRailXValidationResult pickerFacing = VerifyFrontRearPickerFacingYTargets(plan);
            if (!pickerFacing.Allowed)
                return pickerFacing;

            Dictionary<SharedRailXAxis, SharedRailXAxisSetting> settingMap =
                settings.Where(x => x != null && x.Axis != null).ToDictionary(x => x.RailAxis);

            foreach (SharedRailXTarget target in plan.Targets)
            {
                SharedRailXAxisSetting setting;
                if (!settingMap.TryGetValue(target.Axis, out setting) || setting == null || setting.Axis == null)
                    return SharedRailXValidationResult.Block("SharedRailX target axis is not mapped. axis=" + target.Axis);

                string reason;
                if (!MotionGuardRuntime.VerifyAxisMoveWithoutSharedRailX(setting.Axis, target.TargetPosition, out reason))
                    return SharedRailXValidationResult.Block(reason);
            }

            return SharedRailXValidationResult.Allow();
        }

        private SharedRailXValidationResult VerifyFrontRearPickerFacingYTargets(SharedRailXMovePlan plan)
        {
            try
            {
                if (plan == null || plan.Targets == null)
                    return SharedRailXValidationResult.Allow();

                double frontTarget;
                double rearTarget;
                bool hasFrontTarget = plan.TryGetTarget(SharedRailXAxis.FrontPickerX, out frontTarget);
                bool hasRearTarget = plan.TryGetTarget(SharedRailXAxis.RearPickerX, out rearTarget);
                if (!hasFrontTarget && !hasRearTarget)
                    return SharedRailXValidationResult.Allow();

                string detail;
                // 현재 기준: SharedRailX 그룹 이동은 Front/Rear PickerX 목표까지 포함해서 양쪽 PickerY 돌출 충돌을 먼저 차단한다.
                if (PickerZoneInterlockRules.CanMovePickerXPairByFacingYInterlock(
                    _machine,
                    hasFrontTarget ? (double?)frontTarget : null,
                    hasRearTarget ? (double?)rearTarget : null,
                    plan.Name,
                    out detail))
                {
                    return SharedRailXValidationResult.Allow();
                }

                return SharedRailXValidationResult.Block(
                    "SharedRailX PickerX group move blocked. " + detail);
            }
            catch (Exception ex)
            {
                return SharedRailXValidationResult.Block(
                    "SharedRailX PickerX group facing-Y check exception. error=" + ex.Message);
            }
            finally
            {
            }
        }

        public Task<int> MoveAsync(SharedRailXAxis axis, double targetPosition, double velocity)
        {
            return MoveAsync(SharedRailXMovePlan.Create(axis.ToString(), velocity)
                .Add(axis, targetPosition));
        }

        public Task<int> MoveInputVisionAsync(double targetPosition, double velocity)
        {
            return MoveAsync(SharedRailXAxis.InputVisionX, targetPosition, velocity);
        }

        public Task<int> MoveOutputVisionAsync(double targetPosition, double velocity)
        {
            return MoveAsync(SharedRailXAxis.OutputVisionX, targetPosition, velocity);
        }

        public Task<int> MoveFrontPickerAsync(double targetPosition, double velocity)
        {
            return MoveAsync(SharedRailXAxis.FrontPickerX, targetPosition, velocity);
        }

        public Task<int> MoveRearPickerAsync(double targetPosition, double velocity)
        {
            return MoveAsync(SharedRailXAxis.RearPickerX, targetPosition, velocity);
        }

        public Task<int> MoveWaferVisionAndFrontPickerAsync(
            double waferVisionTarget,
            double frontPickerTarget,
            double velocity)
        {
            return MoveAsync(SharedRailXMovePlan.Create("InputVisionX+FrontPickerX", velocity)
                .Add(SharedRailXAxis.InputVisionX, waferVisionTarget)
                .Add(SharedRailXAxis.FrontPickerX, frontPickerTarget));
        }

        public Task<int> MoveWaferVisionAndRearPickerAsync(
            double waferVisionTarget,
            double rearPickerTarget,
            double velocity)
        {
            return MoveAsync(SharedRailXMovePlan.Create("InputVisionX+RearPickerX", velocity)
                .Add(SharedRailXAxis.InputVisionX, waferVisionTarget)
                .Add(SharedRailXAxis.RearPickerX, rearPickerTarget));
        }

        public Task<int> MoveBinVisionAndFrontPickerAsync(
            double binVisionTarget,
            double frontPickerTarget,
            double velocity)
        {
            return MoveAsync(SharedRailXMovePlan.Create("OutputVisionX+FrontPickerX", velocity)
                .Add(SharedRailXAxis.OutputVisionX, binVisionTarget)
                .Add(SharedRailXAxis.FrontPickerX, frontPickerTarget));
        }

        public Task<int> MoveBinVisionAndRearPickerAsync(
            double binVisionTarget,
            double rearPickerTarget,
            double velocity)
        {
            return MoveAsync(SharedRailXMovePlan.Create("OutputVisionX+RearPickerX", velocity)
                .Add(SharedRailXAxis.OutputVisionX, binVisionTarget)
                .Add(SharedRailXAxis.RearPickerX, rearPickerTarget));
        }

        public Task<int> MoveFrontAndRearPickerAsync(
            double frontPickerTarget,
            double rearPickerTarget,
            double velocity)
        {
            return MoveAsync(SharedRailXMovePlan.Create("FrontPickerX+RearPickerX", velocity)
                .Add(SharedRailXAxis.FrontPickerX, frontPickerTarget)
                .Add(SharedRailXAxis.RearPickerX, rearPickerTarget));
        }

        public Task<int> MoveAllAsync(
            double waferVisionTarget,
            double binVisionTarget,
            double frontPickerTarget,
            double rearPickerTarget,
            double velocity)
        {
            return MoveAsync(SharedRailXMovePlan.Create("SharedRailX-All", velocity)
                .Add(SharedRailXAxis.InputVisionX, waferVisionTarget)
                .Add(SharedRailXAxis.OutputVisionX, binVisionTarget)
                .Add(SharedRailXAxis.FrontPickerX, frontPickerTarget)
                .Add(SharedRailXAxis.RearPickerX, rearPickerTarget));
        }

        private Task<int> DispatchMoveAsync(
            SharedRailXMovePlan plan,
            IReadOnlyList<SharedRailXTarget> targets,
            IReadOnlyList<SharedRailXAxisSetting> settings)
        {
            // SharedRailX is intentionally commanded by individual absolute moves.
            // AXM.MoveMultiplePosition is not used until its equipment behavior is fully verified.
            return MoveSoftwareParallelAsync(plan, targets, settings);
        }

        private async Task<int> MoveSoftwareParallelAsync(
            SharedRailXMovePlan plan,
            IReadOnlyList<SharedRailXTarget> targets,
            IReadOnlyList<SharedRailXAxisSetting> settings)
        {
            var tasks = new List<Task<int>>();
            Dictionary<SharedRailXAxis, SharedRailXAxisSetting> settingMap =
                settings.ToDictionary(x => x.RailAxis);

            using (SharedRailXMotionRuntime.EnterInternalDispatch())
            {
                foreach (SharedRailXTarget target in targets)
                {
                    SharedRailXAxisSetting setting;
                    settingMap.TryGetValue(target.Axis, out setting);
                    if (setting == null || setting.Axis == null)
                        return RaiseBlocked("SharedRailX target axis is not mapped. axis=" + target.Axis);

                    double velocity = target.Velocity.HasValue ? target.Velocity.Value : plan.Velocity;
                    if (target.Acceleration.HasValue && target.Deceleration.HasValue)
                    {
                        tasks.Add(SharedRailXMotionRuntime.MoveAxisWithTemporaryMotionAsync(
                            setting.Axis,
                            target.TargetPosition,
                            velocity,
                            target.Acceleration.Value,
                            target.Deceleration.Value,
                            plan.ForceMove));
                    }
                    else
                    {
                        if (plan.ForceMove)
                        {
                            tasks.Add(MoveAbsoluteForceAsync(
                                setting.Axis,
                                target.TargetPosition,
                                velocity));
                        }
                        else
                        {
                            tasks.Add(setting.Axis.MoveAbsoluteAsync(target.TargetPosition, velocity));
                        }
                    }
                }

                int[] results = await Task.WhenAll(tasks);
                int fail = results.FirstOrDefault(x => x != 0);
                return fail;
            }
        }

        private static async Task<int> MoveAbsoluteForceAsync(BaseAxis axis, double targetPosition, double velocity)
        {
            using (BaseAxis.BeginForceMoveScope())
                return await axis.MoveAbsoluteAsync(targetPosition, velocity).ConfigureAwait(false);
        }

        private void Add(List<SharedRailXAxisSetting> list, SharedRailXAxis railAxis, BaseAxis axis)
        {
            if (axis == null)
                return;

            list.Add(new SharedRailXAxisSetting(railAxis, axis)
            {
                SafetyDistance = _config.DefaultSafetyDistance
            });
        }

        private SharedRailXConfig ResolveEffectiveConfig(
            SharedRailXMovePlan plan,
            IReadOnlyList<SharedRailXAxisSetting> settings)
        {
            try
            {
                if (_config == null || _config.CollisionPairs == null || _config.CollisionPairs.Count == 0)
                    return _config;

                var pairs = new List<SharedRailXAxisPair>();
                foreach (SharedRailXAxisPair pair in _config.CollisionPairs)
                {
                    if (ShouldUseCollisionPair(pair, plan, settings))
                        pairs.Add(pair);
                }

                if (pairs.Count == _config.CollisionPairs.Count)
                    return _config;

                return new SharedRailXConfig
                {
                    DefaultSafetyDistance = _config.DefaultSafetyDistance,
                    RequireSameVelocityForGroupMove = _config.RequireSameVelocityForGroupMove,
                    InputVisionRetreatExtraClearance = _config.InputVisionRetreatExtraClearance,
                    OutputVisionRetreatExtraClearance = _config.OutputVisionRetreatExtraClearance,
                    VisionFollowEntryTimeoutMs = _config.VisionFollowEntryTimeoutMs
                }.SetCollisionPairs(pairs);
            }
            catch
            {
                return _config;
            }
            finally
            {
            }
        }

        private bool ShouldUseCollisionPair(
            SharedRailXAxisPair pair,
            SharedRailXMovePlan plan,
            IReadOnlyList<SharedRailXAxisSetting> settings)
        {
            if (IsInputVisionPickerPair(pair, SharedRailXAxis.FrontPickerX))
            {
                // 현재 기준: InputVisionX-FrontPickerX는 Process zone이어도 SharedRailX 거리 검사를 항상 적용한다.
                return true;
                // 기존 조건 필요 여부: 사용하지 않음. Bottom/Side workArea에서 우회하면 실제 X 겹침을 놓칠 수 있다.
                // return IsInputVisionPickerPairRequired(SharedRailXAxis.FrontPickerX, true);
            }

            if (IsInputVisionPickerPair(pair, SharedRailXAxis.RearPickerX))
            {
                // 현재 기준: InputVisionX-RearPickerX는 Process zone이어도 SharedRailX 거리 검사를 항상 적용한다.
                return true;
                // 기존 조건 필요 여부: 사용하지 않음. Bottom/Side workArea에서 우회하면 실제 X 겹침을 놓칠 수 있다.
                // return IsInputVisionPickerPairRequired(SharedRailXAxis.RearPickerX, false);
            }

            if (IsOutputVisionPickerPair(pair, SharedRailXAxis.FrontPickerX))
                return true;

            if (IsOutputVisionPickerPair(pair, SharedRailXAxis.RearPickerX))
                return true;

            return true;
        }

        private static bool IsInputVisionPickerPair(SharedRailXAxisPair pair, SharedRailXAxis pickerAxis)
        {
            return pair.Matches(SharedRailXAxis.InputVisionX, pickerAxis);
        }

        private bool IsInputVisionPickerPairRequired(SharedRailXAxis pickerAxis, bool isFront)
        {
            // 현재 기준: InputVisionX-PickerX pair는 어떤 zone에서도 SharedRailX 거리 검사가 필요하다.
            return true;

            // 기존 조건 필요 여부: 사용하지 않음. Process workArea라는 논리 zone으로 물리 X 거리 검사를 우회하면 안 된다.
            // try
            // {
            //     PickerWorkZone workZone;
            //     string owner;
            //     bool workAreaActive = PickerZoneInterlockRules.TryGetPickerWorkArea(isFront, out workZone, out owner);
            //     if (workAreaActive && PickerZoneInterlockRules.IsProcessZone(workZone))
            //     {
            //         WriteBottomBypassLogThrottled(pickerAxis, isFront, owner);
            //         return false;
            //     }
            //
            //     return true;
            // }
            // catch
            // {
            //     return true;
            // }
        }

        private static void WriteBottomBypassLogThrottled(SharedRailXAxis pickerAxis, bool isFront, string owner)
        {
            string side = isFront ? "Front" : "Rear";
            string safeOwner = string.IsNullOrWhiteSpace(owner) ? "-" : owner;

            if (!IsSimulationModeConfigured())
            {
                WriteBottomBypassLog(pickerAxis, side, safeOwner);
                return;
            }

            string key = pickerAxis + "|" + side + "|" + safeOwner;
            DateTime now = DateTime.UtcNow;

            lock (_bottomBypassLogLock)
            {
                DateTime last;
                if (_bottomBypassLogUtc.TryGetValue(key, out last) &&
                    (now - last).TotalSeconds < 1.0)
                {
                    return;
                }

                _bottomBypassLogUtc[key] = now;
            }

            WriteBottomBypassLog(pickerAxis, side, safeOwner);
        }

        private static void WriteBottomBypassLog(SharedRailXAxis pickerAxis, string side, string owner)
        {
            QMC.Common.Log.Write("SharedRailX",
                "InputVisionX/" + pickerAxis +
                " pair clearance bypassed because picker is in Bottom inspection work area. side=" +
                side +
                ", owner=" + owner);
        }

        private static bool IsSimulationModeConfigured()
        {
            try
            {
                AppSettings settings = QMC.CDT320.AppSettingsStore.Current;
                return settings != null && settings.SimulationMode;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsOutputVisionPickerPair(SharedRailXAxisPair pair, SharedRailXAxis pickerAxis)
        {
            return pair.Matches(SharedRailXAxis.OutputVisionX, pickerAxis);
        }

        private SharedRailXValidationResult ValidateJogCurrentDistance(
            IReadOnlyList<SharedRailXAxisSetting> settings,
            SharedRailXAxis movingRailAxis,
            int direction,
            bool stopAtLimit)
        {
            if (settings == null)
                return SharedRailXValidationResult.Block("SharedRailX settings are empty.");

            SharedRailXAxisSetting moving = settings.FirstOrDefault(x => x != null && x.Axis != null && x.RailAxis == movingRailAxis);
            if (moving == null)
                return SharedRailXValidationResult.Block("SharedRailX jog axis is not mapped. axis=" + movingRailAxis);

            foreach (SharedRailXAxisSetting other in settings)
            {
                if (other == null || other.Axis == null || other.RailAxis == movingRailAxis)
                    continue;
                if (!_config.IsCollisionPairEnabled(movingRailAxis, other.RailAxis))
                    continue;

                SharedRailXAxisPair pair;
                if (!_config.TryGetCollisionPair(movingRailAxis, other.RailAxis, out pair))
                    continue;

                if (!pair.HasClearanceRule)
                {
                    return SharedRailXValidationResult.Block(
                        "SharedRailX jog pair clearance rule is not configured. pair=" +
                        movingRailAxis + "<->" + other.RailAxis);
                }

                SharedRailXValidationResult clearance = ValidateJogPairClearance(
                    moving,
                    other,
                    pair,
                    direction,
                    stopAtLimit);
                if (!clearance.Allowed)
                    return clearance;
            }

            return SharedRailXValidationResult.Allow();
        }

        private static SharedRailXValidationResult ValidateJogPairClearance(
            SharedRailXAxisSetting moving,
            SharedRailXAxisSetting other,
            SharedRailXAxisPair pair,
            int direction,
            bool stopAtLimit)
        {
            int movingSign;
            int otherSign;
            ResolvePairSigns(moving.RailAxis, other.RailAxis, pair, out movingSign, out otherSign);

            double required = pair.SafetyDistance.HasValue
                ? pair.SafetyDistance.Value
                : Math.Max(moving.SafetyDistance, other.SafetyDistance);
            double clearance = CalculatePairClearance(
                pair.HomeClearance,
                movingSign,
                moving.Axis.ActualPosition,
                otherSign,
                other.Axis.ActualPosition);
            bool movingTowardOther = movingSign * direction > 0;

            if (movingTowardOther && clearance <= required)
            {
                return SharedRailXValidationResult.Block(
                    "SharedRailX jog pair clearance is too close. " +
                    moving.Axis.Name + " axis=" + moving.Axis.ActualPosition.ToString("F3") +
                    ", " + other.Axis.Name + " axis=" + other.Axis.ActualPosition.ToString("F3") +
                    ", direction=" + (direction < 0 ? "-" : "+") +
                    ", clearance=" + clearance.ToString("F3") +
                    ", required=" + required.ToString("F3") +
                    ", homeClearance=" + pair.HomeClearance.ToString("F3") +
                    ", movingToward=" + (movingTowardOther ? "Y" : "N") +
                    ", stopAtLimit=" + (stopAtLimit ? "Y" : "N"));
            }

            return SharedRailXValidationResult.Allow();
        }

        private static void ResolvePairSigns(
            SharedRailXAxis axisA,
            SharedRailXAxis axisB,
            SharedRailXAxisPair pair,
            out int signA,
            out int signB)
        {
            if (pair.AxisA == axisA && pair.AxisB == axisB)
            {
                signA = pair.AxisATowardSign;
                signB = pair.AxisBTowardSign;
                return;
            }

            signA = pair.AxisBTowardSign;
            signB = pair.AxisATowardSign;
        }

        private static double CalculatePairClearance(
            double homeClearance,
            int aTowardSign,
            double aPosition,
            int bTowardSign,
            double bPosition)
        {
            return homeClearance - (aTowardSign * aPosition) - (bTowardSign * bPosition);
        }

        private SharedRailXAutoMoveGuard StartAutoMoveGuard(
            SharedRailXMovePlan plan,
            IReadOnlyList<SharedRailXTarget> targets,
            IReadOnlyList<SharedRailXAxisSetting> settings,
            SharedRailXConfig effectiveConfig)
        {
            if (plan == null || targets == null || settings == null)
                return null;

            Dictionary<SharedRailXAxis, SharedRailXAxisSetting> settingMap = settings
                .Where(x => x != null && x.Axis != null)
                .ToDictionary(x => x.RailAxis);
            Dictionary<SharedRailXAxis, double> targetMap = targets
                .GroupBy(x => x.Axis)
                .ToDictionary(x => x.Key, x => x.Last().TargetPosition);

            var guardPairs = new List<SharedRailXGuardPair>();
            SharedRailXConfig guardConfig = effectiveConfig ?? _config;
            if (guardConfig == null || guardConfig.CollisionPairs == null)
                return null;

            foreach (SharedRailXAxisPair pair in guardConfig.CollisionPairs)
            {
                if (!targetMap.ContainsKey(pair.AxisA) && !targetMap.ContainsKey(pair.AxisB))
                    continue;
                if (!pair.HasClearanceRule)
                    continue;

                SharedRailXAxisSetting settingA;
                SharedRailXAxisSetting settingB;
                if (!settingMap.TryGetValue(pair.AxisA, out settingA) ||
                    !settingMap.TryGetValue(pair.AxisB, out settingB))
                {
                    continue;
                }

                double targetA = targetMap.ContainsKey(pair.AxisA)
                    ? targetMap[pair.AxisA]
                    : settingA.Axis.ActualPosition;
                double targetB = targetMap.ContainsKey(pair.AxisB)
                    ? targetMap[pair.AxisB]
                    : settingB.Axis.ActualPosition;
                double initialClearance = CalculatePairClearance(
                    pair.HomeClearance,
                    pair.AxisATowardSign,
                    settingA.Axis.ActualPosition,
                    pair.AxisBTowardSign,
                    settingB.Axis.ActualPosition);
                double targetClearance = CalculatePairClearance(
                    pair.HomeClearance,
                    pair.AxisATowardSign,
                    targetA,
                    pair.AxisBTowardSign,
                    targetB);
                double required = pair.SafetyDistance.HasValue
                    ? pair.SafetyDistance.Value
                    : Math.Max(settingA.SafetyDistance, settingB.SafetyDistance);

                guardPairs.Add(new SharedRailXGuardPair(
                    pair,
                    settingA,
                    settingB,
                    required,
                    initialClearance,
                    targetClearance));
            }

            if (guardPairs.Count == 0)
                return null;

            return new SharedRailXAutoMoveGuard(plan.Name, settings, guardPairs);
        }

        private static int RaiseBlocked(string reason)
        {
            AlarmManager.Raise(AlarmSeverity.Error, "SHARED-RAIL-X", "SharedRailX", reason);
            return -11;
        }

        private sealed class SharedRailXGuardPair
        {
            private const double Epsilon = 0.001;

            public readonly SharedRailXAxisPair Pair;
            public readonly SharedRailXAxisSetting SettingA;
            public readonly SharedRailXAxisSetting SettingB;
            public readonly double RequiredClearance;
            public readonly double InitialClearance;
            public readonly double TargetClearance;
            private double _previousClearance;

            public SharedRailXGuardPair(
                SharedRailXAxisPair pair,
                SharedRailXAxisSetting settingA,
                SharedRailXAxisSetting settingB,
                double requiredClearance,
                double initialClearance,
                double targetClearance)
            {
                Pair = pair;
                SettingA = settingA;
                SettingB = settingB;
                RequiredClearance = requiredClearance;
                InitialClearance = initialClearance;
                TargetClearance = targetClearance;
                _previousClearance = initialClearance;
            }

            public bool IsUnsafe(out string reason)
            {
                reason = string.Empty;
                double clearance = CalculatePairClearance(
                    Pair.HomeClearance,
                    Pair.AxisATowardSign,
                    SettingA.Axis.ActualPosition,
                    Pair.AxisBTowardSign,
                    SettingB.Axis.ActualPosition);

                bool startedUnsafe = InitialClearance <= RequiredClearance;
                bool targetMovesAway = TargetClearance > InitialClearance + Epsilon;
                bool clearanceImproving = clearance + Epsilon >= _previousClearance;
                _previousClearance = clearance;

                if (clearance > RequiredClearance)
                    return false;

                if (startedUnsafe && targetMovesAway && clearanceImproving)
                    return false;

                reason =
                    "SharedRailX real-time clearance guard stopped motion. pair=" +
                    Pair.AxisA + "<->" + Pair.AxisB +
                    ", " + SettingA.Axis.Name + "=" + SettingA.Axis.ActualPosition.ToString("F3") +
                    ", " + SettingB.Axis.Name + "=" + SettingB.Axis.ActualPosition.ToString("F3") +
                    ", clearance=" + clearance.ToString("F3") +
                    ", required=" + RequiredClearance.ToString("F3") +
                    ", initialClearance=" + InitialClearance.ToString("F3") +
                    ", targetClearance=" + TargetClearance.ToString("F3") +
                    ", homeClearance=" + Pair.HomeClearance.ToString("F3") +
                    ", signs=" + Pair.AxisA + ":" + Pair.AxisATowardSign + "," +
                    Pair.AxisB + ":" + Pair.AxisBTowardSign;
                return true;
            }
        }

        private sealed class SharedRailXAutoMoveGuard : IDisposable
        {
            private readonly string _planName;
            private readonly IReadOnlyList<SharedRailXAxisSetting> _settings;
            private readonly IReadOnlyList<SharedRailXGuardPair> _pairs;
            private readonly CancellationTokenSource _cts;
            private readonly CancellationToken _token;
            private readonly Task _task;
            private int _blocked;
            private int _disposed;

            public SharedRailXAutoMoveGuard(
                string planName,
                IReadOnlyList<SharedRailXAxisSetting> settings,
                IReadOnlyList<SharedRailXGuardPair> pairs)
            {
                _planName = string.IsNullOrWhiteSpace(planName) ? "SharedRailX" : planName;
                _settings = settings;
                _pairs = pairs;
                _cts = new CancellationTokenSource();
                _token = _cts.Token;
                _task = Task.Run(() => MonitorAsync(_token), _token);
            }

            public bool Blocked
            {
                get { return _blocked != 0; }
            }

            public void Dispose()
            {
                if (Interlocked.Exchange(ref _disposed, 1) != 0)
                    return;

                try
                {
                    _cts.Cancel();
                }
                catch (ObjectDisposedException)
                {
                }
                catch (Exception ex)
                {
                    AlarmManager.Raise(
                        AlarmSeverity.Error,
                        "SHARED-RAIL-X-MOVE-GUARD",
                        "SharedRailX",
                        "SharedRailX 실시간 감시 취소 중 예외가 발생했습니다. plan=" +
                        _planName + ", message=" + ex.Message);
                }

                Task task = _task;
                if (task == null)
                {
                    DisposeCancellationTokenSource();
                    return;
                }

                task.ContinueWith(
                    t =>
                    {
                        try
                        {
                            if (t.IsFaulted && t.Exception != null)
                                t.Exception.Handle(_ => true);
                        }
                        catch
                        {
                        }
                        finally
                        {
                            DisposeCancellationTokenSource();
                        }
                    },
                    CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
            }

            private void DisposeCancellationTokenSource()
            {
                try
                {
                    _cts.Dispose();
                }
                catch
                {
                }
            }

            private async Task MonitorAsync(CancellationToken ct)
            {
                bool movementSeen = false;

                try
                {
                    while (!ct.IsCancellationRequested)
                    {
                        await Task.Delay(20, ct).ConfigureAwait(false);

                        bool anyMoving = _settings.Any(x => x != null && x.Axis != null && x.Axis.IsMoving);
                        if (!anyMoving)
                        {
                            if (movementSeen)
                                break;
                            continue;
                        }

                        movementSeen = true;
                        foreach (SharedRailXGuardPair pair in _pairs)
                        {
                            string reason;
                            if (pair.IsUnsafe(out reason))
                            {
                                Interlocked.Exchange(ref _blocked, 1);
                                StopSharedRailAxes();
                                AlarmManager.Raise(
                                    AlarmSeverity.Error,
                                    "SHARED-RAIL-X-MOVE-GUARD",
                                    "SharedRailX",
                                    "plan=" + _planName + ". " + reason);
                                return;
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception ex)
                {
                    AlarmManager.Raise(
                        AlarmSeverity.Error,
                        "SHARED-RAIL-X-MOVE-GUARD",
                        "SharedRailX",
                        "SharedRailX real-time guard exception. plan=" + _planName + ", message=" + ex.Message);
                }
            }

            private void StopSharedRailAxes()
            {
                using (SharedRailXMotionRuntime.EnterInternalDispatch())
                {
                    foreach (SharedRailXAxisSetting setting in _settings)
                    {
                        if (setting == null || setting.Axis == null)
                            continue;

                        try { setting.Axis.Stop(); } catch { }
                    }
                }
            }
        }
    }
}
