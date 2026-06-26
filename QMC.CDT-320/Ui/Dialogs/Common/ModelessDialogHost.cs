using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Dialogs
{
    internal static class ModelessDialogHost
    {
        private static readonly Dictionary<string, Form> OpenDialogs = new Dictionary<string, Form>();

        public static T Show<T>(string key, IWin32Window owner, Func<T> create, Action<T> onActivate = null)
            where T : Form
        {
            if (string.IsNullOrWhiteSpace(key))
                key = typeof(T).FullName;

            Form existing;
            if (OpenDialogs.TryGetValue(key, out existing) && existing != null && !existing.IsDisposed)
            {
                T typedExisting = existing as T;
                if (typedExisting != null)
                {
                    if (!typedExisting.Visible)
                        typedExisting.Show(ResolveOwner(owner));

                    onActivate?.Invoke(typedExisting);
                    typedExisting.BringToFront();
                    typedExisting.Activate();
                    return typedExisting;
                }

                OpenDialogs.Remove(key);
            }

            T dialog = create();
            OpenDialogs[key] = dialog;
            dialog.FormClosed += (sender, args) =>
            {
                Form current;
                if (OpenDialogs.TryGetValue(key, out current) && ReferenceEquals(current, sender))
                    OpenDialogs.Remove(key);
            };

            Form ownerForm = ResolveOwner(owner);
            if (ownerForm != null && !ownerForm.IsDisposed)
                dialog.Show(ownerForm);
            else
                dialog.Show();

            onActivate?.Invoke(dialog);
            dialog.BringToFront();
            dialog.Activate();
            return dialog;
        }

        private static Form ResolveOwner(IWin32Window owner)
        {
            Form form = owner as Form;
            if (form != null)
                return form;

            Control control = owner as Control;
            return control != null ? control.FindForm() : null;
        }
    }
}
