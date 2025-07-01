using System;
using System.Collections.Generic;
using Awaken.TG.Assets;
using Awaken.TG.Graphics.Transitions;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Saving.Models;
using Awaken.TG.Main.Stories;
using Awaken.TG.MVC;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Actions.Customs {
    public partial class Stonehenge : AbstractLocationAction, IRefreshedByAttachment<StonehengeAttachment> {
        public override ushort TypeForSerialization => SavedModels.Stonehenge;

        StonehengeAttachment _spec;
        Dictionary<Transform, Vector3> _initialPositionsByTransform = new();

        SaveBlocker _blocker;
        Location _druid1, _druid2;
        int _visualsLoaded = 0;

        public override ActionAvailability GetAvailability(Hero hero, IInteractableWithHero interactable) => _blocker == null ? ActionAvailability.Available : ActionAvailability.Disabled;

        public void InitFromAttachment(StonehengeAttachment spec, bool isRestored) {
            _spec = spec;
            _spec.volume.weight = 0f;
            StoreTransformPositions(_initialPositionsByTransform);
        }

        protected override void OnInitialize() {
            ParentModel.ListenTo(Location.Events.LocationVisibilityChanged, OnVisibilityChange, this);
        }

        protected override void OnStart(Hero hero, IInteractableWithHero interactable) {
            if (_spec == null || _spec.volume == null || _spec.stoneCircle == null) {
                throw new ArgumentException();
            }
            
            Show().Forget();
        }

        async UniTaskVoid Show() {
            _blocker = World.Add(new SaveBlocker(ParentModel));
            
            _visualsLoaded = 0;
            
            Vector3 position = Ground.SnapToGround(ParentModel.Coords);
            PrefabPool.InstantiateAndReturn(_spec.shockWavePrefab, position, Quaternion.identity).Forget();

            float val = 0f;
            DOTween.To(() => val, SetShowProgress, 1f, 0.7f).SetEase(Ease.OutQuad).Forget();
            await UniTask.Delay(200);
            await World.Services.Get<TransitionService>().TransitionToBlack(0.5f);
            _spec.pedestalRef.MatchingLocations(null).ForEach(l => l.SetInteractability(LocationInteractability.Hidden));
            ShowDruids();
            
            void SetShowProgress(float v) {
                val = v;
                _spec.volume.weight = v;
                foreach (var go in _initialPositionsByTransform.Keys) {
                    Vector3 initial = _initialPositionsByTransform[go];
                    go.position = Vector3.Lerp(initial, initial + Vector3.down * 10f, v);
                }
            }
        }

        async UniTaskVoid Hide() {
            Dictionary<Transform, Vector3> positions = new();
            StoreTransformPositions(positions);
            
            await World.Services.Get<TransitionService>().TransitionToBlack(0.5f);

            if (!_druid1.HasBeenDiscarded) {
                _druid1.Discard();
            }
            if (!_druid2.HasBeenDiscarded) {
                _druid2.Discard();
            }
            
            float val = 1f;
            await DOTween.To(() => val, SetHideProgress, 0f, 1f).SetEase(Ease.InQuad);
            
            await World.Services.Get<TransitionService>().TransitionFromBlack(0.5f);
            
            if (!HasBeenDiscarded) {
                _blocker.Discard();
                _blocker = null;
                Discard();
            }
            
            void SetHideProgress(float v) {
                val = v;
                _spec.volume.weight = v;
                foreach (var go in _initialPositionsByTransform.Keys) {
                    go.position = Vector3.Lerp(_initialPositionsByTransform[go], positions[go], v);
                }
            }
        }

        void OnVisibilityChange(bool active) {
            if (!active) {
                Hide().Forget();
            }
        }

        void ShowDruids() {
            try {
                _druid1 = _spec.Druid1.SpawnLocation(_spec.druid1Pos.position, _spec.druid1Pos.rotation);
                _druid2 = _spec.Druid2.SpawnLocation(_spec.druid2Pos.position, _spec.druid2Pos.rotation);
                
                _druid1.OnVisualLoaded(OnVisualLoaded);
                _druid2.OnVisualLoaded(OnVisualLoaded);
            } catch (Exception e) {
                // Bypass DoTween's silent exceptions
                Debug.LogException(e);
                throw;
            }
        }

        void OnVisualLoaded(Transform _) {
            if (++_visualsLoaded == 2) {
                Story.StartStory(StoryConfig.Base(_spec.storyRef, typeof(VDialogue))
                    .WithLocation(_druid1)
                    .WithLocation(_druid2));
            }
        }

        void StoreTransformPositions(Dictionary<Transform, Vector3> dictionary) {
            foreach (var go in _spec.stoneCircle.WhereNotNull()) {
                dictionary[go.transform] = go.transform.position;
            }
        }
    }
}