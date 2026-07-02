using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace QMC.Common.Diagnostics
{
    public static class CrashDumpWriter
    {
        [Flags]
        private enum MiniDumpType : uint
        {
            MiniDumpNormal = 0x00000000,
            MiniDumpWithDataSegs = 0x00000001,
            MiniDumpWithHandleData = 0x00000004,
            MiniDumpWithUnloadedModules = 0x00000020,
            MiniDumpWithProcessThreadData = 0x00000100,
            MiniDumpWithFullMemoryInfo = 0x00000800,
            MiniDumpWithThreadInfo = 0x00001000,
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        private struct MiniDumpExceptionInformation
        {
            public uint ThreadId;
            public IntPtr ExceptionPointers;
            [MarshalAs(UnmanagedType.Bool)]
            public bool ClientPointers;
        }

        [DllImport("dbghelp.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        private static extern bool MiniDumpWriteDump(
            IntPtr hProcess,
            uint processId,
            IntPtr hFile,
            MiniDumpType dumpType,
            IntPtr exceptionParam,
            IntPtr userStreamParam,
            IntPtr callbackParam);

        [DllImport("kernel32.dll", ExactSpelling = true)]
        private static extern uint GetCurrentThreadId();

        public static string WriteCurrentProcessDump(string source, Exception exception)
        {
            string directory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CrashDumps");
            Directory.CreateDirectory(directory);

            string safeSource = SanitizeFilePart(source);
            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
            using (Process process = Process.GetCurrentProcess())
            {
                string basePath = Path.Combine(directory, process.ProcessName + "_" + stamp + "_" + process.Id + "_" + safeSource);
                string dumpPath = basePath + ".dmp";
                string textPath = basePath + ".txt";

                WriteDumpFile(process, dumpPath);
                WriteTextReport(textPath, source, exception, dumpPath);

                return dumpPath;
            }
        }

        private static void WriteDumpFile(Process process, string dumpPath)
        {
            IntPtr exceptionParam = IntPtr.Zero;
            try
            {
                IntPtr exceptionPointers = Marshal.GetExceptionPointers();
                if (exceptionPointers != IntPtr.Zero)
                {
                    MiniDumpExceptionInformation info = new MiniDumpExceptionInformation
                    {
                        ThreadId = GetCurrentThreadId(),
                        ExceptionPointers = exceptionPointers,
                        ClientPointers = false
                    };

                    exceptionParam = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(MiniDumpExceptionInformation)));
                    Marshal.StructureToPtr(info, exceptionParam, false);
                }

                using (FileStream stream = new FileStream(dumpPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    MiniDumpType dumpType =
                        MiniDumpType.MiniDumpNormal |
                        MiniDumpType.MiniDumpWithDataSegs |
                        MiniDumpType.MiniDumpWithHandleData |
                        MiniDumpType.MiniDumpWithUnloadedModules |
                        MiniDumpType.MiniDumpWithProcessThreadData |
                        MiniDumpType.MiniDumpWithFullMemoryInfo |
                        MiniDumpType.MiniDumpWithThreadInfo;

                    bool ok = MiniDumpWriteDump(
                        process.Handle,
                        (uint)process.Id,
                        stream.SafeFileHandle.DangerousGetHandle(),
                        dumpType,
                        exceptionParam,
                        IntPtr.Zero,
                        IntPtr.Zero);

                    if (!ok)
                        throw new Win32Exception(Marshal.GetLastWin32Error(), "MiniDumpWriteDump failed.");
                }
            }
            finally
            {
                if (exceptionParam != IntPtr.Zero)
                    Marshal.FreeHGlobal(exceptionParam);
            }
        }

        private static void WriteTextReport(string textPath, string source, Exception exception, string dumpPath)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(textPath, false))
                {
                    writer.WriteLine("Source: " + (source ?? ""));
                    writer.WriteLine("Time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                    writer.WriteLine("Dump: " + dumpPath);
                    writer.WriteLine("Machine: " + Environment.MachineName);
                    writer.WriteLine("OS: " + Environment.OSVersion);
                    writer.WriteLine("CLR: " + Environment.Version);
                    writer.WriteLine("Is64BitProcess: " + Environment.Is64BitProcess);
                    writer.WriteLine();
                    writer.WriteLine(exception != null ? exception.ToString() : "Unknown exception.");
                }
            }
            catch
            {
            }
        }

        private static string SanitizeFilePart(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "UNKNOWN";

            char[] invalid = Path.GetInvalidFileNameChars();
            char[] chars = value.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                for (int j = 0; j < invalid.Length; j++)
                {
                    if (chars[i] == invalid[j])
                    {
                        chars[i] = '_';
                        break;
                    }
                }
            }

            string result = new string(chars).Trim();
            return result.Length == 0 ? "UNKNOWN" : result;
        }
    }
}
