using System.Collections.Generic;
using Awaken.TG.Assets;
using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.AudioSystem.Biomes;
using Awaken.TG.Main.Heroes.CharacterSheet.Journal.Tabs;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.Main.Memories.Journal;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.Main.UI;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications.MiddleScreen;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications.MiddleScreen.FancyPanel;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications.MiddleScreen.Fishing;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility;
using Awaken.Utility;
using Awaken.Utility.Animations;
using Awaken.Utility.Collections;
using Awaken.Utility.Maths;
using Sirenix.OdinInspector;
using Unity.Collections;
using UnityEngine;
using AudioType = Awaken.TG.Main.AudioSystem.AudioType;

namespace Awaken.TG.Main.Heroes.Fishing {
    public class CharacterFishingRod : CharacterWeapon {
        const float StopFishingDelay = 3;
        const string FishingGroup = "Fishing";
        const float StartingPosition = 50;
        
        const int ThrowMask = RenderLayers.Mask.Default |
                              RenderLayers.Mask.Walkable |
                              RenderLayers.Mask.Water |
                              RenderLayers.Mask.Objects |
                              RenderLayers.Mask.Terrain |
                              RenderLayers.Mask.Vegetation |
                              RenderLayers.Mask.Ragdolls |
                              RenderLayers.Mask.AIs;

        [FoldoutGroup(FishingGroup), SerializeField] Transform visualFirePoint;
        [FoldoutGroup(FishingGroup), SerializeField, ARAssetReferenceSettings(new[] { typeof(FishingBobber) })] ARAssetReference fishingBobber;
        [FoldoutGroup(FishingGroup), SerializeField] float maxThrowDistance = 30f;
        [FoldoutGroup(FishingGroup), SerializeField] float throwVelocity = 20f;
        [FoldoutGroup(FishingGroup), SerializeField] int linePoints = 20;
        [FoldoutGroup(FishingGroup), SerializeField] float looseLineHeight = 1f;
        [FoldoutGroup(FishingGroup), SerializeField] float maxLineLength = 50f;
        [FoldoutGroup(FishingGroup), SerializeField] public float rodDamage = 15f;
        [FoldoutGroup(FishingGroup), SerializeField] public float rodHealth = 200f;
        [FoldoutGroup(FishingGroup), SerializeField] public float rodDownfallSpeedPerSecond = 4f;
        [FoldoutGroup(FishingGroup), SerializeField] public float pullDistance = 10f;

        LineRenderer _lineRenderer;
        ARAsyncOperationHandle<GameObject> _fishingBobberPrefab;
        FishingBobber _fishingBobberInstance;
        bool _isCurrentlyFishing;
        float _timeToStopFishing;
        IEventListener _fishPopupListener;
        CommonReferences _commonReferences;
        FishingFSM _fishingFSM;
        IFishVolume _currentFishVolume;
        FishingMiniGame _fishingMiniGame;
        FishData.FightingFish _fish;
        
        ref readonly FishingAudio Audio => ref _commonReferences.AudioConfig.FishingAudio;

        public new static class Events {
            public static readonly Event<Hero, Hero> AbortFishing = new(nameof(AbortFishing));
            public static readonly Event<Hero, CharacterFishingRod> FightingFishAcquired = new(nameof(FightingFishAcquired));
            public static readonly Event<Hero, Hero> OnFishingBobberDestroyed = new(nameof(OnFishingBobberDestroyed));
            public static readonly Event<Hero, IEnumerable<IFishVolume>> OnFishVolumesCulminated = new(nameof(OnFishVolumesCulminated));
        }

        void Awake() {
            _lineRenderer = visualFirePoint.GetComponent<LineRenderer>();
            _lineRenderer.positionCount = linePoints;
            _commonReferences = World.Services.Get<CommonReferences>();
            _fishingFSM = Hero.Current.Element<FishingFSM>();
        }

