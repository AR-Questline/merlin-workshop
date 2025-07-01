using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Code.Utility;
using Awaken.TG.Main.AI.Combat.Behaviours.Abstracts;
using Awaken.TG.Main.AI.Combat.Behaviours.BossBehaviours.Mistbearer;
using Awaken.TG.Main.AI.Combat.Behaviours.MagicBehaviours;
using Awaken.TG.Main.AI.SummonsAndAllies;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.Main.Timing.ARTime.Modifiers;
using Awaken.TG.MVC;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.Attachments.Bosses {
    [Serializable]
    public partial class MistbearerCombat : MistbearerCombatBase {
        public override ushort TypeForSerialization => SavedModels.MistbearerCombat;

        [FoldoutGroup("TeleportTriggers"), SerializeField] int amountOfHits = 5;
        [FoldoutGroup("TeleportTriggers"), SerializeField] float hpPercentageLost = 0.1f;
        [FoldoutGroup("PushbackAdditionalTriggers"), SerializeField] float closeRangeThreshold = 5f;
        [FoldoutGroup("PushbackAdditionalTriggers"), SerializeField] int pushbackSpecialAttackIndex = 1;
        [FoldoutGroup("PushbackAdditionalTriggers"), SerializeField] float chancePerCloseRangeHit = 0.3f;
        [FoldoutGroup("Phases"), SerializeField] int[] armorBonusPerPhase = Array.Empty<int>();
        [FoldoutGroup("Copies"), SerializeField] int[] amountOfCopies = Array.Empty<int>();
        [FoldoutGroup("Copies"), SerializeField, TemplateType(typeof(LocationTemplate))] TemplateReference copiesRef;
        [FoldoutGroup("DebugTemporary"), SerializeField] float summonsSpeedMultiplier = 1.75f;
        [FoldoutGroup("DebugTemporary"), SerializeField] float bossSpeedMultiplier = 1.25f;

        int _hitsCounter;
        float _percentHpThreshold;
        List<MistbearerCombatCopy> _mistbearerCopies = new();
        
        public override int AmountOfCopies => amountOfCopies[CurrentPhase];
        public override bool AllCopiesLoaded => _mistbearerCopies.All(c => c.HasBeenDiscarded || c.BaseBehavioursLoaded);
        
        public override void InitFromAttachment(BossCombatAttachment spec, bool isRestored) {
            if (spec.BossBaseClass is not MistbearerCombat mistbearerCombat) {
                Log.Critical?.Error("MistbearerCombat: Spec is not MistbearerCombat!");
                return;
            }
            amountOfHits = mistbearerCombat.amountOfHits;
            hpPercentageLost = mistbearerCombat.hpPercentageLost;
            closeRangeThreshold = mistbearerCombat.closeRangeThreshold;
            pushbackSpecialAttackIndex = mistbearerCombat.pushbackSpecialAttackIndex;
            chancePerCloseRangeHit = mistbearerCombat.chancePerCloseRangeHit;
            armorBonusPerPhase = mistbearerCombat.armorBonusPerPhase;
            amountOfCopies = mistbearerCombat.amountOfCopies;
            copiesRef = mistbearerCombat.copiesRef;
            summonsSpeedMultiplier = mistbearerCombat.summonsSpeedMultiplier;
            bossSpeedMultiplier = mistbearerCombat.bossSpeedMultiplier;

            _hitsCounter = 0;
            _percentHpThreshold = 1f - hpPercentageLost;
            base.InitFromAttachment(spec, isRestored);
        }

        protected override void AfterVisualLoaded(Transform parentTransform) {
            base.AfterVisualLoaded(parentTransform);
            this.ListenTo(SummonGroupOfAlliesBehaviour.Events.AllSummonsKilled, OnAllSummonsKilled, this);
            this.ListenTo(Events.BehaviourStarted, OnMainMistbearerBehaviourStarted, this);
            NpcElement.GetOrCreateTimeDependent().AddTimeModifier(new MultiplyTimeModifier("Mistbearer", bossSpeedMultiplier));
        }

        void OnAllSummonsKilled(bool _) {
            if (CurrentBehaviour.Get() is MistbearerVulnerableBehaviour or MistbearerTeleportBehaviour) {
                return;
            }
            StartBehaviour(Element<MistbearerVulnerableBehaviour>());
            SummonBehaviour?.Trigger(SummonGroupOfAlliesBehaviour.Events.ResetGroupSpawner, true);
            allSummonsKilled = true;
        }

        protected override void OnDamageTaken(DamageOutcome damageOutcome) {
            float hpPercentage = NpcElement.Health.Percentage;
            if (!NpcElement.IsAlive || hpPercentage <= 0f) {
                return;
            }
            
            if (CurrentPhase == 0 && hpPercentage <= 0.66f) {
                IncreaseMistbearerPhase();
            } else if (CurrentPhase == 1 && hpPercentage <= 0.33f) {
                IncreaseMistbearerPhase();
            }
            
            if (CurrentBehaviour.Get() is MistbearerTeleportBehaviour or MistbearerVulnerableBehaviour) {
                return;
            }

            if (damageOutcome.Damage.IsPrimary && !damageOutcome.Damage.IsDamageOverTime) {
                _hitsCounter++;
                if (_hitsCounter > amountOfHits) {
                    StartTeleportBehaviour();
                    return;
                }
            }
            if (hpPercentage <= _percentHpThreshold) {
                StartTeleportBehaviour();
                return;
            }
            
            if (damageOutcome.Damage.IsPrimary && DistanceToTarget < closeRangeThreshold) {
                var currentBehaviour = CurrentBehaviour.Get();
                bool isPerformingPushback = currentBehaviour is AoeKnockBackBehaviour aoeKnockBackBehaviour &&
                                            aoeKnockBackBehaviour.SpecialAttackIndex == pushbackSpecialAttackIndex;
                if (!isPerformingPushback && RandomUtil.WithProbability(chancePerCloseRangeHit)) {
                    TryStartSpecialAttackBehaviour<AoeKnockBackBehaviour>(pushbackSpecialAttackIndex);
                }
            }
        }

        public void ResetDamageTakenCounters() {
            _hitsCounter = 0;
            _percentHpThreshold = NpcElement.Health.Percentage - hpPercentageLost;
        }

        public override void StartTeleportBehaviour() {
            if (CurrentBehaviour.Get() is MistbearerTeleportBehaviour) {
                return;
            }
            ResetDamageTakenCounters();
            if (CurrentPhase != 0) {
                KillAllCopies();
            }
            StartBehaviour(Element<MistbearerTeleportBehaviour>());
        }

        void IncreaseMistbearerPhase() {
            IncrementPhase();
            int armorGain = armorBonusPerPhase[CurrentPhase] - armorBonusPerPhase[CurrentPhase - 1];
            NpcElement.Stat(AliveStatType.Armor).IncreaseBy(armorGain);
        }

        // Mistbearer Copies
        void KillAllCopies() {
            foreach (var copy in _mistbearerCopies) {
                if (copy is { HasBeenDiscarded: false }) {
                    copy.ParentModel.Kill();
                }
            }
            _mistbearerCopies.Clear();
        }

        public override void SpawnNewCopies(TeleportDestination[] copyDestinations) {
            KillAllCopies();
            if (copiesRef is not { IsSet: true }) {
                Log.Critical?.Error("MistbearerCombat: No copies template set!");
                return;
            }
            var copyTemplate = copiesRef.Get<LocationTemplate>();
            foreach (var copyDestination in copyDestinations) {
                Location copy = copyTemplate.SpawnLocation(copyDestination.position, copyDestination.Rotation);
                copy.AfterFullyInitialized(() => {
                    if (ParentModel.HasBeenDiscarded) {
                        copy.Kill();
                        return;
                    }
                    if (!copy.TryGetElement<NpcElement>(out var npc)) {
                        copy.Kill();
                        return;
                    }
                    npc.AfterFullyInitialized(() => {
                        NpcAI.EnterCombatWith(NpcElement.GetCurrentTarget(), true);
                        if (copy.TryGetElement<MistbearerCombatCopy>(out var mistbearerCombatCopy)) {
                            mistbearerCombatCopy.CopyMistbearerStats(this);
                            _mistbearerCopies.Add(mistbearerCombatCopy);
                            npc.GetOrCreateTimeDependent().AddTimeModifier(new MultiplyTimeModifier("MistbearerCopy", bossSpeedMultiplier));
                        } else {
                            copy.Kill();
                        }
                    });
                });
            } 
        }
        
        void OnMainMistbearerBehaviourStarted(IBehaviourBase behaviourBase) {
            foreach (var copy in _mistbearerCopies) {
                if (copy is not { HasBeenDiscarded: false }) {
                    continue;
                }
                var type = behaviourBase.GetType();

                if (behaviourBase is EnemyBehaviourBase enemyBehaviourBase && enemyBehaviourBase.SpecialAttackIndex != 0) {
                    copy.TryStartSpecialAttackBehaviour(type, enemyBehaviourBase.SpecialAttackIndex);
                    continue;
                }

                copy.TryStartBehaviour(type);
            }
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            base.OnDiscard(fromDomainDrop);
            KillAllCopies();
        }
    }
}