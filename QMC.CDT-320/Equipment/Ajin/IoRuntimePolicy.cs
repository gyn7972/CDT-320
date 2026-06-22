using QMC.Common.IO;
using System;

namespace QMC.CDT320.Ajin
{
    public enum DryRunInputHandling
    {
        UseHardware,
        BypassWait
    }

    /// <summary>
    /// Central runtime policy for deciding how DI/DO should behave in real,
    /// dry-run, and simulation modes.
    /// </summary>
    public static class IoRuntimePolicy
    {
        // Dry-run must keep these inputs as real hardware signals.
        // Add safety, operator, cylinder, and equipment-state signals here.
        private static readonly string[] DryRunHardwareInputTokens =
        {
            "Emo",
            "Emergency",
            "EStop",
            "Safety",
            "Interlock",
            "Door",
            "Button",
            "Switch",
            "Start",
            "Stop",
            "Reset",
            "Pressure",
            "Overload",
            "Guide",
            "Clamp",
            "Unclamp",
            "Lift",
            "Lock",
            "Bw",
            "Fw",
            "Up",
            "Down",
            "Reticle",
            "Cda"
        };

        // Dry-run can bypass these process/material detection inputs.
        // Keep this list narrow; hardware/safety tokens above always win.
        private static readonly string[] DryRunBypassInputTokens =
        {
            "CassetteCheck",
            "Ring",
            "Mapping",
            "Jut",
            "Flow",
            "Touch",
            "Vacuum",
            "Exist",
            "Detect",
            "Material"
        };

        public static bool ShouldUseInputSimulation(BaseDigitalInput input, bool simulationRequested, bool hardwareReady)
        {
            return simulationRequested || !hardwareReady || input is SimDigitalInput;
        }

        public static bool ShouldUseOutputSimulation(BaseDigitalOutput output, bool simulationRequested, bool hardwareReady)
        {
            return simulationRequested || !hardwareReady || output is SimDigitalOutput;
        }

        public static bool ShouldUseCylinderSimulation(BaseCylinder cylinder, bool simulationRequested, bool hardwareReady)
        {
            return simulationRequested || !hardwareReady || cylinder is SimCylinder;
        }

        public static DryRunInputHandling GetDryRunInputHandling(BaseDigitalInput input)
        {
            string name = input != null ? input.Name ?? string.Empty : string.Empty;
            if (string.IsNullOrWhiteSpace(name))
                return DryRunInputHandling.UseHardware;

            if (ContainsAny(name, DryRunHardwareInputTokens))
                return DryRunInputHandling.UseHardware;

            if (ContainsAny(name, DryRunBypassInputTokens))
                return DryRunInputHandling.BypassWait;

            return DryRunInputHandling.UseHardware;
        }

        public static bool ShouldBypassInputWaitInDryRun(BaseDigitalInput input)
        {
            return GetDryRunInputHandling(input) == DryRunInputHandling.BypassWait;
        }

        private static bool ContainsAny(string value, string[] tokens)
        {
            if (string.IsNullOrWhiteSpace(value) || tokens == null)
                return false;

            for (int i = 0; i < tokens.Length; i++)
            {
                string token = tokens[i];
                if (!string.IsNullOrWhiteSpace(token) &&
                    value.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            return false;
        }
    }
}
