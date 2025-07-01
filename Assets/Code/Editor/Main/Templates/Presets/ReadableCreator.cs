using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.Editor.Utility.Assets;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Heroes.Items.LootTables;
using Awaken.TG.Main.Locations.Actions.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates;
using Awaken.Utility.GameObjects;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Awaken.TG.Editor.Main.Templates.Presets {
    public class ReadableCreator : OdinEditorWindow {
        [ValueDropdown(nameof(AllBookPrefabs)), HorizontalGroup("ReadablePrefab")]
        public Object readableVisual;
        [ValueDropdown(nameof(AllReadableItems)), HorizontalGroup("ItemTemplate")] 
        public ItemTemplate itemTemplate;
        
        LocationSpec _spec;

        IEnumerable<ItemTemplate> AllReadableItems => TemplatesProvider.EditorGetAllOfType<ItemTemplate>()
            .Where(item => item.GetComponent<ItemReadSpec>() != null);

        IEnumerable<Object> AllBookPrefabs => PrefabReferencesSettings.Instance.bookPrefabs;

        public static void Show(LocationSpec spec) {
            ReadableCreator creator = GetWindow<ReadableCreator>("Readable Creator", true);
            creator._spec = spec;
        }

        [Button("Ping"), HorizontalGroup("ReadablePrefab")]
        void PingPrefab() => EditorGUIUtility.PingObject(readableVisual);
        
        [Button("Ping"), HorizontalGroup("ItemTemplate")]
        void PingItem() {
            if (itemTemplate) {
                EditorGUIUtility.PingObject(itemTemplate.gameObject);
            }
        }

        [PropertySpace(50), Button(ButtonSizes.Medium), HorizontalGroup("Buttons")]
        void Cancel() => Close();

        [PropertySpace(50), Button(ButtonSizes.Medium), HorizontalGroup("Buttons")]
        void Approve() {
            GameObject go = _spec.gameObject;
            CommonPresets.RemoveAllExcept(go, typeof(PickItemAttachment), typeof(LocationSpec));
            LocationSpec specInPrefab = go.GetComponent<LocationSpec>();
            // Location Spec
            GameObjects.SetStaticRecursively(go, false);
            
            //Prefab
            specInPrefab.prefabReference = new ARAssetReference(AssetsUtils.ObjectToGuid(readableVisual));
            
            //Item template
            PickItemAttachment pickItemAttachment = go.GetOrAddComponent<PickItemAttachment>();
            TemplateReferenceDrawer.ValidateDraggedObject(itemTemplate.gameObject);
            TemplatesUtil.EDITOR_AssignGuid(itemTemplate, itemTemplate.gameObject);
            pickItemAttachment.itemReference = new ItemSpawningData(new TemplateReference(itemTemplate));
            
            // Apply overrides to prefab
            PrefabUtility.ApplyPrefabInstance(PrefabUtility.GetNearestPrefabInstanceRoot(go), InteractionMode.AutomatedAction);
            
            Close();
        }
    }
}