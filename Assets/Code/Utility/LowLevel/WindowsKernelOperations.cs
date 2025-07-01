#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System;
using System.Runtime.InteropServices;

namespace Awaken.Utility.LowLevel {
    public static class WindowsKernelOperations {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CreateProcessW(
            string lpApplicationName,
            [In] string lpCommandLine,
            IntPtr procSecAttrs,
            IntPtr threadSecAttrs,
            bool bInheritHandles,
            ProcessCreationFlags dwCreationFlags,
            IntPtr lpEnvironment,
            string lpCurrentDirectory,
            ref STARTUPINFO lpStartupInfo,
            ref PROCESS_INFORMATION lpProcessInformation
        );

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool TerminateProcess(IntPtr processHandle, uint exitCode);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(ProcessAccessRights access, bool inherit, uint processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr GetCurrentProcess();

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr CreateJobObject([In] ref SECURITY_ATTRIBUTES lpJobAttributes, string lpName);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetInformationJobObject(
            IntPtr hJob, JobObjectInfoType infoType, IntPtr lpJobObjectInfo, UInt32 cbJobObjectInfoLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool AssignProcessToJobObject(IntPtr job, IntPtr process);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern uint GetLastError();

        [DllImport("kernel32.dll")]
        public static extern uint GetCurrentProcessId();

        [StructLayout(LayoutKind.Sequential)]
        public struct STARTUPINFO {
            internal uint cb;
            internal IntPtr lpReserved;
            internal IntPtr lpDesktop;
            internal IntPtr lpTitle;
            internal uint dwX;
            internal uint dwY;
            internal uint dwXSize;
            internal uint dwYSize;
            internal uint dwXCountChars;
            internal uint dwYCountChars;
            internal uint dwFillAttribute;
            internal uint dwFlags;
            internal ushort wShowWindow;
            internal ushort cbReserved2;
            internal IntPtr lpReserved2;
            internal IntPtr hStdInput;
            internal IntPtr hStdOutput;
            internal IntPtr hStdError;
        }

        [Flags]
        public enum ProcessCreationFlags : uint {
            [UnityEngine.Scripting.Preserve] NONE = 0,
            [UnityEngine.Scripting.Preserve] CREATE_BREAKAWAY_FROM_JOB = 0x01000000,
            [UnityEngine.Scripting.Preserve] CREATE_DEFAULT_ERROR_MODE = 0x04000000,
            [UnityEngine.Scripting.Preserve] CREATE_NEW_CONSOLE = 0x00000010,
            [UnityEngine.Scripting.Preserve] CREATE_NEW_PROCESS_GROUP = 0x00000200,
            [UnityEngine.Scripting.Preserve] CREATE_NO_WINDOW = 0x08000000,
            [UnityEngine.Scripting.Preserve] CREATE_PROTECTED_PROCESS = 0x00040000,
            [UnityEngine.Scripting.Preserve] CREATE_PRESERVE_CODE_AUTHZ_LEVEL = 0x02000000,
            [UnityEngine.Scripting.Preserve] CREATE_SECURE_PROCESS = 0x00400000,
            [UnityEngine.Scripting.Preserve] CREATE_SEPARATE_WOW_VDM = 0x00000800,
            [UnityEngine.Scripting.Preserve] CREATE_SHARED_WOW_VDM = 0x00001000,
            [UnityEngine.Scripting.Preserve] CREATE_SUSPENDED = 0x00000004,
            [UnityEngine.Scripting.Preserve] CREATE_UNICODE_ENVIRONMENT = 0x00000400,
            [UnityEngine.Scripting.Preserve] DEBUG_ONLY_THIS_PROCESS = 0x00000002,
            [UnityEngine.Scripting.Preserve] DEBUG_PROCESS = 0x00000001,
            [UnityEngine.Scripting.Preserve] DETACHED_PROCESS = 0x00000008,
            [UnityEngine.Scripting.Preserve] EXTENDED_STARTUPINFO_PRESENT = 0x00080000,
            [UnityEngine.Scripting.Preserve] INHERIT_PARENT_AFFINITY = 0x00010000
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PROCESS_INFORMATION {
            internal IntPtr hProcess;
            internal IntPtr hThread;
            internal uint dwProcessId;
            internal uint dwThreadId;
        }

        [Flags]
        public enum ProcessAccessRights : uint {
            [UnityEngine.Scripting.Preserve] PROCESS_CREATE_PROCESS = 0x0080, //  Required to create a process.
            [UnityEngine.Scripting.Preserve] PROCESS_CREATE_THREAD = 0x0002, //  Required to create a thread.
            [UnityEngine.Scripting.Preserve] PROCESS_DUP_HANDLE = 0x0040, // Required to duplicate a handle using DuplicateHandle.
            [UnityEngine.Scripting.Preserve] PROCESS_QUERY_INFORMATION = 0x0400, //  Required to retrieve certain information about a process, such as its token, exit code, and priority class (see OpenProcessToken, GetExitCodeProcess, GetPriorityClass, and IsProcessInJob).
            [UnityEngine.Scripting.Preserve] PROCESS_QUERY_LIMITED_INFORMATION = 0x1000, //  Required to retrieve certain information about a process (see QueryFullProcessImageName). A handle that has the PROCESS_QUERY_INFORMATION access right is automatically granted PROCESS_QUERY_LIMITED_INFORMATION. Windows Server 2003 and Windows XP/2000:  This access right is not supported.
            [UnityEngine.Scripting.Preserve] PROCESS_SET_INFORMATION = 0x0200, //    Required to set certain information about a process, such as its priority class (see SetPriorityClass).
            [UnityEngine.Scripting.Preserve] PROCESS_SET_QUOTA = 0x0100, //  Required to set memory limits using SetProcessWorkingSetSize.
            [UnityEngine.Scripting.Preserve] PROCESS_SUSPEND_RESUME = 0x0800, // Required to suspend or resume a process.
            [UnityEngine.Scripting.Preserve] PROCESS_TERMINATE = 0x0001, //  Required to terminate a process using TerminateProcess.
            [UnityEngine.Scripting.Preserve] PROCESS_VM_OPERATION = 0x0008, //   Required to perform an operation on the address space of a process (see VirtualProtectEx and WriteProcessMemory).
            [UnityEngine.Scripting.Preserve] PROCESS_VM_READ = 0x0010, //    Required to read memory in a process using ReadProcessMemory.
            [UnityEngine.Scripting.Preserve] PROCESS_VM_WRITE = 0x0020, //   Required to write to memory in a process using WriteProcessMemory.
            [UnityEngine.Scripting.Preserve] DELETE = 0x00010000, // Required to delete the object.
            [UnityEngine.Scripting.Preserve] READ_CONTROL = 0x00020000, //   Required to read information in the security descriptor for the object, not including the information in the SACL. To read or write the SACL, you must request the ACCESS_SYSTEM_SECURITY access right. For more information, see SACL Access Right.
            [UnityEngine.Scripting.Preserve] SYNCHRONIZE = 0x00100000, //    The right to use the object for synchronization. This enables a thread to wait until the object is in the signaled state.
            [UnityEngine.Scripting.Preserve] WRITE_DAC = 0x00040000, //  Required to modify the DACL in the security descriptor for the object.
            [UnityEngine.Scripting.Preserve] WRITE_OWNER = 0x00080000, //    Required to change the owner in the security descriptor for the object.
            [UnityEngine.Scripting.Preserve] STANDARD_RIGHTS_REQUIRED = 0x000f0000,
            [UnityEngine.Scripting.Preserve] PROCESS_ALL_ACCESS = STANDARD_RIGHTS_REQUIRED | SYNCHRONIZE | 0xFFF //    All possible access rights for a process object.
        }

        public enum AffinityStatus {
            [UnityEngine.Scripting.Preserve]  NotEnabled,
            [UnityEngine.Scripting.Preserve]  Fail,
            [UnityEngine.Scripting.Preserve]  Success,
            [UnityEngine.Scripting.Preserve]  InProgress
        }

        public enum JobObjectInfoType {
            [UnityEngine.Scripting.Preserve] AssociateCompletionPortInformation = 7,
            [UnityEngine.Scripting.Preserve] BasicLimitInformation = 2,
            [UnityEngine.Scripting.Preserve] BasicUIRestrictions = 4,
            [UnityEngine.Scripting.Preserve] EndOfJobTimeInformation = 6,
            [UnityEngine.Scripting.Preserve] ExtendedLimitInformation = 9,
            [UnityEngine.Scripting.Preserve] SecurityLimitInformation = 5,
            [UnityEngine.Scripting.Preserve] GroupInformation = 11,
            [UnityEngine.Scripting.Preserve] JobObjectGroupInformationEx = 14
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct _GROUP_AFFINITY {
            public UIntPtr Mask;
            [MarshalAs(UnmanagedType.U2)] public ushort Group;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3, ArraySubType = UnmanagedType.U2)]
            public ushort[] Reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SECURITY_ATTRIBUTES {
            public UInt32 nLength;
            public IntPtr lpSecurityDescriptor;
            public Int32 bInheritHandle;
        }
    }
}
#endif