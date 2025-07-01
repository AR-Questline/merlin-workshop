#if UNITY_EDITOR_WIN
using UnityEngine;
using System;
using System.Runtime.InteropServices;

namespace Awaken.Utility.Editor.WindowPositioning {
    public static class User32Utils {
        // Win32 API to get the monitor info for a specific point
        [DllImport("user32.dll")] // https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-monitorfrompoint
        static extern IntPtr MonitorFromPoint(Vector2Int pt, int dwFlags);

        [DllImport("user32.dll")] // https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getmonitorinfoa
        static extern bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInfo lpmi);
        
        [DllImport("user32.dll")] // https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getcursorpos
        static extern bool GetCursorPos(out Point lpPoint);

        const int MonitorDefaultToNearest = 2;

        [StructLayout(LayoutKind.Sequential)]
        struct Rect {
            public int Left, Top, Right, Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct MonitorInfo {
            public int cbSize;
            public Rect rcMonitor;
            public Rect rcWork;
            public uint dwFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct Point {
            public int X;
            public int Y;
        }

        /// <summary>
        /// Gets mouse position using Win32 API. https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getcursorpos
        /// </summary>
        /// <returns></returns>
        public static Vector2Int GetMousePosition() {
            GetCursorPos(out Point point);
            return new Vector2Int(point.X, point.Y);
        }
        
        /// <summary>
        /// Gets the bounds of the monitor that contains the specified point using Win32 API. https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-monitorfrompoint
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public static UnityEngine.Rect GetMonitorBoundsForPoint(Vector2Int point) {
            IntPtr monitor = MonitorFromPoint(point, MonitorDefaultToNearest);
            MonitorInfo info = new ();
            info.cbSize = Marshal.SizeOf(typeof(MonitorInfo));
            if (GetMonitorInfo(monitor, ref info)) {
                return new UnityEngine.Rect(
                    info.rcMonitor.Left,
                    info.rcMonitor.Top,
                    info.rcMonitor.Right - info.rcMonitor.Left,
                    info.rcMonitor.Bottom - info.rcMonitor.Top
                );
            }

            return new UnityEngine.Rect(0, 0, Screen.currentResolution.width, Screen.currentResolution.height);
        }
    }
}
#endif