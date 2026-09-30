using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using QMC.Common.Ui.Standards;

namespace QMC.Common.Ui.Controls
{
    public enum UiStandardButtonRole
    {
        Default,
        Primary,
        Dark,
        Danger
    }

    // 기본 Button의 입력/클릭/포커스 동작은 그대로 두고 역할 색상만 제공한다.
    // 배치, 크기, 글꼴은 이 클래스에서 변경하지 않는다.
    public class UiStandardButton : Button
    {
        private UiStandardButtonRole _role;
        private bool _hasBackColorOverride;
        private bool _hasForeColorOverride;

        public UiStandardButton()
        {
            ResetBackColor();
            ResetForeColor();
            base.FlatAppearance.BorderColor = GetRoleBorderColor();
        }

        [Category("UI Standard")]
        [Description("기본 색상 역할입니다. 개별 지정한 색상과 크기·배치는 유지합니다.")]
        [DefaultValue(UiStandardButtonRole.Default)]
        [RefreshProperties(RefreshProperties.All)]
        public UiStandardButtonRole Role
        {
            get { return _role; }
            set
            {
                if (value < UiStandardButtonRole.Default || value > UiStandardButtonRole.Danger)
                    throw new InvalidEnumArgumentException(nameof(value), (int)value, typeof(UiStandardButtonRole));
                if (_role == value)
                    return;

                // FlatAppearance는 기본 Button의 속성을 그대로 노출한다.
                // 이전 역할 기본값을 쓰던 테두리만 바꾸고 개별 지정값은 보존한다.
                bool useRoleBorder = base.FlatAppearance.BorderColor == GetRoleBorderColor();
                bool useRoleBorderSize = base.FlatAppearance.BorderSize == GetRoleBorderSize();
                _role = value;
                if (!_hasBackColorOverride)
                    base.BackColor = GetRoleBackColor();
                if (!_hasForeColorOverride)
                    base.ForeColor = GetRoleForeColor();
                if (useRoleBorder)
                    base.FlatAppearance.BorderColor = GetRoleBorderColor();
                if (useRoleBorderSize)
                    base.FlatAppearance.BorderSize = GetRoleBorderSize();
            }
        }

        [Category("Appearance")]
        [Description("개별 배경색입니다. Reset하면 현재 Role의 기본색으로 돌아갑니다.")]
        public override Color BackColor
        {
            get { return base.BackColor; }
            set
            {
                _hasBackColorOverride = !value.IsEmpty;
                base.BackColor = value.IsEmpty ? GetRoleBackColor() : value;
            }
        }

        [Category("Appearance")]
        [Description("개별 글자색입니다. Reset하면 현재 Role의 기본색으로 돌아갑니다.")]
        public override Color ForeColor
        {
            get { return base.ForeColor; }
            set
            {
                _hasForeColorOverride = !value.IsEmpty;
                base.ForeColor = value.IsEmpty ? GetRoleForeColor() : value;
            }
        }

        // 기본 역할의 테두리 RGB가 Designer에 고정 저장되지 않게 직렬화 여부만 판단한다.
        // 개별 FlatAppearance를 수정하면 기본 Button처럼 해당 내용이 저장된다.
        [Category("Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public new FlatButtonAppearance FlatAppearance
        {
            get { return base.FlatAppearance; }
        }

        public override void ResetBackColor()
        {
            _hasBackColorOverride = false;
            base.BackColor = GetRoleBackColor();
        }

        public override void ResetForeColor()
        {
            _hasForeColorOverride = false;
            base.ForeColor = GetRoleForeColor();
        }

        // 색상이 우연히 기본색과 같아도 사용자가 명시적으로 지정했으면 보존한다.
        private bool ShouldSerializeBackColor() { return _hasBackColorOverride; }
        private bool ShouldSerializeForeColor() { return _hasForeColorOverride; }

        private bool ShouldSerializeFlatAppearance()
        {
            return base.FlatAppearance.BorderColor != GetRoleBorderColor() ||
                base.FlatAppearance.BorderSize != GetRoleBorderSize() ||
                !base.FlatAppearance.CheckedBackColor.IsEmpty ||
                !base.FlatAppearance.MouseDownBackColor.IsEmpty ||
                !base.FlatAppearance.MouseOverBackColor.IsEmpty;
        }

        private Color GetRoleBackColor()
        {
            switch (_role)
            {
                case UiStandardButtonRole.Primary: return UiStandardPalette.PrimaryBackColor;
                case UiStandardButtonRole.Dark: return UiStandardPalette.DarkBackColor;
                case UiStandardButtonRole.Danger: return UiStandardPalette.DangerBackColor;
                default: return UiStandardPalette.DefaultBackColor;
            }
        }

        private Color GetRoleForeColor()
        {
            switch (_role)
            {
                case UiStandardButtonRole.Primary: return UiStandardPalette.PrimaryForeColor;
                case UiStandardButtonRole.Dark: return UiStandardPalette.DarkForeColor;
                case UiStandardButtonRole.Danger: return UiStandardPalette.DangerForeColor;
                default: return UiStandardPalette.DefaultForeColor;
            }
        }

        private Color GetRoleBorderColor()
        {
            return _role == UiStandardButtonRole.Default
                ? UiStandardPalette.DefaultBorderColor : GetRoleBackColor();
        }

        private int GetRoleBorderSize()
        {
            return _role == UiStandardButtonRole.Default ? 1 : 0;
        }
    }
}
