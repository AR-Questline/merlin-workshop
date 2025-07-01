using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Graphics.ScriptedEvents.Triggers;
using Awaken.TG.Main.Locations.Views;
using Awaken.TG.Main.UI.Stickers;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using Awaken.Utility.GameObjects;
using JetBrains.Annotations;
using Unity.VisualScripting;
using UnityEngine;

namespace Awaken.TG.Main.Locations {
    public static class LocationUtils {
        public static void SafelyMoveAndRotateTo(this Location location, Vector3 position, Quaternion rotation, bool teleport = false) {
            if (location is { IsStatic: false, IsNonMovable: false, IsVisualLoaded: true }) {
                location.View<VDynamicLocation>()?.SyncPositionAndRotation();
            }
            location.MoveAndRotateTo(position, rotation, teleport);
        }
        
        public static void SafelyMoveTo(this Location location, Vector3 position, bool teleport = false) {
            if (location is { IsStatic: false, IsNonMovable: false, IsVisualLoaded: true }) {
                location.View<VDynamicLocation>()?.SyncPositionAndRotation();
            }
            location.MoveAndRotateTo(position, location.Rotation, teleport);
        }
        
        public static void SafelyRotateTo(this Location location, Quaternion rotation) {
            if (location is { IsStatic: false, IsNonMovable: false, IsVisualLoaded: true }) {
                (location.LocationView as VDynamicLocation)?.SyncPositionAndRotation();
            }
            location.MoveAndRotateTo(location.Coords, rotation);
        }

        [UnityEngine.Scripting.Preserve]
        public static bool InCameraView(Transform transform, Transform cameraTransform) {
            float dot = Vector3.Dot((transform.position - cameraTransform.position).normalized, cameraTransform.forward);
            if (dot < 0f) {
                return false;
            }
            Vector2 screenPosition = cameraTransform.GetComponent<Camera>().WorldToViewportPoint(transform.position);
            return screenPosition.x >= 0 && screenPosition.x <= 1 && screenPosition.y >= 0 && screenPosition.y <= 1;
        }

        [UnityEngine.Scripting.Preserve]
        public static bool ScrambledEquals<T>(IEnumerable<T> list1, IEnumerable<T> list2) {
            var cnt = new Dictionary<T, int>();
            foreach (T s in list1) {
                if (cnt.ContainsKey(s)) {
                    cnt[s]++;
                }
                else {
                    cnt.Add(s, 1);
                }
            }
            foreach (T s in list2) {
                if (cnt.ContainsKey(s)) {
                    cnt[s]--;
                }
                else {
                    return false;
                }
            }
            return cnt.Values.All(c => c == 0);
        }

        [UnityEngine.Scripting.Preserve]
        public static Transform DetermineLocationStickerHost([CanBeNull] Location place) {
            Services services = World.Services;
            Transform reference = null;
            float yOffset = 0;
            if (place != null) {
                reference = GameObjects.FindRecursively<Transform>(place.MainView.gameObject, "CharacterPlacement");
                if (reference != null) {
                    yOffset = 12f;
                } else {
                    reference = place.MainView.TryGrabChild<Transform>("Prefab");
                    if (reference != null) {
                        Bounds bounds = TransformBoundsUtil.FindBounds(reference, false);
                        // Sum distance from bounds center to top (extent), distance from bounds center to location pivot and small offset (0.1f)
                        yOffset = bounds.extents.y + (bounds.center.y - reference.position.y) + 0.1f;
                        yOffset = Mathf.Min(yOffset, 20f);
                    }
                }
            }

            if (reference == null) {
                // nothing to stick to, popup on middle of the screen
                return services.Get<ViewHosting>().OnMainCanvas();
            }

            return services.Get<MapStickerUI>().StickTo(reference, new StickerPositioning {
                pivot = new Vector2(0.5f, 0.5f),
                worldOffset = new Vector3(0, yOffset, 0),
                underneath = false
            });
        }

        public static bool HasAnyLocationIndependentLogic(Transform transform, bool allowTriggerColliders) {
            if (transform == null) {
                return false;
            }
            if (transform.GetComponentInChildren<ScriptMachine>() != null) {
                return true;
            }
            if (transform.GetComponentInChildren<HeroTrigger>() != null) {
                return true;
            }
            foreach (var coll in transform.GetComponentsInChildren<Collider>()) {
                if (!allowTriggerColliders || !coll.isTrigger) {
                    return true;
                }
            }
            return false;
        }
    }
}