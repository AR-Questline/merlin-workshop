using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.Editor.Localizations;
using Awaken.TG.Editor.SceneCaches.Core;
using Awaken.TG.Editor.Utility;
using Awaken.TG.Graphics.Scene;
using Awaken.TG.Main.General.Caches;
using Awaken.TG.Main.Locations.Attachments.Attachment;
using Awaken.TG.Main.Scenes.SceneConstructors.SubdividedScenes;
using Awaken.TG.Main.Stories.Steps;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Awaken.TG.Editor.SceneCaches.Scenes {
    public static class SceneCrawler {
        static SceneConfigs s_sceneConfigs;
        static SceneConfigs SceneConfigs => s_sceneConfigs ??= BuildTools.GetSceneConfigs();
        static ScenesCache Cache => ScenesCache.Get;
        
        public static IEnumerable<SceneReference> CrawlAllScenes(SceneReference initialScene) {
            Cache.regions.Clear();
            
            foreach (var scene in CrawlScene(initialScene)) {
                yield return scene;
            }
            
            Cache.MarkBaked();
        }

        public static IEnumerable<SceneReference> CrawlScene(SceneReference scene, SceneRegion openWorldRegion = null,
            HashSet<SceneReference> visitedScenes = null) {
            visitedScenes ??= new HashSet<SceneReference>();
            if (!visitedScenes.Add(scene)) {
                yield break;
            }

            if (openWorldRegion == null || SceneConfigs.IsOpenWorld(scene)) {
                openWorldRegion = new SceneRegion(scene);
                Cache.regions.Add(openWorldRegion);
            } else {
                if (SceneConfigs.IsAdditive(scene)) {
                    openWorldRegion.interiors.Add(scene);
                } else {
                    openWorldRegion.dungeons.Add(scene);
                }
            }
            
            var editorAccessToSceneReference = new SceneReference.EditorAccess(scene);
            string scenePath = AssetDatabase.GUIDToAssetPath(editorAccessToSceneReference.Reference.Address);
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            BuildTools.LoadChildScenes();
            
            if (scene.RetrieveMapScene() is SubdividedScene subdividedScene) {
                openWorldRegion.subscenes.AddRange(subdividedScene.GetAllScenes(true));
            }

            List<SceneReference> scenesToCrawl = new();

            foreach (var portal in Object.FindObjectsByType<PortalAttachment>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)) {
                scenesToCrawl.Add(portal.targetScene);
            }

            foreach (var (story, _) in CacheBakerUtils.ForEachStory(scene)) {
                foreach (var element in StoryExplorerUtil.ExtractElements(story)) {
                    if (element is SEditorMapChange mapChange) {
                        scenesToCrawl.Add(mapChange.scene);
                    }
                }
            }
            
            // TODO: Add DeathUI teleports
            yield return scene;
            
            // Recurrence
            foreach (var sceneToCrawl in scenesToCrawl.Where(s => s is { IsSet: true })) {
                foreach (var s in CrawlScene(sceneToCrawl, openWorldRegion, visitedScenes)) {
                    yield return s;
                }
            }
        }
    }
}