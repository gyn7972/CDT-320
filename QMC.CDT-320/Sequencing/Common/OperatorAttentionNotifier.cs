using System;
using System.Threading;
using System.Threading.Tasks;
using QMC.Common;

namespace QMC.CDT320.Sequencing
{
    /// <summary>
    /// [비전 작업자 확인 2026-08-25] 시퀀스가 어떤 대기 Task를 오래 기다릴 때 작업자를 호출하는 도우미.
    /// 대기 자체는 바꾸지 않고, grace 경과 후 interval 간격으로 부저 1회(짧게)와
    /// 경광등 녹+황(작업자 확인 표시)을 반복한다. 대기가 끝나면 경광등을 현재 상태 색으로 복원한다.
    /// 알림 실패는 전부 무해 처리되어 시퀀스 결과에 영향을 주지 않는다.
    /// (부저/경광등 표시는 BarcodeOperatorPromptService의 작업자 호출 규칙(2026-08-05)을 따르되,
    ///  사용자 확정(2026-08-25)에 따라 호출음은 간격마다 1회씩 짧게 울린다.)
    /// </summary>
    internal static class OperatorAttentionNotifier
    {
        private const int BeepOnMs = 250;
        private const int MinDelayMs = 1000;

        /// <summary>
        /// waitTask가 끝날 때까지 기다리며 주기 알림을 울린다. 결과와 예외는 waitTask의 것을 그대로 전달한다.
        /// </summary>
        public static async Task<int> AwaitWithAttentionAsync(
            Task<int> waitTask,
            MachineSequenceContext context,
            string description,
            int graceMs,
            int intervalMs)
        {
            if (waitTask == null)
                return -1;
            if (waitTask.IsCompleted)
                return await waitTask.ConfigureAwait(false);

            bool attentionShown = false;
            try
            {
                int nextDelayMs = Math.Max(MinDelayMs, graceMs);
                while (!waitTask.IsCompleted)
                {
                    // Delay에 취소 토큰을 걸지 않는다 — 취소는 waitTask(게이트 링크 토큰)가 스스로
                    // 깨어나 예외로 전달하고, 남은 Delay 타이머는 무해하게 소멸한다(미관찰 예외 없음).
                    Task completed = await Task.WhenAny(waitTask, Task.Delay(nextDelayMs)).ConfigureAwait(false);
                    if (ReferenceEquals(completed, waitTask))
                        break;

                    attentionShown = true;
                    nextDelayMs = Math.Max(MinDelayMs, intervalMs);
                    BeepOnceAndShowAttention(context, description);
                }

                return await waitTask.ConfigureAwait(false);
            }
            finally
            {
                if (attentionShown)
                    RestoreAttention(context);
            }
        }

        private static void BeepOnceAndShowAttention(MachineSequenceContext context, string description)
        {
            try
            {
                var panel = context != null && context.Machine != null ? context.Machine.OpPanelUnit : null;
                if (panel == null)
                    return;

                try { panel.TowerLampOperatorAttention(); }
                catch { }

                Log.Write("Main", "SYSTEM", "OperatorAttention",
                    "작업자 확인 대기 호출음(부저 1회). desc=" + (description ?? "-") + " - Wait");

                // 부저는 시퀀스 대기 루프를 막지 않도록 백그라운드에서 짧게 1회만 울린다.
                Task.Run(async () =>
                {
                    try
                    {
                        panel.Buzzer?.On();
                        await Task.Delay(BeepOnMs).ConfigureAwait(false);
                        panel.Buzzer?.Off();
                    }
                    catch
                    {
                        try { panel.Buzzer?.Off(); }
                        catch { }
                    }
                });
            }
            catch
            {
            }
        }

        private static void RestoreAttention(MachineSequenceContext context)
        {
            try
            {
                var panel = context != null && context.Machine != null ? context.Machine.OpPanelUnit : null;
                if (panel == null)
                    return;

                try { panel.Buzzer?.Off(); }
                catch { }

                // 작업자 확인 표시(녹+황)를 현재 장비 상태 색으로 되돌린다(Barcode 프롬프트 복원 규칙 미러).
                if (context.Controller != null && context.Controller.Status == EquipmentStatus.AutoRunning)
                    panel.TowerLampRunning();
                else
                    panel.TowerLampWarning();
            }
            catch
            {
            }
        }
    }
}
