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
            public List<InputDieVisionPreparedItem> Items;
        }

        private static readonly object Sync = new object();
        private static readonly Dictionary<PickerSequenceSide, Permission> Permissions =
            new Dictionary<PickerSequenceSide, Permission>();

        public static void Grant(PickerSequenceSide side, IEnumerable<InputDieVisionPreparedItem> items)
        {
            lock (Sync)
            {
                Permission oldPermission;
                if (Permissions.TryGetValue(side, out oldPermission))
                    ReleaseItems(oldPermission != null ? oldPermission.Items : null);

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

                Permissions[side] = new Permission
                {
                    GrantedAt = DateTime.Now,
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
                         ", count=" + items.Count +
                         ", side=" + side;
                return true;
            }
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

            foreach (InputDieVisionPreparedItem item in items)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.DieId))
                    continue;

                MaterialStateService.ReleaseInputStagePickReservation(
                    item.DieId,
                    item.PickTarget != null ? item.PickTarget.PickerLocation : MaterialLocationKind.Unknown,
                    item.PickerNo);
            }
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
                VisionOffset = CloneVisionOffset(item.VisionOffset),
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
