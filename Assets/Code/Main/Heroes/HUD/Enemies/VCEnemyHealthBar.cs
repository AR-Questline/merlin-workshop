using System;
using System.Threading;
using Awaken.TG.Main.AI.SummonsAndAllies;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Memories.FilePrefs;
using Awaken.TG.Main.UI.Menu.DeathUI;
using Awaken.TG.Main.UI.TitleScreen.Loading;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.Utils;
using Awaken.Utility.GameObjects;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.HUD.Enemies {
    public class VCEnemyHealthBar : ViewComponent<Hero> {
        const float FadeDuration = 0.15f;
        const float DisappearDelay = 3f;
        
        [SerializeField] TextMeshProUGUI enemyName;
        [SerializeField] CanvasGroup canvasGroup;
        [SerializeField] EnemyStatusHUD statusHUD;

        [SerializeField] VCEnemyBars normalEnemyBars;
        [SerializeField] VCEnemyBars tooStrongEnemyBars;
        [SerializeField] VCEnemyBars eliteEnemyBars;
        [SerializeField] VCEnemyBars bossEnemyBars;
        
        WeakModelRef<Location> _currentLocation;
        
        bool _isPointing;
        bool _isVisible;
        Sequence _alphaSequence;
        Sequence _staggerBarPulsating;
        CancellationTokenSource _blinkTokenSource;
        VCEnemyBars _currentBars;
        
        bool InCombat => Target.HeroCombat.IsHeroInFight;
        bool IsBeingShown {
            get => _isVisible;
            set {
                _isVisible = value;
                _currentLocation.TryGet(out var l);
                Target.Trigger(Events.EnemyHealthBarShown, new EnemyHealthBarData {location = l, isShown = _isVisible});
            }
        }

        public struct EnemyHealthBarData {
            public Location location;
            public bool isShown;
        }

        public static class Events {
            public static readonly Event<Hero, EnemyHealthBarData> EnemyHealthBarShown = new(nameof(EnemyHealthBarShown));
        }

        // === Initialization
        protected override void OnAttach() {
            canvasGroup.alpha = 0;
            canvasGroup.TrySetActiveOptimized(false);
            IsBeingShown = false;
            
            Target.ListenTo(VCHeroRaycaster.Events.PointsTowardsIWithHealthBar, OnPointingTowardsLocationWithHP, this);
            Target.ListenTo(VCHeroRaycaster.Events.StoppedPointingTowardsLocation, OnStoppedPointingTowardsLocation, this);
            Target.ListenTo(ICharacter.Events.CombatExited, OnCombatExited, this);
            Target.ListenTo(HealthElement.Events.BeforeDamageDealt, BeforeDamageDealt, this);
            Target.ListenTo(Events.EnemyHealthBarShown, statusHUD.Refresh, this);
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelFullyInitialized<DeathUI>(), this, TryHideHealthBar);
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelFullyInitialized<LoadingScreenUI>(), this, HideHealthBarInstant);
        }

        // === Update
        void Update() {
            if (HasBeenDiscarded) {
                return;
            }
            
            if (_currentLocation.TryGet(out var location) && _currentBars) {
                _currentBars.UpdateHP(location);
            } else {
                StopPointing(true);
            }
        }

        // === Conditions & Listener Callbacks
        void OnPointingTowardsLocationWithHP(Location location) {
            if (CanShowHealthOf(location)) {
                StartPointing(location);
            } else {
                StopPointing();
            }
        }
        
        void OnStoppedPointingTowardsLocation(Location location) {
            StopPointing();
        }

        void OnCombatExited(ICharacter _) {
            if (!_isPointing) {
                TryHideHealthBar();
            } 
        }

        void BeforeDamageDealt(Damage damage) {
            if (!_isPointing && damage.Target is Element<Location> element) {
                BlinkWith(element.ParentModel).Forget();
            }
        }

        bool CanShowHealthOf(Location location) {
            var npc = location.TryGetElement<NpcElement>();
            if (npc == null) {
                return !HasHealthBarBlocker(location, location.TryGetElement<IWithHealthBar>());
            }
            if (HasHealthBarBlocker(location, npc)) {
                return false;
            }
            return npc.IsHostileTo(Target) || npc.HasElement<NpcHeroSummon>() ||
                   npc.HasElement<UnconsciousElement>() || npc.HasElement<NpcAlly>() || npc.Health.Percentage < 1f;
        }

        bool HasHealthBarBlocker(Location location, [CanBeNull] IWithHealthBar iWithHealthBar) {
            return location.HasElement<IHealthBarHiddenMarker>() || (iWithHealthBar?.HasElement<IHealthBarHiddenMarker>() ?? false);
        }
        
        // === Pointing
        void StartPointing(Location location) {
            IWithHealthBar withHealthBar = location.TryGetElement<IWithHealthBar>();
            if (withHealthBar == null) {
                return;
            }
            
            _isPointing = true;
            _blinkTokenSource?.Cancel();
            
            float percent = withHealthBar.HealthStat.Percentage;
            NpcElement npcElement = location.TryGetElement<NpcElement>();
            _currentLocation.TryGet(out var currentLocation);
            enemyName.text = DebugProjectNames.Basic ? location.DebugName : location.DisplayName;
            if (_currentBars != null) _currentBars.TrySetActiveOptimized(false);

            if (npcElement == null) {
                SetupNonNpcHpBarVisuals();
            } else {
                SetupNpcHpBarVisuals(npcElement);
            }
            
            if (currentLocation != location) {
                if (npcElement != null && _currentBars != null) {
                    _currentBars.SetBars(percent, npcElement.CharacterStats.Stamina.Percentage, true);
                    Target.Trigger(Events.EnemyHealthBarShown, new EnemyHealthBarData {location = location, isShown = true}); 
                }
            } else {
                if (npcElement != null && _currentBars != null) {
                    _currentBars.SetBars(percent, npcElement.CharacterStats.Stamina.Percentage);
                }
            }
            
            _currentLocation = new WeakModelRef<Location>(location);
            TryShowHealthBar();
        }

        void SetupNonNpcHpBarVisuals() {
            _currentBars = normalEnemyBars;
            _currentBars.TrySetActiveOptimized(true);
        }
        
        void SetupNpcHpBarVisuals(NpcElement npcElement) {
            int currentLevelDiff = npcElement.CharacterStats.Level.ModifiedInt - Hero.Current.CharacterStats.Level.ModifiedInt;
            bool isEnemyTooStrong = currentLevelDiff >= GameConstants.Get.tooStrongEnemyLevelDiff;
            
            _currentBars = npcElement.NpcType switch {
                NpcType.Critter => null,
                NpcType.Trash or NpcType.Normal => isEnemyTooStrong ? tooStrongEnemyBars : normalEnemyBars,
                NpcType.Elite => eliteEnemyBars,
                NpcType.MiniBoss or NpcType.Boss => bossEnemyBars,
                _ => throw new ArgumentOutOfRangeException()
            };
            
            if (_currentBars != null) _currentBars.TrySetActiveOptimized(true);
        }

        void StopPointing(bool force = false) {
            _isPointing = false;
            
            if (!InCombat || force) {
                TryHideHealthBar();
            }
        }

        async UniTaskVoid BlinkWith(Location location) {
            if (CanShowHealthOf(location)) {
                StartPointing(location);
                _blinkTokenSource = new CancellationTokenSource();
                if (await AsyncUtil.DelayTime(this, FadeDuration, true, _blinkTokenSource)) {
                    StopPointing();
                }
            }
        }

        // === Health
        void TryShowHealthBar() {
            if (IsBeingShown) return;
            ResetAlphaSequence();
            IsBeingShown = true;
            canvasGroup.TrySetActiveOptimized(true);
            _alphaSequence = DOTween.Sequence().SetUpdate(true).Append(canvasGroup.DOFade(1f, FadeDuration));
        }

        void TryHideHealthBar() {
            if (!_isVisible && (_currentLocation.Equals(default) || (_alphaSequence?.active ?? false))) return;
            _isVisible = false;
            ResetAlphaSequence();
            _alphaSequence = DOTween.Sequence()
                .SetUpdate(true)
                .SetDelay(DisappearDelay)
                .Append(canvasGroup.DOFade(0f, FadeDuration))
                .OnKill(HideHealthBarInstantInternal);
        }

        void ResetAlphaSequence() {
            if (_alphaSequence != null) {
                _alphaSequence.onKill = null;
                _alphaSequence.Kill();
                _alphaSequence = null;
            }
        }

        void HideHealthBarInstant() {
            if (!_isVisible) {
                return;
            }
            ResetAlphaSequence();
            HideHealthBarInstantInternal();
        }

        void HideHealthBarInstantInternal() {
            canvasGroup.alpha = 0f;
            canvasGroup.TrySetActiveOptimized(false);
            _currentLocation = default;
            IsBeingShown = false;
            _alphaSequence = null;
        }
    }
}