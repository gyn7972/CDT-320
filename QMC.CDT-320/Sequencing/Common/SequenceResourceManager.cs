using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using QMC.Common;
using QMC.Common.Alarms;

namespace QMC.CDT320.Sequencing
{
    public sealed class SequenceResourceManager
    {
        private sealed class ResourceSlot
        {
            public readonly SemaphoreSlim Gate = new SemaphoreSlim(1, 1);
            public string Holder = "";
        }

        private readonly ConcurrentDictionary<SequenceResourceKind, ResourceSlot> _slots =
            new ConcurrentDictionary<SequenceResourceKind, ResourceSlot>();

        public async Task<SequenceResourceLease> AcquireAsync(
            SequenceResourceKind resource,
            string holder,
            int timeoutMs,
            CancellationToken ct)
        {
            return await AcquireAsync(resource, holder, timeoutMs, ct, true).ConfigureAwait(false);
        }

        public async Task<SequenceResourceLease> AcquireAsync(
            SequenceResourceKind resource,
            string holder,
            int timeoutMs,
            CancellationToken ct,
            bool raiseAlarmOnTimeout)
        {
            ResourceSlot slot = _slots.GetOrAdd(resource, _ => new ResourceSlot());
            string safeHolder = string.IsNullOrWhiteSpace(holder) ? "Unknown" : holder;

            bool acquired = false;
            try
            {
                if (timeoutMs <= 0)
                {
                    await slot.Gate.WaitAsync(ct).ConfigureAwait(false);
                    acquired = true;
                }
                else
                {
                    acquired = await slot.Gate.WaitAsync(timeoutMs, ct).ConfigureAwait(false);
                }

                if (!acquired)
                {
                    string message = resource + " resource acquire timeout. holder=" + safeHolder +
                                     ", current=" + slot.Holder;
                    if (raiseAlarmOnTimeout)
                    {
                        Log.Write("Main", "INTERLOCK", "SequenceResource", message + " - Blocked");
                        AlarmManager.Raise(AlarmSeverity.Error, "SEQ-RESOURCE-TIMEOUT", safeHolder, message);
                    }
                    return null;
                }

                slot.Holder = safeHolder;
                Log.Write("Main", "SYSTEM", "SequenceResource",
                    resource + " acquired by " + safeHolder + " - Ok");
                return new SequenceResourceLease(this, resource, safeHolder);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                if (acquired)
                    Release(resource, safeHolder);

                string message = resource + " resource acquire failed. holder=" + safeHolder +
                                 ", error=" + ex.Message;
                Log.Write("Main", "INTERLOCK", "SequenceResource", message + " - Failed");
                AlarmManager.Raise(AlarmSeverity.Error, "SEQ-RESOURCE-EX", safeHolder, message);
                return null;
            }
            finally
            {
            }
        }

        public bool IsOccupied(SequenceResourceKind resource)
        {
            ResourceSlot slot;
            if (!_slots.TryGetValue(resource, out slot))
                return false;

            return !string.IsNullOrWhiteSpace(slot.Holder);
        }

        public string GetHolder(SequenceResourceKind resource)
        {
            ResourceSlot slot;
            if (!_slots.TryGetValue(resource, out slot))
                return "";

            return slot.Holder ?? "";
        }

        internal void Release(SequenceResourceKind resource, string holder)
        {
            ResourceSlot slot;
            if (!_slots.TryGetValue(resource, out slot))
                return;

            try
            {
                if (string.IsNullOrWhiteSpace(slot.Holder) ||
                    string.Equals(slot.Holder, holder, StringComparison.OrdinalIgnoreCase))
                {
                    slot.Holder = "";
                }

                slot.Gate.Release();
                Log.Write("Main", "SYSTEM", "SequenceResource",
                    resource + " released by " + holder + " - Ok");
            }
            catch (SemaphoreFullException)
            {
                Log.Write("Main", "SYSTEM", "SequenceResource",
                    resource + " release ignored because semaphore is already full. holder=" + holder + " - Check");
            }
            catch (Exception ex)
            {
                Log.Write("Main", "INTERLOCK", "SequenceResource",
                    resource + " release failed. holder=" + holder + ", error=" + ex.Message + " - Failed");
            }
            finally
            {
            }
        }
    }
}
