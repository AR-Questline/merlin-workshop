using System;
using System.Collections.Generic;
using Awaken.TG.Code.Utility;
using Awaken.TG.Main.AI.Combat.CustomDeath;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.NPCs;
using UnityEngine;

namespace Awaken.TG.Main.Fights.Finishers {
    [CreateAssetMenu(fileName = "FinishersList", menuName = "TG/Animancer/FinishersList")]
    public class FinishersList : ScriptableObject {
        static readonly List<FinisherData> ReusableFinisherList = new(5);

        [field: SerializeReference] List<ICustomDeathAnimationConditions> globalConditions = new();
        [SerializeField] FinisherHealthCondition defaultHealthCondition = FinisherHealthCondition.Default;
        [SerializeField] FinisherData[] finishers = Array.Empty<FinisherData>();

        public void Init() {
            foreach (var finisher in finishers) {
                finisher.Init();
            }
        }
        
        public bool CheckGlobalConditions(DamageOutcome damageOutcome) {
            if (damageOutcome.Target is not NpcElement { HasBeenDiscarded: false, IsAlive: true, CanUseExternalCustomDeath: true }) {
                return false;
            }
            
            foreach (var condition in globalConditions) {
                if (!condition.Check(damageOutcome, false)) {
                    return false;
                }
            }
            return true;
        }
        
        public bool CheckDefaultHpCondition(DamageOutcome damageOutcome, float predictedDmg) {
            return defaultHealthCondition.IsFulfilled(predictedDmg, damageOutcome.FinalAmount, damageOutcome.Target as NpcElement);
        }

        public bool TryFindRandomValidFinisher(DamageOutcome damageOutcome, float predictedDmg, out FinisherData foundFinisher) {
            if (!CheckGlobalConditions(damageOutcome)) {
                foundFinisher = null;
                return false;
            }
            var hpConditionIsFulfilled = CheckDefaultHpCondition(damageOutcome, predictedDmg);
            
            foreach (var finisher in finishers) {
                if (finisher.CheckConditions(damageOutcome, predictedDmg, hpConditionIsFulfilled)) {
                    ReusableFinisherList.Add(finisher);
                }
            }
            
            if (ReusableFinisherList.Count > 0) {
                foundFinisher = ReusableFinisherList[RandomUtil.UniformInt(0, ReusableFinisherList.Count - 1)];
                ReusableFinisherList.Clear();
                return true;
            }
            
            foundFinisher = null;
            return false;
        }

        public bool TryFindFirstValidFinisher(DamageOutcome damageOutcome, float predictedDmg, out FinisherData foundFinisher) {
            if (!CheckGlobalConditions(damageOutcome)) {
                foundFinisher = null;
                return false;
            }
            var hpConditionIsFulfilled = CheckDefaultHpCondition(damageOutcome, predictedDmg);
            foreach (var finisher in finishers) {
                if (finisher.CheckConditions(damageOutcome, predictedDmg, hpConditionIsFulfilled)) {
                    foundFinisher = finisher;
                    return true;
                }
            }
            foundFinisher = null;
            return false;
        }
        
        public void Unload() {
            foreach (var finisher in finishers) {
                finisher.Unload();
            }
        }
    }
}
