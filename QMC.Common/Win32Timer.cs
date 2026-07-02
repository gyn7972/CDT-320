using System.Runtime.InteropServices;

namespace QMC.Common
{
    public static class Win32Timer
    {
        private const uint ResolutionMs = 1;
        private static readonly object Sync = new object();
        private static bool _active;

        [DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
        private static extern uint TimeBeginPeriod(uint milliseconds);

        [DllImport("winmm.dll", EntryPoint = "timeEndPeriod")]
        private static extern uint TimeEndPeriod(uint milliseconds);

        public static bool SetHighResolution()
        {
            lock (Sync)
            {
                if (_active)
                    return true;

                uint result = TimeBeginPeriod(ResolutionMs);
                _active = result == 0;
                return _active;
            }
        }

        public static void RestoreResolution()
        {
            lock (Sync)
            {
                if (!_active)
                    return;

                TimeEndPeriod(ResolutionMs);
                _active = false;
            }
        }
    }
}
