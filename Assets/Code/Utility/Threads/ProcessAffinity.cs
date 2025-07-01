#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using Awaken.Utility.LowLevel;
using static Awaken.Utility.LowLevel.WindowsKernelOperations;

namespace Awaken.Utility.Threads {
    /// <summary>
    /// Mostly taken from: https://github.com/dotnet/runtime/issues/82220
    /// adjusted to not use settings and removed unneeded helpers
    /// </summary>
    public static class ProcessAffinity {

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        static void InitTest() {
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += ProcessAffinity.Close;
        }
#endif

        static IntPtr s_handle;

        public static bool Setup(ushort groupID, long targetAffinity, out string result) => TrySetInformationJobObject(WindowsKernelOperations.GetCurrentProcessId(), groupID, targetAffinity, out result);

        public static bool TrySetInformationJobObject(
            uint targetProcess, ushort groupId, long cpuMask, out string result) {
            result = string.Empty;
            try {
                _GROUP_AFFINITY newAffinity = new _GROUP_AFFINITY {
                    Group = groupId,
                    Mask = new UIntPtr((ulong)cpuMask),
                    Reserved = new ushort[3]
                };

                SECURITY_ATTRIBUTES lpJobAttributes = new();

                if (s_handle == IntPtr.Zero) {
                    s_handle = WindowsKernelOperations.CreateJobObject(ref lpJobAttributes, null);

                    if (!AddProcess(GetProcessHandle(targetProcess))) {
                        result = $"{AffinityStatus.Fail}, Cannot add current process {targetProcess} to the Job, groupId:{groupId}, cpuMask:{cpuMask}";
                        return false;
                    }
                }

                int length = Marshal.SizeOf(typeof(_GROUP_AFFINITY));
                IntPtr newAffinityPtr = Marshal.AllocHGlobal(length);
                Marshal.StructureToPtr(newAffinity, newAffinityPtr, false);

                try {
                    if (WindowsKernelOperations.SetInformationJobObject(
                            s_handle,
                            JobObjectInfoType.JobObjectGroupInformationEx,
                            newAffinityPtr,
                            (uint)length)) {
                        result = $"{AffinityStatus.Success}, TrySetInformationJobObject succeeded, ProcessId:{targetProcess}, ProcessAffinity:{cpuMask}, newAffinity:{newAffinity.Group}::{newAffinity.Mask}, groupId:{groupId}, cpuMask:{cpuMask}";
                        return true;
                    }

                    result = $"{AffinityStatus.Fail}, TrySetInformationJobObject failed, ProcessAffinity:{cpuMask} failed, ErrorCode:{(int)WindowsKernelOperations.GetLastError()}, groupId:{groupId}, cpuMask:{cpuMask}";
                    return false;
                } catch (Exception ex) {
                    result = $"{AffinityStatus.InProgress}, TrySetInformationJobObject failed, {ex.Message}, groupId:{groupId}, cpuMask:{cpuMask}";
                    return false;
                }
            } catch (Exception ex) {
                result = $"{AffinityStatus.Fail}, TrySetInformationJobObject failed, {ex.Message}, ErrorCode:{(ex is Win32Exception ? (ex as Win32Exception).NativeErrorCode : ex.HResult)}, groupId:{groupId}, cpuMask:{cpuMask}";
                return false;
            }
        }

        public static bool TryGetCpuMask(string processAffinity, int[] cores, out long cpuMask, out string result) {
            cpuMask = 0;
            try {
                if (cores.Min() < 0 || cores.Max() >= Environment.ProcessorCount) {
                    result = $"{AffinityStatus.Fail}, Invalid core number(s) found in the range {processAffinity}. Core number must be in between 0 and {Environment.ProcessorCount - 1}";
                    return false;
                }

                foreach (int core in cores) {
                    cpuMask |= 1L << core;
                }
            } catch (Exception ex) {
                result = $"{AffinityStatus.Fail}, Obtaining CpuMask failed, Error={ex.Message}";
                return false;
            }

            result = $"{AffinityStatus.InProgress}";
            return true;
        }

        static bool AddProcess(IntPtr processHandle) => WindowsKernelOperations.AssignProcessToJobObject(s_handle, processHandle);

        public static void Close() {
            if (s_handle == IntPtr.Zero) return;
            WindowsKernelOperations.CloseHandle(s_handle);
            s_handle = IntPtr.Zero;
        }

        static IntPtr GetProcessHandle(uint processID) => WindowsKernelOperations.OpenProcess(ProcessAccessRights.PROCESS_ALL_ACCESS, false, processID);
    }
}
#endif