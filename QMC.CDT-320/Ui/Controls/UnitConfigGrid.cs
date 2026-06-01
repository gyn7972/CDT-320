using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using QMC.Common.Data.Store;
using QMC.CDT_320.Ui.Localization;

namespace QMC.CDT_320.Ui.Controls
{
    /// <summary>
    /// Recipe가 아닌 Unit Config 항목을 표시/편집하는 우측 Grid.
    /// - object를 BindConfig(cfg)로 받아 Reflection으로 Public 속성(Browsable=true)을 표시
    /// - Apply: UI 값 → object 반영 후 JSON 저장
    /// - Reload: JSON에서 다시 읽어 object 갱신 후 UI 재구성
    /// </summary>
    public partial class UnitConfigGrid : UserControl
    {
        private object _config;
        private string _savePath;

        public event EventHandler ConfigApplied;
        public event EventHandler ConfigReloaded;

        /// <summary>제목(헤더 라벨에 표시)</summary>
        public string Title
        {
            get => lblTitle.Text;
            set => lblTitle.Text = value ?? string.Empty;
        }

        public UnitConfigGrid()
        {
            InitializeComponent();
            ApplyTheme();
        }

        /// <summary>
        /// 표시할 Config 객체와 (선택)JSON 저장 경로를 바인딩.
        /// savePath가 null이면 저장 시 파일 I/O는 건너뛰고 메모리 객체만 갱신.
        /// </summary>
        public void BindConfig(object config, string savePath = null)
        {
            _config = config;
            _savePath = savePath;
            RebuildGrid();
        }

        private void ApplyTheme()
        {
            BackColor = QMC.CDT_320.Ui.UiTheme.OptionPanelBg;
            lblTitle.BackColor = QMC.CDT_320.Ui.UiTheme.OptionHeaderBg;
            lblTitle.ForeColor = QMC.CDT_320.Ui.UiTheme.OptionHeaderFg;
            lblTitle.Font = QMC.CDT_320.Ui.UiTheme.SectionFont;

            btnApply.Font = QMC.CDT_320.Ui.UiTheme.ButtonFont;
            btnReload.Font = QMC.CDT_320.Ui.UiTheme.ButtonFont;

            grid.BackgroundColor = QMC.CDT_320.Ui.UiTheme.OptionPanelBg;
            grid.DefaultCellStyle.Font = QMC.CDT_320.Ui.UiTheme.ValueFont;
            grid.ColumnHeadersDefaultCellStyle.Font = QMC.CDT_320.Ui.UiTheme.ButtonFont;
            grid.ColumnHeadersDefaultCellStyle.BackColor = QMC.CDT_320.Ui.UiTheme.OptionHeaderBg;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = QMC.CDT_320.Ui.UiTheme.OptionHeaderFg;
            grid.EnableHeadersVisualStyles = false;
        }

        private IEnumerable<PropertyInfo> EnumerateProperties()
        {
            if (_config == null)
                return Enumerable.Empty<PropertyInfo>();

            return _config.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.CanWrite)
                .Where(p =>
                {
                    var br = p.GetCustomAttributes(true).OfType<BrowsableAttribute>().FirstOrDefault();
                    return br == null || br.Browsable;
                })
                // 단순 값 타입 + string + enum 만 표시
                .Where(p => IsSupportedType(p.PropertyType))
                .OrderBy(p =>
                {
                    var cat = p.GetCustomAttributes(true).OfType<CategoryAttribute>().FirstOrDefault();
                    return cat?.Category ?? "General";
                })
                .ThenBy(p => GetDisplayName(p));
        }

        private static bool IsSupportedType(Type t)
        {
            if (t.IsEnum) return true;
            if (t == typeof(string)) return true;
            return t.IsPrimitive || t == typeof(decimal);
        }

        private static string GetDisplayName(PropertyInfo p)
        {
            var dn = p.GetCustomAttributes(true).OfType<DisplayNameAttribute>().FirstOrDefault();
            return string.IsNullOrWhiteSpace(dn?.DisplayName) ? p.Name : dn.DisplayName;
        }

        private static string GetCategory(PropertyInfo p)
        {
            var cat = p.GetCustomAttributes(true).OfType<CategoryAttribute>().FirstOrDefault();
            return string.IsNullOrWhiteSpace(cat?.Category) ? "General" : cat.Category;
        }

