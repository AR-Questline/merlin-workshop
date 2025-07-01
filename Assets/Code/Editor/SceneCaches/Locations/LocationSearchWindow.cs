using System;
using System.Linq;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Stories.Tags;
using Awaken.Utility.Debugging;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;

namespace Awaken.TG.Editor.SceneCaches.Locations {
    public class LocationSearchWindow : OdinEditorWindow {
        // === Displayed data
        [ShowInInspector, PropertyOrder(-1)] public string LastBake => LocationCache.Get.LastBake;

        public LocationReference locationToFind;
        
        [InfoBox("Lack of matches does not mean that deleting the location is safe!", InfoMessageType.Warning, nameof(ShowDeleteNotSafeWarning))]
        public LocationSource[] foundSources = Array.Empty<LocationSource>();
        
        bool ShowDeleteNotSafeWarning => locationToFind is { TargetsTemplates: true } && foundSources.Length == 0;
        
        [InitializeOnLoadMethod]
        static void InitLocationReferenceCallback() {
            LocationReference.findWindowCallback -= OpenWindowOn;
            LocationReference.findWindowCallback += OpenWindowOn;
        }

        // === Creators
        [MenuItem("TG/Design/Location Finder")]
        public static void ShowWindow() {
            CreateWindow();
        }

        public static void OpenWindowOn(LocationReference locationReference) {
            var window = CreateWindow();
            window.locationToFind = new(locationReference);
            window.Execute();
        }

        static LocationSearchWindow CreateWindow() {
            var window = CreateWindow<LocationSearchWindow>("Location Finder");
            window.Show();
            return window;
        }

        [Button]
        void Execute() {
            foundSources = LocationCache.Get.locations.SelectMany(sl => sl.data)
                .Where(l => IsMatching(locationToFind, l))
                .ToArray();
            Log.Important?.Info("Location Finder: Found " + foundSources.Length + " locations.");
        }

        bool IsMatching(LocationReference reference, LocationSource source) {
            if (reference.TargetsActors) {
                return reference.actors.Any(a => a.guid == source.actorGuid);
            }

            if (reference.TargetsTags) {
                if (reference.targetTypes == TargetType.Tags) {
                    return TagUtils.HasRequiredTags(source, reference.tags);
                }

                return TagUtils.HasAnyTag(source.Tags, reference.tags);
            }

            if (reference.TargetsTemplates) {
                LocationTemplate template = source.locationTemplate;
                LocationTemplate spawnedTemplate = source.SpawnedLocationTemplate;
                return template != null && reference.LocationTemplates.Contains(template) ||
                       spawnedTemplate != null && reference.LocationTemplates.Contains(spawnedTemplate);
            }

            return false;
        }
    }
}