using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.AI.Barks;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Locations.Actions.Lockpicking;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;

namespace Awaken.TG.Main.Heroes.Thievery {
    public partial class IllegalActionTracker : Element<Hero> {
        public sealed override bool IsNotSaved => true;

        List<NpcCrimeReactions> _watchingNpcsReactions = new(3);

        public bool IsCrouching { get; private set; }
        
        TrespassingTracker TrespassingTracker => ParentModel.Element<TrespassingTracker>();
        bool IsTrespassing => TrespassingTracker.IsTrespassing;
        public bool IsLockpicking => World.Any<LockpickingInteraction>();
        public bool IsActingSuspiciously => IsCrouching || IsTrespassing || IsLockpicking;
        
        public bool IsBeingWatched => _watchingNpcsReactions.Any(npc => npc.ParentModel.Template.CanTriggerAggroMusic);
        public float BeingWatchedLosePercent => _watchingNpcsReactions.Max(r => r.IsSeeingHero
                                                                            ? r.IsLosingHeroSight
                                                                                ? r.SeeingHeroLoseFactor
                                                                                : 1f
                                                                            : 0f);

        public List<NpcCrimeReactions> WatchingNpcs => _watchingNpcsReactions;

        public new static class Events {
            public static readonly Event<Hero, bool> IllegalActivityPerformed = new(nameof(IllegalActivityPerformed));

            public static readonly Event<NpcElement, CrimeArchetype> NPCNoticedCrime = new(nameof(NPCNoticedCrime));
        }

        protected override void OnFullyInitialized() {
            ParentModel.HeroItems.ListenTo(ICharacterInventory.Events.ItemDropped, OnItemDropped, this);
            
            ParentModel.ListenTo(Hero.Events.ShowWeapons, OnWeaponShown, this);
            ParentModel.ListenTo(Hero.Events.HeroJumped, OnHeroJumped, this);
            
            ParentModel.ListenTo(Hero.Events.HeroCrouchToggled, OnHeroCrouchToggled, this);
            ParentModel.ListenTo(ICharacter.Events.OnAttackStart, OnHeroAttack, this);
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelFullyInitialized<LockpickingInteraction>(), this, OnStartedLockpicking);
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelDiscarded<LockpickingInteraction>(), this, OnStoppedLockpicking);
            
            ParentModel.GetOrCreateTimeDependent().WithUpdate(OnUpdate);
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            ParentModel.GetTimeDependent()?.WithoutUpdate(OnUpdate);
        }

        void OnUpdate(float deltaTime) {
            if (IsActingSuspiciously) {
                foreach (var reactions in _watchingNpcsReactions) {
                    reactions.ObserveSuspiciousActivity();
                }
            }
        }

        public void AddWatchingNpc(NpcCrimeReactions reactions) {
            _watchingNpcsReactions.Add(reactions);
            
            var trespassingTracker = TrespassingTracker;
            if (trespassingTracker.IsCrime) {
                reactions.ReactToCrime(CrimeArchetype.Trespassing);
            } else if (trespassingTracker.IsTrespassing) {
                trespassingTracker.Warn(reactions.ParentModel);
            }

            if (World.Any<LockpickingInteraction>()) {
                reactions.StartedLockpicking();
            }

            if (reactions.IsGuard) {
                TemporaryBounty.TryGet()?.GuardApplyCrimes();
            }

            foreach (HeroCrimeWithProlong crimeWithProlong in ParentModel.Elements<HeroCrimeWithProlong>().ToArraySlow()) {
                crimeWithProlong.TryCommitCrime(reactions.ParentModel);
            }
        }
        
        public void RemoveWatchingNpc(NpcCrimeReactions reactions) {
            _watchingNpcsReactions.Remove(reactions);
        }

        public void StartTrespassing(TrespassingTracker tracker) {
            foreach (var reactions in _watchingNpcsReactions) {
                tracker.Warn(reactions.ParentModel);
                reactions.ObserveSuspiciousActivity();
            }
        }

        public void PerformingSuspiciousInteraction() {
            foreach (var reactions in _watchingNpcsReactions) {
                reactions.ObserveSuspiciousActivity();
            }
        }

        public bool SeenByNPC(NpcElement npc) {
            return _watchingNpcsReactions.Any(reactions => reactions.ParentModel == npc);
        }
        
        void OnStartedLockpicking(Model _) {
            foreach (var reactions in _watchingNpcsReactions) {
                reactions.StartedLockpicking();
            }
        }
        
        void OnStoppedLockpicking(Model _) {
            foreach (var reactions in _watchingNpcsReactions) {
                reactions.StoppedLockpicking();
            }
        }
        
        void OnWeaponShown(bool _) {
            foreach (var npc in WatchingNpcs) {
                npc.ParentModel.TryGetElement<BarkElement>()?.OnNoticeWeaponDrawn();
            }
        }
        
        void OnItemDropped(DroppedItemData _) {
            foreach (var npc in WatchingNpcs) {
                npc.ParentModel.TryGetElement<BarkElement>()?.OnNoticeItemDrop();
            }
        }
        
        void OnHeroJumped(bool _) {
            foreach (var npc in WatchingNpcs) {
                npc.ParentModel.TryGetElement<BarkElement>()?.OnNoticeHeroJump();
            }
        }
        
        void OnHeroCrouchToggled(bool crouching) {
            IsCrouching = crouching;
            //TODO: maybe this needs to be somewhere else?
            const float Range = 2f;
            (float rangeMultiplier, float soundStrength, float theftStrength) = ParentModel.Element<ArmorWeight>().ArmorNoiseStrength();
            float noiseRange = Range * rangeMultiplier;
            AINoises.MakeNoise(noiseRange, soundStrength, false, ParentModel.Coords, ParentModel);
            ThieveryNoise.MakeNoise(noiseRange, theftStrength, false, ParentModel.Coords, ParentModel);
            
            if (!crouching) {
                return;
            }
            foreach (var npc in WatchingNpcs) {
                npc.ParentModel.TryGetElement<BarkElement>()?.OnNoticeHeroSneak();
            }
        }

        void OnHeroAttack(AttackParameters _) {
            foreach (var npc in WatchingNpcs) {
                npc.ParentModel.TryGetElement<BarkElement>()?.OnNoticeWeaponUse();
            }
        }
    }
}