        protected override void OnInitializedForHero(Hero hero) {
            base.OnInitializedForHero(hero);
            hero.ListenTo(FishingFSM.Events.StartThrow, OnStartThrow, this);
            hero.ListenTo(FishingFSM.Events.Throw, Throw, this);
            hero.ListenTo(FishingFSM.Events.StartFight, TryStartMiniGame, this);
            hero.ListenTo(FishingFSM.Events.Abort, Abort, this);
            hero.ListenTo(FishingFSM.Events.Inspect, InspectFish, this);
            hero.ListenTo(FishingFSM.Events.PullOut, DestroyBobber, this);
            hero.ListenTo(FishingFSM.Events.Fail, FishingFail, this);
            hero.ListenTo(Hero.Events.OnWeaponBeginEquip, () => WaterFishingAction.fishingAvailable = true, this);
            hero.ListenTo(Hero.Events.OnWeaponBeginUnEquip, () => WaterFishingAction.fishingAvailable = false, this);
            _fishingBobberPrefab = fishingBobber.LoadAsset<GameObject>();
            Target.GetOrCreateTimeDependent().WithLateUpdate(OnLateUpdate);
        }

        void OnLateUpdate(float deltaTime) {
            if (_fishingBobberInstance) {
                _lineRenderer.enabled = true;

                var start = visualFirePoint.position;
                var end = _fishingBobberInstance.transform.position;

                var points = new NativeArray<Vector3>(linePoints, ARAlloc.Temp, NativeArrayOptions.UninitializedMemory);
                points[0] = start;
                points[^1] = end;
                for (int i = 1; i < linePoints - 1; i++) {
                    var t = i / (float)(linePoints - 1);
                    var p = Vector3.Lerp(start, end, t);
                    p.y += looseLineHeight * t * (t - 1);
                    points[i] = p;
                }

                _lineRenderer.SetPositions(points);
                points.Dispose();

                if (start.SquaredDistanceTo(end) > maxLineLength * maxLineLength) {
                    FMODManager.PlayOneShot(Audio.lineBreak);
                    Hero.Current.Trigger(Events.AbortFishing, Hero.Current);
                }
            } else if (_lineRenderer != null) {
                _lineRenderer.enabled = false;
            }

            if (!_isCurrentlyFishing && _timeToStopFishing > 0) {
                _timeToStopFishing -= Time.unscaledDeltaTime;
                if (_timeToStopFishing <= 0) {
                    StopFishing();
                }
            }
        }
        
        protected override void OnWeaponHidden() {
            base.OnWeaponHidden();
            WaterFishingAction.fishingAvailable = false;
            if (_fishingBobberInstance) {
                DestroyBobber();
            }
        }

        protected override IBackgroundTask OnDiscard() {
            if (_fishingBobberInstance) {
                DestroyBobber();
            }

            _fishingBobberPrefab.Release();
            return base.OnDiscard();
        }

        public void OnStartThrow(Hero hero) {
            FMODManager.PlayOneShot(Audio.rodCastingStart);
            WaterFishingAction.fishingAvailable = false;
            TryStartFishing();
        }

        public void Throw(Hero hero) {
            if (_fishingBobberInstance) {
                DestroyBobber();
            }

            _fishingBobberInstance = Instantiate(_fishingBobberPrefab.Result, visualFirePoint.position, Quaternion.identity).GetComponent<FishingBobber>();

            hero.VHeroController.Raycaster.GetViewRay(out var position, out var forward);
            var focusPoint = Physics.Raycast(position, forward, out var hit, maxThrowDistance, ThrowMask)
                ? hit.point
                : position + forward * maxThrowDistance;
            _fishingBobberInstance.Throw(focusPoint, throwVelocity, false);
            FMODManager.PlayOneShot(Audio.rodCastingThrow);
            TryStartFishing();
        }

        public void InspectFish(Hero hero) {
            var caughtFishCollection = hero.Element<HeroCaughtFish>();
            Hero.Current.HeroItems.Add(_fish.CreateItem());
            bool firstTimeCatch = false;
            bool isRecord = false;
            string prevRecord = "";

            if (_fish.isFish) {
                firstTimeCatch = !caughtFishCollection.WasPreviouslyCaught(_fish.FishTemplate.GUID, out FishEntry prevFish);

                if (!firstTimeCatch) {
                    isRecord = _fish.length >= prevFish.length;
                    prevRecord = prevFish.length.ToString("0.0");
                    
                    if (isRecord) {
                        caughtFishCollection.UpdateRecord(new FishEntry(_fish.FishTemplate, _fish.length, _fish.weight));
                    }
                } else {
                    isRecord = true;
                }
            }

            var data = new FishCaughtData(_fish.ItemTemplate, _fish.FishTemplate, _fish.isFish, firstTimeCatch, isRecord, _fish.weight, _fish.length, _fish.itemsCount, prevRecord);
            AdvancedNotificationBuffer.Push<MiddleScreenNotificationBuffer>(new FishCaughtNotification(data));
            
            _fishPopupListener = World.Only<FishCaughtNotification>().ListenTo(Model.Events.AfterDiscarded, () => AcceptFishNotification(firstTimeCatch, caughtFishCollection, hero), this);
            _isCurrentlyFishing = false;
        }

