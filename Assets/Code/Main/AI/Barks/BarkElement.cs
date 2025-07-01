using System;
using Awaken.TG.Main.AI.Idle.Interactions;
using Awaken.TG.Main.AI.States;
using Awaken.TG.Main.AI.States.Flee;
using Awaken.TG.Main.AI.States.ReturnToSpawn;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Executions;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.Main.Utility.StateMachines;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.Utility;
using UnityEngine;
using static Awaken.TG.Main.AI.Barks.BarkRangeExtensions;
using Log = Awaken.Utility.Debugging.Log;

namespace Awaken.TG.Main.AI.Barks {
    public partial class BarkElement : Element<NpcElement> {
        const float RefreshDelay = 2f;

        const float LocalCriticalCooldown = 3f;
        const float LocalImportantCooldown = 8f;
        const float LocalNotImportantCooldown = 14f;
        const float LocalIdleCooldown = 24f;

        // === State
        BarkConfig _config;
        BarkConfig _wyrdConvertedConfig;
        float _lastCheck;
        float _lastCriticalBark = float.MinValue;
        float _lastImportantBark = float.MinValue;
        float _lastNotImportantBark = float.MinValue;
        float _lastIdleBark = float.MinValue;
        Story _currentBark;

        BarkConfig Config => _wyrdConvertedConfig != null && (Npc?.WyrdConverted ?? false) ? _wyrdConvertedConfig : _config;
        NpcElement Npc => ParentModel;
        Hero Hero => Hero.Current;
        public sealed override bool IsNotSaved => true;

        protected override void OnInitialize() {
            _config = ParentModel.Actor.BarkConfig;
            _wyrdConvertedConfig = GameConstants.Get.wyrdConvertedConfig;
        }

        // === Initialization
        public void OnLoaded() {
            if (HasBeenDiscarded || NpcElement.DEBUG_DoNotSpawnAI) {
                return;
            }
            
            ParentModel.GetOrCreateTimeDependent().WithUpdate(UpdateBarks);
            Npc.ListenTo(NpcAI.Events.NpcStateChanged, OnStateChanged, this);
            Npc.ListenTo(HealthElement.Events.OnDamageDealt, OnDamageDealt, this);
            Npc.HealthElement.ListenTo(HealthElement.Events.OnDamageTaken, OnDamageTaken, this);
            Npc.ListenTo(UnconsciousElement.Events.LoseConscious, StopBark, this);
            ParentModel.TryGetElement<IAlive>()?.ListenTo(IAlive.Events.AfterDeath, Discard, this);
            Npc.ListenTo(NpcElement.Events.NpcIsInDialogueChanged, OnNpcIsInDialogueChanged, this);
            Npc.ListenTo(Events.BeforeDiscarded, Discard, this);
        }

        void OnNpcIsInDialogueChanged(bool isInDialogue) {
            if (isInDialogue) {
                return;
            }
            
            UpdateCooldownInternal(BarkType.Critical, Time.time);
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            StopBark();
        }

        void StopBark() {
            if (_currentBark is { WasDiscarded: false }) {
                _currentBark.FinishStory(true);
            }
            _currentBark = null;
        }

        // === Bark operations
        void UpdateBarks(float deltaTime) {
            _lastCheck += deltaTime;
            if (_lastCheck < RefreshDelay) {
                return;
            }
            _lastCheck = 0f;
            Services.Get<MitigatedExecution>().Register(PassiveBark, this, MitigatedExecution.Cost.Light, MitigatedExecution.Priority.Low, RefreshDelay, true);
        }

