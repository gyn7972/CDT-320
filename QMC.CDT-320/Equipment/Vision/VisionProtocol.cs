using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace QMC.CDT320.VisionComm
{
    public enum VisionProtocolCommand
    {
        Unknown = 0,
        Ping,
        Expose,
        Grab,
        Match,
        MatchAsync,
        MatchResult,
        Inspect,
        InspectAsync,
        InspectResult,
        Train,
        Scale,
        RotationCenter,
        Distort,
        CameraSwitch,
        FocusStart,
        FocusValue,
        FocusBest,
        CameraSetting,
        Recipe
    }

    public static class VisionProtocolCommands
    {
        public const string Ping = "PING";
        public const string Expose = "EXPOSE";
        public const string Grab = "GRAB";
        public const string Match = "MATCH";
        public const string MatchAsync = "MATCHASYNC";
        public const string MatchResult = "MATCHRESULT";
        public const string Inspect = "INSPECT";
        public const string InspectAsync = "INSPECTASYNC";
        public const string InspectResult = "INSPECTRESULT";
        public const string Train = "TRAIN";
        public const string Scale = "SCALE";
        public const string RotationCenter = "ROT_CENTER";
        public const string Distort = "DISTORT";
        public const string CameraSwitch = "CAM_SWITCH";
        public const string FocusStart = "FOCUS_START";
        public const string FocusValue = "FOCUS_VAL";
        public const string FocusBest = "FOCUS_BEST";
        public const string CameraSetting = "CAM_SETTING";
        public const string Recipe = "RECIPE";

        public static string ToText(VisionProtocolCommand command)
        {
            switch (command)
            {
                case VisionProtocolCommand.Ping:
                    return Ping;
                case VisionProtocolCommand.Expose:
                    return Expose;
                case VisionProtocolCommand.Grab:
                    return Grab;
                case VisionProtocolCommand.Match:
                    return Match;
                case VisionProtocolCommand.MatchAsync:
                    return MatchAsync;
                case VisionProtocolCommand.MatchResult:
                    return MatchResult;
                case VisionProtocolCommand.Inspect:
                    return Inspect;
                case VisionProtocolCommand.InspectAsync:
                    return InspectAsync;
                case VisionProtocolCommand.InspectResult:
                    return InspectResult;
                case VisionProtocolCommand.Train:
                    return Train;
                case VisionProtocolCommand.Scale:
                    return Scale;
                case VisionProtocolCommand.RotationCenter:
                    return RotationCenter;
                case VisionProtocolCommand.Distort:
                    return Distort;
                case VisionProtocolCommand.CameraSwitch:
                    return CameraSwitch;
                case VisionProtocolCommand.FocusStart:
                    return FocusStart;
                case VisionProtocolCommand.FocusValue:
                    return FocusValue;
                case VisionProtocolCommand.FocusBest:
                    return FocusBest;
                case VisionProtocolCommand.CameraSetting:
                    return CameraSetting;
                case VisionProtocolCommand.Recipe:
                    return Recipe;
                default:
                    return string.Empty;
            }
        }

        public static VisionProtocolCommand FromText(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
                return VisionProtocolCommand.Unknown;

            string value = command.Trim();
            if (string.Equals(value, Ping, StringComparison.OrdinalIgnoreCase))
                return VisionProtocolCommand.Ping;
            if (string.Equals(value, Expose, StringComparison.OrdinalIgnoreCase))
                return VisionProtocolCommand.Expose;
            if (string.Equals(value, Grab, StringComparison.OrdinalIgnoreCase))
                return VisionProtocolCommand.Grab;
            if (string.Equals(value, Match, StringComparison.OrdinalIgnoreCase))
                return VisionProtocolCommand.Match;
            if (string.Equals(value, MatchAsync, StringComparison.OrdinalIgnoreCase))
                return VisionProtocolCommand.MatchAsync;
            if (string.Equals(value, MatchResult, StringComparison.OrdinalIgnoreCase))
                return VisionProtocolCommand.MatchResult;
            if (string.Equals(value, Inspect, StringComparison.OrdinalIgnoreCase))
                return VisionProtocolCommand.Inspect;
            if (string.Equals(value, InspectAsync, StringComparison.OrdinalIgnoreCase))
                return VisionProtocolCommand.InspectAsync;
            if (string.Equals(value, InspectResult, StringComparison.OrdinalIgnoreCase))
                return VisionProtocolCommand.InspectResult;
            if (string.Equals(value, Train, StringComparison.OrdinalIgnoreCase))
                return VisionProtocolCommand.Train;
            if (string.Equals(value, Scale, StringComparison.OrdinalIgnoreCase))
                return VisionProtocolCommand.Scale;
            if (string.Equals(value, RotationCenter, StringComparison.OrdinalIgnoreCase))
                return VisionProtocolCommand.RotationCenter;
            if (string.Equals(value, Distort, StringComparison.OrdinalIgnoreCase))
                return VisionProtocolCommand.Distort;
            if (string.Equals(value, CameraSwitch, StringComparison.OrdinalIgnoreCase))
                return VisionProtocolCommand.CameraSwitch;
            if (string.Equals(value, FocusStart, StringComparison.OrdinalIgnoreCase))
                return VisionProtocolCommand.FocusStart;
            if (string.Equals(value, FocusValue, StringComparison.OrdinalIgnoreCase))
                return VisionProtocolCommand.FocusValue;
            if (string.Equals(value, FocusBest, StringComparison.OrdinalIgnoreCase))
                return VisionProtocolCommand.FocusBest;
            if (string.Equals(value, CameraSetting, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "CAMERA_SETTING", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "CAM_SETTING_REQ", StringComparison.OrdinalIgnoreCase))
                return VisionProtocolCommand.CameraSetting;
            if (string.Equals(value, Recipe, StringComparison.OrdinalIgnoreCase))
                return VisionProtocolCommand.Recipe;

            return VisionProtocolCommand.Unknown;
        }
    }

    public static class VisionProtocolPushCommands
    {
        public const string ExposureDone = "FPD";
        public const string LegacyExposureDone = "EPD";
        public const string Alarm = "ARM";
        public const string RecipeRequest = "RECIPEREQ";
    }

    public sealed class VisionProtocolMessage
    {
        public string Module { get; private set; }
        public string Command { get; private set; }
        public string[] Arguments { get; private set; }

        public static VisionProtocolMessage Create(string module, string command, params object[] arguments)
        {
            var message = new VisionProtocolMessage();
            message.Module = module ?? string.Empty;
            message.Command = command ?? string.Empty;

            if (arguments == null || arguments.Length == 0)
            {
                message.Arguments = new string[0];
                return message;
            }

            var values = new string[arguments.Length];
            for (int i = 0; i < arguments.Length; i++)
                values[i] = FormatArgument(arguments[i]);
            message.Arguments = values;
            return message;
        }

        public static VisionProtocolMessage Create(string module, VisionProtocolCommand command, params object[] arguments)
        {
            return Create(module, VisionProtocolCommands.ToText(command), arguments);
        }

        public string ToLine()
        {
            var sb = new StringBuilder();
            sb.Append(Module);
            sb.Append('|');
            sb.Append(Command);

            if (Arguments != null)
            {
                foreach (string argument in Arguments)
                {
                    sb.Append('|');
                    sb.Append(argument ?? string.Empty);
                }
            }

            return sb.ToString();
        }

        private static string FormatArgument(object value)
        {
            if (value == null)
                return string.Empty;

            if (value is bool)
                return (bool)value ? "1" : "0";

            if (value is IFormattable)
                return ((IFormattable)value).ToString(null, CultureInfo.InvariantCulture);

            return value.ToString();
        }
    }

    public sealed class VisionProtocolResponse
    {
        public string RawLine { get; private set; }
        public string Header { get; private set; }
        public string Module { get; private set; }
        public string Command { get; private set; }
        public string[] Fields { get; private set; }
        public Dictionary<string, string> Values { get; private set; }

        public bool IsAck
        {
            get { return string.Equals(Header, "ACK", StringComparison.OrdinalIgnoreCase); }
        }

        public bool IsError
        {
            get { return string.Equals(Header, "ERR", StringComparison.OrdinalIgnoreCase); }
        }

        public bool IsPush
        {
            get
            {
                return string.Equals(Header, VisionProtocolPushCommands.ExposureDone, StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(Header, VisionProtocolPushCommands.LegacyExposureDone, StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(Header, VisionProtocolPushCommands.Alarm, StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(Header, VisionProtocolPushCommands.RecipeRequest, StringComparison.OrdinalIgnoreCase);
            }
        }

        public string Payload
        {
            get
            {
                if (Fields == null || Fields.Length == 0)
                    return string.Empty;

                return Fields[Fields.Length - 1] ?? string.Empty;
            }
        }

        public string ResultToken
        {
            get
            {
                string payload = Payload;
                if (string.IsNullOrEmpty(payload))
                    return string.Empty;

                int index = payload.IndexOf(';');
                return (index >= 0 ? payload.Substring(0, index) : payload).Trim();
            }
        }

        public string ErrorMessage
        {
            get
            {
                if (IsError)
                    return Payload;

                if (string.Equals(ResultToken, "ERR", StringComparison.OrdinalIgnoreCase))
                    return Payload;

                return string.Empty;
            }
        }

        public static VisionProtocolResponse Parse(string line)
        {
            var response = new VisionProtocolResponse();
            response.RawLine = line ?? string.Empty;
            response.Fields = new string[0];
            response.Values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(line))
                return response;

            string[] parts = line.Split('|');
            response.Header = parts.Length > 0 ? parts[0].Trim() : string.Empty;

            if (response.IsAck || response.IsError)
            {
                response.Module = parts.Length > 1 ? parts[1].Trim() : string.Empty;
                response.Command = parts.Length > 2 ? parts[2].Trim() : string.Empty;
                response.Fields = CopyFields(parts, 3);
            }
            else if (response.IsPush)
            {
                response.Module = parts.Length > 1 ? parts[1].Trim() : string.Empty;
                response.Command = response.Header;
                response.Fields = CopyFields(parts, 2);
            }
            else
            {
                response.Module = parts.Length > 1 ? parts[1].Trim() : string.Empty;
                response.Command = parts.Length > 2 ? parts[2].Trim() : string.Empty;
                response.Fields = CopyFields(parts, 3);
            }

            response.ParseKeyValues();
            return response;
        }

        public bool IsAckFor(string command)
        {
            return IsAck && string.Equals(Command, command, StringComparison.OrdinalIgnoreCase);
        }

        public bool IsResult(params string[] resultTokens)
        {
            if (resultTokens == null || resultTokens.Length == 0)
                return false;

            string token = ResultToken;
            for (int i = 0; i < resultTokens.Length; i++)
            {
                if (string.Equals(token, resultTokens[i], StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        public string GetValue(string key)
        {
            if (string.IsNullOrEmpty(key) || Values == null)
                return null;

            string value;
            return Values.TryGetValue(key, out value) ? value : null;
        }

        public bool TryGetDouble(string key, out double value)
        {
            value = 0;
            string raw = GetValue(key);
            return TryParseDouble(raw, out value);
        }

        public bool TryGetDoubleAny(out double value, params string[] keys)
        {
            value = 0;
            if (keys == null)
                return false;

            for (int i = 0; i < keys.Length; i++)
            {
                if (TryGetDouble(keys[i], out value))
                    return true;
            }

            return false;
        }

        public bool TryGetInt(string key, out int value)
        {
            value = 0;
            string raw = GetValue(key);
            if (string.IsNullOrWhiteSpace(raw))
                return false;

            return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out value) ||
                   int.TryParse(raw, NumberStyles.Integer, CultureInfo.CurrentCulture, out value);
        }

        public bool TryGetIntAny(out int value, params string[] keys)
        {
            value = 0;
            if (keys == null)
                return false;

            for (int i = 0; i < keys.Length; i++)
            {
                if (TryGetInt(keys[i], out value))
                    return true;
            }

            return false;
        }

        public string GetValueAny(params string[] keys)
        {
            if (keys == null)
                return null;

            for (int i = 0; i < keys.Length; i++)
            {
                string value = GetValue(keys[i]);
                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }

            return null;
        }

        public static bool TryParseDouble(string raw, out double value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(raw))
                return false;

            return double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value) ||
                   double.TryParse(raw, NumberStyles.Float, CultureInfo.CurrentCulture, out value);
        }

        private static string[] CopyFields(string[] parts, int startIndex)
        {
            if (parts == null || parts.Length <= startIndex)
                return new string[0];

            var fields = new string[parts.Length - startIndex];
            Array.Copy(parts, startIndex, fields, 0, fields.Length);
            return fields;
        }

        private void ParseKeyValues()
        {
            if (Fields == null)
                return;

            foreach (string field in Fields)
                ParseKeyValuesFromField(field);
        }

        private void ParseKeyValuesFromField(string field)
        {
            if (string.IsNullOrEmpty(field))
                return;

            string[] tokens = field.Split(';');
            foreach (string token in tokens)
            {
                int eq = token.IndexOf('=');
                if (eq <= 0)
                    continue;

                string key = token.Substring(0, eq).Trim();
                string value = token.Substring(eq + 1).Trim();
                if (key.Length == 0)
                    continue;

                Values[key] = value;
            }
        }
    }

    public sealed class VisionScaleResult
    {
        public bool Success { get; set; }
        public double ScaleX { get; set; }
        public double ScaleY { get; set; }
        public string Raw { get; set; }

        public static VisionScaleResult Parse(string line)
        {
            VisionProtocolResponse response = VisionProtocolResponse.Parse(line);
            var result = new VisionScaleResult();
            result.Success = response.IsAck && response.IsResult("OK");
            result.Raw = line;
            response.TryGetDoubleAny(out var scaleX, "scaleX", "scale_x", "sx", "pixelToMmX", "pixel_to_mm_x", "resolutionX", "resX");
            response.TryGetDoubleAny(out var scaleY, "scaleY", "scale_y", "sy", "pixelToMmY", "pixel_to_mm_y", "resolutionY", "resY");
            result.ScaleX = scaleX;
            result.ScaleY = scaleY;
            return result;
        }
    }

    public sealed class VisionCameraSettingResult
    {
        public bool Success { get; set; }
        public double WidthPixel { get; set; }
        public double HeightPixel { get; set; }
        public double ScaleX { get; set; }
        public double ScaleY { get; set; }
        public string Raw { get; set; }

        public static VisionCameraSettingResult Parse(string line)
        {
            VisionProtocolResponse response = VisionProtocolResponse.Parse(line);
            var result = new VisionCameraSettingResult();
            result.Success = response.IsAck && response.IsResult("OK");
            result.Raw = line;

            response.TryGetDoubleAny(out var width, "w", "width", "imageW", "imageWidth", "image_width", "imgW");
            response.TryGetDoubleAny(out var height, "h", "height", "imageH", "imageHeight", "image_height", "imgH");
            response.TryGetDoubleAny(out var scaleX, "scaleX", "scale_x", "sx", "pixelToMmX", "pixel_to_mm_x", "mmPerPixelX", "mm_per_pixel_x", "resolutionX", "resX");
            response.TryGetDoubleAny(out var scaleY, "scaleY", "scale_y", "sy", "pixelToMmY", "pixel_to_mm_y", "mmPerPixelY", "mm_per_pixel_y", "resolutionY", "resY");

            result.WidthPixel = width;
            result.HeightPixel = height;
            result.ScaleX = scaleX;
            result.ScaleY = scaleY;
            if (result.Success && (result.WidthPixel <= 0 || result.HeightPixel <= 0 || result.ScaleX == 0 || result.ScaleY == 0))
                result.Success = false;

            return result;
        }
    }

    public sealed class VisionRotationCenterResult
    {
        public bool Success { get; set; }
        public double X0 { get; set; }
        public double Y0 { get; set; }
        public double X1 { get; set; }
        public double Y1 { get; set; }
        public string Raw { get; set; }

        public static VisionRotationCenterResult Parse(string line)
        {
            VisionProtocolResponse response = VisionProtocolResponse.Parse(line);
            var result = new VisionRotationCenterResult();
            result.Success = response.IsAck && response.IsResult("OK");
            result.Raw = line;
            response.TryGetDoubleAny(out var x0, "x0", "centerX0", "center_x0", "cx0");
            response.TryGetDoubleAny(out var y0, "y0", "centerY0", "center_y0", "cy0");
            response.TryGetDoubleAny(out var x1, "x1", "centerX1", "center_x1", "cx1");
            response.TryGetDoubleAny(out var y1, "y1", "centerY1", "center_y1", "cy1");
            result.X0 = x0;
            result.Y0 = y0;
            result.X1 = x1;
            result.Y1 = y1;
            return result;
        }
    }

    public sealed class VisionCameraSwitchResult
    {
        public bool Success { get; set; }
        public string Tool { get; set; }
        public bool LiveOn { get; set; }
        public string Raw { get; set; }

        public static VisionCameraSwitchResult Parse(string line)
        {
            VisionProtocolResponse response = VisionProtocolResponse.Parse(line);
            var result = new VisionCameraSwitchResult();
            result.Success = response.IsAck && response.IsResult("OK");
            result.Raw = line;
            result.Tool = response.GetValueAny("tool", "camera", "module") ?? string.Empty;
            string live = response.GetValueAny("live", "liveOn", "live_on");
            result.LiveOn = string.Equals(live, "1", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(live, "true", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(live, "ON", StringComparison.OrdinalIgnoreCase);
            return result;
        }
    }

    public sealed class VisionFocusStartResult
    {
        public bool Success { get; set; }
        public string Camera { get; set; }
        public string Target { get; set; }
        public string Raw { get; set; }

        public static VisionFocusStartResult Parse(string line)
        {
            VisionProtocolResponse response = VisionProtocolResponse.Parse(line);
            var result = new VisionFocusStartResult();
            result.Success = response.IsAck && response.IsResult("OK");
            result.Raw = line;
            result.Camera = response.GetValueAny("camera", "cam", "module") ?? string.Empty;
            result.Target = response.GetValueAny("target", "focusTarget", "focus_target") ?? string.Empty;
            return result;
        }
    }

    public sealed class VisionFocusValueResult
    {
        public bool Success { get; set; }
        public double Z { get; set; }
        public double Score { get; set; }
        public int PickupNo { get; set; }
        public bool IsInitial { get; set; }
        public string Raw { get; set; }

        public static VisionFocusValueResult Parse(string line)
        {
            VisionProtocolResponse response = VisionProtocolResponse.Parse(line);
            var result = new VisionFocusValueResult();
            result.Success = response.IsAck && response.IsResult("OK");
            result.Raw = line;
            response.TryGetDoubleAny(out var z, "z", "motorZ", "motor_z", "focusZ", "focus_z");
            response.TryGetDoubleAny(out var score, "score", "focusScore", "focus_score", "s");
            response.TryGetIntAny(out var pickup, "pickup", "pickupNo", "pickup_no", "collet", "colletNo", "collet_no");
            result.Z = z;
            result.Score = score;
            result.PickupNo = pickup;
            string init = response.GetValueAny("init", "initial", "first");
            result.IsInitial = string.Equals(init, "1", StringComparison.OrdinalIgnoreCase) ||
                               string.Equals(init, "true", StringComparison.OrdinalIgnoreCase) ||
                               string.Equals(init, "ON", StringComparison.OrdinalIgnoreCase);
            return result;
        }
    }

    public sealed class VisionFocusBestResult
    {
        public bool Success { get; set; }
        public double BestZ { get; set; }
        public double BestScore { get; set; }
        public string Raw { get; set; }

        public static VisionFocusBestResult Parse(string line)
        {
            VisionProtocolResponse response = VisionProtocolResponse.Parse(line);
            var result = new VisionFocusBestResult();
            result.Success = response.IsAck && response.IsResult("OK");
            result.Raw = line;
            response.TryGetDoubleAny(out var bestZ, "bestZ", "best_z", "z", "focusZ", "focus_z", "p0z", "p1z", "p2z", "p3z", "p4z");
            response.TryGetDoubleAny(out var bestScore, "bestScore", "best_score", "score", "focusScore", "focus_score", "p0s", "p1s", "p2s", "p3s", "p4s");
            result.BestZ = bestZ;
            result.BestScore = bestScore;
            return result;
        }
    }
}