        private void RebuildGrid()
        {
            grid.Rows.Clear();

            if (_config == null)
                return;

            string lastCategory = null;
            foreach (var p in EnumerateProperties())
            {
                string cat = GetCategory(p);
                if (!string.Equals(cat, lastCategory, StringComparison.Ordinal))
                {
                    int catIdx = grid.Rows.Add($"── {cat} ──", string.Empty);
                    var catRow = grid.Rows[catIdx];
                    catRow.ReadOnly = true;
                    catRow.DefaultCellStyle.BackColor = Color.Gainsboro;
                    catRow.DefaultCellStyle.Font = new Font(grid.Font, FontStyle.Bold);
                    catRow.Tag = null; // 카테고리 행은 PropertyInfo 없음
                    lastCategory = cat;
                }

                object val = null;
                try { val = p.GetValue(_config, null); } catch { /* ignore */ }

                int idx = grid.Rows.Add(GetDisplayName(p), val?.ToString() ?? string.Empty);
                grid.Rows[idx].Tag = p;
                grid.Rows[idx].Cells[0].ReadOnly = true;
            }
        }

        private void btnApply_Click(object sender, EventArgs e)
        {
            if (_config == null) return;

            try
            {
                foreach (DataGridViewRow row in grid.Rows)
                {
                    var pi = row.Tag as PropertyInfo;
                    if (pi == null) continue;

                    string text = row.Cells[1].Value?.ToString() ?? string.Empty;
                    object converted = ConvertValue(text, pi.PropertyType);
                    pi.SetValue(_config, converted, null);
                }

                SaveJson();
                ConfigApplied?.Invoke(this, EventArgs.Empty);
                QMC.Common.MessageDialog.Show("Config 저장 완료", "Unit Config",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show("저장 실패: " + ex.Message, "Unit Config",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnReload_Click(object sender, EventArgs e)
        {
            if (_config == null) return;

            try
            {
                LoadJson();
                RebuildGrid();
                ConfigReloaded?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                QMC.Common.MessageDialog.Show("로드 실패: " + ex.Message, "Unit Config",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static object ConvertValue(string text, Type t)
        {
            if (t == typeof(string)) return text;
            if (string.IsNullOrWhiteSpace(text))
                return t.IsValueType ? Activator.CreateInstance(t) : null;

            if (t.IsEnum) return Enum.Parse(t, text, true);
            if (t == typeof(bool))
            {
                if (bool.TryParse(text, out var b)) return b;
                return text == "1" || text.Equals("yes", StringComparison.OrdinalIgnoreCase);
            }
            return Convert.ChangeType(text, t, System.Globalization.CultureInfo.InvariantCulture);
        }

        private static DataContractJsonSerializerSettings CreateJsonSettings()
        {
            return new DataContractJsonSerializerSettings
            {
                UseSimpleDictionaryFormat = true,
                EmitTypeInformation = EmitTypeInformation.Never
            };
        }

        private void SaveJson()
        {
            if (string.IsNullOrWhiteSpace(_savePath) || _config == null) return;

            var dir = Path.GetDirectoryName(_savePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            using (var fs = File.Create(_savePath))
                JsonPrettySerializer.WriteObject(fs, _config.GetType(), _config, CreateJsonSettings());
        }

        private void LoadJson()
        {
            if (string.IsNullOrWhiteSpace(_savePath) || !File.Exists(_savePath) || _config == null) return;

            var type = _config.GetType();
            var serializer = new DataContractJsonSerializer(type, CreateJsonSettings());

            object loaded;
            using (var fs = File.OpenRead(_savePath))
            {
                loaded = serializer.ReadObject(fs);
            }
            if (loaded == null) return;

            // DataContractJsonSerializer는 새 인스턴스를 생성하므로,
            // 기존 _config 참조를 유지하기 위해 public read/write 속성만 복사한다.
            foreach (var p in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                  .Where(p => p.CanRead && p.CanWrite))
            {
                try
                {
                    var value = p.GetValue(loaded, null);
                    p.SetValue(_config, value, null);
                }
                catch { /* ignore unsettable properties */ }
            }
        }
    }
}
