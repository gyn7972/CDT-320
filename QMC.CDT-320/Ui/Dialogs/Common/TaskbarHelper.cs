using System;
using System.Runtime.InteropServices;

namespace QMC.CDT_320.Ui.Dialogs
{
    internal static class TaskbarHelper
    {
        [DllImport("shell32.dll", SetLastError = true)]
        private static extern int SHGetPropertyStoreForWindow(IntPtr hwnd, ref Guid iid, out IPropertyStore propertyStore);

        [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99")]
        private interface IPropertyStore
        {
            int GetCount(out uint cProps);
            int GetAt(uint iProp, out PropertyKey pkey);
            int GetValue(ref PropertyKey key, out PropVariant pv);
            int SetValue(ref PropertyKey key, ref PropVariant pv);
            int Commit();
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        private struct PropertyKey
        {
            public Guid fmtid;
            public uint pid;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct PropVariant
        {
            [FieldOffset(0)] public ushort vt;
            [FieldOffset(8)] public IntPtr pointerValue;

            public static PropVariant FromString(string value)
            {
                return new PropVariant
                {
                    vt = 31,
                    pointerValue = Marshal.StringToCoTaskMemUni(value)
                };
            }

            public void Clear()
            {
                if (pointerValue != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(pointerValue);
                    pointerValue = IntPtr.Zero;
                }
            }
        }

        public static void SetAppId(IntPtr hwnd, string appId)
        {
            if (hwnd == IntPtr.Zero || string.IsNullOrWhiteSpace(appId)) return;

            IPropertyStore propStore;
            Guid iid = new Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99");
            int hr = SHGetPropertyStoreForWindow(hwnd, ref iid, out propStore);
            if (hr != 0 || propStore == null) return;

            var key = new PropertyKey
            {
                fmtid = new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3"),
                pid = 5
            };
            var value = PropVariant.FromString(appId);
            try
            {
                propStore.SetValue(ref key, ref value);
                propStore.Commit();
            }
            finally
            {
                value.Clear();
                Marshal.ReleaseComObject(propStore);
            }
        }
    }
}
