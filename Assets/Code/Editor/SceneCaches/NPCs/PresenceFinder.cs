using System;
using System.Linq;
using Awaken.TG.Main.General.Caches;
using Awaken.TG.Main.Utility.RichLabels;
using Awaken.TG.Main.Utility.RichLabels.SO;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;

namespace Awaken.TG.Editor.SceneCaches.NPCs {
    public class PresenceFinder : OdinEditorWindow {
        public RichLabelUsage richLabelUsageToFind = new(RichLabelConfigType.Presence);
        [UsedImplicitly] 
        public PresenceSource[] foundSources = Array.Empty<PresenceSource>();

        public static void OpenWindowOn(RichLabelUsage richLabelUsage) {
            var window = CreateWindow();
            window.richLabelUsageToFind = new RichLabelUsage(richLabelUsage);
            window.Execute();
        }

        [MenuItem("TG/Design/Presence Finder")]
        static void ShowWindow() {
            CreateWindow();
        }

        static PresenceFinder CreateWindow() {
            var window = GetWindow<PresenceFinder>("Rich Label Finder");
            window.Show();
            return window;
        }

        [Button]
        void Execute() {
            foundSources = PresenceCache.Get.GetMatchingPresenceData(richLabelUsageToFind.RichLabelUsageEntries).ToArray();
        }
    }
}