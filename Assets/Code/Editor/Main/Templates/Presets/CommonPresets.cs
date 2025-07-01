using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Editor.Assets;
using Awaken.TG.Editor.Utility.Paths;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Templates;
using JetBrains.Annotations;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Main.Templates.Presets {
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public static class CommonPresets {
        [Preset(typeof(ItemTemplate))]
        public static PresetButton GetReadablePreset(object obj) {
            return new PresetButton("Readable", t => {
                ItemReadSpec readSpec = t.gameObject.GetOrAddComponent<ItemReadSpec>();
                StoryGraph graph;
                if (EditorUtility.DisplayDialog("Story", "Do you want to use existing Story Graph or create new one?", "Create new",
                        "Use existing")) {
                    graph = (StoryGraph) TemplateCreation.CreateScriptableObject(StoryGraph.CreateGraph, "Assets/Data/Templates/Stories", false);
                } else {
                    string path = EditorUtility.OpenFilePanel("Choose Story Graph", "Assets/Data/Templates/Stories", "asset");
                    if (string.IsNullOrEmpty(path)) {
                        return;
                    }
                    path = PathUtils.FilesystemToAssetPath(path);
                    graph = AssetDatabase.LoadAssetAtPath<StoryGraph>(path);
                }
                TemplateReferenceDrawer.ValidateDraggedObject(graph);
                TemplatesUtil.EDITOR_AssignGuid(graph, graph);
                readSpec.EDITOR_SetStoryGraph(graph);
            });
        }
        
        [Preset(typeof(LocationSpec))]
        public static PresetButton GetNpcPreset(object obj) {
            return new PresetButton("NPC", t => {
                NpcCreator.Show((LocationSpec) t);
            });
        }
        
        [Preset(typeof(LocationSpec))]
        public static PresetButton GetReadableLocationSpecPreset(object obj) {
            return new PresetButton("Readable", t => {
                ReadableCreator.Show((LocationSpec) t);
            });
        }

        public static void RemoveAllExcept(GameObject go, params Type[] types) {
            bool ShouldRemove(Type t) => t != typeof(Transform) && !types.Contains(t);
            List<Component> toRemove = go.GetComponents<Component>()
                .Where(c => ShouldRemove(c.GetType()))
                .ToList();
            
            foreach (var c in toRemove) {
                Undo.DestroyObjectImmediate(c);
            }
        }
    }
}