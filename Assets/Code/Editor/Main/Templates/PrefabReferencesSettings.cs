using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Awaken.TG.Editor.Main.Templates {
    public class PrefabReferencesSettings : ScriptableObject {
        public const string PrefabSettingsPath = "Assets/Data/Settings/PrefabReferencesSettings.asset";

        static PrefabReferencesSettings s_instance;

        public static PrefabReferencesSettings Instance {
            get {
                if (s_instance == null) {
                    s_instance = AssetDatabase.LoadAssetAtPath<PrefabReferencesSettings>(PrefabSettingsPath);

                    if (s_instance == null) {
                        s_instance = ScriptableObject.CreateInstance<PrefabReferencesSettings>();
                        AssetDatabase.CreateAsset(s_instance, PrefabSettingsPath);
                    }
                }

                return s_instance;
            }
        }

        //NPC
        [SerializeField] Object defaultMalePrefab, defaultFemalePrefab, defaultAIPrefab, defaultNpcTemplatePrefab;
        //Books
        public List<Object> bookPrefabs;

        public PrefabReferenceData DefaultAI => new(defaultAIPrefab);
        public PrefabReferenceData DefaultMale => new(defaultMalePrefab);
        public PrefabReferenceData DefaultFemale => new(defaultFemalePrefab);
        public PrefabReferenceData DefaultNpcTemplate => new(defaultNpcTemplatePrefab);

        public class PrefabReferenceData {
            Object Prefab { get; }
            public string Path => AssetDatabase.GetAssetPath(Prefab);
            public string Guid => AssetDatabase.AssetPathToGUID(Path);

            public PrefabReferenceData(Object prefabObject) {
                Prefab = prefabObject;
            }
        }
    }
}