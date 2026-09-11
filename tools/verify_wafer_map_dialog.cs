// 독립 다이얼로그/Designer만 실행하는 STA 검증. Form1과 장비 초기화는 링크하지 않는다.
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using QMC.CDT320.DieMaps;
using QMC.CDT_320.Ui.Dialogs;

internal static class VerifyWaferMapDialog
{
    private static int _checks;
    private static string _output;

    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            _output = Path.GetFullPath(args[0]);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            WindowsFormsSynchronizationContext.AutoInstall = false;
            SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());
            Control.CheckForIllegalCrossThreadCalls = true;
            using (var dialog = Open(new WaferMapGenerationSettings(287.400m, 10.370m, 7.913m, .300m, .300m)))
            {
                Check(dialog.GeneratedMap == null, "initial result requires generation");
                Check(Find<NumericUpDown>(dialog, "numDiameter").Value == 287.400m, "initial diameter retained");
                Click(dialog, "btnGenerate");
                AwaitMap(dialog, 684);
                var original = dialog.GeneratedMap;
                Check(original.UsedColumns == 26 && original.UsedRows == 34, "684 map dimensions");
                Check(original.EdgeCounts.Top >= 3, "top correction sample available");
                Capture(dialog, "dialog-original.png");

                Set(dialog, "numEdgeTop", original.EdgeCounts.Top - 2);
                Check(dialog.GeneratedMap == null, "pending edge edit cannot return stale result");
                Click(dialog, "btnApplyEdges");
                AwaitMap(dialog, 682);
                Check(dialog.GeneratedMap.EdgeCounts.Top == original.EdgeCounts.Top - 2, "requested top count applied");
                Check(object.ReferenceEquals(dialog.GeneratedMap.BaseMap, original), "base result retained");
                Check(dialog.GeneratedMap.Dies.All(d => original.Dies.Contains(d)), "coordinates preserved by adjustment");
                Capture(dialog, "dialog-adjusted.png");

                Set(dialog, "numEdgeTop", original.EdgeCounts.Top - 1);
                Click(dialog, "btnApplyEdges");
                AwaitMap(dialog, 683);
                Check(dialog.GeneratedMap.EdgeCounts.Top == original.EdgeCounts.Top - 1, "repeated adjustment is not cumulative");
                Click(dialog, "btnRestore");
                AwaitMap(dialog, 684);
                Check(object.ReferenceEquals(dialog.GeneratedMap, original), "restore returns original result");
                Check(Find<NumericUpDown>(dialog, "numEdgeTop").Value == original.EdgeCounts.Top, "restore resets counts");

                Set(dialog, "numEdgeTop", original.EdgeCounts.Top - 1);
                Set(dialog, "numEdgeBottom", original.EdgeCounts.Bottom - 1);
                Set(dialog, "numEdgeLeft", original.EdgeCounts.Left - 1);
                Set(dialog, "numEdgeRight", original.EdgeCounts.Right - 1);
                Click(dialog, "btnApplyEdges");
                AwaitMap(dialog, 680);
                Check(dialog.GeneratedMap.EdgeCounts.Top == original.EdgeCounts.Top - 1 &&
                    dialog.GeneratedMap.EdgeCounts.Bottom == original.EdgeCounts.Bottom - 1 &&
                    dialog.GeneratedMap.EdgeCounts.Left == original.EdgeCounts.Left - 1 &&
                    dialog.GeneratedMap.EdgeCounts.Right == original.EdgeCounts.Right - 1, "all four UI fields map to the correct edge");
                Click(dialog, "btnRestore");
                AwaitMap(dialog, 684);

                Set(dialog, "numDiameter", 287.640m);
                Check(dialog.GeneratedMap == null, "geometry edit invalidates result");
                Check(!Find<Button>(dialog, "btnApplyEdges").Enabled, "geometry edit disables edge apply");
                Click(dialog, "btnGenerate");
                AwaitMap(dialog, 682);
                Check(dialog.GeneratedMap.UsedRows == 33, "new diameter changes row layout");
                Check(!dialog.GeneratedMap.IsAdjusted, "regeneration creates fresh base");

                dialog.Size = dialog.MinimumSize;
                AwaitDrawing(dialog);
                Check(Find<Button>(dialog, "btnClose").Width > 30, "minimum-size close button laid out");
                Capture(dialog, "dialog-minimum.png");

                Set(dialog, "numDiameter", 0m);
                Click(dialog, "btnGenerate");
                Wait(() => !dialog.IsBusy, "invalid generation completes");
                Check(dialog.GeneratedMap == null, "invalid geometry cannot return old map");
                Check(Find<Label>(dialog, "lblStatus").Text.Length > 0, "validation message visible");
            }

            using (var empty = Open(new WaferMapGenerationSettings(1m, 20m, 20m, 0m, 0m)))
            {
                Click(empty, "btnGenerate");
                Wait(() => !empty.IsBusy, "empty generation completes");
                Check(empty.GeneratedMap != null && empty.GeneratedMap.Count == 0, "empty result supported");
                Check(!Find<Button>(empty, "btnApplyEdges").Enabled, "empty result cannot be adjusted");
            }

            using (var zero = Open(new WaferMapGenerationSettings(200m, 0m, 0m, 0m, 0m)))
            {
                Check(Find<NumericUpDown>(zero, "numDieX").Value == 0m, "unset specification opens for editing");
            }

            VerifyPageEntry();
            using (var closing = Open(new WaferMapGenerationSettings(99.8m, .1m, .1m, 0m, 0m)))
            {
                Click(closing, "btnGenerate");
                closing.Close();
                var timer = Stopwatch.StartNew();
                while (timer.ElapsedMilliseconds < 500) { Application.DoEvents(); Thread.Sleep(5); }
                Check(closing.IsDisposed, "close during worker generation is safe");
            }
            Check(QMC.Common.MessageDialog.UnexpectedCount == 0, "no unexpected UI failures");
            Console.WriteLine("PASS: " + _checks + " dialog/entry assertions; standalone controls only.");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }

    private static WaferMapCreateDialog Open(WaferMapGenerationSettings settings)
    {
        var dialog = new WaferMapCreateDialog(settings);
        dialog.Opacity = 0;
        dialog.ShowInTaskbar = false;
        dialog.Show();
        Application.DoEvents();
        return dialog;
    }

    private static void VerifyPageEntry()
    {
        using (var host = new Form { Opacity = 0, ShowInTaskbar = false, ClientSize = new Size(1526, 764) })
        using (var page = new QMC.CDT_320.Ui.Pages.Recipe.TapeFrameSubsetPage())
        {
            page.Dock = DockStyle.Fill;
            host.Controls.Add(page);
            host.Show();
            Application.DoEvents();
            var preview = Find<Button>(page, "btnPreviewWaferMap");
            Check(preview.Visible && preview.Height >= 30, "preview entry visible");
            Check(preview.Bottom <= preview.Parent.ClientSize.Height, "preview entry fits parent row");
            Set(page, "_nDiameter", 287.640m);
            Set(page, "_nDieSizeX", 10.370m);
            Set(page, "_nDieSizeY", 7.913m);
            Set(page, "_nPitchX", .300m);
            Set(page, "_nPitchY", .300m);
            bool opened = false;
            Exception failure = null;
            using (var timer = new System.Windows.Forms.Timer { Interval = 25 })
            {
                timer.Tick += delegate
                {
                    var dialog = Application.OpenForms.OfType<WaferMapCreateDialog>().FirstOrDefault();
                    if (dialog == null) return;
                    timer.Stop();
                    try
                    {
                        Check(Find<NumericUpDown>(dialog, "numDiameter").Value == 287.640m, "entry copies precise diameter");
                        Check(Find<NumericUpDown>(dialog, "numDieY").Value == 7.913m, "entry copies die size");
                        Check(Find<NumericUpDown>(dialog, "numGapX").Value == .300m, "entry copies gap");
                        Set(dialog, "numDiameter", 300m);
                        opened = true;
                    }
                    catch (Exception ex) { failure = ex; }
                    finally { dialog.Close(); }
                };
                timer.Start();
                preview.PerformClick();
            }
            if (failure != null) throw failure;
            Check(opened, "real entry event opens independent dialog");
            Check(Find<NumericUpDown>(page, "_nDiameter").Value == 287.640m, "dialog edits do not change parent values");
            Capture(page, "dialog-entry.png");
            host.Close();
        }
    }

    private static T Find<T>(Control parent, string name) where T : Control
    {
        var found = parent.Controls.Find(name, true).OfType<T>().FirstOrDefault();
        if (found == null) throw new InvalidOperationException("Missing control: " + name);
        return found;
    }

    private static void Set(Control dialog, string name, decimal value) { Find<NumericUpDown>(dialog, name).Value = value; }
    private static void Click(Control dialog, string name)
    {
        var button = Find<Button>(dialog, name);
        if (!button.Enabled || !button.Visible) throw new InvalidOperationException("Unavailable action: " + name);
        button.PerformClick();
        Application.DoEvents();
    }
    private static void AwaitMap(WaferMapCreateDialog dialog, int count)
    {
        Wait(() => !dialog.IsBusy && dialog.GeneratedMap != null, "generation/adjustment result");
        Check(dialog.GeneratedMap.Count == count, "expected map count " + count + ", actual=" + dialog.GeneratedMap.Count + ", status=" + Find<Label>(dialog, "lblStatus").Text);
        AwaitDrawing(dialog);
    }
    private static void AwaitDrawing(WaferMapCreateDialog dialog)
    {
        var view = Find<Control>(dialog, "mapView");
        var property = view.GetType().GetProperty("IsRendering");
        Wait(() => !(bool)property.GetValue(view, null), "map bitmap rendering");
    }
    private static void Wait(Func<bool> condition, string operation)
    {
        var timer = Stopwatch.StartNew();
        while (!condition())
        {
            Application.DoEvents();
            if (timer.ElapsedMilliseconds > 30000) throw new TimeoutException(operation);
            Thread.Sleep(5);
        }
        Application.DoEvents();
    }
    private static void Check(bool value, string message)
    {
        if (!value) throw new InvalidOperationException("FAIL: " + message);
        _checks++;
    }
    private static void Capture(Control control, string name)
    {
        using (var bitmap = new Bitmap(control.Width, control.Height))
        {
            control.DrawToBitmap(bitmap, new Rectangle(0, 0, control.Width, control.Height));
            bitmap.Save(Path.Combine(_output, name), ImageFormat.Png);
        }
    }
}

namespace QMC.Common
{
    // 예상 입력 오류는 실제 다이얼로그의 상태 라벨로 처리한다. 예상 밖 메시지박스는 테스트 실패로 수집한다.
    public static class MessageDialog
    {
        public static int UnexpectedCount;
        public static DialogResult Show(IWin32Window owner, string text, string caption,
            MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            UnexpectedCount++;
            Console.Error.WriteLine("Unexpected dialog: " + text);
            return DialogResult.OK;
        }
    }
    public static class Log
    {
        public static void Write(string category, string user, string operation, string text)
        {
            Console.Error.WriteLine(operation + ": " + text);
        }
    }
}
