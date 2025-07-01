using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.Graphics.Transitions;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.General.Caches;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.Main.Locations.Attachments.Attachment;
using Awaken.TG.Main.Memories;
using Awaken.TG.Main.Memories.FilePrefs;
using Awaken.TG.Main.UI.RoguePreloader;
using Awaken.TG.Main.UI.TitleScreen.Loading;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Utils;
using Awaken.Utility;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Elements {
    /// <summary>
    /// Marks location as potential target of fast travel
    /// </summary>
    public partial class Portal : Element<Location>, ILocationActionProvider, IVisitationProvider, IRefreshedByAttachment<PortalAttachment>, ILocationNameModifier {
        static readonly SceneReference Hos = SceneReference.ByName(SpecialSceneNames.HornsOfTheSouth);
        static readonly SceneReference Cuanacht = SceneReference.ByName(SpecialSceneNames.Cuanacht);
        static readonly SceneReference Forlorn = SceneReference.ByName(SpecialSceneNames.Forlorn);
        
        public override ushort TypeForSerialization => SavedModels.Portal;

        // === Properties
        string _tag;
        SceneReference _targetScene;
        PortalType _portalType;
        InteractionType _interaction;
        Vector3 _offset;
        bool _isSpawn;
        float _lastExecutedFrame;
        string _customInteractLabel;
        bool _isHiddenFromUI;
        NpcInteractability _npcInteractability;
        bool _isLocationNameHidden;
        float? _targetPitchOnExit = null;
        float _delayedNpcExitTime;
        bool _debugFastPortal;
        bool _doNotAutoSaveAfterPortaling;

        public bool IsVisited => _targetScene?.Name != null && VisitedInGameplayMemory;
        bool VisitedInGameplayMemory => Services.Get<GameplayMemory>().Context(SceneService.VisitedSceneContextName).Get<bool>(_targetScene.Name);
        public PortalType Type => _portalType;
        public bool IsTo => _portalType is PortalType.To or PortalType.TwoWay;
        public bool IsFrom => _portalType is PortalType.From or PortalType.TwoWay;
        public bool IsHiddenFromUI => _isHiddenFromUI;
        [UnityEngine.Scripting.Preserve] public NpcInteractability NpcInteractability => _npcInteractability;
        public string LocationName => GetLocationName();

        public bool DebugFastPortal => _debugFastPortal;
        public bool DoNotAutoSaveAfterPortaling => _doNotAutoSaveAfterPortaling;
        public string LocationDebugName => ParentModel.ViewParent.name;
        public int ModificationOrder => 10;

        // === Static Accessors
        public static Portal FindDefaultEntry() {
            Func<Portal, bool> predicate = p => p._isSpawn && p.CurrentDomain == Domain.CurrentScene();
            return World.All<Portal>().Where(PortalActive).FirstOrDefault(predicate) ??
                   World.All<Portal>().FirstOrDefault(predicate) ??
                   World.Any<Portal>();
        }

        public static Portal FindWithTagOrDefault(SceneReference prevScene, string tag, Portal except = null) {
            var predicate = TagPredicate(prevScene, tag, except, Domain.CurrentScene());
            return World.All<Portal>().Where(PortalActive).FirstOrDefault(predicate) ??
                   World.All<Portal>().FirstOrDefault(predicate) ??
                   FindDefaultEntry();
        }

        public static Portal FindForInstantReturnToCampaign(SceneReference prevScene, SceneReference targetScene, string tag, Portal except = null) {
            var predicate = TagPredicate(prevScene, tag, except, targetScene.Domain);
            return World.All<Portal>().Where(PortalActive).FirstOrDefault(predicate) ??
                   World.All<Portal>().FirstOrDefault(predicate);
        }

        static bool PortalActive(Portal portal) {
            return portal.ParentModel.Interactability == LocationInteractability.Active;
        }

        static Func<Portal, bool> TagPredicate(SceneReference prevScene, string tag, Portal except, Domain targetDomain) {
            return p => {
                if (p._targetScene.IsSet && p._targetScene != prevScene) return false;
                if (p._tag != tag) return false;
                if (p.CurrentDomain != targetDomain) return false;
                if (except != null && except == p) return false;

                return true;
            };
        }

        [UsedImplicitly, UnityEngine.Scripting.Preserve, Obsolete]
        public static Portal FindAny(SceneReference scene) {
            return FindAnyFromScene(scene);
        }

        public static Portal FindAnyFromScene(SceneReference sourceScene, PortalType type = PortalType.None) {
            return World.All<Portal>().FirstOrDefault(p => {
                bool isCorrectScene = p._targetScene == sourceScene;
                bool requestedType = (type == PortalType.None || p._portalType == type);
                bool sceneCheck = p.CurrentDomain == Domain.CurrentScene();
                return isCorrectScene && requestedType && sceneCheck;
            });
        }

        [UnityEngine.Scripting.Preserve]
        public static Portal FindAny(PortalType type = PortalType.None) {
            return World.All<Portal>().FirstOrDefault(p => {
                bool requestedType = type == PortalType.None || p._portalType == type;
                bool sceneCheck = p.CurrentDomain == Domain.CurrentScene();
                return requestedType && sceneCheck;
            });
        }

        public static Portal FindAnyEntry() {
            return World.All<Portal>().FirstOrDefault(p => {
                bool requestedType = p.IsTo;
                bool sceneCheck = p.CurrentDomain == Domain.CurrentScene();
                return requestedType && sceneCheck;
            });
        }
        
        /// <summary>
        /// Finds the closest portal to the given scene. If no portal is found, it will fallback to finding the closest exit.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="targetScene"></param>
        /// <returns></returns>
        public static Portal FindClosestWithFallback(IGrounded from, SceneReference targetScene) {
            return FindClosest(targetScene, from, null, false, true) ?? FindClosestExit(from, true);
        }
        
        public static Portal FindClosest(SceneReference scene, IGrounded from, bool? isSpawn = null, bool searchForUI = false, bool searchForNPC = false) {
            return FindClosest(scene, from.Coords, isSpawn, searchForUI, searchForNPC);
        }

        public static Portal FindClosest(SceneReference scene, Vector3 position, bool? isSpawn = null, bool searchForUI = false, bool searchForNPC = false) {
            var checkIsSpawn = isSpawn is null;
            var portals = World.All<Portal>();
            var currentDomain = Domain.CurrentScene();

            SceneService sceneService = World.Services.Get<SceneService>();
            ScenesCache scenesCache = ScenesCache.Get;
            SceneReference targetRegion = scenesCache.GetOpenWorldRegion(scene);
            
            // Final fallback for cases where the player starts tracking the quest in Forlorn but is currently in the HOS area, or vice versa.
            if (sceneService.IsOpenWorld) {
                var currentRegion = scenesCache.GetOpenWorldRegion(sceneService.MainSceneRef);
                if ((currentRegion == Hos && targetRegion == Forlorn)
                    || (currentRegion == Forlorn && targetRegion == Hos)) {
                    targetRegion = Cuanacht;
                }
            }

            Portal closestPortalToRegion = null;
            Portal closestPortalToOpenWorld = null;
            Portal closestPortal = null;
            float closestDistanceToOpenWorld = float.MaxValue;
            float closestDistanceToRegion = float.MaxValue;
            float closestDistance = float.MaxValue;
            foreach (var portal in portals) {
                if (portal.CurrentDomain != currentDomain) continue;
                if (!searchForNPC && !IsHiddenFromUI(portal)) {
                    if (portal._targetScene == targetRegion && CheckSpawnCondition(portal)) {
                        var distanceToRegionPortal = (portal.ParentModel.Coords - position).sqrMagnitude;
                        if (distanceToRegionPortal < closestDistanceToRegion) {
                            closestPortalToRegion = portal;
                            closestDistanceToRegion = distanceToRegionPortal;
                        }
                    } else if (scenesCache.IsOpenWorld(portal._targetScene)) {
                        var distanceToOpenWorldPortal = (portal.ParentModel.Coords - position).sqrMagnitude;
                        if (distanceToOpenWorldPortal < closestDistanceToOpenWorld) {
                            closestPortalToOpenWorld = portal;
                            closestDistanceToOpenWorld = distanceToOpenWorldPortal;
                        }
                    }
                }
                
                if (portal._targetScene != scene) continue;
                if (searchForNPC) {
                    if (!CanNpcFindPortal(portal._npcInteractability, portal._isHiddenFromUI, portal._portalType, portal._interaction)) {
                        continue;
                    }
                } else {
                    if (!CheckSpawnCondition(portal)) continue;
                    if (IsHiddenFromUI(portal)) continue;
                }

                var distance = (portal.ParentModel.Coords - position).sqrMagnitude;
                if (distance < closestDistance) {
                    closestPortal = portal;
                    closestDistance = distance;
                }
            }

            if (!closestPortal) {
                closestPortal = closestPortalToRegion ?? closestPortalToOpenWorld;
            }

            return closestPortal;

            bool CheckSpawnCondition(Portal portal) => checkIsSpawn || portal._isSpawn == isSpawn;
            bool IsHiddenFromUI(Portal portal) => searchForUI && portal.IsHiddenFromUI;

        }

        public static bool CanNpcFindPortal(NpcInteractability npcInteractability, bool isHiddenFromUI, PortalType type, InteractionType interaction) {
            switch (npcInteractability) {
                case NpcInteractability.Default:
                    if (isHiddenFromUI) return false;
                    if (type is not PortalType.TwoWay) return false;
                    if (interaction is InteractionType.TriggerFromStory) return false;
                    return true;
                case NpcInteractability.Allowed:
                    return true;
                case NpcInteractability.Forbidden:
                    return false;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static Portal FindClosestExit(IGrounded from, bool searchForUI = false) {
            return FindClosestExit(from.Coords, searchForUI);
        }

        public static Portal FindClosestExit(Vector3 position, bool searchForUI = false) {
            var portals = World.All<Portal>();
            var currentDomain = Domain.CurrentScene();
            Portal closestPortal = null;
            float closestDistance = float.MaxValue;
            foreach (var portal in portals) {
                if (!portal.IsFrom || portal.CurrentDomain != currentDomain || (searchForUI && portal.IsHiddenFromUI)) {
                    continue;
                }

                var distance = (portal.ParentModel.Coords - position).sqrMagnitude;
                if (distance < closestDistance) {
                    closestPortal = portal;
                    closestDistance = distance;
                }
            }

            return closestPortal;
        }

        // === Constructors
        public void InitFromAttachment(PortalAttachment spec, bool isRestored) {
            _tag = spec.indexTag;
            _portalType = spec.type;
            _targetScene = spec.targetScene;
            _offset = spec.offset;
            _interaction = spec.interaction;
            _isSpawn = spec.isSpawn;
            _customInteractLabel = spec.customInteractLabel;
            _targetPitchOnExit = spec.ShouldSetPitchOnExit
                                     ? spec.useTransform
                                           ? PitchFromRotation(spec.transform.rotation)
                                           : spec.PitchOnExit
                                     : null;
            _isHiddenFromUI = spec.isHiddenFromUI;
            _npcInteractability = spec.npcInteractability;
            _debugFastPortal = spec.debugFastPortal;
            _isLocationNameHidden = spec.isLocationNameHidden;
            _doNotAutoSaveAfterPortaling = spec.doNotAutoSaveAfterPortaling;
        }

        // === Initialization
        protected override void OnFullyInitialized() {
            if (!IsFrom) {
                return;
            }
            
            if (_interaction == InteractionType.OnTrigger) {
                VPortal view = ParentModel.Spec.gameObject.AddComponent<VPortal>();
                World.BindView(this, view, true, true);
            } else if (_interaction == InteractionType.OnInteract) {
                AddElement(new TravelAction(_customInteractLabel));
            }
        }

        // === Operations
        public TeleportDestination GetDestination() {
            Vector3 parentDestination = ParentModel.Coords + Vector3.up * 0.2f + _offset;

            return new TeleportDestination {
                position = parentDestination,
                SetForward = ParentModel.Forward(),
            };
        }

        public void Execute(Hero hero) {
            var portalMessage = Elements<IPortalOverride>().FirstOrDefault(o => o.Override);
            if (portalMessage != null) {
                portalMessage.Execute(hero);
            } else {
                ExecuteInternal(hero);
            }
        }

        public void ExecuteInternal(Hero hero) {
            // Check setup
            if (_targetScene == null || !_targetScene.IsSet) {
                throw new ArgumentException($"Invalid Portal setup. Scene: {_targetScene?.Name ?? "NULL"}, Tag: {_tag}");
            }

            // Ignore incoming portal calls for 5 frames after triggering one to avoid multiple executions
            if (_lastExecutedFrame + 5 > Time.frameCount) {
                return;
            }

            _lastExecutedFrame = Time.frameCount;

            // If target is in another scene we need to change map, otherwise fast travel
            SceneReference currentScene = Services.Get<SceneService>().ActiveSceneRef;
            WalkedThroughPortal(hero);
            if (_targetScene != currentScene) {
                MapChangeTo(hero, this, currentScene);
            } else {
                Portal ft = FindWithTagOrDefault(_targetScene, _tag, except: this);
                FastTravel.To(hero, ft, withTransition: !_debugFastPortal).Forget();
            }
        }

        // === Actions Provider
        public IEnumerable<IHeroAction> GetAdditionalActions(Hero hero) {
            return Elements<IHeroActionModel>().GetManagedEnumerator();
        }

        public string ModifyName(string original) => LocationName;

        public float GetExitDelayForNPC() {
            const float ExitDelay = 2f;
            if (_delayedNpcExitTime <= Time.time) {
                _delayedNpcExitTime = Time.time + ExitDelay;
                return 0;
            }
            float exitDelay = _delayedNpcExitTime - Time.time;
            _delayedNpcExitTime += ExitDelay;
            return exitDelay;
        }

        // === Static Travel Helper (every Portal travel goes through one of these 2 methods)
        static void MapChangeTo(Hero hero, Portal portal, SceneReference currentScene) {
            FastTravelOrPortal(hero, portal._targetScene, currentScene, portal._tag);
            ScenePreloader.ChangeMap(portal._targetScene);
        }

        public static void MapChangeTo(Hero hero, SceneReference teleportTo, SceneReference teleportFrom, string tag) {
            hero.WalkThroughPortal();
            FastTravelOrPortal(hero, teleportTo, teleportFrom, tag);

            ScenePreloader.ChangeMap(teleportTo);
        }

        static void FastTravelOrPortal(Hero hero, SceneReference teleportTo, SceneReference teleportFrom, string tag) {
            // When returning from an additive scene we know that the target scene is already loaded.
            // this allows us to teleport instantly before map change so that assets can be loaded quicker
            if (teleportFrom.IsAdditive && !teleportTo.IsAdditive && FindForInstantReturnToCampaign(teleportFrom, teleportTo, tag) is { } targetPortal) {
                ModelUtils.DoForFirstModelOfType<LoadingScreenUI>(
                    lsUI => lsUI.ListenTo(LoadingScreenUI.Events.AfterDroppedPreviousDomain,
                        () => FastTravel.To(hero, targetPortal, withTransition: false).Forget(),
                        hero),
                    hero);
            } else {
                hero.SetPortalTarget(teleportFrom, tag, p => p?.ArrivedAtPortal(hero));
            }
        }

        public static class FastTravel {
            const float StepInSeconds = TransitionService.QuickFadeIn;
            const float WaitInStepSeconds = 0.05f;
            const float StepOutSeconds = TransitionService.QuickFadeOut;

            public static async UniTask To(Hero hero, Portal portal, bool withTransition = true) {
                if (withTransition) {
                    await FadeIn();
                }

                // teleport
                hero.TeleportTo(
                    portal.GetDestination(),
                    () => {
                        portal.ArrivedAtPortal(hero);
                        hero.Trigger(Hero.Events.FastTraveled, hero);
                    });
                
                if (withTransition) {
                    await FadeOut();
                }
            }

            public static async UniTaskVoid To(Hero hero, Vector3 position, bool withTransition = true) {
                if (withTransition) {
                    await FadeIn();
                }

                hero.TeleportTo(position, afterTeleported: () => hero.Trigger(Hero.Events.FastTraveled, hero));

                if (withTransition) {
                    await FadeOut();
                }
            }
            static async UniTask FadeIn() {
                Hero.Current.Element<HeroFallDamage>().SetFallDamageEnabled(false);
                
                var transition = World.Services.Get<TransitionService>();
                transition.KillSequences();
                
                await transition.ToBlack(StepInSeconds, ignoreTimescale: true);
                
                if (Hero.Current is not { HasBeenDiscarded: false }) {
                    return;
                }

                Hero.Current.AllowNpcTeleport = true;
            }
            
            static async UniTask FadeOut() {
                if (!await AsyncUtil.DelayTime(Hero.Current, WaitInStepSeconds)) {
                    return;
                }

                await LoadingScreenUI.SmoothFPSFast(Hero.Current);
                
                Hero.Current.AllowNpcTeleport = false;
                Hero.Current.Element<HeroFallDamage>().SetFallDamageEnabled(true);
                
                await World.Services.Get<TransitionService>().ToCamera(StepOutSeconds);
            }
        }

        string GetLocationName() {
            if (DebugProjectNames.Basic) {
                return "Debug: " + (_targetScene?.Name ?? "No Target Scene");
            }

            if (_isLocationNameHidden) {
                return string.Empty;
            }
            return LocTerms.GetSceneName(_targetScene);
        }

        // === Callbacks
        void WalkedThroughPortal(Hero hero) {
            hero.WalkThroughPortal();
            ParentModel.TriggerVisualScriptingEvent("WalkedThroughPortal");
        }

        void ArrivedAtPortal(Hero hero) {
            hero.ArrivedAtPortal(this);
            if (_targetPitchOnExit != null) {
                hero.VHeroController.HeroCamera.SetPitch(_targetPitchOnExit.Value);
            }

            ParentModel.TriggerVisualScriptingEvent("ArrivedAtPortal");
        }

        static float PitchFromRotation(Quaternion q) => math.degrees(math.atan2(2 * q.x * q.w - 2 * q.y * q.z, 1 - 2 * q.x * q.x - 2 * q.z * q.z));
    }

    public struct TeleportDestination {
        public Vector3 position;
        public Quaternion? Rotation { get; set; }

        public Vector3 SetForward {
            set {
                value.y = 0;
                Rotation = Quaternion.LookRotation(value);
            }
        }

        public static TeleportDestination Zero { get; } = new() {
            position = Vector3.zero,
            Rotation = Quaternion.identity
        };
    }
}