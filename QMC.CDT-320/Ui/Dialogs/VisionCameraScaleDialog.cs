using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using QMC.CDT320;
using QMC.CDT320.Calibration;
using QMC.CDT320.VisionComm;
using QMC.Common.Logging;

namespace QMC.CDT_320.Ui.Dialogs
{
    public partial class VisionCameraScaleDialog : Form
    {
        private readonly List<CameraScaleRowInfo> _cameraRows = new List<CameraScaleRowInfo>();
        private bool _loading;
        private bool _busy;

        public static VisionCameraScaleDialog Open(IWin32Window owner)
        {
            return ModelessDialogHost.Show(
                "VisionCameraScaleDialog",
                owner,
                () => new VisionCameraScaleDialog());
        }

        public VisionCameraScaleDialog()
        {
            try
            {
                InitializeComponent();
                BuildCameraRows();
                LoadCameraScaleSettings();
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Alarm, "VISION", "VISION-CAMERA-SCALE-DIALOG-INIT", "Camera Scale 설정 창 초기화 실패: " + ex.Message);
                throw;
            }
            finally
            {
            }
        }

        private void BuildCameraRows()
        {
            _cameraRows.Clear();
            _cameraRows.Add(new CameraScaleRowInfo("Bottom Camera", "Bottom", AutoVisionChannel.Bottom));
            _cameraRows.Add(new CameraScaleRowInfo("Input Camera", "Input", AutoVisionChannel.Wafer));
            _cameraRows.Add(new CameraScaleRowInfo("Output Camera", "Output", AutoVisionChannel.Bin));
            _cameraRows.Add(new CameraScaleRowInfo("Front Side Camera", "FrontSide", AutoVisionChannel.FrontSide));
            _cameraRows.Add(new CameraScaleRowInfo("Rear Side Camera", "RearSide", AutoVisionChannel.RearSide));
        }

        private void btnReload_Click(object sender, EventArgs e)
        {
            LoadCameraScaleSettings();
        }

