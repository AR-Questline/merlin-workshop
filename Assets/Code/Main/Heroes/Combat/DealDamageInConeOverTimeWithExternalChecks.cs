using System;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Combat {
    public class DealDamageInConeOverTimeWithExternalChecks : DealDamageInConeOverTime {
        Func<Collider, bool> _externalCheck;

        public DealDamageInConeOverTimeWithExternalChecks(ConeDamageParameters parameters, Vector3 origin, ICharacter attacker, Func<Collider, bool> externalCheck) : base(parameters, origin, attacker) {
            _externalCheck = externalCheck;
        }
        
        protected override void DealDamageInstant() {
            DamageUtils.DealDamageInConeWithAdditionalCheckInstantaneous(Attacker, _coneDamageParameters, _origin, _externalCheck);
        }

        protected override void DealDamageOverTime(float percentage) {
            float radius = _coneDamageParameters.sphereDamageParameters.endRadius * percentage;
            DamageUtils.DealDamageInConeWithAdditionalCheckOverTime(Attacker, _coneDamageParameters, _origin, radius, in _damageDealtTo, _externalCheck);
        }
    }
}