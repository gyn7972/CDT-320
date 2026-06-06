using System;

namespace QMC.CDT320.Interlocks
{
    internal static class MotionGuardRuleHelpers
    {
        public static bool IsMoving(MotionGuardRuleContext request, params string[] names)
        {
            if (request == null || names == null)
                return false;

            for (int i = 0; i < names.Length; i++)
            {
                string name = names[i];
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                string key = InterlockCheckMatrix.NormalizeName(name);
                if (string.Equals(request.MovingKey, key, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(request.MovingName, name, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        public static bool Block(string movingName, string message, out string reason)
        {
            reason = "Interlock blocked. moving=" + movingName + ". " + message;
            return false;
        }
    }
}