        private async void btnCameraSettingReq_Click(object sender, EventArgs e)
        {
            await RequestSelectedCameraSettingAsync().ConfigureAwait(true);
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            SaveCameraScaleSettings(true);
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void gridCameraScale_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (_loading || e.RowIndex < 0)
                    return;

                if (e.ColumnIndex == colWidth.Index ||
                    e.ColumnIndex == colHeight.Index ||
                    e.ColumnIndex == colScaleX.Index ||
                    e.ColumnIndex == colScaleY.Index)
                {
                    gridCameraScale.Rows[e.RowIndex].Cells[colSource.Index].Value = "Manual";
                }
            }
            catch
            {
            }
        }

        private void gridCameraScale_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            try
            {
                if (gridCameraScale.IsCurrentCellDirty)
                    gridCameraScale.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
            catch
            {
            }
        }

        private void LoadCameraScaleSettings()
        {
            try
            {
                _loading = true;
                gridCameraScale.Rows.Clear();
                VisionCameraCalibrationData data = ResolveCameraCalibrationData();
                if (data != null)
                    data.EnsureObjects();

                foreach (CameraScaleRowInfo info in _cameraRows)
                {
                    VisionCameraPixelCalibration camera = ResolvePixelCalibration(data, info.Key);
                    AddCameraScaleRow(info, camera);
                }

                SetStatus("Camera Scale 설정을 불러왔습니다.", false);
            }
            catch (Exception ex)
            {
                SetStatus("Camera Scale 표시 실패: " + ex.Message, true);
                EventLogger.Write(EventKind.Warning, "VISION", "VISION-CAMERA-SCALE-LOAD", "Camera Scale 표시 실패: " + ex.Message);
            }
            finally
            {
                _loading = false;
            }
        }

        private void AddCameraScaleRow(CameraScaleRowInfo info, VisionCameraPixelCalibration camera)
        {
            if (camera == null)
            {
                int emptyIndex = gridCameraScale.Rows.Add(info.DisplayName, "-", "-", "-", "-", "데이터 없음");
                gridCameraScale.Rows[emptyIndex].Tag = info;
                return;
            }

            int rowIndex = gridCameraScale.Rows.Add(
                info.DisplayName,
                camera.ImageWidthPixel.ToString("F0", CultureInfo.InvariantCulture),
                camera.ImageHeightPixel.ToString("F0", CultureInfo.InvariantCulture),
                camera.PixelToMmX.ToString("F9", CultureInfo.InvariantCulture),
                camera.PixelToMmY.ToString("F9", CultureInfo.InvariantCulture),
                camera.ResolutionFromVision ? "Vision" : "Manual");
            gridCameraScale.Rows[rowIndex].Tag = info;
        }

        private async Task RequestSelectedCameraSettingAsync()
        {
            try
            {
                if (_busy)
                    return;

                DataGridViewRow row = ResolveSelectedRow();
                CameraScaleRowInfo info = row != null ? row.Tag as CameraScaleRowInfo : null;
                if (info == null)
                {
                    QMC.Common.MessageDialog.Show(this, "Camera Setting을 요청할 카메라를 선택하세요.", "VISION", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!VisionCommandService.IsConnected(info.Channel))
                {
                    QMC.Common.MessageDialog.Show(this, info.DisplayName + " VisionPC가 연결되지 않았습니다. 연결 후 다시 요청하세요.", "VISION", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                SetBusy(true);
                SetStatus(info.DisplayName + " Camera Setting을 VisionPC에 요청 중입니다.", false);

                VisionCameraSettingResult result = await VisionCommandService.CameraSettingAsync(info.Channel, 5000, CancellationToken.None).ConfigureAwait(true);
                if (result == null || !result.Success)
                {
                    string raw = result != null ? result.Raw : "-";
                    throw new InvalidOperationException("VisionPC Camera Setting 응답이 유효하지 않습니다. raw=" + raw);
                }

                row.Cells[colWidth.Index].Value = result.WidthPixel.ToString("F0", CultureInfo.InvariantCulture);
                row.Cells[colHeight.Index].Value = result.HeightPixel.ToString("F0", CultureInfo.InvariantCulture);
                row.Cells[colScaleX.Index].Value = result.ScaleX.ToString("F9", CultureInfo.InvariantCulture);
                row.Cells[colScaleY.Index].Value = result.ScaleY.ToString("F9", CultureInfo.InvariantCulture);
                row.Cells[colSource.Index].Value = "Vision";

                if (!SaveCameraScaleSettings(false))
                    return;

                SetStatus(info.DisplayName + " Camera Setting을 VisionPC에서 받아 저장했습니다. Center는 Width/2, Height/2로 자동 계산됩니다.", false);
                EventLogger.Write(EventKind.Event, "VISION", "VISION-CAMERA-SETTING-REQ",
                    info.DisplayName + " Camera Setting을 VisionPC에서 받아 저장했습니다. width=" +
                    result.WidthPixel.ToString("F0", CultureInfo.InvariantCulture) +
                    ", height=" + result.HeightPixel.ToString("F0", CultureInfo.InvariantCulture) +
                    ", scaleX=" + result.ScaleX.ToString("F9", CultureInfo.InvariantCulture) +
                    ", scaleY=" + result.ScaleY.ToString("F9", CultureInfo.InvariantCulture));
            }
            catch (Exception ex)
            {
                string message = "Camera Setting 요청 실패: " + ex.Message;
                SetStatus(message, true);
                EventLogger.Write(EventKind.Alarm, "VISION", "VISION-CAMERA-SETTING-REQ-FAIL", message);
                QMC.Common.MessageDialog.Show(this, message, "VISION", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private DataGridViewRow ResolveSelectedRow()
        {
            if (gridCameraScale.CurrentRow != null && !gridCameraScale.CurrentRow.IsNewRow)
                return gridCameraScale.CurrentRow;

            if (gridCameraScale.SelectedRows.Count > 0)
                return gridCameraScale.SelectedRows[0];

            return null;
        }

        private bool SaveCameraScaleSettings(bool showMessage)
        {
            try
            {
                Form1 host = FindHostForm();
                if (host == null || host.Machine == null || host.Machine.VisionUnit == null || host.Machine.VisionUnit.Config == null)
                    throw new InvalidOperationException("VisionUnit Config가 준비되지 않아 Camera Scale을 저장할 수 없습니다.");

                VisionCameraCalibrationData data = host.Machine.VisionUnit.Config.CameraCalibration;
                if (data == null)
                {
                    data = new VisionCameraCalibrationData();
                    host.Machine.VisionUnit.Config.CameraCalibration = data;
                }

                data.EnsureObjects();
                foreach (DataGridViewRow row in gridCameraScale.Rows)
                {
                    if (row == null || row.IsNewRow)
                        continue;

                    CameraScaleRowInfo info = row.Tag as CameraScaleRowInfo;
                    if (info == null)
                        continue;

                    VisionCameraPixelCalibration camera = ResolvePixelCalibration(data, info.Key);
                    if (camera == null)
                        continue;

                    double width = ParsePositiveDouble(row.Cells[colWidth.Index].Value, info.DisplayName + " width");
                    double height = ParsePositiveDouble(row.Cells[colHeight.Index].Value, info.DisplayName + " height");
                    double scaleX = ParseNonZeroDouble(row.Cells[colScaleX.Index].Value, info.DisplayName + " scaleX");
                    double scaleY = ParseNonZeroDouble(row.Cells[colScaleY.Index].Value, info.DisplayName + " scaleY");
                    bool fromVision = string.Equals(Convert.ToString(row.Cells[colSource.Index].Value), "Vision", StringComparison.OrdinalIgnoreCase);

                    camera.ImageWidthPixel = width;
                    camera.ImageHeightPixel = height;
                    camera.ImageCenterPixelX = width / 2.0;
                    camera.ImageCenterPixelY = height / 2.0;
                    camera.PixelToMmX = scaleX;
                    camera.PixelToMmY = scaleY;
                    camera.ResolutionFromVision = fromVision;
                    camera.ResolutionUpdatedAt = DateTime.Now;
                }

                RecalculateReticleMm(data.BottomReticle, data.BottomCamera);
                RecalculateReticleMm(data.InputReticle, data.InputCamera);
                RecalculateReticleMm(data.OutputReticle, data.OutputCamera);
                data.Valid = false;
                data.UpdatedAt = DateTime.Now;
                data.UpdatedBy = "CameraScaleDialog";

                host.SaveMachineSettings();
                EventLogger.Write(EventKind.Event, "VISION", "VISION-CAMERA-SCALE-SAVE", "카메라 Pixel Scale을 VisionUnit Config에 저장했습니다.");
                if (showMessage)
                    QMC.Common.MessageDialog.Show(this, "카메라 Pixel Scale을 저장했습니다.\r\nCenter는 Width/2, Height/2로 자동 계산됩니다.\r\nVision Camera Cal Offset은 CALC/SAVE를 다시 실행해 갱신하세요.", "VISION", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return true;
            }
            catch (Exception ex)
            {
                string message = "카메라 Pixel Scale 저장 실패: " + ex.Message;
                SetStatus(message, true);
                EventLogger.Write(EventKind.Alarm, "VISION", "VISION-CAMERA-SCALE-SAVE-FAIL", message);
                if (showMessage)
                    QMC.Common.MessageDialog.Show(this, message, "VISION", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            finally
            {
            }
        }

        private VisionCameraCalibrationData ResolveCameraCalibrationData()
        {
            Form1 host = FindHostForm();
            if (host == null || host.Machine == null || host.Machine.VisionUnit == null || host.Machine.VisionUnit.Config == null)
                return null;

            if (host.Machine.VisionUnit.Config.CameraCalibration == null)
                host.Machine.VisionUnit.Config.CameraCalibration = new VisionCameraCalibrationData();

            host.Machine.VisionUnit.Config.CameraCalibration.EnsureObjects();
            return host.Machine.VisionUnit.Config.CameraCalibration;
        }

        private static VisionCameraPixelCalibration ResolvePixelCalibration(VisionCameraCalibrationData data, string key)
        {
            if (data == null)
                return null;

            switch (key)
            {
                case "Bottom": return data.BottomCamera;
                case "Input": return data.InputCamera;
                case "Output": return data.OutputCamera;
                case "FrontSide": return data.FrontSideCamera;
                case "RearSide": return data.RearSideCamera;
                default: return null;
            }
        }

        private static void RecalculateReticleMm(VisionReticleMeasurement measurement, VisionCameraPixelCalibration camera)
        {
            if (measurement == null || camera == null || !measurement.Valid)
                return;

            measurement.MmX = camera.PixelToMmOffsetX(measurement.PixelX);
            measurement.MmY = camera.PixelToMmOffsetY(measurement.PixelY);
        }

        private static double ParsePositiveDouble(object value, string name)
        {
            double parsed = ParseDouble(value, name);
            if (parsed <= 0)
                throw new InvalidOperationException(name + " 값은 0보다 커야 합니다.");

            return parsed;
        }

        private static double ParseNonZeroDouble(object value, string name)
        {
            double parsed = ParseDouble(value, name);
            if (parsed == 0)
                throw new InvalidOperationException(name + " 값은 0이 될 수 없습니다.");

            return parsed;
        }

        private static double ParseDouble(object value, string name)
        {
            string text = Convert.ToString(value);
            double parsed;
            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed))
                return parsed;
            if (double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out parsed))
                return parsed;

            throw new InvalidOperationException(name + " 값을 숫자로 변환할 수 없습니다. value=" + text);
        }

        private Form1 FindHostForm()
        {
            Control current = this;
            while (current != null)
            {
                Form1 host = current as Form1;
                if (host != null)
                    return host;
                current = current.Parent;
            }

            foreach (Form form in Application.OpenForms)
            {
                Form1 host = form as Form1;
                if (host != null)
                    return host;
            }

            return null;
        }

        private void SetBusy(bool busy)
        {
            _busy = busy;
            btnReload.Enabled = !busy;
            btnCameraSettingReq.Enabled = !busy;
            btnSave.Enabled = !busy;
            btnClose.Enabled = !busy;
            gridCameraScale.Enabled = !busy;
        }

        private void SetStatus(string text, bool alarm)
        {
            lblStatus.Text = text ?? string.Empty;
            lblStatus.ForeColor = alarm ? Color.Firebrick : Color.Black;
        }

        private sealed class CameraScaleRowInfo
        {
            public CameraScaleRowInfo(string displayName, string key, AutoVisionChannel channel)
            {
                DisplayName = displayName;
                Key = key;
                Channel = channel;
            }

            public string DisplayName { get; private set; }
            public string Key { get; private set; }
            public AutoVisionChannel Channel { get; private set; }
        }
    }
}
