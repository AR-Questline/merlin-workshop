using System;
using System.IO;
using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Utility.Animations.ARAnimator;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Animations {
    public static class HeroCombatOverridesUpdator {
        
        [MenuItem("TG/Animations/Copy Events For All Hero Combat Overrides")]
        static void Convert() {
            
            string[] fileEntries = Directory.GetFiles(
                Application.dataPath + "/Data/AnimationOverrides/Hero_Combat_Overrides", 
                "*.asset", 
                SearchOption.AllDirectories);
            
            foreach (string fileName in fileEntries) {
                string localPath = fileName[fileName.IndexOf("Assets", StringComparison.Ordinal)..];

                var t = AssetDatabase.LoadAssetAtPath(localPath, typeof(ARHeroStateToAnimationMapping));
                if (t is not ARHeroStateToAnimationMapping mapping) {
                    continue;
                }
                
                mapping.EDITOR_CopyEventsFromAnimationClip();
                EditorUtility.SetDirty(mapping);
            }
        }
    }
}