using System.Linq;
using System.Threading;
using Awaken.TG.Assets;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Locations.Attachments.Attachment;
using Awaken.TG.Main.Locations.Geysers;
using Awaken.TG.Main.Scenes;
using Awaken.TG.Main.Stories.Steps.Helpers;
using Awaken.TG.Main.UI.TitleScreen.Loading;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Awaken.Utility.Extensions;
using Awaken.Utility.Maths;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using FMODUnity;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Elements {
    public partial class TheTowerOfBoneAndTimberCoordinator : Element<Location>, IRefreshedByAttachment<TheTowerOfBoneAndTimberCoordinatorAttachment> {
        public override ushort TypeForSerialization => SavedModels.TheTowerOfBoneAndTimberCoordinator;

        TheTowerOfBoneAndTimberCoordinatorAttachment _spec;
        IEventListener _listener;
        CancellationTokenSource _cts = new();
        bool _heroHasItemDisablingShoutDmg;
        
        MonsterEggLauncher[] _eggLaunchers;
        GeyserElement[] _geysers;
        
        [Saved]
        bool _completed;

        bool _incrementedForceCombat;

        public void InitFromAttachment(TheTowerOfBoneAndTimberCoordinatorAttachment spec, bool isRestored) {
            _spec = spec;
        }

        protected override void OnFullyInitialized() {
            if (_completed) {
                World.EventSystem.LimitedListenTo(EventSelector.AnySource, SceneLifetimeEvents.Events.SafeAfterSceneChanged, this, _ => DelayedPortalHero().Forget(), 1);
                return;
            }
            _listener = ParentModel.ListenTo(Location.Events.InteractabilityChanged, OnLocationVisibilityChanged, this);
        }

        async UniTaskVoid DelayedPortalHero() {
            if (World.Any<LoadingScreenUI>() is {} ls) {
                await AsyncUtil.WaitForDiscard(ls);
            }
            _spec.portalTags?.MatchingLocations(null).FirstOrDefault()?.Element<Portal>().Execute(Hero.Current);
        }

        void OnLocationVisibilityChanged(LocationInteractability interactibility) {
            if (interactibility == LocationInteractability.Active) {
                World.EventSystem.DisposeListener(ref _listener);
                _eggLaunchers = _spec.launcherTags.MatchingLocations(null).Select(l => l.Element<MonsterEggLauncher>()).ToArray();
                _geysers = _spec.geyserTags.MatchingLocations(null).Select(l => l.Element<GeyserElement>()).ToArray();
                HeroCombat.forceCombatCount++;
                _incrementedForceCombat = true;
                Stage1().Forget();
            }
        }

        async UniTaskVoid Stage1() {
            _heroHasItemDisablingShoutDmg = !_spec.itemDisablingShoutDamageGUID.IsNullOrWhitespace() 
                                            && Hero.Current.HeroItems.Inventory.Any(item => item.Template.GUID == _spec.itemDisablingShoutDamageGUID);
            
            Log.Marking?.Warning("Starting Tower of Bone and Timber Stage 1");
            LaunchShoutSkill().Forget();
            
            var settings = _spec.GetStageSettings(1);
            
            // Enable spawner 1 and listen to discard
            Location stage1Spawner = SpawnSpawner(settings, 1);
            
            stage1Spawner.ListenTo(Events.BeforeDiscarded, () => {
                _cts.Cancel();
                _cts = new CancellationTokenSource();
                Stage2().Forget();
            }, this);

            await CannonsAtIntervals(settings.spawnerIntensityMultiplier, settings.activeTime, settings.inactiveTime);
        }

        async UniTaskVoid Stage2() {
            Log.Marking?.Warning("Starting Tower of Bone and Timber Stage 2");
            LaunchShoutSkill().Forget();
            
            var settings = _spec.GetStageSettings(2);
            
            RandomizeGeyzerPositions().Forget();
            
            // Enable spawner 2 and listen to discard
            Location stage2Spawner = SpawnSpawner(settings, 2);
            stage2Spawner.ListenTo(Events.BeforeDiscarded, () => {
                _cts.Cancel();
                _cts = new CancellationTokenSource();
                Stage3().Forget();
            }, this);

            await CannonsAtIntervals(settings.spawnerIntensityMultiplier, settings.activeTime, settings.inactiveTime);
        }

        async UniTaskVoid Stage3() {
            int currentStage = 3;
            Log.Marking?.Warning("Starting Tower of Bone and Timber Stage " + currentStage);
            LaunchShoutSkill().Forget();
            
            var settings = _spec.GetStageSettings(currentStage);
            
            RandomizeGeyzerPositions().Forget();
            
            // Enable spawner 3 and listen to discard
            Location stage3Spawner = SpawnSpawner(settings, currentStage);
            stage3Spawner.ListenTo(Events.BeforeDiscarded, () => {
                _cts.Cancel();
                _cts = new CancellationTokenSource();
                Stage4().Forget();
            }, this);

            await CannonsAtIntervals(settings.spawnerIntensityMultiplier, settings.activeTime, settings.inactiveTime);
        }

        async UniTaskVoid Stage4() {
            int currentStage = 4;
            Log.Marking?.Warning("Starting Tower of Bone and Timber Stage " + currentStage);
            LaunchShoutSkill().Forget();
            
            var settings = _spec.GetStageSettings(currentStage);
            
            RandomizeGeyzerPositions().Forget();
            
            // Enable spawner 4 and listen to discard
            Location stage4Spawner = SpawnSpawner(settings, currentStage);
            stage4Spawner.ListenTo(Events.BeforeDiscarded, OnStage4End, this);

            await CannonsAtIntervals(settings.spawnerIntensityMultiplier, settings.activeTime, settings.inactiveTime);
        }

        void OnStage4End() {
            _cts?.Cancel();
            _cts = null;
            
            _completed = true;
            if (_incrementedForceCombat) {
                _incrementedForceCombat = false;
                HeroCombat.forceCombatCount--;
            }
            StoryUtils.TryStartStory(_spec.endOfFightStory);
            
            EndOfFightSequence().Forget();
        }

        async UniTaskVoid EndOfFightSequence() {
            foreach (MonsterEggLauncher monsterEggLauncher in _eggLaunchers) {
                if (monsterEggLauncher.HasBeenDiscarded) continue;
                monsterEggLauncher.DisableLaunches();
            }
            foreach (GeyserElement geyserElement in _geysers) {
                if (geyserElement.HasBeenDiscarded) continue;
                geyserElement.DeactivateWhenHidden().Forget();
            }
            
            // Disable GameObject if configured
            if (_spec.endOfFightToDisable != null) {
                _spec.endOfFightToDisable.SetActive(false);
            }

            // Move all configured transforms
            if (_spec.endOfFightMovements != null && _spec.endOfFightMovements.Length > 0) {
                var moveTasks = new UniTask[_spec.endOfFightMovements.Length];
                for (int i = 0; i < _spec.endOfFightMovements.Length; i++) {
                    var movement = _spec.endOfFightMovements[i];
                    if (movement.transform != null) {
                        // RuntimeManager.PlayOneShotAttached(movement.movementSFX, movement.transform.gameObject);
                        // moveTasks[i] = movement.transform
                        //     .DOMove(movement.targetPosition, movement.movementDuration)
                        //     .SetEase(Ease.InOutQuad)
                        //     .ToUniTask();
                    }
                }
                
                await UniTask.WhenAll(moveTasks);
            }
            
            Log.Marking?.Warning("Completed Tower of Bone and Timber Coordinator");
        }

        async UniTaskVoid LaunchShoutSkill() {
            Vector3 origin = _spec.shoutPosition;
            var shout = _spec.shoutSkill;
            
            await TrySpawnShoutVfx(origin, shout.vfx, shout.vfxDuration, shout.damageRadius);
            
            // Setup damage parameters
            if (!_heroHasItemDisablingShoutDmg) {
                shout.ApplySphereDamage(origin);
            }
        }

        async UniTask TrySpawnShoutVfx(Vector3 position, ShareableARAssetReference vfx, float duration, float distortionSize) {
            if (vfx is not { IsSet: true }) {
                Log.Important?.Warning("Shouting VFX is not configured for Tower of Bone and Timber");
                return;
            }
            
            var result = await PrefabPool.InstantiateAndReturn(vfx, position, Quaternion.identity, duration);
            if (result.Instance != null) {
                var vfxComponent = result.Instance.GetComponentInChildren<UnityEngine.VFX.VisualEffect>(true);
                if (vfxComponent != null) {
                    vfxComponent.SetFloat("EffectLifetime", duration);
                    vfxComponent.SetFloat("DistortionSize", distortionSize);
                }
            }
        }
        
        Location SpawnSpawner(TheTowerOfBoneAndTimberCoordinatorAttachment.StageSettings settings, int stage) {
            Vector3 randomPoint = (Random.insideUnitSphere * _spec.rangeFromSpawnerPointToPlaceSpawner).X0Z() + _spec.spawnerPosition;
            Location spawner = settings.Spawner.SpawnLocation(randomPoint, Quaternion.identity, overridenLocationName: $"Tower of Bone and Timber - Stage {stage} Spawner");
            return spawner;
        }

        async UniTaskVoid RandomizeGeyzerPositions() {
            var token = _cts.Token;
            while (token.IsCancellationRequested == false) {
                UniTask[] geyserElements = new UniTask[5];
                int i = 0;
                foreach (GeyserElement geyserElement in _geysers) {
                    geyserElements[i++] = geyserElement.DeactivateWhenHidden();
                }
                await UniTask.WhenAll(geyserElements);
                
                if (token.IsCancellationRequested) {
                    return;
                }

                foreach (GeyserElement geyserElement in _geysers) {
                    Location geyserLocation = geyserElement.ParentModel;
                    Vector3 randomPoint = Random.insideUnitSphere * _spec.rangeFromSpawnerPointToMoveGeysers + _spec.spawnerPosition;
                    geyserLocation.MoveAndRotateTo(randomPoint.WithY(geyserLocation.Coords.y), Quaternion.identity, true);
                    geyserElement.ActivateWithDelay(Random.Range(0, 3f)).Forget();
                }
                
                if (!await AsyncUtil.DelayTime(this, _spec.geyserRepositionInterval, _cts.Token)) {
                    return;
                }
            }
        }
        
        async UniTask CannonsAtIntervals(float spawnerIntensityMultiplier, float activeTime, float inactiveTime) {
            var token = _cts.Token;
            while (token.IsCancellationRequested == false) {
                foreach (MonsterEggLauncher monsterEggLauncher in _eggLaunchers) {
                    monsterEggLauncher.SetExternalIntervalMultiplier(1 / spawnerIntensityMultiplier);
                    monsterEggLauncher.EnableLaunches();
                }

                if (!await AsyncUtil.DelayTime(this, activeTime, token)) {
                    break;
                }
                
                foreach (MonsterEggLauncher monsterEggLauncher in _eggLaunchers) {
                    monsterEggLauncher.DisableLaunches();
                }
                
                if (!await AsyncUtil.DelayTime(this, inactiveTime, token)) {
                    break;
                }
            }
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            if (_incrementedForceCombat) {
                _incrementedForceCombat = false;
                HeroCombat.forceCombatCount--;
            }
            _cts?.Cancel();
            _cts = null;
            base.OnDiscard(fromDomainDrop);
        }
    }
}