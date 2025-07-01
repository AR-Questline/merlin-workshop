using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Editor.Utility;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Templates;
using Awaken.Utility.Debugging;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Editor.Graphics.IconRenderer {
    public class IconRendererWindow : OdinEditorWindow {
        static IconRendererWindow s_instance;

        bool ShowFiltered => filteredSettings?.Count > 0;
        
        [HorizontalGroup] [TemplateType(typeof(ItemTemplate)), OnValueChanged(nameof(FindInSettings))]
        public TemplateReference findItemTemplate;

        [field: SerializeField, HideIf(nameof(ShowFiltered))]
        IconRendererSettings settings;
        
        void FindInSettings() {
            if (findItemTemplate == null || !findItemTemplate.IsSet) {
                ClearSearch();
                return;
            }

            var template = findItemTemplate.Get<ItemTemplate>();
            var found = IconRenderer.Settings.categories
                .Where(c => c.IconsRenderingSettings.Exists(p=>p.prefab == template.gameObject))
                .ToList();

            filteredSettings ??= new List<IconRendererCategory>();
            if (found.Count > 0) {
                filteredSettings.Clear();
                foreach (IconRendererCategory setting in found) {
                    filteredSettings.Add(setting);
                    Log.Important?.Info($"{template.name} found in {setting.category}");
                }
            } else {
                ClearSearch();
                Log.Important?.Info($"No entries found for {template}");
            }
        }

        [HorizontalGroup]
        [Button, ShowIf(nameof(ShowFiltered))]
        void ClearSearch() {
            findItemTemplate = null;
            filteredSettings?.Clear();
        }

        [SerializeField, ShowIf(nameof(ShowFiltered)),
         ListDrawerSettings(CustomAddFunction = nameof(AddCategoryToFiltered), CustomRemoveElementFunction = nameof(RemoveCategoryFromFiltered))]
        public List<IconRendererCategory> filteredSettings;

        int AddCategoryToFiltered() {
            var newSetting = new IconRendererCategory("New Category");
            filteredSettings.Add(newSetting);
            IconRenderer.Settings.categories.Add(newSetting);
            return filteredSettings.Count;
        }

        int RemoveCategoryFromFiltered(IconRendererCategory setting) {
            filteredSettings.Remove(setting);
            IconRenderer.Settings.categories.Remove(setting);
            return filteredSettings.Count;
        }

        [MenuItem("ArtTools/Icon Renderer")]
        static void OpenWindow() {
            if (s_instance != null) {
                s_instance.Show();
            } else {
                s_instance = GetWindow<IconRendererWindow>();
                // s_instance.position = GUIHelper.GetEditorWindowRect().AlignCenter(530, 650);
            }
            
            s_instance.settings = s_instance.settings ? s_instance.settings : AssetDatabase.LoadAssetAtPath<IconRendererSettings>("Assets/2DAssets/RawRenderedIcons/IconRenderingSettings.asset");
        }

        [Button(ButtonSizes.Gigantic)]
        void StartRendering() {
            IconRenderer.RenderIcons();
        }

        public static bool TryGetRig(out SerializableDictionary<string, TransformValues> rigTransformValues) {
            rigTransformValues = new();
            if (s_instance == null) {
                return false;
            }

            var bones = IconRenderer.GetBonesFromScene();
            if (bones == null) {
                return false;
            }

            foreach (var transform in bones) {
                bool added = rigTransformValues.TryAdd(transform.name, new TransformValues(transform));
                if (!added) {
                    Log.Important?.Warning($"IconRenderer: Bone {transform.name} already exists in current config.");
                }
            }

            return true;
        }
        
    }
}