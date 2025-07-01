using System.Collections.Generic;
using System.Linq;
using Awaken.ECS.DrakeRenderer.Authoring;
using Awaken.TG.Assets;
using Awaken.TG.Main.Locations.Setup;
using Awaken.Utility.Maths;
using JetBrains.Annotations;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.VFX;

namespace Awaken.TG.Editor.Main.Scenes.SubdividedScenes {
    public class SubsceneEditorData : ScriptableObject {
        [SerializeField] bool hasBounds;
        [SerializeField] Bounds bounds;

        public Bounds? Bounds => hasBounds ? bounds : null;

        public void UpdateFor(Scene scene) {
            Bounds? calculatedBounds = null;
            foreach (var root in scene.GetRootGameObjects()) {
                if (!root.activeSelf) {
                    continue;
                }
                foreach (var renderer in root.GetComponentsInChildren<Renderer>()) {
                    if (renderer is TrailRenderer or VFXRenderer) {
                        continue;
                    }
                    calculatedBounds.Encapsulate(renderer.bounds);
                }

                foreach (DrakeMeshRenderer renderer in root.GetComponentsInChildren<DrakeMeshRenderer>()) {
                    MinMaxAABB rendererWorldBounds = renderer.WorldBounds;
                    var drakeBounds = new Bounds(rendererWorldBounds.Center(), rendererWorldBounds.Size());
                    calculatedBounds.Encapsulate(drakeBounds);
                }

                foreach (LocationSpec location in root.GetComponentsInChildren<LocationSpec>()) {
                    calculatedBounds.Encapsulate(location.transform.position);
                }
            }

            if (calculatedBounds is { } newBounds ? hasBounds && newBounds.Equals(bounds) : !hasBounds) {
                return;
            }
            
            hasBounds = calculatedBounds.HasValue;
            bounds = calculatedBounds ?? default(Bounds);
            EditorUtility.SetDirty(this);
        }

        public static void GroupSubscenesByProximity(Vector3 position, 
            [CanBeNull] List<(string, SceneReference)> sceneInBounds = null, int inBoundsExtent = 10, 
            [CanBeNull] List<(string, SceneReference)> nearbyScenes = null, int nearbyExtent = 250, 
            [CanBeNull] List<(string, SceneReference)> farScenes = null, 
            [CanBeNull] List<(string, SceneReference)> scenesWithoutGeometry = null
        ) {
            if (!SubdividedSceneTracker.TryGet(out var scene)) {
                return;
            }
            Dictionary<int, (string, SceneReference)> scenesByDistanceDict = new();
            
            foreach (var (path, sceneRef) in scene.AllScenesWithPath) {
                if (SubsceneEditorDataManager.TryFindFor(sceneRef, out var data) && data.Bounds is { } bounds) {
                    if (bounds.Expanded(inBoundsExtent).Contains(position)) {
                        sceneInBounds?.Add((path, sceneRef));
                    } else {
                        scenesByDistanceDict.Add((int) bounds.SqrDistance(position), (path, sceneRef));
                    }
                } else {
                    scenesWithoutGeometry?.Add((path, sceneRef));
                }
            }
            
            var scenesByDistance = scenesByDistanceDict.OrderBy(pair => pair.Key).Select(pair => pair.Value).ToArray();
            for (int i = 0; i < scenesByDistance.Length; i++) {
                if (i < 8) {
                    nearbyScenes?.Add(scenesByDistance[i]);
                } else {
                    farScenes?.Add(scenesByDistance[i]);
                }
            }
        }
    }
}