        void PassiveBark() {
            if (Npc.NpcAI.InAlert) {
                IState state = Npc.NpcAI.Behaviour.CurrentState;
                if (state is StateAlertLookAt) {
                    TryBark(BarkBookmarks.LookAtAlertSource, BarkType.Important, RangeLong);
                } else if (state is StateAlertWander wander) {
                    TryBark(BarkBookmarks.MoveToAlertSource, BarkType.Important, RangeLong);
                }
            } else if (Npc.NpcAI.InIdle) {
                var npcInteractionBase = Npc.Interactor.CurrentInteraction as NpcInteractionBase;
                float? overridenCooldown = (npcInteractionBase != null && npcInteractionBase.OverrideIdleBarksCooldown) ? npcInteractionBase.IdleBarksCooldown : null;
                if (npcInteractionBase != null) {
                    var barkId = npcInteractionBase.SelectedInteractionType switch {
                        InteractionType.Default => BarkBookmarks.Idle,
                        InteractionType.Work => BarkBookmarks.WorkIdle,
                        InteractionType.Sleeping => BarkBookmarks.SleepingIdle,
                        _ => BarkBookmarks.Idle
                    };
                    TryBark(barkId, BarkType.Idle, RangeShort, true, overridenCooldown);
                } else {
                    TryBark(BarkBookmarks.Idle, BarkType.Idle, RangeShort, true, overridenCooldown);
                }
            } else if (Npc.NpcAI.InCombat) {
                TryBark(BarkBookmarks.CombatIdle, BarkType.Important, RangeMedium);
            } else if (Npc.NpcAI.InFlee) {
                TryBark(BarkBookmarks.FleeingFromCombat, BarkType.Important, RangeMedium);
            }
        }

        void OnStateChanged(Change<IState> change) {
            (IState previous, IState current) = change;
            if (current is StateReturnAfterVictory) {
                TryBark(BarkBookmarks.ToIdleFromCombatVictory, BarkType.Critical, RangeLong);
            } else if (current is StateIdle or StateReturn) {
                if (previous is StateCombat) {
                    TryBark(BarkBookmarks.ToIdleFromCombatDisengage, BarkType.Important, RangeLong);
                } else if (previous != null && previous is not StateReturn) {
                    TryBark(BarkBookmarks.ToIdleFromAlert, BarkType.Important, RangeLong);
                }
            } else if (current is StateCombat) {
                TryBark(BarkBookmarks.ToCombat, BarkType.Important, RangeLong);
            } else if (current is StateAlertLookAt) {
                TryBark(BarkBookmarks.LookAtAlertSource, BarkType.Important, RangeLong);
            } else if (current is StateFlee) {
                TryBark(BarkBookmarks.FleeingFromCombat, BarkType.Critical, RangeLong);
            }
        }

        public void OnWanderSpeedChanged(StateAlertWander wander) {
            TryBark(BarkBookmarks.MoveToAlertSource, BarkType.NotImportant, RangeLong);
        }
        
        void OnDamageDealt(DamageOutcome _) {
            TryBark(BarkBookmarks.CombatOnHit, BarkType.Important, RangeLong);
        }
        
        void OnDamageTaken(DamageOutcome outcome) {
            if (outcome.Target is {IsDying: false} && outcome.Target is not NpcElement {IsUnconscious: true}) {
                TryBark(BarkBookmarks.CombatOnGetHit, BarkType.Important, RangeLong);
            }
        }
        
        public void OnNPCNoticedCrimeAttempt() {
            TryBark(BarkBookmarks.CrimeToAlert, BarkType.Important, RangeLong);
        }

        public void OnGuardNoticedCrime(in CrimeArchetype archetype) {
            TryBark(BarkBookmarks.BountyHunterOnCriminalSight, BarkType.Important, RangeLong);
        }

        public void OnDefenderNoticedCrime(CrimeArchetype archetype) {
            TryBark(BarkBookmarks.CrimeToCombat, BarkType.Important, RangeLong);
        }
        
        public void OnVigilanteNoticedCrime(CrimeArchetype archetype) {
            TryBark(BarkBookmarks.CrimeToCombat, BarkType.Important, RangeLong);
        }

