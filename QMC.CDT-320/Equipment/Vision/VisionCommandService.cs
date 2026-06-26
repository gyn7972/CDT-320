using System;
using System.Threading;
using System.Threading.Tasks;

namespace QMC.CDT320.VisionComm
{
    public static class VisionCommandService
    {
        public static VisionTcpClient ResolveClient(AutoVisionChannel channel)
        {
            switch (channel)
            {
                case AutoVisionChannel.Wafer:
                    return VisionHub.Wafer;
                case AutoVisionChannel.Bottom:
                    return VisionHub.Inspection;
                case AutoVisionChannel.Bin:
                    return VisionHub.Bin;
                case AutoVisionChannel.Main:
                    return VisionHub.Main;
                case AutoVisionChannel.FrontSide:
                    return VisionHub.TopSide != null && VisionHub.TopSide.IsConnected ? VisionHub.TopSide : VisionHub.Inspection;
                case AutoVisionChannel.RearSide:
                    return VisionHub.BottomSide != null && VisionHub.BottomSide.IsConnected ? VisionHub.BottomSide : VisionHub.Inspection;
                default:
                    return null;
            }
        }

        public static bool IsConnected(AutoVisionChannel channel)
        {
            VisionTcpClient client = ResolveClient(channel);
            return client != null && client.IsConnected;
        }

        public static async Task<bool> ExposeAsync(AutoVisionChannel channel, int index, int timeoutMs, CancellationToken ct)
        {
            VisionTcpClient client = ResolveClient(channel);
            if (client == null)
                return false;

            return await client.ExposeAsync(index, timeoutMs, ct).ConfigureAwait(false);
        }

        public static async Task<bool> GrabAsync(AutoVisionChannel channel, int index, int timeoutMs, CancellationToken ct)
        {
            VisionTcpClient client = ResolveClient(channel);
            if (client == null)
                return false;

            return await client.GrabAsync(index, timeoutMs, ct).ConfigureAwait(false);
        }

        public static async Task<MatchResultDto> MatchAsync(AutoVisionChannel channel, string finder, int index, int timeoutMs, CancellationToken ct)
        {
            VisionTcpClient client = ResolveClient(channel);
            if (client == null)
                return new MatchResultDto { Success = false, RawError = "Vision client is null." };

            return await client.MatchAsync(finder, index, timeoutMs, ct).ConfigureAwait(false);
        }

        public static async Task<InspectionResultDto> InspectAsync(AutoVisionChannel channel, string inspector, int index, int timeoutMs, CancellationToken ct)
        {
            VisionTcpClient client = ResolveClient(channel);
            if (client == null)
                return new InspectionResultDto { IsPass = false, Raw = "Vision client is null." };

            return await client.InspectAsync(inspector, index, timeoutMs, ct).ConfigureAwait(false);
        }

        public static async Task<bool> MatchAsyncStartAsync(AutoVisionChannel channel, string finder, int index, int timeoutMs, CancellationToken ct)
        {
            VisionTcpClient client = ResolveClient(channel);
            if (client == null)
                return false;

            VisionProtocolResponse response = await client.SendCommandAsync(
                VisionProtocolCommand.MatchAsync,
                timeoutMs,
                ct,
                finder,
                index).ConfigureAwait(false);
            return response.IsAck;
        }

        public static async Task<AsyncMatchPoll> PollMatchResultAsync(AutoVisionChannel channel, string finder, int timeoutMs, CancellationToken ct)
        {
            VisionTcpClient client = ResolveClient(channel);
            if (client == null)
                return new AsyncMatchPoll { Error = true, Raw = "Vision client is null." };

            VisionProtocolResponse response = await client.SendCommandAsync(
                VisionProtocolCommand.MatchResult,
                timeoutMs,
                ct,
                finder).ConfigureAwait(false);
            return AsyncMatchPoll.Parse(response.RawLine);
        }

        public static async Task<bool> InspectAsyncStartAsync(AutoVisionChannel channel, string inspector, int index, int timeoutMs, CancellationToken ct)
        {
            VisionTcpClient client = ResolveClient(channel);
            if (client == null)
                return false;

            VisionProtocolResponse response = await client.SendCommandAsync(
                VisionProtocolCommand.InspectAsync,
                timeoutMs,
                ct,
                inspector,
                index).ConfigureAwait(false);
            return response.IsAck;
        }

