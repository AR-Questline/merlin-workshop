using Awaken.CommonInterfaces.Animations;
using Awaken.TG.Main.AI.Idle;
using Awaken.TG.Main.AI.Utils;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.HUD.Summons;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Heroes.Statuses.Duration;
using Awaken.TG.Main.Heroes.Thievery;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.UI.TitleScreen.Loading;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.Utils;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace Awaken.TG.Main.AI.SummonsAndAllies {
    [SpawnsView(typeof(VHeroSummonPreview))]
    public partial class NpcHeroSummon : NpcAlly, INpcSummon, IAnimatorBridgeStateProvider {
        public override ushort TypeForSerialization => SavedModels.NpcHeroSummon;

        const float DisableMovementDuration = 1.5f;
        
        [Saved] WeakModelRef<Item> _ownerItem;
        [Saved] float _manaExpended;
        bool _allyInCombat;
        bool _allyInDialogue;
        AnimatorBridge _animatorBridge;
        Collider[] _walkThroughColliders;

        [UnityEngine.Scripting.Preserve] public Location Location => ParentModel.ParentModel;
        public Item Item => _ownerItem.Get();
        public float ManaExpended => _manaExpended;
        public bool IsAlive => !HasBeenDiscarded && ParentModel.IsAlive;
        public bool AlwaysAnimate => IsAlive;
        protected override bool AlwaysUpdate => true;
        protected override bool AdditionalMovePrevent => _allyInDialogue;
        Location NpcLocation => ParentModel.ParentModel;

        public float DurationLeftNormalized {
            get {
                TimeDuration duration = DiscardAfterDuration?.Duration as TimeDuration;
                return duration?.TimeLeftNormalized ?? 1f;
            }
        }

        CharacterLimitedLocationTimeoutAfterDuration DiscardAfterDuration => TryGetElement<CharacterLimitedLocationTimeoutAfterDuration>();

        // === Constructing
        [JsonConstructor, UnityEngine.Scripting.Preserve] public NpcHeroSummon() { }
        public NpcHeroSummon(Hero owner, Item ownerItem, float manaExpended) : base(owner) {
            _ownerItem = ownerItem;
            _manaExpended = manaExpended;
        }
        
        // === Initialization
        protected override void OnInitialize() {
            ParentModel.IsHeroSummon = true;
            ParentModel.OnCompletelyInitialized(npc => {
                var npcController = npc.Controller;
                npcController.RichAI.canBePaused = false;
                _walkThroughColliders = npcController.AlivePrefab.GetComponentsInChildren<Collider>();
                ToggleWalkThroughColliders();
                _animatorBridge = AnimatorBridge.GetOrAddDefault(npcController.Animator);
                _animatorBridge.RegisterStateProvider(this);
            });
            base.OnInitialize();
            Owner.Trigger(INpcSummon.Events.SummonSpawned, this);
            GameplayUniqueLocation.InitializeForLocation(NpcLocation);
        }
        
        protected override void OnRestore() {
            base.OnRestore();
        }

        protected override void Init() {
            base.Init();
            if (Ally == null) {
                return;
            }
            StatTweak.Add(Hero.Current.CharacterStats.MaxMana, -_manaExpended, parentModel: this).MarkedNotSaved = true;
            StatTweak.AddPreMultiply(Hero.Current.HeroStats.MaxManaReservation, _manaExpended, parentModel: this).MarkedNotSaved = true;
            NpcLocation.AddElement<HideHealthBar>();
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelInitialized<IHeroInvolvement>(), this, OnHeroInvolvementAdded);
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelDiscarded<IHeroInvolvement>(), this, OnHeroInvolvementDiscarded);
            if (!Ally.IsInCombat()) {
                ParentModel.AddElement<HeroSummonInvisibility>();
            }
            PreventMovement().Forget();
        }

        async UniTaskVoid PreventMovement() {
            _movementPrevented = true;
            if (await AsyncUtil.DelayTime(this, DisableMovementDuration)) {
                _movementPrevented = false;
            }
        }

        protected override void AfterVisualLoaded(Transform parentTransform) {
            base.AfterVisualLoaded(parentTransform);
            if (Ally == null) {
                return;
            }
            Hero.Current.ListenTo(Hero.Events.ArrivedAtPortal, OnHeroArrivedAtPortal, this);
            Hero.Current.ListenTo(Hero.Events.FastTraveled, OnHeroFastTraveled, this);
            Hero.Current.ListenTo(Hero.Events.HeroLongTeleported, OnHeroAfterLongTeleport, this);
            Ally.ListenTo(ICharacter.Events.CombatEntered, OnAllyEnteredCombat, this);
            Ally.ListenTo(ICharacter.Events.CombatExited, OnAllyExitedCombat, this);
            ParentModel.HealthElement.ListenTo(HealthElement.Events.TakingDamage, TryPreventFriendlyFire, this);
            ParentModel.ParentModel.RemoveElementsOfType<SearchAction>();
            ParentModel.ParentModel.RemoveElementsOfType<PickpocketAction>();
        }
        
        // === Listener Callbacks
        void OnHeroArrivedAtPortal(Portal portal) {
            LoadingScreenUI loadingScreenUI = World.Any<LoadingScreenUI>();
            if (loadingScreenUI) {
                HeroArrivedAtPortal(loadingScreenUI).Forget();
                return;
            }
            TeleportToNewScene();
        }

        async UniTaskVoid HeroArrivedAtPortal(LoadingScreenUI loadingScreenUI) {
            if (await AsyncUtil.WaitWhile(this, () => !loadingScreenUI.HasBeenDiscarded) == false) {
                return;
            }
            TeleportToNewScene();
        }

        void TeleportToNewScene() {
            TeleportToAlly(DistanceToAllySqr, TeleportContext.AllyTooFar, out Vector3 teleportPosition);
            NpcLocation.Element<GameplayUniqueLocation>().TeleportIntoCurrentScene(teleportPosition);
        }
        
        void OnHeroFastTraveled(Hero hero) {
            TeleportToAlly(DistanceToAllySqr, TeleportContext.SummonAfterFastTravel, out _);
        }

        void OnHeroAfterLongTeleport(Hero hero) {
            TeleportToAlly(DistanceToAllySqr, TeleportContext.AllyTooFar, out _);
        }
        
        void OnHeroInvolvementAdded(IModel model) {
            _allyInDialogue = true;
        }

        void OnHeroInvolvementDiscarded(IModel model) {
            _allyInDialogue = World.HasAny<IHeroInvolvement>();
        }

        void OnAllyEnteredCombat() {
            _allyInCombat = true;
            EnterCombat();
            ToggleWalkThroughColliders();
        }

        public void EnterCombat() {
            ParentModel.RemoveMarkerElement<HeroSummonInvisibility>();
            FindTarget();
        }

        void OnAllyExitedCombat() {
            _allyInCombat = false;
            ToggleWalkThroughColliders();
            if (HasElement<HeroSummonTargetOverride>()) {
                return;
            }
            ExitCombat();
        }

        void ExitCombat() {
            ParentModel.ForceEndCombat();
            ParentModel.AddMarkerElement<HeroSummonInvisibility>();
        }

        public void TryExitCombat() {
            if (!_allyInCombat) {
                ExitCombat();
            }
        }

        protected override bool CanRecalculateTarget() {
            return World.Any<IHeroInvolvement>() == null;
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            if (_animatorBridge != null) {
                _animatorBridge.UnregisterStateProvider(this);
            }
            _walkThroughColliders = null;
            base.OnDiscard(fromDomainDrop);
        }

        // === Helpers
        void TryPreventFriendlyFire(HookResult<HealthElement, Damage> hook) {
            if (hook.Value.DamageDealer is Hero h && h.Development.DontDealDamageToSummons) {
                hook.Prevent();
            }
        }

        void ToggleWalkThroughColliders() {
            if (_walkThroughColliders == null) {
                return;
            }
            foreach (Collider walkThroughCollider in _walkThroughColliders) {
                if (walkThroughCollider == null) {
                    continue;
                }
                
                walkThroughCollider.enabled = _allyInCombat;
            }
        }
        
        // === ICharacterLimitedLocation
        public ICharacter Owner => Ally;
        public CharacterLimitedLocationType Type => CharacterLimitedLocationType.HeroSummon;
        public int LimitForCharacter(ICharacter character) => Hero.Current.HeroStats.SummonLimit.ModifiedInt;

        public void Destroy() {
            ParentModel.ParentModel.Kill();
        }
    }
}