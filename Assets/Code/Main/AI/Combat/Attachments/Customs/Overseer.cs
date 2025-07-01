using System;
using Awaken.TG.Main.AI.Combat.Behaviours.CustomBehaviours;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using UnityEngine;

namespace Awaken.TG.Main.AI.Combat.Attachments.Customs {
    [Serializable]
    public partial class Overseer : CustomCombatBaseClass {
        public override ushort TypeForSerialization => SavedModels.Overseer;

        // === Fields
        [SerializeField] int specialAfterTakenHits = 6;
        [SerializeField] float specialAttackAfterTakenDamage = 40;
        [SerializeField] float timeToResetCounters = 0.25f;
        float _resetTimer = float.MinValue;
        float _damageTaken;
        float _hitsTaken;
        
        // === Properties
        bool UsingSpecialAttack => CurrentBehaviour.Get() is MeleeAttackHammerSmash;
        
        // === Initialization
        public override void InitFromAttachment(CustomCombatAttachment spec, bool isRestored) {
            base.InitFromAttachment(spec, isRestored);
            if (spec.CustomCombatBaseClass is not Overseer baseSpec) {
                Log.Important?.Error($"BaseClass of provided spec:{spec} is not of required type {nameof(Overseer)}");
                return;
            }
            specialAfterTakenHits = baseSpec.specialAfterTakenHits;
            specialAttackAfterTakenDamage = baseSpec.specialAttackAfterTakenDamage;
            timeToResetCounters = baseSpec.timeToResetCounters;
        }

        protected override void AfterVisualLoaded(Transform parentTransform) {
            base.AfterVisualLoaded(parentTransform);
            EquipWeapons(false, out _);
        }

        // === Events
        protected override void Tick(float deltaTime, NpcElement npc) {
            if (_resetTimer > 0) {
                _resetTimer -= deltaTime;

                if (_resetTimer <= 0) {
                    Reset();
                }
            }
            
            base.Tick(deltaTime, npc);
        }
        
        protected override void OnDamageTaken(DamageOutcome damageOutcome) {
            base.OnDamageTaken(damageOutcome);
            
            if (UsingSpecialAttack || NpcElement.IsDying) {
                return;
            }
            
            _resetTimer = timeToResetCounters;
            _damageTaken += damageOutcome.FinalAmount;
            _hitsTaken++;

            if (_hitsTaken >= specialAfterTakenHits || _damageTaken >= specialAttackAfterTakenDamage) {
                TryStartBehaviour<MeleeAttackHammerSmash>();
                Reset();
            }
        }

        // === Helpers
        void Reset() {
            _damageTaken = 0;
            _hitsTaken = 0;
        }
    }
}