        public static async Task<AsyncInspectPoll> PollInspectResultAsync(AutoVisionChannel channel, string inspector, int timeoutMs, CancellationToken ct)
        {
            VisionTcpClient client = ResolveClient(channel);
            if (client == null)
                return new AsyncInspectPoll { Error = true, Raw = "Vision client is null." };

            VisionProtocolResponse response = await client.SendCommandAsync(
                VisionProtocolCommand.InspectResult,
                timeoutMs,
                ct,
                inspector).ConfigureAwait(false);
            return AsyncInspectPoll.Parse(response.RawLine);
        }

        public static async Task<VisionFocusStartResult> FocusStartAsync(AutoVisionChannel channel, string camera, string target, int timeoutMs, CancellationToken ct)
        {
            VisionTcpClient client = ResolveClient(channel);
            if (client == null)
                return new VisionFocusStartResult { Success = false, Raw = "Vision client is null." };

            VisionProtocolResponse response = await client.SendCommandAsync(VisionProtocolCommand.FocusStart, timeoutMs, ct, camera, target).ConfigureAwait(false);
            return VisionFocusStartResult.Parse(response.RawLine);
        }

        public static async Task<VisionFocusValueResult> FocusValueAsync(AutoVisionChannel channel, double motorZ, string camera, string target, int pickupNo, bool initial, int timeoutMs, CancellationToken ct)
        {
            VisionTcpClient client = ResolveClient(channel);
            if (client == null)
                return new VisionFocusValueResult { Success = false, Raw = "Vision client is null." };

            VisionProtocolResponse response = await client.SendCommandAsync(
                VisionProtocolCommand.FocusValue,
                timeoutMs,
                ct,
                motorZ,
                camera,
                target,
                pickupNo,
                initial ? 1 : 0).ConfigureAwait(false);
            return VisionFocusValueResult.Parse(response.RawLine);
        }

        public static async Task<VisionFocusBestResult> FocusBestAsync(AutoVisionChannel channel, string camera, string target, int pickupNo, int timeoutMs, CancellationToken ct)
        {
            VisionTcpClient client = ResolveClient(channel);
            if (client == null)
                return new VisionFocusBestResult { Success = false, Raw = "Vision client is null." };

            VisionProtocolResponse response = await client.SendCommandAsync(
                VisionProtocolCommand.FocusBest,
                timeoutMs,
                ct,
                camera,
                target,
                pickupNo).ConfigureAwait(false);
            return VisionFocusBestResult.Parse(response.RawLine);
        }

        public static async Task<VisionScaleResult> ScaleAsync(AutoVisionChannel channel, double chipWidthMm, double chipHeightMm, int timeoutMs, CancellationToken ct)
        {
            VisionTcpClient client = ResolveClient(channel);
            if (client == null)
                return new VisionScaleResult { Success = false, Raw = "Vision client is null." };

            VisionProtocolResponse response = await client.SendCommandAsync(VisionProtocolCommand.Scale, timeoutMs, ct, chipWidthMm, chipHeightMm).ConfigureAwait(false);
            return VisionScaleResult.Parse(response.RawLine);
        }

        public static async Task<VisionRotationCenterResult> RotationCenterAsync(AutoVisionChannel channel, int timeoutMs, CancellationToken ct)
        {
            VisionTcpClient client = ResolveClient(channel);
            if (client == null)
                return new VisionRotationCenterResult { Success = false, Raw = "Vision client is null." };

            VisionProtocolResponse response = await client.SendCommandAsync(VisionProtocolCommand.RotationCenter, timeoutMs, ct).ConfigureAwait(false);
            return VisionRotationCenterResult.Parse(response.RawLine);
        }

        public static async Task<bool> DistortAsync(AutoVisionChannel channel, int timeoutMs, CancellationToken ct)
        {
            VisionTcpClient client = ResolveClient(channel);
            if (client == null)
                return false;

            VisionProtocolResponse response = await client.SendCommandAsync(VisionProtocolCommand.Distort, timeoutMs, ct).ConfigureAwait(false);
            return response.IsAck;
        }

        public static async Task<VisionCameraSwitchResult> SwitchCameraAsync(AutoVisionChannel channel, string tool, bool liveOn, int timeoutMs, CancellationToken ct)
        {
            VisionTcpClient client = ResolveClient(channel);
            if (client == null)
                return new VisionCameraSwitchResult { Success = false, Raw = "Vision client is null." };

            VisionProtocolResponse response = await client.SendCommandAsync(VisionProtocolCommand.CameraSwitch, timeoutMs, ct, tool, liveOn ? 1 : 0).ConfigureAwait(false);
            return VisionCameraSwitchResult.Parse(response.RawLine);
        }
    }
}
