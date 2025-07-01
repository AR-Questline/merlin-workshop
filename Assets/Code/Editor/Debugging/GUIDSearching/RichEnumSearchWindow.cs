using System.Collections.Generic;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.Utility.Enums;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Debugging.GUIDSearching {
    public class RichEnumSearchWindow : OdinEditorWindow {
        const string OtherGUIDToolsGroup = "Other GUID Tools";
        const string OtherGUIDToolsButtonsGroup = OtherGUIDToolsGroup+"/Buttons";
        
        [ShowInInspector, PropertyOrder(-10)]
        public string LastBake => GUIDCache.Instance?.LastBake;
        
        [Title("Input")]
        [SerializeField, RichEnumExtends(typeof(RichEnum))] RichEnumReference richEnumReference;
        
        [Title("Output")]
        [ShowInInspector, TableList(IsReadOnly = true, AlwaysExpanded = true), PropertyOrder(1), Space(10), Indent]
        List<GUIDSearchWindow.SearchResultObject> _foundUsages = new();
        
        public static void OpenWindow() {
            var window = GetWindow<RichEnumSearchWindow>(GUIDSearchWindow.DesiredDockTypes);
            window.Show();
        }
        
        [MenuItem("TG/Assets/Find by RichEnum", priority = -100)]
        static void CreateWindow() {
            var window = CreateWindow<RichEnumSearchWindow>(GUIDSearchWindow.DesiredDockTypes);
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
            foreach (string path in GUIDCache.Instance.GetDependent(richEnumReference.EnumRef)) {
                var so = new GUIDSearchWindow.SearchResultObject(path);
                if (so.asset != GUIDCache.Instance) {
                    _foundUsages.Add(so);
                }
            }
        }
        
        [BoxGroup(OtherGUIDToolsGroup), HorizontalGroup(OtherGUIDToolsButtonsGroup), PropertyOrder(-1)]
        [Button(ButtonSizes.Small)]
        void OpenUnusedSearchWindow() {
            UnusedSearchWindow.OpenWindow();
        }
        
        [HorizontalGroup(OtherGUIDToolsButtonsGroup), PropertyOrder(-1)]
        [Button(ButtonSizes.Small)]
        void OpenGUIDSearchWindow() {
            GUIDSearchWindow.OpenWindow();
        }
    }
}