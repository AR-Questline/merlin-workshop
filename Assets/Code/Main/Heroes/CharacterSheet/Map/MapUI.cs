using Awaken.TG.Main.Crafting.Fireplace;
using Awaken.TG.Main.Heroes.CharacterSheet.Tabs;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.UI.Components.Tabs;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.Utility;
using DG.Tweening;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Map {
    public partial class MapUI : CharacterSheetTab<VMapUI>, MapSubTabsUI.ITabParent<VMapUI> {
        public static bool FogOfWarEnabled { get; private set; } = true;
        
        Tween _fadeFastTravel;
        bool _fastTravelAllowed;
        
        Prompt _markerActionPrompt;
        
        public MapSubTabType CurrentType { get; set; }
        public Tabs<MapUI, VMapSubTabsUI, MapSubTabType, MapSceneUI> TabsController { get; set; }

        public Camera MarkersCamera => View<VMapCamera>() is { } view ? view.MarkersCamera : null;

        public bool FastTravelAllowed => _fastTravelAllowed || World.HasAny<FireplaceUI>();
        
        protected override void OnInitialize() {
            var prompts = ParentModel.Prompts;
            _markerActionPrompt = prompts.AddPrompt(Prompt.VisualOnlyTap(KeyBindings.UI.Map.PlaceCustomMarker, LocTerms.UIMapPlaceCustomMarker.Translate()), this);
            prompts.AddPrompt(Prompt.VisualOnlyTap(KeyBindings.UI.Map.MapTranslate, LocTerms.UIMapMove.Translate()), this);
            prompts.AddPrompt(Prompt.VisualOnlyTap(KeyBindings.UI.Map.MapZoom, LocTerms.UIMapZoom.Translate()), this);

        }

        protected override void AfterViewSpawned(VMapUI view) {
            ParentModel.SetHeroOnRenderVisible(false);
            view.transform.SetParent(ParentModel.MapHost, false);
            var scenes = MapSubTabsUI.CollectAvailableScenes(out var currentType);
            CurrentType = currentType;
            if (scenes.Length > 0) {
                World.SpawnView<VMapCamera>(this);
                AddElement(new MapSubTabsUI(scenes));
            }
        }

        public void AllowFastTravel() {
            _fastTravelAllowed = true;
        }

        public static bool IsOnSceneWithMap() {
            var activeScene = World.Services.Get<SceneService>().ActiveSceneRef;
            return CommonReferences.Get.MapData.byScene.ContainsKey(activeScene);
        }

        public void SetPointingPromptActionName(bool remove, bool untrackedQuest) {
            var name = remove ? LocTerms.UIMapRemoveCustomMarker : LocTerms.UIMapPlaceCustomMarker;
            name = untrackedQuest ? LocTerms.QuestNotificationTrack : name;
            _markerActionPrompt.ChangeName(name.Translate());
        }

        public static bool ToggleFogOfWar(bool? fogOfWarEnabled) {
            return FogOfWarEnabled = fogOfWarEnabled ?? !FogOfWarEnabled;
        }
    }
}