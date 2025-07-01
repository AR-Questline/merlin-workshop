using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Awaken.TG.Main.Templates;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using Object = UnityEngine.Object;

namespace Awaken.TG.Debugging {
    public class AddressablesInfo {
        static AddressablesInfo s_instance;
        public static AddressablesInfo Instance {
            get {
                return s_instance ??= new();
            }
        }

        static readonly Type[] ExcludedTypes = {
            typeof(ITemplate), typeof(IResourceLocation), typeof(Locale), typeof(StringTable),
        };

        public Dictionary<object, AsyncOperationHandle> TrackingData { get; private set; }
        public object[] TrackedAssets { get; private set; }
        public object[] NullAssets { get; private set; }
        public string[] NullAssetsNames { get; private set; }
        public object[] NullGameObjectAssets { get; private set; }
        public object[] OtherNullsAssets { get; private set; }
        public static readonly Dictionary<int, string> NameByHash = new();

        public void LoadTrackingData() {
            var addressablesImpl = typeof(Addressables)
                .GetProperty("Instance",
                    BindingFlags.NonPublic | BindingFlags.Static)
                .GetValue(null, null);
            TrackingData = addressablesImpl.GetType()
                .GetField("m_resultToHandle", BindingFlags.Instance | BindingFlags.GetField | BindingFlags.NonPublic)
                .GetValue(addressablesImpl) as Dictionary<object, AsyncOperationHandle>;

            if (TrackingData != null) {
                BakeTracked();
            }
        }

        public void BakeTracked() {
            TrackedAssets = Array.Empty<object>();
            NullAssets = Array.Empty<object>();
            NullAssetsNames = Array.Empty<string>();
            NullGameObjectAssets = Array.Empty<object>();
            OtherNullsAssets = Array.Empty<object>();
            if (TrackingData == null) {
                return;
            }
            var trackedNoTemplate = new List<object>();
            var trackedNulls = new List<object>();
            var trackedNullNames = new List<string>();
            var trackedNullGameObjects = new List<object>();
            foreach (var trackedObject in TrackingData.Keys) {
                if (trackedObject is IEnumerable<object> enumerable) {
                    foreach (var item in enumerable) {
                        ProcessTrackedObject(item, trackedNoTemplate, trackedNullGameObjects, trackedNulls,
                            trackedNullNames);
                    }
                } else {
                    ProcessTrackedObject(trackedObject, trackedNoTemplate, trackedNullGameObjects, trackedNulls,
                        trackedNullNames);
                }
            }

            TrackedAssets = trackedNoTemplate.ToArray();
            NullAssets = trackedNulls.ToArray();
            NullAssetsNames = trackedNullNames.ToArray();
            NullGameObjectAssets = trackedNullGameObjects.ToArray();
            OtherNullsAssets = trackedNulls.Except(trackedNullGameObjects).ToArray();
        }

        static void ProcessTrackedObject(object trackedObject,
            List<object> trackedNoTemplate, List<object> trackedNullGameObjects, List<object> trackedNulls,
            List<string> nullAssetNames) {
            if (IsAssetObject(trackedObject)) {
                trackedNoTemplate.Add(trackedObject);
                NameByHash[trackedObject.GetHashCode()] = $"{((Object)trackedObject).name} - {trackedObject.GetType().Name}";
            } else {
                if (IsNullGameObject(trackedObject)) {
                    trackedNullGameObjects.Add(trackedObject);
                }
                if (IsNullAsset(trackedObject)) {
                    trackedNulls.Add(trackedObject);
                    if (NameByHash.TryGetValue(trackedObject.GetHashCode(), out var name)) {
                        nullAssetNames.Add(name);
                    } else {
                        nullAssetNames.Add("Unknown name");
                    }
                }
            }
        }

        public void Clear() {
            TrackingData = null;
            TrackedAssets = Array.Empty<object>();
            NullAssets = Array.Empty<object>();
            NullAssetsNames = Array.Empty<string>();
            NullGameObjectAssets = Array.Empty<object>();
            OtherNullsAssets = Array.Empty<object>();
        }

        static bool IsAssetObject(object obj) {
            if (obj == null || (obj as Object) == null) {
                return false;
            }
            var type = obj.GetType();
            if (ExcludedTypes.Any(et => et.IsAssignableFrom(type))) {
                return false;
            }
            if (obj is GameObject gameObject && gameObject.GetComponent<ITemplate>() != null) {
                return false;
            }
            return true;
        }

        static bool IsNullGameObject(object obj) {
            return obj is GameObject gameObject && gameObject == null;
        }

        static bool IsNullAsset(object obj) {
            return obj is Object unityObject && unityObject == null;
        }
    }
}