        public void OnPeasantNoticedCrime(CrimeArchetype archetype) {
            TryBark(BarkBookmarks.CallGuards, BarkType.Important, RangeLong);
        }

        public void OnNoticeWeaponDrawn() {
            TryBark(BarkBookmarks.NoticeWeaponDrawnGeneric, BarkType.NotImportant, RangeShort);
        }
        
        public void OnNoticeItemDrop() {
            TryBark(BarkBookmarks.NoticeItemDroppedGeneric, BarkType.NotImportant, RangeShort);
        }

        public void OnNoticeHeroJump() {
            TryBark(BarkBookmarks.NoticeHeroJumped, BarkType.NotImportant, RangeShort);
        }

        public void OnNoticeHeroSneak() {
            TryBark(BarkBookmarks.NoticeHeroSneak, BarkType.NotImportant, RangeShort);
        }

        public void OnNoticeWeaponUse() {
            TryBark(BarkBookmarks.NoticeWeaponUse, BarkType.NotImportant, RangeShort);
        }
        
        public void OnTrespasserSpotted() {
            TryBark(BarkBookmarks.CrimeOnTrespassing, BarkType.Important, RangeUnlimited);
        }
        
        public void OnNoticeHero() {
            if (Hero.IsCrouching && TryBark(BarkBookmarks.NoticeHeroSneak, BarkType.NotImportant, RangeShort)) {
                return;
            }

            if (!Hero.Grounded && TryBark(BarkBookmarks.NoticeHeroJumped, BarkType.NotImportant, RangeShort)) {
                return;
            }

            if (Hero.IsWeaponEquipped && TryBark(BarkBookmarks.NoticeWeaponDrawnGeneric, BarkType.NotImportant, RangeShort)) {
                return;
            }
            
            TryBark(BarkBookmarks.NoticePlayerGeneric, BarkType.NotImportant, RangeShort);
        }

        public void OnCorpseFound() {
            TryBark(BarkBookmarks.ToAlertDeadBodiesFound, BarkType.Important, RangeLong);
        }

        public void BumpedInto() {
            TryBark(BarkBookmarks.BumpedInto, BarkType.Important, RangeShort);
        }

        public void SlidedInto() {
            TryBark(BarkBookmarks.SlidedInto, BarkType.Important, RangeShort);
        }

        public void OnFailedDialogue() {
            if (Npc.Behaviours.CurrentUnwrappedInteraction is NpcInteraction {SelectedDialogueType: DialogueType.Bark} interaction) {
                var barkId = interaction.SelectedInteractionType == InteractionType.Sleeping
                    ? BarkBookmarks.SleepingResponse
                    : BarkBookmarks.BusyResponse;
                Bark(barkId, BarkType.Idle, false, out _);
            }
        }
        
        /// <summary>
        /// Try to bid farewell to the player when the story concludes.
        /// </summary>
        /// <param name="sourceStory">The story graph that is reaching its end.</param>
        public void TrySayGoodbyeOnStoryEnd(Story sourceStory) {
            if(!string.IsNullOrEmpty(sourceStory?.Graph.guid) 
               && sourceStory.Graph.guid == _currentBark?.Graph.guid) {
                return;
            }
            TryBark(BarkBookmarks.SayGoodbyeGeneric, BarkType.Important, RangeShort);
        }

        public bool TryBark(string barkId, BarkType barkType, float maxDistance, bool checkCooldown = true, float? overridenCooldown = null) {
            // check if Npc allows barks
            bool allowBarks = Npc.Behaviours.CurrentInteraction?.AllowBarks ?? true;
            if (!allowBarks) {
                return false;
            }

            if (Npc?.IsUnconscious ?? false) {
                return false;
            }
            
            // check if isn't still barking last bark
            if (_currentBark is { WasDiscarded: false }) {
                return false;
            }
            // check distance
            float sqrDistance = Vector3.SqrMagnitude(Hero.Coords - ParentModel.Coords);
            if (sqrDistance > maxDistance * maxDistance) {
                return false;
            }
            
            return Bark(barkId, barkType, checkCooldown, out _, overridenCooldown);
        }

