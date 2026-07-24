using System;
using System.Collections.Generic;
using QMC.CDT320.Materials;

namespace QMC.CDT320.Sequencing
{
    internal static class InputCameraPickUpPermissionStore
    {
        private sealed class Permission
        {
            public DateTime GrantedAt;
            // FIFO 순번(단조 증가). 진입 요청이 큐에 등록된 순서를 나타낸다.
            // 같은 side 재발급(픽업 롤백) 시에는 보존해 큐 순서를 유지한다.
            public long Seq;
            public List<InputDieVisionPreparedItem> Items;
        }

        private static readonly object Sync = new object();
        private static readonly Dictionary<PickerSequenceSide, Permission> Permissions =
            new Dictionary<PickerSequenceSide, Permission>();
        // 허가 발급 순번 카운터. Grant 시 신규 side에만 next 값을 부여한다.
        private static long _seqCounter;

        public static void Grant(PickerSequenceSide side, IEnumerable<InputDieVisionPreparedItem> items)
        {
            lock (Sync)
            {
                Permission oldPermission;
                bool hadOld = Permissions.TryGetValue(side, out oldPermission) && oldPermission != null;
                if (hadOld)
                    ReleaseItems(oldPermission.Items);

                var clonedItems = new List<InputDieVisionPreparedItem>();
                if (items != null)
                {
                    foreach (InputDieVisionPreparedItem item in items)
                    {
                        InputDieVisionPreparedItem clone = CloneItem(item);
                        if (clone != null)
                            clonedItems.Add(clone);
                    }
                }

                // 동일 side 재발급(픽업 소비 후 물리 안전 미충족으로 롤백)이면 기존 순번을 보존하고,
                // 신규 발급이면 큐 맨 뒤 순번(단조 증가)을 부여한다.
                long seq = hadOld ? oldPermission.Seq : ++_seqCounter;

                Permissions[side] = new Permission
                {
                    GrantedAt = DateTime.Now,
                    Seq = seq,
                    Items = clonedItems
                };
            }
        }

        public static bool TryConsume(PickerSequenceSide side, out List<InputDieVisionPreparedItem> items, out string reason)
        {
            lock (Sync)
            {
                items = null;
                reason = string.Empty;

                Permission permission;
                if (!Permissions.TryGetValue(side, out permission) || permission == null)
                {
                    reason = "permission not granted. side=" + side;
                    return false;
                }

                // 픽업 명령 권한은 큐(FIFO) 순서대로만 부여한다. 나보다 앞선(seq 작은) 다른 side
                // 허가가 살아 있으면 소비를 거부한다. at-most-one 불변식(카메라 존이 발급을
                // 직렬화)상 정상 운전에서는 항상 head라 즉시 통과하는 belt-and-suspenders다.
                if (!IsHeadSideNoLock(side, out string headDetail))
                {
                    reason = "permission is not FIFO head. side=" + side + ", " + headDetail;
                    return false;
                }

                Permissions.Remove(side);

                if (permission.Items == null || permission.Items.Count == 0)
                {
                    reason = "permission has no inspected item. side=" + side;
                    items = new List<InputDieVisionPreparedItem>();
                    return true;
                }

                items = new List<InputDieVisionPreparedItem>();
                for (int i = 0; i < permission.Items.Count; i++)
                {
                    InputDieVisionPreparedItem clone = CloneItem(permission.Items[i]);
                    if (clone != null)
                        items.Add(clone);
                }

                reason = "grantedAt=" + permission.GrantedAt.ToString("yyyy-MM-dd HH:mm:ss.fff") +
                         ", seq=" + permission.Seq +
                         ", count=" + items.Count +
                         ", side=" + side;
                return true;
            }
        }

        // 나보다 앞선(seq가 더 작은) 다른 side의 미소비 허가가 있는지. 데드락 절단 게이트가
        // '카메라 존을 잡기 전' 이 값이 true인 동안 존을 양보하는 데 사용한다.
        public static bool HasForeignPermission(PickerSequenceSide side, out string detail)
        {
            lock (Sync)
            {
                detail = string.Empty;

                Permission mine;
                bool hasMine = Permissions.TryGetValue(side, out mine) &&
                               mine != null && mine.Items != null && mine.Items.Count > 0;
                long mySeq = hasMine ? mine.Seq : long.MaxValue;

                foreach (KeyValuePair<PickerSequenceSide, Permission> pair in Permissions)
                {
                    if (pair.Key == side)
                        continue;

                    Permission other = pair.Value;
                    if (other == null || other.Items == null || other.Items.Count == 0)
                        continue;

                    // 내 허가가 아직 없으면(선행검사 진행 중, 발급 전) 상대 허가는 모두 나보다 앞선다.
                    if (other.Seq < mySeq)
                    {
                        detail = "foreign=" + pair.Key + ":seq=" + other.Seq +
                                 ",count=" + other.Items.Count +
                                 ",grantedAt=" + other.GrantedAt.ToString("HH:mm:ss.fff") +
                                 (hasMine ? "; mine=" + side + ":seq=" + mySeq : "; mine=none");
                        return true;
                    }
                }

                return false;
            }
        }

        // 이 side 허가가 존재하고 그것이 큐 맨 앞(최소 seq)인지.
        public static bool IsHeadSide(PickerSequenceSide side)
        {
            lock (Sync)
            {
                string detail;
                return IsHeadSideNoLock(side, out detail);
            }
        }

