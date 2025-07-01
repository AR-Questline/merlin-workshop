using System;
using System.Collections.Generic;
using Awaken.TG.Assets;
using Awaken.TG.Graphics.MapServices;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.UI.Components.Tabs;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Map {
    public partial class MapSubTabsUI : Tabs<MapUI, VMapSubTabsUI, MapSubTabType, MapSceneUI> {
        protected override KeyBindings Previous => KeyBindings.UI.Generic.PreviousAlt;
        protected override KeyBindings Next => KeyBindings.UI.Generic.NextAlt;

        public MapSubTabType[] Types { get; }
        
        public MapSubTabsUI(MapSubTabType[] types) {
            Types = types;
        }
        
        public static MapSubTabType[] CollectAvailableScenes(out MapSubTabType current) {
            current = null;

            var activeScene = World.Services.Get<SceneService>().ActiveSceneRef;
            ref readonly var mapData = ref CommonReferences.Get.MapData;
            var mapService = World.Services.Get<MapService>();
            foreach (var group in mapData.sceneGroups) {
                if (Array.IndexOf(group.scenes, activeScene) >= 0) {
                    var scenes = new List<MapSubTabType>(group.scenes.Length);
                    foreach (var scene in group.scenes) {
                        if (mapService.WasVisited(scene) && mapData.byScene.TryGetValue(scene, out var data)) {
                            var type = new MapSubTabType(scene, data.Name);
                            if (scene == activeScene) {
                                current = type;
                            }
                            scenes.Add(type);
                        }
                    }
                    return scenes.ToArray();
                }
            }
            {
                if (mapData.byScene.TryGetValue(activeScene, out var data)) {
                    current = new MapSubTabType(activeScene, data.Name);
                    return new[] { current };
                } else {
                    return Array.Empty<MapSubTabType>();
                }
            }
        }
    }

    public class MapSubTabType : MapSubTabsUI.ITabType {
        readonly SceneReference _scene;
        readonly string _name;

        public string Name => _name;

        public MapSubTabType(SceneReference scene, string name) {
            _scene = scene;
            _name = name;
        }

        public MapSceneUI Spawn(MapUI target) => new(_scene);
        public bool IsVisible(MapUI target) => true;
    }
}