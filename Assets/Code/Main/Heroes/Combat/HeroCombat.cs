using System.Collections.Generic;
using Awaken.TG.Main.AI;
using Awaken.TG.Main.AI.Combat.Attachments;
using Awaken.TG.Main.AI.Combat.Behaviours.Abstracts;
using Awaken.TG.Main.AI.Combat.Utils;
using Awaken.TG.Main.AI.SummonsAndAllies;
using Awaken.TG.Main.AudioSystem.Biomes;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Crafting.Fireplace;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.Relations;
using Awaken.TG.MVC.Utils;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.Maths.Data;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.Heroes.Combat {
    public partial class HeroCombat : Element<Hero> {
        const float CombatLevelUpdateSpeed = 0.3f;
        const float TimeToNextGuardIntervention = 15f;

        public static int forceCombatCount;
        public static bool ForceCombat => forceCombatCount > 0;

        public sealed override bool IsNotSaved => true;

        bool _isAlerted;
        DelayedValue _audioCombatLevel, _highestTierAlerted;
        HeroCombatAntagonism _heroCombatAntagonism;
        Dictionary<NpcElement, PeacefulNpcData> _peacefulNpcData = new(32);
        OnDemandCache<Faction, FactionData> _factionData = new(_ => new());
        
        public float AudioCombatLevel => _audioCombatLevel.Value;
        public float DesiredAudioCombatLevel => _audioCombatLevel.Target;
        public float HighestTierAlerted => _highestTierAlerted.Value;
        public int TierForAlert { get; private set; }
        public float MaxEnemiesAlert { get; private set; }
        public float MaxHeroVisibility { get; private set; }
        public int EnemiesAlerted { get; private set; }
        public bool IsHeroInFight { get; private set; }
        public float FightTime { get; private set; }
        public List<NpcAI> NearAIs { get; } = new(32);
        public IEnumerable<NpcElement> Allies => World.All<NpcHeroSummon>().Select(summon => summon.ParentModel);
        
        HeroCombatAntagonism Antagonism => ParentModel.CachedElement(ref _heroCombatAntagonism);

        protected override void OnInitialize() {
            ParentModel.GetOrCreateTimeDependent().WithUpdate(ProcessUpdate);
            ParentModel.ListenTo(AITargetingUtils.Relations.IsTargetedBy.Events.Changed, OnTargetingChanged, this);
            ParentModel.ListenTo(HealthElement.Events.OnDamageDealt, OnDamageDealt, this);
            ParentModel.ListenTo(ICharacter.Events.CombatExited, OnCombatEnded, this);
            // --- Toggling AudioCore
            ParentModel.ListenTo(IAlive.Events.BeforeDeath, static _ => Services.TryGet<AudioCore>()?.Stop(), this);
            Services.TryGet<AudioCore>()?.Play();
        }

        public void RegisterNearNpcAI(NpcAI npcAI) {
            if (NearAIs.Contains(npcAI)) {
                return;
            }
            NearAIs.Add(npcAI);
        }

        public void UnregisterNearNpcAI(NpcAI npcAI) {
            NearAIs.Remove(npcAI);
        }

        void ProcessUpdate(float deltaTime) {
            int countAlerted = 0;
            float maxHeroVisibility = 0f;
            float maxAlert = 0f;
            int highestTierAlerted = 0;
            for (int i = NearAIs.Count - 1; i >= 0; i--) {
                var nearAI = NearAIs[i];
                if (nearAI.HasBeenDiscarded) {
                    Log.Important?.Error($"NearAI has been discarded, but wasn't Unregistered from NearAI list! {nearAI}");
                    NearAIs.Remove(nearAI);
                    continue;
                }
                
                if (nearAI.NpcElement.IsUnconscious || !nearAI.NpcElement.CanTriggerAggroMusic) {
                    continue;
                }
                
                float aiHeroVisibility = nearAI.HeroVisibility;
                float aiAlert = nearAI.AlertValue;
                if (aiHeroVisibility > 0 || aiAlert > 0) {
                    // Alerted!
                    countAlerted++;
                    maxAlert = Mathf.Max(maxAlert, aiAlert);
                    maxHeroVisibility = Mathf.Max(maxHeroVisibility, aiHeroVisibility);
                    highestTierAlerted = Mathf.Max(highestTierAlerted, nearAI.ParentModel.MusicTier);
                }
            }
            
            // --- Update Audio Params
            _audioCombatLevel.Update(deltaTime, CombatLevelUpdateSpeed);
            _highestTierAlerted.Set(highestTierAlerted);
            _highestTierAlerted.Update(deltaTime, CombatLevelUpdateSpeed);
            TierForAlert = highestTierAlerted;

            EnemiesAlerted = countAlerted;
            bool inFight = ForceCombat || ParentModel.AnyPossibleAttackerForHero();
            
            MaxEnemiesAlert = maxAlert;
            MaxHeroVisibility = maxHeroVisibility;
            SetHeroInFight(inFight, maxHeroVisibility > 0);

            if (IsHeroInFight) {
                FightTime += deltaTime;
            } else {
                FightTime = 0;
                foreach (var data in _factionData.Values) {
                    if (data.timeToNextGuardIntervention > 0) {
                        data.timeToNextGuardIntervention -= deltaTime;
                    }
                }
            }
        }

        void OnTargetingChanged(RelationEventData relationData) {
            if (relationData.to is NpcElement npc && ShouldApplyAntagonism(npc)) {
                if (relationData.newState) {
                    if (npc.TryGetBehaviour(out var enemy, out var behaviour) && !behaviour.IsPeaceful && Crime.Combat(npc).IsCrime()) {
                        Antagonism.ApplyCombatAntagonism(npc);
                    } else {
                        var data = new PeacefulNpcData {
                            behaviourStartedListener = enemy.ListenTo(EnemyBaseClass.Events.BehaviourStarted, RefreshBehaviourAntagonism, this),
                            antagonismChangedListener = npc.ListenTo(FactionService.Events.AntagonismChanged, OnPeacefulNpcAntagonismChanged, this)
                        };
                        _peacefulNpcData.Add(npc, data);
                    }
                } else {
                    if (_peacefulNpcData.Remove(npc, out var data)) {
                        World.EventSystem.TryDisposeListener(ref data.behaviourStartedListener);
                        World.EventSystem.TryDisposeListener(ref data.antagonismChangedListener);
                    }
                }
            }
            
            static void RefreshBehaviourAntagonism(IBehaviourBase behaviour) {
                if (!behaviour.IsPeaceful) {
                    var npc = behaviour.ParentModel.NpcElement;
                    if (Crime.Combat(npc).IsCrime()) {
                        var heroCombat = Hero.Current.HeroCombat;
                        if (heroCombat._peacefulNpcData.Remove(npc, out var data)) {
                            World.EventSystem.DisposeListener(ref data.behaviourStartedListener);
                            heroCombat.Antagonism.ApplyCombatAntagonism(npc);
                        } else {
                            Log.Important?.Error("Cannot find npc in non hostile npc data!");
                        }
                    }
                }
            }
        }
        
        void OnPeacefulNpcAntagonismChanged(ICharacter character) {
            if (character is NpcElement npc && !ShouldApplyAntagonism(npc)) {
                if (_peacefulNpcData.Remove(npc, out var data)) {
                    World.EventSystem.TryDisposeListener(ref data.behaviourStartedListener);
                    World.EventSystem.TryDisposeListener(ref data.antagonismChangedListener);
                }
            }
        }

        void OnDamageDealt(DamageOutcome outcome) {
            if (outcome.Target is NpcElement npc && ShouldApplyAntagonism(npc) && Crime.Combat(npc).IsCrime()) {
                if (!_factionData[npc.Faction].combatBountyApplied.Contains(npc)) {
                    CrimeSituation crimeSituation = CommitCrime.GetSituation(instantReport: npc.Template.CrimeReactionArchetype == CrimeReactionArchetype.Guard);
                    if (CommitCrime.Combat(npc, crimeSituation)) {
                        _factionData[npc.Faction].combatBountyApplied.Add(npc);
                    }
                }
                Antagonism.ApplyCombatAntagonism(npc);
            }
        }
        
        void OnCombatEnded() {
            foreach ((Faction _, FactionData value) in _factionData) {
                value.combatBountyApplied.Clear();
            }
        }

        bool ShouldApplyAntagonism(NpcElement npc) {
            return npc.Faction != ParentModel.Faction && !npc.Faction.IsHostileTo(ParentModel.Faction);
        }

        void SetHeroInFight(bool inFight, bool alerted) {
            if (World.HasAny<IHeroInvolvement>() && !World.HasAny<FireplaceUI>()) {
                inFight = false;
                alerted = false;
            }
            
            if (!IsHeroInFight && inFight) {
                IsHeroInFight = true;
                ParentModel.Trigger(ICharacter.Events.CombatEntered, ParentModel);
            } else if (IsHeroInFight && !inFight) {
                IsHeroInFight = false;
                ParentModel.Trigger(ICharacter.Events.CombatExited, ParentModel);
            }

            int desiredCombatLevel = inFight ? 2 : (alerted ? 1 : 0);
            _audioCombatLevel.Set(desiredCombatLevel);
        }
        
        // === Helpers
        [UnityEngine.Scripting.Preserve]
        bool CanBeFought(ICharacter character) {
            if (character is NpcElement npc) {
                return npc.CanTriggerAggroMusic;
            }
            return true;
        }

        public void NotifyGuardIntervention(NpcElement guard) {
            _factionData[guard.Faction].timeToNextGuardIntervention = TimeToNextGuardIntervention;
        }
        
        public void ResetGuardInterventionCooldown(NpcElement guard) {
            _factionData[guard.Faction].timeToNextGuardIntervention = 0;
        }

        public void NotifyBountyPaid(NpcElement guard) {
            var data = _factionData[guard.Faction];
            data.timeToNextGuardIntervention = 0;
        }

        public bool CanGuardIntervene(NpcElement npc) {
            return _factionData[npc.Faction].timeToNextGuardIntervention <= 0;
        }

        struct PeacefulNpcData {
            public IEventListener behaviourStartedListener;
            public IEventListener antagonismChangedListener;
        }

        class FactionData {
            public float timeToNextGuardIntervention;
            public readonly HashSet<WeakModelRef<NpcElement>> combatBountyApplied = new();
        }
    }
}