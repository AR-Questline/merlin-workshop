#if UNITY_EDITOR

using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Awaken.TG.EditorOnly.Utils {
    public static class AnimationClipRootBoneUtil {
        public static string GetRootBonePath(this ModelImporter modelImporter) {
            if (modelImporter == null) {
                return "";
            }
            
            var modelImporterObj = new SerializedObject(modelImporter);
            const string RootNodePropertyName = "m_HumanDescription.m_RootMotionBoneName";
            SerializedProperty rootNodeProperty = modelImporterObj.FindProperty(RootNodePropertyName);

            var rootNodeName = rootNodeProperty.stringValue;
            if (string.IsNullOrEmpty(rootNodeName)) {
                return "";
            }
            
            var rootBonePath = modelImporter.transformPaths.FirstOrDefault(path => path.EndsWith(rootNodeName));
            return rootBonePath;
        }

        public static Vector3 MeasureRootEulerAnglesDelta(this AnimationClip clip, float samplingInterval = 0.016f) {
            GameObject motionMeasurementRootObject = new("MotionMeasurementRoot");
            GameObject rootNodeObject = clip.PrepareObjectForRootMotionMeasurement(motionMeasurementRootObject, out bool keepOriginalRotation);

            if (keepOriginalRotation) {
                Object.DestroyImmediate(motionMeasurementRootObject);
                return Vector3.zero;
            }
            
            Quaternion previousRotation = Quaternion.identity;
            Vector3 eulerAnglesDelta = Vector3.zero;
            int samplesCount = Mathf.FloorToInt(Mathf.Max(2, 1 + clip.length / samplingInterval));
            for (int i = 0; i <= samplesCount; i++) {
                float sampleTime = i * clip.length / samplesCount;
                clip.SampleAnimation(motionMeasurementRootObject, sampleTime);
                Quaternion newRotation = rootNodeObject.transform.rotation;

                if (i > 0) {
                    Quaternion deltaRotation = newRotation * Quaternion.Inverse(previousRotation);
                    
                    Vector3 absoluteDeltaEulerAngles = deltaRotation.eulerAngles;
                    // Normalize the delta angles to be in the range of -180 to 180
                    Vector3 normalizedDeltaEulerAngles = new(
                        Mathf.DeltaAngle(0f, absoluteDeltaEulerAngles.x),
                        Mathf.DeltaAngle(0f, absoluteDeltaEulerAngles.y),
                        Mathf.DeltaAngle(0f, absoluteDeltaEulerAngles.z)
                    );
                    
                    eulerAnglesDelta += normalizedDeltaEulerAngles;
                }
                
                previousRotation = newRotation;
            }
            
            Object.DestroyImmediate(motionMeasurementRootObject);

            return eulerAnglesDelta;
        }

        static GameObject PrepareObjectForRootMotionMeasurement(this AnimationClip clip, GameObject motionMeasurementRoot, out bool keepOriginalRotation) {
            GameObject nodeObject = motionMeasurementRoot;
            ModelImporter modelImporter = clip.GetModelImporter();

            if (modelImporter == null) {
                keepOriginalRotation = true;
                return null;
            }

            keepOriginalRotation = GetKeepOriginalRotation(clip, modelImporter);
            var rootBonePathNodes = modelImporter.GetRootBonePath().Split("/");
            foreach (var pathNode in rootBonePathNodes) {
                var childNodeObject = new GameObject(pathNode);
                childNodeObject.transform.SetParent(nodeObject.transform);
                nodeObject = childNodeObject;
            }

            return nodeObject;
        }
        
        static ModelImporter GetModelImporter(this AnimationClip clip) {
            return AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(clip)) as ModelImporter;
        }

        static bool GetKeepOriginalRotation(AnimationClip clip, ModelImporter modelImporter) {
            var clipSetting = modelImporter.clipAnimations.FirstOrDefault(c => c.name == clip.name);
            clipSetting ??= modelImporter.defaultClipAnimations.FirstOrDefault(c => c.name == clip.name);
            return clipSetting?.lockRootRotation ?? false;
        }
    }
}

#endif