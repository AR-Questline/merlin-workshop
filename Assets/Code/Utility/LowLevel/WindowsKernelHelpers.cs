#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using static Awaken.Utility.LowLevel.WindowsKernelOperations;

namespace Awaken.Utility.LowLevel {
    public static class WindowsKernelHelpers {
        [UnityEngine.Scripting.Preserve] 
        public static uint Start(string path, string dir, bool hidden = false) {
            ProcessCreationFlags flags = hidden ? ProcessCreationFlags.CREATE_NO_WINDOW : ProcessCreationFlags.NONE;
            STARTUPINFO startupinfo = new STARTUPINFO {
                cb = (uint)Marshal.SizeOf<STARTUPINFO>()
            };
            PROCESS_INFORMATION processinfo = new PROCESS_INFORMATION();
            if (!WindowsKernelOperations.CreateProcessW(null, path, IntPtr.Zero, IntPtr.Zero, false, flags, IntPtr.Zero, dir, ref startupinfo, ref processinfo)) {
                throw new Win32Exception();
            }

            return processinfo.dwProcessId;
        }

        [UnityEngine.Scripting.Preserve] 
        public static int KillProcess(uint pid) {
            IntPtr handle = WindowsKernelOperations.OpenProcess(ProcessAccessRights.PROCESS_ALL_ACCESS, false, pid);

            if (handle == IntPtr.Zero) {
                return -1;
            }
            if (!WindowsKernelOperations.TerminateProcess(handle, 0)) {
                throw new Win32Exception();
            }
            if (!WindowsKernelOperations.CloseHandle(handle)) {
                throw new Win32Exception();
            }

            return 0;
        }

        [UnityEngine.Scripting.Preserve] 
        public static int KillCurrentProcess() {
            IntPtr handle = WindowsKernelOperations.GetCurrentProcess();

            if (handle == IntPtr.Zero) {
                return -1;
            }
            if (!WindowsKernelOperations.TerminateProcess(handle, 0)) {
                throw new Win32Exception();
            }
            if (!WindowsKernelOperations.CloseHandle(handle)) {
                throw new Win32Exception();
            }

            return 0;
        }
    }
}
#endif