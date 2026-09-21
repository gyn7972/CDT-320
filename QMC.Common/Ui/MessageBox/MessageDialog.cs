using System;
using System.Windows.Forms;
using System.Resources;
using QMC.Common.Localization;

namespace QMC.Common
{
    public static class MessageDialog
    {
        private static readonly LocalizationService _defaultLocalization = new LocalizationService(
            new ILocalizationSource[]
            {
                new ResourceLocalizationSource(new ResourceManager(
                    "QMC.Common.Localization.Resources.CommonStrings", typeof(MessageDialog).Assembly), "en")
            }, new[] { "ko", "en", "zh-CN", "ja" }, "ko", new[] { "en", "ko" });
        private static LocalizationService _localization = _defaultLocalization;

        /// <summary>호스트의 언어 서비스를 연결합니다. 메시지의 반환값과 명령은 번역하지 않습니다.</summary>
        public static LocalizationService Localization
        {
            get { return _localization; }
            set { _localization = value ?? _defaultLocalization; }
        }

        internal static string ButtonCaption(string original)
        {
            string key;
            switch ((original ?? string.Empty).Replace("&", string.Empty).ToUpperInvariant())
            {
                case "OK": case "확인": key = "ok"; break;
                case "YES": case "예": key = "yes"; break;
                case "NO": case "아니오": key = "no"; break;
                case "CANCEL": case "취소": key = "cancel"; break;
                case "RETRY": case "재시도": key = "retry"; break;
                case "SKIP": case "건너뛰기": key = "skip"; break;
                default: return original;
            }
            return Localization.GetString("common.message.button." + key);
        }

        internal static string TitleCaption(string original)
        {
            string key;
            switch ((original ?? string.Empty).ToUpperInvariant())
            {
                case "MESSAGE": case "메시지": key = "message"; break;
                case "ERROR": case "오류": key = "error"; break;
                case "WARNING": case "경고": key = "warning"; break;
                case "INFORMATION": case "NOTIFICATION": case "알림": key = "information"; break;
                case "CONFIRM": case "CONFIRMATION": case "확인": key = "confirm"; break;
                case "QUESTION": case "질문": key = "question"; break;
                default: return original;
            }
            return Localization.GetString("common.message.title." + key);
        }

        public static DialogResult Show(string text)
        {
            try
            {
                return Show(null, text, "Message", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch
            {
                return DialogResult.None;
            }
            finally
            {
            }
        }

        public static DialogResult Show(string text, string caption)
        {
            try
            {
                return Show(null, text, caption, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch
            {
                return DialogResult.None;
            }
            finally
            {
            }
        }

        public static DialogResult Show(string text, string caption, MessageBoxButtons buttons)
        {
            try
            {
                return Show(null, text, caption, buttons, MessageBoxIcon.Information);
            }
            catch
            {
                return DialogResult.None;
            }
            finally
            {
            }
        }

        public static DialogResult Show(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            try
            {
                return Show(null, text, caption, buttons, icon);
            }
            catch
            {
                return DialogResult.None;
            }
            finally
            {
            }
        }

        public static DialogResult Show(IWin32Window owner, string text, string caption)
        {
            try
            {
                return Show(owner, text, caption, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch
            {
                return DialogResult.None;
            }
            finally
            {
            }
        }

        public static DialogResult Show(IWin32Window owner, string text, string caption, MessageBoxButtons buttons)
        {
            try
            {
                return Show(owner, text, caption, buttons, MessageBoxIcon.Information);
            }
            catch
            {
                return DialogResult.None;
            }
            finally
            {
            }
        }

        public static DialogResult Show(IWin32Window owner, string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            try
            {
                if (buttons == MessageBoxButtons.YesNo)
                    return ShowYesNo(owner, caption, text, "예", "아니오", DialogResult.Yes, DialogResult.No);

                if (buttons == MessageBoxButtons.OKCancel)
                    return ShowYesNo(owner, caption, text, "확인", "취소", DialogResult.OK, DialogResult.Cancel);

                if (buttons == MessageBoxButtons.YesNoCancel)
                    return ShowYesNoCancel(owner, caption, text, "예", "아니오", "취소");

                return ShowOk(owner, caption, text);
            }
            catch
            {
                return DialogResult.None;
            }
            finally
            {
            }
        }

        private static DialogResult ShowOk(IWin32Window owner, string caption, string text)
        {
            try
            {
                using (MessageBoxOk dialog = new MessageBoxOk())
                {
                    dialog.StartPosition = owner == null ? FormStartPosition.CenterScreen : FormStartPosition.CenterParent;
                    if (owner == null)
                        return dialog.ShowDialog(caption, text);

                    dialog.Title = caption;
                    dialog.Message = text;
                    return dialog.ShowDialog(owner);
                }
            }
            catch
            {
                return DialogResult.None;
            }
            finally
            {
            }
        }

        private static DialogResult ShowYesNo(IWin32Window owner, string caption, string text, string yesText, string noText, DialogResult yesResult, DialogResult noResult)
        {
            try
            {
                using (MessageBoxYesNo dialog = new MessageBoxYesNo())
                {
                    DialogResult result = dialog.ShowDialog(caption, text, owner, new[] { yesText, noText });
                    return result == DialogResult.Yes ? yesResult : noResult;
                }
            }
            catch
            {
                return DialogResult.None;
            }
            finally
            {
            }
        }

        private static DialogResult ShowYesNoCancel(IWin32Window owner, string caption, string text, string yesText, string noText, string cancelText)
        {
            try
            {
                using (MessageBoxYesNo dialog = new MessageBoxYesNo())
                {
                    return dialog.ShowDialog(caption, text, owner, new[] { yesText, noText, cancelText });
                }
            }
            catch
            {
                return DialogResult.None;
            }
            finally
            {
            }
        }
    }
}
