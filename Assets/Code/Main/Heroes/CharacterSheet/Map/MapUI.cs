﻿using Awaken.TG.Main.Crafting.Fireplace;
using Awaken.TG.Main.Heroes.CharacterSheet.Tabs;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.UI.Components.Tabs;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.Utility;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Map {
    public partial class MapUI : CharacterSheetTab<VMapUI>, MapSubTabsUI.ITabParent<VMapUI> {
        public const float MarginSize = 0.05f;
        // Margin size works for orthographic camera. IDK why for real world size multiplier is 21, but it works.  
        public const float MarginSizeWorldMultiplier = 21;

        bool _fastTravelAllowed;
        Prompt _markerActionPrompt;
        
        public static bool FogOfWarEnabled { get; private set; } = true;
        public MapSubTabType CurrentType { get; set; }
        public Tabs<MapUI, VMapSubTabsUI, MapSubTabType, MapSceneUI> TabsController { get; set; }

        public bool FastTravelAllowed => _fastTravelAllowed || World.HasAny<FireplaceUI>();
        
        VMapUI VMapUI => View<VMapUI>();

        protected override void AfterViewSpawned(VMapUI view) {
            ParentModel.SetHeroOnRenderVisible(false);
            view.transform.SetParent(ParentModel.MapHost, false);
            var scenes = MapSubTabsUI.CollectAvailableScenes(out var currentType);
            CurrentType = currentType;
            if (scenes.Length > 0) {
                AddElement(new MapSubTabsUI(scenes));
            }
            
            InitPrompts();
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

        void InitPrompts() {
            var prompts = ParentModel.Prompts;
            ParentModel.Prompts.BindPrompt(Prompt.VisualOnlyTap(null, LocTerms.UIMapMove.Translate()), this, VMapUI.MapTranslateCustomPrompt);
            VMapUI.MapTranslateCustomPrompt.transform.SetParent(ParentModel.PromptsHost);
            prompts.AddPrompt(Prompt.VisualOnlyTap(KeyBindings.UI.Map.MapZoom, LocTerms.UIMapZoom.Translate()), this);
            _markerActionPrompt = prompts.AddPrompt(Prompt.VisualOnlyTap(KeyBindings.UI.Map.PlaceCustomMarker, LocTerms.UIMapPlaceCustomMarker.Translate()), this);
        }
        
        public static float GetOrthoSize(float zoom, in Vector3 boundsSize, float mapAspectRatio) {
            float maxZ = math.max(boundsSize.z, boundsSize.x / mapAspectRatio);
            float maxSize = maxZ / 2;
            return (zoom + MarginSize) * maxSize;
        }
    }
}