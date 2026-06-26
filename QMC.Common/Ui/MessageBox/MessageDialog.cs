using System;
using System.Windows.Forms;

namespace QMC.Common
{
    public static class MessageDialog
    {
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