        public bool Bark(string barkId, BarkType type, bool checkCooldown, out Story story, float? overridenCooldown = null) {
            Log.Debug?.Info($"{ParentModel.Actor.Name} Trying to bark: " + barkId);
            story = null;
            
            if (StoryBookmark.ToSpecificChapter(Config.StoryRef, barkId, out var bookmark) == false) {
                return false;
            }
            var config = StoryConfig.Location(Npc.ParentModel, bookmark, typeof(VBark));
            if (Story.TryStartStoryDeferred(config, out var newBark) == false) {
                return false;
            }
            float currentTime = Time.time;
            if (checkCooldown == false || CheckCooldown(type, currentTime, overridenCooldown)) {
                UpdateCooldown(type, currentTime);

                Story.FinishStartStoryDeferred(config, newBark);
                if (newBark.WasDiscarded) {
                    _currentBark = null;
                    return false;
                }

                _currentBark = newBark;
                story = newBark;
                _currentBark.ListenTo(Events.AfterDiscarded, StopBark, this);
                return true;
            }
            return false;
        }

        bool CheckCooldown(BarkType type, float currentTime, float? overridenCooldown) {
            if (World.Services.Get<BarkSystem>().CheckCooldown(type, currentTime) == false) {
                return false;
            }

            return type switch {
                BarkType.Critical => CanDoCritical(),
                BarkType.Important => CanDoImportant(),
                BarkType.NotImportant => CanDoNotImportant(),
                BarkType.Idle => CanDoLeastImportant(),
                _ => true
            };

            bool CanDoCritical() => currentTime > _lastCriticalBark + GetCd(LocalCriticalCooldown);
            bool CanDoImportant() => CanDoCritical() && currentTime > _lastImportantBark + GetCd(LocalImportantCooldown);
            bool CanDoNotImportant() => CanDoImportant() && currentTime > _lastNotImportantBark + GetCd(LocalNotImportantCooldown);
            bool CanDoLeastImportant() => CanDoNotImportant() && currentTime > _lastIdleBark + GetCd(LocalIdleCooldown);
            
            float GetCd(float defaultCd) => overridenCooldown ?? defaultCd;
        }
        
        void UpdateCooldown(BarkType type, float currentTime) {
            World.Services.Get<BarkSystem>().UpdateCooldown(type, currentTime);
            UpdateCooldownInternal(type, currentTime);
        }

        void UpdateCooldownInternal(BarkType type, float currentTime) {
            switch (type) {
                case BarkType.Critical:
                    _lastCriticalBark = _lastImportantBark = _lastNotImportantBark = _lastIdleBark = currentTime;
                    break;
                case BarkType.Important:
                    _lastImportantBark = _lastNotImportantBark = _lastIdleBark = currentTime;
                    break;
                case BarkType.NotImportant:
                    _lastNotImportantBark = _lastIdleBark = currentTime;
                    break;
                case BarkType.Idle:
                    _lastIdleBark = currentTime;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
        
        /// <summary>
        /// Represents different levels of NPC dialogue ("barks") based on importance.
        /// </summary>
        public enum BarkType : byte
        {
            /// <summary>
            /// Direct most important actions of NPC, or responses to such events. (e.g. killing someone).
            /// </summary>
            Critical,

            /// <summary>
            /// Direct actions of NPC or Reactions to events that are important but not of the highest priority.
            /// </summary>
            Important,

            /// <summary>
            /// Generic reactions to hero shenanigans, like jumping or sneaking in plain view. 
            /// </summary>
            NotImportant,

            /// <summary>
            /// Generic idle barks, like commenting on the weather or the time of day.
            /// </summary>
            Idle
        }
    }
}