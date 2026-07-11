using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace QMC.Common.Motion
{
    public sealed class AjinVirtualCoordinateLease : IDisposable
    {
        private AjinVirtualCoordinatePool _owner;

        internal AjinVirtualCoordinateLease(AjinVirtualCoordinatePool owner, int coordinate)
        {
            _owner = owner;
            Coordinate = coordinate;
        }

        public int Coordinate { get; private set; }

        public void Dispose()
        {
            AjinVirtualCoordinatePool owner = Interlocked.Exchange(ref _owner, null);
            if (owner != null)
                owner.Return(Coordinate);
        }
    }

    public sealed class AjinVirtualCoordinatePool
    {
        public const int MinCoordinate = 1;
        public const int MaxCoordinate = 7;

        private readonly object _syncRoot = new object();
        private readonly SortedSet<int> _available;
        private readonly HashSet<int> _leased;
        private readonly SemaphoreSlim _availableSignal;

        public static AjinVirtualCoordinatePool Instance { get; } = new AjinVirtualCoordinatePool();

        private AjinVirtualCoordinatePool()
        {
            _available = new SortedSet<int>(Enumerable.Range(MinCoordinate, MaxCoordinate - MinCoordinate + 1));
            _leased = new HashSet<int>();
            _availableSignal = new SemaphoreSlim(_available.Count, _available.Count);
        }

        public async Task<AjinVirtualCoordinateLease> RentAsync(int preferredCoordinate, int timeoutMs, CancellationToken ct)
        {
            int waitMs = timeoutMs > 0 ? timeoutMs : 5000;
            bool entered = await _availableSignal.WaitAsync(waitMs, ct).ConfigureAwait(false);
            if (!entered)
                return null;

            lock (_syncRoot)
            {
                int coordinate = PickCoordinate(preferredCoordinate);
                _available.Remove(coordinate);
                _leased.Add(coordinate);
                return new AjinVirtualCoordinateLease(this, coordinate);
            }
        }

        public string BuildStateText()
        {
            lock (_syncRoot)
            {
                return "available=[" + string.Join(",", _available) + "], leased=[" + string.Join(",", _leased.OrderBy(x => x)) + "]";
            }
        }

        private int PickCoordinate(int preferredCoordinate)
        {
            if (preferredCoordinate >= MinCoordinate &&
                preferredCoordinate <= MaxCoordinate &&
                _available.Contains(preferredCoordinate))
            {
                return preferredCoordinate;
            }

            return _available.Min;
        }

        internal void Return(int coordinate)
        {
            lock (_syncRoot)
            {
                if (!_leased.Remove(coordinate))
                    return;

                _available.Add(coordinate);
            }

            _availableSignal.Release();
        }
    }
}
