using System;
using Awaken.TG.Main.General.Configs;
using JetBrains.Annotations;
using Sirenix.Utilities;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Utility {
    public static class Versioning {
        // Versioning structure: (0)Major.(00)Minor.(000)Micro, example: 1.00.000
        // Public version structure: (0)Major.(0)Minor[abc..]Micro, example: 0.5c
        // Micro in version and public is not the same counter.
        // Version micro must always be incremented to make sure that we can differentiate between all builds
        
        [UsedImplicitly]
        public static void IncrementMicroVersion() {
            string[] version = Application.version.Split('.');
            
            int microVersion = int.Parse(version[2]);
            microVersion++;
            version[2] = microVersion.ToString("000");
            
            PlayerSettings.bundleVersion = string.Join(".", version);
            AssetDatabase.SaveAssets();
        }

        [MenuItem("TG/Build/Increment Minor Version", false, 501)]
        public static void IncrementMinorVersion() {
            string[] version = Application.version.Split('.');
            
            int minorVersion = int.Parse(version[1]);
            minorVersion++;
            version[1] = minorVersion.ToString("00");
            // Increment version letter in game constants
            GameConstants gameConstants = GameConstants.Get;
            string gameVersion = gameConstants.gameVersion;
            
            if (int.TryParse(gameVersion[^1].ToString(), out _)) {
                gameConstants.gameVersion += "a";
            } /*else if (gameVersion.FastEndsWith("z")) {
                gameConstants.gameVersion = gameVersion[..^1] + "a";
            } */else {
                gameConstants.gameVersion = gameVersion[..^1] + (char) (Convert.ToUInt16(gameVersion[^1]) + 1);
            }
            EditorUtility.SetDirty(gameConstants);
            // Reset micro version
            version[2] = "001";
            
            PlayerSettings.bundleVersion = string.Join(".", version);
            AssetDatabase.SaveAssets();
        }

        [MenuItem("TG/Build/Increment Public Version", false, 502)]
        public static void IncrementPublicVersion() {
            IncrementMinorVersion();
            
            // remove all letters from end of version
            GameConstants gameConstants = GameConstants.Get;
            string gameVersion = gameConstants.gameVersion;

            for (int i = gameVersion.Length - 1; i >= 0; i--) {
                if (!char.IsLetter(gameVersion[i])) {
                    gameVersion = gameVersion[..(i + 1)];
                    break;
                }
            }
            // Increment public version
            var version = gameVersion.Split('.');
            version[1] = (int.Parse(version[1]) + 1).ToString();
            gameConstants.gameVersion = string.Join(".", version);
            EditorUtility.SetDirty(gameConstants);
            AssetDatabase.SaveAssets();
        }

        // [MenuItem("TG/Build/Increment Major Version", false, 503)]
        /// <summary>
        /// Only for manual use
        /// </summary>
        public static void IncrementMajorVersion_ThinkBeforeUsing() {
            string[] version = Application.version.Split('.');
            
            int majorVersion = int.Parse(version[0]);
            majorVersion++;
            version[0] = majorVersion.ToString();
            
            // Increment public major version
            GameConstants gameConstants = GameConstants.Get;
            string gameVersion = gameConstants.gameVersion;
            var publicVersion = gameVersion.Split('.');
            publicVersion[0] = (int.Parse(publicVersion[0]) + 1).ToString();
            // Reset public minor version
            publicVersion[1] = "0";
            gameConstants.gameVersion = string.Join(".", publicVersion);
            EditorUtility.SetDirty(gameConstants);
            
            // Reset minor version
            version[1] = "00";
            // Reset micro version
            version[2] = "001";
            
            PlayerSettings.bundleVersion = string.Join(".", version);
            AssetDatabase.SaveAssets();
        }
    }
}