        public void Abort(Hero hero) {
            if (_fishingBobberInstance) {
                _fishingBobberInstance.Catch(Hero.Current.Head.position, throwVelocity);
                DestroyBobber();
            }

            if (_fishPopupListener == null) {
                WaterFishingAction.fishingAvailable = true;
            }

            _isCurrentlyFishing = false;
        }

        public void DestroyBobber() {
            _fishingMiniGame?.StopMiniGame();

            Destroy(_fishingBobberInstance.gameObject);
            _fishingBobberInstance = null;
            PauseAudioClip();
            Hero.Current.Trigger(Events.OnFishingBobberDestroyed, Hero.Current);
        }
        
        public void DebugSetLowFishHp() {
            _fish.health = 0.001f;
        }
        
        void AcceptFishNotification(bool firstTimeCatch, HeroCaughtFish caughtFishCollection, Hero hero) {
            WaterFishingAction.fishingAvailable = true;
            _fishPopupListener = null;
            _fishingFSM.SetCurrentState(HeroStateType.FishingTakeFish);

            if (_currentFishVolume is FishVolumeWithStoryBookmark volumeWithStory) {
                volumeWithStory.OnFishCaught();
            }

            if (firstTimeCatch) {
                caughtFishCollection.AddToCaughtFishCollection(new FishEntry(_fish.FishTemplate, _fish.length, _fish.weight));
                World.Only<PlayerJournal>().SendNotification(_fish.FishTemplate.itemName, JournalSubTabType.Fish);
            }
        }

        void FishingFail() {
            _fishingFSM.CameraShakesMultiplier = 1f;
            RewiredHelper.StopVibration();
            _fishingFSM.SetCurrentState(HeroStateType.FishingFail);
            Abort(Hero.Current);
        }

        void TryStartMiniGame(Hero hero) {
            if (_fishingBobberInstance) {
                if (_fishingBobberInstance.TryGetFish(out var fish)) {
                    _fish = fish;
                    _fishingFSM.SetCurrentState(HeroStateType.FishingFight);
                    _currentFishVolume = _fishingBobberInstance.CurrentFishVolume;
                    _fishingMiniGame ??= FishingMiniGame.Show();
                    hero.Trigger(Events.FightingFishAcquired, this);
                    _fishingMiniGame.StartMiniGame(StartingPosition, StartingPosition, this, _fish, _fishingBobberInstance.InWaterPosition);
                    return;
                }

                var notification = new LowerFancyPanelNotification(LocTerms.FishingFail.Translate(), typeof(VLowerFancyPanelNotification));
                AdvancedNotificationBuffer.Push<LowerMiddleScreenNotificationBuffer>(notification);
                
                WaterFishingAction.fishingAvailable = true;
                FMODManager.PlayOneShot(Audio.rodCatch);
                _fishingBobberInstance.Catch(Hero.Current.Head.position, throwVelocity);
                DestroyBobber();
            }
            
            _isCurrentlyFishing = false;
        }
        
        void TryStartFishing() {
            if (!_isCurrentlyFishing) {
                _isCurrentlyFishing = true;
                _timeToStopFishing = StopFishingDelay;
                StartFishing();
            }
        }

        void StartFishing() {
            var audioCore = World.Services.Get<AudioCore>();
            audioCore.RegisterAudioSources(Audio.music, AudioType.Music);
            audioCore.RegisterAudioSources(Audio.ambient, AudioType.Ambient);
            audioCore.RegisterAudioSources(Audio.snapshots, AudioType.Snapshot);
        }

        void StopFishing() {
            var audioCore = World.Services.Get<AudioCore>();
            audioCore.UnregisterAudioSources(Audio.music, AudioType.Music);
            audioCore.UnregisterAudioSources(Audio.ambient, AudioType.Ambient);
            audioCore.UnregisterAudioSources(Audio.snapshots, AudioType.Snapshot);
        }
    }
}