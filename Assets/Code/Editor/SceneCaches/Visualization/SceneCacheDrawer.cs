using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.General.Caches;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Stories.Actors;
using Awaken.Utility.Editor;
using Awaken.Utility.UI;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.SceneCaches.Visualization {
    public static class SceneCacheDrawer {
        const int BigNameScale = 3;
        
        public const float LOD1Scale = 0.4f;
        public const float LOD2Scale = 0.08f;
        public const float CullScale = 0.015f;

        public static readonly float LOD2Height = EditorGUIUtility.singleLineHeight * 5;
        public static readonly float LOD2Width = LOD2Height * 1.5f;
        public static readonly float BigNameHeight = EditorGUIUtility.singleLineHeight * BigNameScale;

        public static readonly AssetDrawer<LocationSpec> DrawerSpec = new("Spec_", "NPC_", "VisualPicker_",
            "Enemy_", "EnemyMonster_", "EnemyZombie_",
            "Generic_", "Elite_",
            "T0_", "T1_", "T2_", "T3_", "T4_", "T5_", 
            "Tier0_", "Tier1_", "Tier2_", "Tier3_", "Tier4_", "Tier5_"
        );
        public static readonly AssetDrawer<NpcTemplate> DrawerNpcTemplate = new("NPCTemplate_");
        public static readonly AssetDrawer<ItemTemplate> DrawerItem = new("ItemTemplate_", 
            "Weapon_", "1H_", "2H_", "Bow_", "Sword_", "Axe_", "Polearm_", "Dagger_", "Tool_", 
            "Tier0_", "Tier1_", "Tier2_", "Tier3_", "Tier4_", "Tier5_", 
            "Armor_", "Ammo_", "Crafting_", "Alchemy_", "Cooking_", 
            "VeryHeavy_", "Heavy_", "Medium_", "Light_", "VeryLight_", 
            "Tier0_", "Tier1_", "Tier2_", "Tier3_", "Tier4_", "Tier5_", 
            "T0_", "T1_", "T2_", "T3_", "T4_", "T5_",
            "Legs_", "Body_", "Head_", "Feet_", "Arms_",
            "Currency_", "Readable_", "Special_"
        );
        
        static GUIStyle s_bigNameStyle;
        static GUIStyle s_assetButtonStyle;
        
        public static GUIStyle BigNameStyle => s_bigNameStyle ??= new(EditorStyles.largeLabel) {
            fontSize = EditorStyles.largeLabel.fontSize * BigNameScale,
        };
        static GUIStyle AssetButtonStyle => s_assetButtonStyle ??= new(EditorStyles.objectField) {
            alignment = TextAnchor.MiddleLeft
        };

        public static AssetData<LocationSpec> GetAssetData(LocationSpec spec) => DrawerSpec.GetAssetData(spec);
        public static AssetData<NpcTemplate> GetAssetData(NpcTemplate template) => DrawerNpcTemplate.GetAssetData(template);
        public static AssetData<ItemTemplate> GetAssetData(ItemTemplate item) => DrawerItem.GetAssetData(item);

        public static void GUIDraw(Rect rect, string label) => EditorGUI.LabelField(rect, label);
        public static void GUIDraw(Rect rect, string label, in ActorRef actorRef) => ActorDrawer.Draw(rect, label, actorRef);
        public static void GUIDraw(Rect rect, in AssetData<LocationSpec> spec) => DrawerSpec.Draw(rect, spec);
        public static void GUIDraw(Rect rect, in AssetData<NpcTemplate> template) => DrawerNpcTemplate.Draw(rect, template);
        public static void GUIDraw(Rect rect, in AssetData<ItemTemplate> item) => DrawerItem.Draw(rect, item);
        
        public static float GetWidth(string label) => GUIUtils.LabelWidth(EditorStyles.label, label);

        public static void GetActorLabel(string label, in ActorRef actorRef, out string text, out float width) {
            text = ActorDrawer.GetLabel(label, actorRef);
            width = GetWidth(text);
        }

        static class ActorDrawer {
            static GameObject s_prefab;
            static ActorsRegister s_actorRegister;

            static void EnsureInit() {
                if (s_actorRegister != null) {
                    return;
                }
                
                if (s_prefab == null) {
                    s_prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ActorsRegister.Path);
                }

                s_actorRegister = s_prefab == null ? null : s_prefab.GetComponent<ActorsRegister>();
            }

            public static void Draw(Rect rect, string label, in ActorRef actorRef) {
                EnsureInit();
                string path = s_actorRegister != null ? s_actorRegister.Editor_GetPathFromGUID(actorRef.guid) : "no Actors prefab";
                EditorGUI.LabelField(rect, $"{label}: {path}");
            }

            public static string GetLabel(string label, in ActorRef actorRef) {
                EnsureInit();
                var path = s_actorRegister != null ? s_actorRegister.Editor_GetPathFromGUID(actorRef.guid) : "no Actors prefab";
                return $"{label}: {path}";
            }
        }

        public class AssetDrawer<T> where T : Object {
            readonly string[] _prefixes;
            
            public AssetDrawer(params string[] prefixes) {
                _prefixes = prefixes;
            }

            public AssetData<T> GetAssetData(T asset) {
                const float padding = 20f;
                if (asset == null) {
                    return default;
                }
                var name = GetAssetName(asset);
                return new AssetData<T>(asset, name, asset.name, GetWidth(name) + padding);
            }
            
            public void Draw(in Rect rect, in AssetData<T> data) {
                if (GUI.Button(rect, GUIUtils.Content(data.name, data.tooltip), AssetButtonStyle)) {
                    EditorGUIUtility.PingObject(data.asset);
                    Selection.activeObject = data.asset;
                }
            }
            
            string GetAssetName(T asset) {
                var name = asset.name;
                foreach (var prefix in _prefixes) {
                    if (name.StartsWith(prefix)) {
                        name = name[prefix.Length..];
                    }
                }
                return name;
            }
        }
        
        public readonly struct AssetData<T> {
            public readonly T asset;
            public readonly string name;
            public readonly string tooltip;
            public readonly float width;
                
            public AssetData(T asset, string name, string tooltip, float width) {
                this.asset = asset;
                this.name = name;
                this.tooltip = tooltip;
                this.width = width;
            }
        }
    }

    public interface ISceneCacheDrawer<in TSource, TMetadata> where TSource : ISceneCacheSource where TMetadata : struct {
        void Init();
        
        int FilterHash();
        int PartsHash();
        
        bool Filter(ref TMetadata metadata);
        void GetSize(in TMetadata metadata, out float width, out float height);
        void Draw(in TMetadata metadata, Rect rect);
        
        string LOD1Name(in TMetadata metadata);
        Vector3 GetPosition(in TMetadata metadata);
        
        TMetadata CreateMetadata(TSource source);
    }
    
    public struct SystemMetadata {
        public Vector3 position;

        public bool filter;
        
        public float lod0Width;
        public float lod0Height;
        
        public string lod1Label;
        public float lod1Width;
    }
}