#if UNITY_GAMECORE
using System;
using UnityEngine.GameCore;
#endif
using System;
using UnityEngine;

namespace Awaken.Utility {
    /// <summary>
    /// Use this if you want to check platform, without using defines in your code.
    /// </summary>
    public static class PlatformUtils {
        public static bool sDebugConsolePlatform;
        
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
        
        public static bool MonoBuildTarget {
            get {
#if UNITY_EDITOR
                return UnityEditor.ScriptingImplementation.Mono2x == UnityEditor.PlayerSettings.GetScriptingBackend(UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(UnityEditor.EditorUserBuildSettings.selectedBuildTargetGroup));
#else
                return false;
#endif
            }
        }
            
        public static bool IsMonoBuild {
            get {
#if ENABLE_MONO
                return true;
#else
                return false;
#endif
            }
        }
        
        public static bool IsPlatformWithLanguageRestrictions() {
            return false; //IsConsole || IsGamePassPC;
        }

        public static Platform GetCurrentPlatform() {
            Platform platform = Platform.None;
            if (IsXboxScarlettX) {
                platform |= Platform.XboxSeriesX;
            }
            if (IsXboxScarlettS) {
                platform |= Platform.XboxSeriesS;
            }
            if (IsWindows) {
                platform |= Platform.Windows;
            }
            if (IsSteamDeck) {
                platform |= Platform.SteamDeck;
            }
            if (IsPS5Pro) {
                platform |= Platform.PS5Pro;
            } else if (IsPS5) {
                platform |= Platform.PS5Base;
            }
            if (IsEditor) {
                platform |= Platform.Editor;
            }
            return platform;
        }

        [Flags]
        public enum Platform : byte {
            None = 0,
            XboxSeriesX = 1 << 0,
            XboxSeriesS = 1 << 1,
            Windows = 1 << 2,
            SteamDeck = 1 << 3,
            PS5Base = 1 << 4,
            PS5Pro = 1 << 5,
            Editor = 1 << 6,

            Xbox = XboxSeriesX | XboxSeriesS,
            PS5 = PS5Base | PS5Pro,
        }
    }
}