        private static bool IsHeadSideNoLock(PickerSequenceSide side, out string detail)
        {
            detail = string.Empty;

            Permission mine;
            if (!Permissions.TryGetValue(side, out mine) || mine == null ||
                mine.Items == null || mine.Items.Count == 0)
            {
                detail = "no own permission";
                return false;
            }

            foreach (KeyValuePair<PickerSequenceSide, Permission> pair in Permissions)
            {
                if (pair.Key == side)
                    continue;

                Permission other = pair.Value;
                if (other == null || other.Items == null || other.Items.Count == 0)
                    continue;

                if (other.Seq < mine.Seq)
                {
                    detail = "head=" + pair.Key + ":seq=" + other.Seq +
                             ", mine=" + side + ":seq=" + mine.Seq;
                    return false;
                }
            }

            detail = "head=" + side + ":seq=" + mine.Seq;
            return true;
        }

        public static bool HasPermission(PickerSequenceSide side)
        {
            lock (Sync)
            {
                Permission permission;
                return Permissions.TryGetValue(side, out permission) &&
                       permission != null &&
                       permission.Items != null &&
                       permission.Items.Count > 0;
            }
        }

        public static bool HasAnyPermission(out string detail)
        {
            lock (Sync)
            {
                detail = string.Empty;
                if (Permissions.Count == 0)
                    return false;

                var parts = new List<string>();
                foreach (KeyValuePair<PickerSequenceSide, Permission> pair in Permissions)
                {
                    Permission permission = pair.Value;
                    int count = permission != null && permission.Items != null ? permission.Items.Count : 0;
                    if (count <= 0)
                        continue;

                    parts.Add(pair.Key + ":count=" + count +
                              ",grantedAt=" + permission.GrantedAt.ToString("HH:mm:ss.fff"));
                }

                if (parts.Count == 0)
                    return false;

                detail = string.Join(";", parts.ToArray());
                return true;
            }
        }

        public static void Clear(PickerSequenceSide side)
        {
            lock (Sync)
            {
                Permission permission;
                if (Permissions.TryGetValue(side, out permission))
                    ReleaseItems(permission != null ? permission.Items : null);

                Permissions.Remove(side);
            }
        }

        private static void ReleaseItems(IEnumerable<InputDieVisionPreparedItem> items)
        {
            if (items == null)
                return;

            List<QMC.CDT320.VisionComm.VisionRequestHandle> pendingHandles = null;
            foreach (InputDieVisionPreparedItem item in items)
            {
                if (item == null)
                    continue;

                // 조기 허가(EPD 시점 허가) 폐기: RESULT 미회수 핸들이 비전 측 orphan으로 남지 않게 드레인 예약.
                if (item.VisionRequest != null)
                {
                    if (pendingHandles == null)
                        pendingHandles = new List<QMC.CDT320.VisionComm.VisionRequestHandle>();
                    pendingHandles.Add(item.VisionRequest);
                }

                if (string.IsNullOrWhiteSpace(item.DieId))
                    continue;

                MaterialStateService.ReleaseInputStagePickReservation(
                    item.DieId,
                    item.PickTarget != null ? item.PickTarget.PickerLocation : MaterialLocationKind.Unknown,
                    item.PickerNo);
            }

            if (pendingHandles != null)
                ScheduleHandleDrain(pendingHandles);
        }

        /// <summary>폐기된 허가의 RESULT 미회수 핸들을 백그라운드로 드레인한다 (lock 밖 비동기, 예외 무해화).</summary>
        private static void ScheduleHandleDrain(List<QMC.CDT320.VisionComm.VisionRequestHandle> handles)
        {
            System.Threading.Tasks.Task.Run(async () =>
            {
                for (int i = 0; i < handles.Count; i++)
                {
                    try
                    {
                        await InputDieVisionPrepareSequence.DrainInputVisionRequestHandleAsync(
                            handles[i],
                            "InputCamera PickUp 허가 폐기 정리",
                            "InputCameraPickUpPermissionStore",
                            System.Threading.CancellationToken.None).ConfigureAwait(false);
                    }
                    catch
                    {
                    }
                }
            });
        }

        private static InputDieVisionPreparedItem CloneItem(InputDieVisionPreparedItem item)
        {
            if (item == null)
                return null;

            return new InputDieVisionPreparedItem
            {
                PickerIndex = item.PickerIndex,
                PickerNo = item.PickerNo,
                DieId = item.DieId,
                PickTarget = ClonePickTarget(item.PickTarget),
                VisionRequestIndex = item.VisionRequestIndex,
                VisionRequest = item.VisionRequest,
                ExposureCompleted = item.ExposureCompleted,
                VisionOffset = CloneVisionOffset(item.VisionOffset),
                VisionOffsetApplied = item.VisionOffsetApplied,
                DiePicked = item.DiePicked
            };
        }

        private static InputStagePickTarget ClonePickTarget(InputStagePickTarget target)
        {
            if (target == null)
                return null;

            return new InputStagePickTarget
            {
                WaferId = target.WaferId,
                DieId = target.DieId,
                OrderIndex = target.OrderIndex,
                DieMapX = target.DieMapX,
                DieMapY = target.DieMapY,
                OffsetX = target.OffsetX,
                OffsetY = target.OffsetY,
                TargetX = target.TargetX,
                TargetY = target.TargetY,
                PickerNo = target.PickerNo,
                PickerLocation = target.PickerLocation
            };
        }

        private static VisionAlignResult CloneVisionOffset(VisionAlignResult offset)
        {
            if (offset == null)
                return null;

            return new VisionAlignResult
            {
                DeltaX = offset.DeltaX,
                DeltaY = offset.DeltaY,
                DeltaTheta = offset.DeltaTheta
            };
        }
    }
}
