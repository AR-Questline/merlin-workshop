using System;
using System.Linq;
using Awaken.TG.Editor.Utility;
using Awaken.TG.Main.Settings.Controllers;
using Awaken.Utility.Graphics;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Awaken.TG.Editor.Settings.Controllers {
    [CustomEditor(typeof(VolumeQualityController))]
    public class VolumeQualityControllerEditor : UnityEditor.Editor {
        /// <summary> These Components are set on ApplicationScene and cannot be overriden </summary>
        static readonly Type[] BannedComponents = {
            typeof(MotionBlur),
            typeof(HDShadowSettings),
            typeof(ContactShadows),
            typeof(MicroShadowing),
            typeof(WaterRendering),
            typeof(LiftGammaGain),
            typeof(ScreenSpaceAmbientOcclusion),
            typeof(Tonemapping),
        };
        
        public override void OnInspectorGUI() {
            if (Event.current.type == EventType.Repaint) {
                var controller = (VolumeQualityController) target;
                var volume = controller.GetComponent<Volume>();
                ValidateVolume(volume, true);
            }
        }

        static void ValidateVolume(Volume volume, bool verbose) {
            var profile = volume.GetSharedOrInstancedProfile();
            if (profile == null) {
                return;
            }
            profile.components.RemoveAll(component => {
                var type = component.GetType();
                if (BannedComponents.Contains(type)) {
                    if (verbose) {
                        EditorUtility.DisplayDialog(
                            "Global Volume Component Detected!",
                            $"{type.Name} can only be set on ApplicationScene",
                            "Remove It"
                        );
                    }
                    DestroyImmediate(component, true);
                    EditorUtility.SetDirty(profile);
                    return true;
                }
                return false;
            });
        }

        [MenuItem("TG/Scene Tools/Validate All Volumes")]
        static void ValidateAllVolumes() {
            foreach (string scenePath in BuildTools.GetAllScenes()) {
                if (scenePath.Contains("ApplicationScene")) {
                    continue;
                }
                using SceneResources sr = new(scenePath, true);
                foreach (var volume in FindObjectsByType<Volume>(FindObjectsSortMode.None)) {
                    var controller = volume.GetComponent<VolumeQualityController>();
                    if (controller is null) {
                        volume.AddComponent<VolumeQualityController>();
                        EditorUtility.SetDirty(volume.gameObject);
                    }
                    ValidateVolume(volume, false);
                }
            }
        }
    }
}