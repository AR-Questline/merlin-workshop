using System;
using Awaken.TG.Assets;
using Awaken.TG.Main.FastTravel;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes.CharacterSheet.Map.Markers;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Discovery;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Stories.Quests;
using Awaken.TG.Main.Stories.Quests.Objectives;
using Awaken.TG.Main.UI.Popup;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.TG.MVC.Utils;
using Awaken.TG.Utility;
using Awaken.Utility.Maths;
using DG.Tweening;
using JetBrains.Annotations;
using UnityEngine;
using Compass = Awaken.TG.Main.Maps.Compasses.Compass;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Map {
    public partial class MapSceneUI : Element<MapUI>, MapSubTabsUI.ITab {
        public static float MinZoom => GameConstants.Get.mapZoomIn;
        public static float MaxZoom => GameConstants.Get.mapZoomOut;
        
        public SceneReference Scene { get; }
        readonly bool _isCurrentScene;
        readonly MapSceneData _data;
        
        Vector2 _startDragTranslation;
        float _fullZoom;
        
        WeakModelRef<MapMarker> _pointedMarkerRef;
        Tween _fadeFastTravel;
        
        bool _canvasCalculated;
        Action _afterFirstCanvasCalculate;
        
        public Vector3 WorldPosition { get; private set; }

        public Type TabView => typeof(VMapSceneUI);
        public MapSceneData Data => _data;
        public float Zoom => Mathf.Lerp(MinZoom, MaxZoom, _fullZoom);
        
        public float GamepadTranslationSpeed => Services.Get<GameConstants>().mapGamepadMoveSpeed * Zoom;
        [CanBeNull] public MapMarker PointedMarker => _pointedMarkerRef.Get();
        
        public new static class Events {
            public static readonly Event<MapUI, MapSceneUI> ParametersChanged = new(nameof(ParametersChanged));
            public static readonly Event<MapUI, MapMarker> SelectedMarkerChanged = new(nameof(SelectedMarkerChanged));
        }
        
        public MapSceneUI(SceneReference scene) {
            Scene = scene;
            _isCurrentScene = scene == Services.Get<SceneService>().ActiveSceneRef;
            _data = CommonReferences.Get.MapData.byScene[scene];
            _fullZoom = 0.5f;
        }
        
        public void AfterViewSpawned(View view) {
            var worldPosition = _isCurrentScene ? Hero.Current.Coords : Data.Bounds.center;
            worldPosition.y = Data.Bounds.max.y;
            WorldPosition = worldPosition;
        }

        public void SortMarkers() {
            View<VMapSceneUI>().SortMarkers();
        }
        
        public void ChangeZoom(float scrollDelta) {
            _fullZoom -= scrollDelta * 0.01f;
            _fullZoom = Mathf.Clamp01(_fullZoom);
            ParentModel.Trigger(Events.ParametersChanged, this);
            Physics.SyncTransforms();
        }

        public void ChangeTranslation(Vector3 deltaPosition) {
            var worldPosition = WorldPosition + deltaPosition;
            var min = _data.Bounds.min;
            var max = _data.Bounds.max;
            worldPosition.x = Mathf.Clamp(worldPosition.x, min.x, max.x);
            worldPosition.z = Mathf.Clamp(worldPosition.z, min.z, max.z);
            WorldPosition = worldPosition;

            ParentModel.Trigger(Events.ParametersChanged, this);
        }
        
        public void PointingTo(MapMarker mapMarker) {
            if (mapMarker?.ID == _pointedMarkerRef.ID) {
                return;
            }

            _pointedMarkerRef = new(mapMarker);
            
            var compass = World.Any<Compass>();
            var customMarker = compass?.CustomMarkerLocation;
            ParentModel.SetPointingPromptActionName(customMarker != null && customMarker == mapMarker?.Grounded, mapMarker?.ParentModel is Objective objective && objective.ParentModel != World.Only<QuestTracker>().ActiveQuest);
            
            bool isFastTravel = mapMarker?.ParentModel is CrossSceneLocationMarker { IsFastTravel: true };
            _fadeFastTravel.Kill();
            var view = ParentModel.View<VMapUI>();
            if (ParentModel.FastTravelAllowed) {
                view.SetFastTravelPromptDefault();
            } else {
                view.SetFastTravelPromptNotAvailable();
            }
            _fadeFastTravel = ParentModel.View<VMapUI>().FastTravelPrompt
                .DOFade(isFastTravel ? 1 : 0, VMapUI.TooltipFadeDuration)
                .SetUpdate(true)
                .SetEase(isFastTravel ? Ease.OutQuad : Ease.InQuad);

            ParentModel.Trigger(Events.SelectedMarkerChanged, mapMarker);
        }
        

        public void MarkerClicked(MapMarker mapMarker) {
            if (mapMarker.ParentModel is Objective objective) {
                if (objective.ParentModel != World.Only<QuestTracker>().ActiveQuest) {
                    World.Only<QuestTracker>().Track(objective.ParentModel);
                    //Refresh quest marker after changing active quest
                    foreach (var marker in World.All<MapMarker>()) {
                        if (World.ViewsFor(marker).Count == 0 && marker.IsFromScene(Scene)) {
                            marker.SpawnView(this);
                        }
                    }

                    _pointedMarkerRef = new(null as MapMarker);
                    ParentModel.Trigger(Events.ParametersChanged, this);
                    ParentModel.Trigger(Events.SelectedMarkerChanged, null);
                    Physics.SyncTransforms();
                    return;
                }
            }

            if (mapMarker.ParentModel is Location location) {
                var compass = World.Any<Compass>();
                bool removed = compass?.TryRemoveCustomMarker(location) == true ||
                               compass?.TryRemoveSpyglassMarker(location) == true;
                
                if (removed) {
                    return;
                }
            }

            GroundClicked(mapMarker.Position.XZ());
        }
        
        public void GroundClicked(Vector2 position) {
            var height = Ground.HeightAt(position.X0Y());
            var coords = position.XCY(height);
            World.Any<Compass>()?.PlaceCustomMarker(coords)?.SpawnView(this);
            ParentModel.Trigger(Events.ParametersChanged, this);
        }
        
        public bool TryFastTravel(MapMarker marker) {
            if (!ParentModel.FastTravelAllowed) {
                return false;
            }
            World.Only<Focus>().DeselectAll();
            if (marker.ParentModel is not CrossSceneLocationMarker { IsFastTravel: true } crossSceneLocation) {
                return false;
            }
            if (Hero.Current.HeroCombat.IsHeroInFight) {
                PopupUI.SpawnNoChoicePopup(
                    typeof(VSmallPopupUI),
                    LocTerms.FastTravelBlockedByCombat.Translate(),
                    LocTerms.FastTravelPopupTitle.Translate()
                );
            } else if (Hero.Current.IsUnderWater) {
                PopupUI.SpawnNoChoicePopup(
                    typeof(VSmallPopupUI),
                    LocTerms.FastTravelBlockedByWater.Translate(),
                    LocTerms.FastTravelPopupTitle.Translate()
                );
            } else if (Hero.Current.IsEncumbered) {
                PopupUI.SpawnNoChoicePopup(
                    typeof(VSmallPopupUI),
                    LocTerms.FastTravelBlockedByEncumbrance.Translate(),
                    LocTerms.FastTravelPopupTitle.Translate()
                );
            } else {
                PopupUI popup = null;
                popup = PopupUI.SpawnSimplePopup(
                    typeof(VSmallPopupUI),
                    LocTerms.FastTravelPopupMessage.Translate(crossSceneLocation.DisplayName),
                    PopupUI.AcceptTapPrompt(
                        () => {
                            crossSceneLocation.Teleport().Forget();
                            popup!.Discard();
                        }),
                    PopupUI.CancelTapPrompt(() => popup!.Discard()),
                    LocTerms.FastTravelPopupTitle.Translate()
                );
            }
            return true;
        }
        
        public void FirstCanvasCalculated() {
            _canvasCalculated = true;
            _afterFirstCanvasCalculate?.Invoke();
            _afterFirstCanvasCalculate = null;
        }
        
        public void AfterFirstCanvasCalculate(Action action) {
            if (_canvasCalculated) {
                action.Invoke();
                return;
            }
            _afterFirstCanvasCalculate += action;
        }
    }
}