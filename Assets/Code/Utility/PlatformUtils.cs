#if UNITY_GAMECORE
using System;
using UnityEngine.GameCore;
#endif
using UnityEngine;

namespace Awaken.Utility {
    /// <summary>
    /// Use this if you want to check platform, without using defines in your code.
    /// </summary>
    public static class PlatformUtils {
        public static bool IsConsole {
            get {
                return false;
            }
        }

        [UnityEngine.Scripting.Preserve]
        public static bool IsXboxScarlett {
            get {
                return false;
            }
        }

        [UnityEngine.Scripting.Preserve]
        public static bool IsXboxScarlettX {
            get {
                return false;
            }
        }

        public static bool IsXboxScarlettS {
            get {
                return false;
            }
        }

        [UnityEngine.Scripting.Preserve]
        public static bool IsXboxScarlettDevkit {
            get {
                return false;
            }
        }

        [UnityEngine.Scripting.Preserve]
        public static bool IsXboxOne {
            get {
                return false;
            }
        }

        public static bool IsXbox {
            get {
                return false;
            }
        }

        public static bool IsMicrosoft {
            get {
                return false;
            }
        }

        public static bool IsGamePassPC {
            get {
                return false;
            }
        }

        public static bool IsEditor {
            get {
                return false;
            }
        }
        
#if UNITY_EDITOR
        public static bool IsPlaying => Application.isPlaying;
#else
        public const bool IsPlaying = true;
#endif
        
        public static bool IsWindows {
            get {
                return false;
            }
        }

        [UnityEngine.Scripting.Preserve]
        public static bool IsMacOS {
            get {
                return false;
            }
        }

        public static bool IsDebug {
            get {
                return false;
            }
        }

        public static bool IsSteamInitialized {
            get {
                return false;
            }
        }

        public static bool IsSteamDeck {
            get {
                return false;
            }
        }

        public static bool IsGogInitialized {
            get {
                return false;
            }
        }

        public static bool IsPS5 {
            get {
                return false;
            }
        }

        public static bool IsPS5Pro {
            get {
                return false;
            }
        }

        public static bool IsJournalDisabled => GameMode.IsDemo; 
        
        public static bool IsPlatformWithLanguageRestrictions() {
            return false; //IsConsole || IsGamePassPC;
        }
    }
}