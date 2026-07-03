using System;
using System.Collections.Generic;
using System.Globalization;

namespace QMC.CDT320.Interlocks
{
    public sealed class MotionGuardMoveIntent
    {
        private readonly Dictionary<string, string> _values;

        private MotionGuardMoveIntent(string targetName)
        {
            RawTargetName = targetName ?? string.Empty;
            _values = ParseValues(RawTargetName);
            NormalizedTargetName = MotionGuardRuleHelpers.NormalizeTargetName(RawTargetName);
            AutoSequence = HasToken("AutoSequence");
            AutoProcessCorrection = HasToken("AutoProcessCorrection");
            InspectionContinuous = HasToken("InspectionContinuous");
            InspectionZHold = HasValue("PickerPhase", "InspectionZHold");
            ColletCalibration = Contains("ColletCalibration");
            ContinuousJog = string.Equals(NormalizedTargetName, "ContinuousJog", StringComparison.OrdinalIgnoreCase);
            PickerZone = ResolvePickerZone();
            InspectionFromZone = ResolveZoneValue("From");
            InspectionToZone = ResolveZoneValue("To");
            InputStageWorkAreaX = ResolveDoubleValue("InputStageWorkAreaX");
            AutoProcessCorrectionMax = ResolveDoubleValue("AutoProcessCorrectionMax");
        }

        public string RawTargetName { get; private set; }
        public string NormalizedTargetName { get; private set; }
        public bool AutoSequence { get; private set; }
        public bool AutoProcessCorrection { get; private set; }
        public bool InspectionContinuous { get; private set; }
        public bool InspectionZHold { get; private set; }
        public bool ColletCalibration { get; private set; }
        public bool ContinuousJog { get; private set; }
        internal PickerWorkZone PickerZone { get; private set; }
        internal PickerWorkZone InspectionFromZone { get; private set; }
        internal PickerWorkZone InspectionToZone { get; private set; }
        public double? InputStageWorkAreaX { get; private set; }
        public double? AutoProcessCorrectionMax { get; private set; }

        public static MotionGuardMoveIntent Parse(string targetName)
        {
            return new MotionGuardMoveIntent(targetName);
        }

        public bool HasToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return false;

            string[] tokens = RawTargetName.Split(';');
            for (int i = 0; i < tokens.Length; i++)
            {
                if (string.Equals(tokens[i].Trim(), token, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return Contains(token);
        }

        public bool TryGetValue(string key, out string value)
        {
            value = string.Empty;
            if (string.IsNullOrWhiteSpace(key))
                return false;

            return _values.TryGetValue(key.Trim(), out value);
        }

        public bool TryGetDouble(string key, out double value)
        {
            value = 0.0;
            string text;
            if (!TryGetValue(key, out text))
                return false;

            return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        public bool Contains(string text)
        {
            return !string.IsNullOrWhiteSpace(text) &&
                   RawTargetName.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private bool HasValue(string key, string expectedValue)
        {
            string value;
            return TryGetValue(key, out value) &&
                   string.Equals(value, expectedValue, StringComparison.OrdinalIgnoreCase);
        }

        private double? ResolveDoubleValue(string key)
        {
            double value;
            return TryGetDouble(key, out value) ? (double?)value : null;
        }

        private PickerWorkZone ResolveZoneValue(string key)
        {
            string value;
            return TryGetValue(key, out value) ? ParsePickerWorkZone(value) : PickerWorkZone.Unknown;
        }

        private PickerWorkZone ResolvePickerZone()
        {
            PickerWorkZone zone = ResolveZoneValue("PickerZone");
            if (zone != PickerWorkZone.Unknown)
                return zone;

            string name = RawTargetName.Replace(" ", string.Empty);
            if (ContainsTokenText(name, "DiePick") || ContainsTokenText(name, "PickPosition"))
                return PickerWorkZone.Input;
            if (ContainsTokenText(name, "DiePlace") || ContainsTokenText(name, "PlacePosition"))
                return PickerWorkZone.Output;
            if (ContainsTokenText(name, "DieBottom") || ContainsTokenText(name, "BottomPosition"))
                return PickerWorkZone.Bottom;
            if (ContainsTokenText(name, "DieSide") || ContainsTokenText(name, "SidePosition"))
                return PickerWorkZone.Side;
            if (ContainsTokenText(name, "AvoidPosition") || ContainsTokenText(name, "SafeRetreat"))
                return PickerWorkZone.Avoid;

            return PickerWorkZone.Unknown;
        }

        private static PickerWorkZone ParsePickerWorkZone(string value)
        {
            string name = (value ?? string.Empty).Trim();
            if (name.Equals("Input", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("Pick", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("PickUp", StringComparison.OrdinalIgnoreCase))
                return PickerWorkZone.Input;
            if (name.Equals("Bottom", StringComparison.OrdinalIgnoreCase))
                return PickerWorkZone.Bottom;
            if (name.Equals("Side", StringComparison.OrdinalIgnoreCase))
                return PickerWorkZone.Side;
            if (name.Equals("Output", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("Place", StringComparison.OrdinalIgnoreCase))
                return PickerWorkZone.Output;
            if (name.Equals("Avoid", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("Safe", StringComparison.OrdinalIgnoreCase))
                return PickerWorkZone.Avoid;

            return PickerWorkZone.Unknown;
        }

        private static bool ContainsTokenText(string value, string text)
        {
            return (value ?? string.Empty).IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static Dictionary<string, string> ParseValues(string targetName)
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(targetName))
                return values;

            string[] tokens = targetName.Split(';');
            for (int i = 0; i < tokens.Length; i++)
            {
                string token = tokens[i] != null ? tokens[i].Trim() : string.Empty;
                if (token.Length == 0)
                    continue;

                int separator = token.IndexOf('=');
                if (separator <= 0 || separator >= token.Length - 1)
                    continue;

                string key = token.Substring(0, separator).Trim();
                string value = token.Substring(separator + 1).Trim();
                if (key.Length == 0)
                    continue;

                values[key] = value;
            }

            return values;
        }
    }
}
