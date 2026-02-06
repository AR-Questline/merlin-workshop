using System.Collections.Generic;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.Utility.Enums;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Debugging.GUIDSearching {
    public class IdOverrideSearchWindow : GUIDSearchWindowBase {
        [ShowInInspector, PropertyOrder(-10)]
        public string LastBake => GUIDCache.Instance?.LastBake;
        
        [Title("Input")]
        [SerializeField] string idOverride;
        
        [Title("Output")]
        [ShowInInspector, TableList(IsReadOnly = true, AlwaysExpanded = true), PropertyOrder(1), Space(10), Indent]
        List<GUIDSearchWindow.SearchResultObject> _foundUsages = new();

        protected override bool ShowIdOverrideSearchButton => false;
        
        public static void OpenWindow() {
            var window = GetWindow<IdOverrideSearchWindow>(GUIDSearchWindow.DesiredDockTypes);
            window.Show();
        }

        [MenuItem("TG/Assets/Find by Id Override", priority = -100)]
        static void CreateWindow() {
            var window = CreateWindow<IdOverrideSearchWindow>(GUIDSearchWindow.DesiredDockTypes);
            window.Show();
        }

        protected override void Initialize() {
            GUIDCache.Load();
        }

        protected override void OnDestroy() {
            GUIDCache.Unload();
        }

        [HorizontalGroup("Buttons"), PropertySpace(SpaceBefore = 5)]
        [Button(ButtonSizes.Medium, ButtonStyle.CompactBox, Icon = SdfIconType.Search)]
        void Search() {
            _foundUsages.Clear();
            foreach (string path in GUIDCache.Instance.GetIdOverrideUsages(idOverride)) {
                var so = new GUIDSearchWindow.SearchResultObject(path);
                if (so.asset != GUIDCache.Instance) {
                    _foundUsages.Add(so);
                }
            }
        }
    }
}