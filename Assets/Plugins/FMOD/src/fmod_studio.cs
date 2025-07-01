using System;
using System.Runtime.InteropServices;

namespace FMOD.Studio
{
    [StructLayout(LayoutKind.Sequential)]
    public struct PARAMETER_ID
    {
        public uint data1;
        public uint data2;

        public bool Equals(PARAMETER_ID other) {
            return data1 == other.data1 && data2 == other.data2;
        }
    }

    public struct EventInstance
    {
        public IntPtr handle;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    public struct MEMORY_USAGE
    {
        public int exclusive;
        public int inclusive;
        public int sampledata;

        public bool Equals(MEMORY_USAGE other) {
            return exclusive == other.exclusive && inclusive == other.inclusive && sampledata == other.sampledata;
        }
    }
}