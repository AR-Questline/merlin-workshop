using System;
using System.Runtime.InteropServices;
#if !UNITY_STANDALONE_WIN || !UNITY_EDITOR_WIN
using Awaken.Utility.Debugging;
#endif

namespace Awaken.TG.Main.Utility {
    public static class KeyboardLayoutDetector {
        // Keyboard layout IDs for some common layouts
        public const int UsKeyboardID = 0x0409;
        public const int UkKeyboardID = 0x0809;
        public const int PolishKeyboardID = 0x0415;
        public const int FrenchKeyboardID = 0x040C;
        public const int BelgianFrenchKeyboardID = 0x080C;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        [DllImport("user32.dll")] static extern IntPtr GetKeyboardLayout(uint idThread);
        [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
        [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
#endif
        
        /// <summary>
        /// Checks if the current keyboard layout is AZERTY.
        /// </summary>
        /// <returns>
        /// True if the current keyboard layout is French or Belgian French.
        /// Returns false if not running on Windows.
        /// </returns>
        public static bool IsAzertyKeyboard() {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            var layoutId = GetKeyboardLayoutID();
            return layoutId is FrenchKeyboardID or BelgianFrenchKeyboardID;
#else
            Log.Debug?.Warning("Keyboard layout detection is only supported on Windows.");
            return false;
#endif
        }

        /// <summary>
        /// Retrieves the ID of the current keyboard layout using user32.dll functions.
        /// This functionality works only on Windows.
        /// </summary>
        /// <returns>
        /// The ID of the current keyboard layout.
        /// Returns 0 if the function fails.
        /// Returns -1 if not running on Windows.
        /// </returns>
        public static int GetKeyboardLayoutID() {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            uint threadId = GetWindowThreadProcessId(GetForegroundWindow(), out _);
            IntPtr layout = GetKeyboardLayout(threadId);
            return layout.ToInt32() & 0xFFFF; // extract language ID
#else
            Log.Debug?.Warning("Keyboard layout detection is only supported on Windows.");
            return -1; 
#endif
        }

        /// <summary>
        /// Retrieves the name of the current keyboard layout.
        /// </summary>
        /// <returns>
        /// The name of the current keyboard layout.
        /// Returns an empty string if the function fails.
        /// Returns an empty string if not running on Windows.
        /// </returns>
        public static string GetKeyboardLayoutName() {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            int keyboardId = GetKeyboardLayoutID();

            return keyboardId switch {
                UsKeyboardID => "QWERTY (US)",
                FrenchKeyboardID => "AZERTY (French)",
                BelgianFrenchKeyboardID => "AZERTY (Belgian French)",
                UkKeyboardID => "QWERTY (UK)",
                PolishKeyboardID => "QWERTY (Polish)",
                _ => $"Other Layout (ID: {keyboardId:X4})"
            };
#else
            Log.Debug?.Warning("Keyboard layout detection is only supported on Windows.");
            return string.Empty;
#endif
        }
    }
}