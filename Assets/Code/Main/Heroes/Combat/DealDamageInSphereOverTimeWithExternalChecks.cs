using System;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Combat {
    public class DealDamageInSphereOverTimeWithExternalChecks : DealDamageInSphereOverTime {
        Func<Collider, bool> _externalCheck;

        public DealDamageInSphereOverTimeWithExternalChecks(SphereDamageParameters parameters, Vector3 origin, ICharacter attacker, Func<Collider, bool> externalCheck) : base(parameters, origin, attacker) {
            _externalCheck = externalCheck;
        }
        
        protected override void DealDamageInstant() {
            DamageUtils.DealDamageInSphereWithAdditionalCheckInstantaneous(Attacker, _sphereDamageParameters, _origin, _externalCheck);
        }

        protected override void DealDamageOverTime(float percentage) {
            float radius = _sphereDamageParameters.endRadius * percentage;
            DamageUtils.DealDamageInSphereWithAdditionalCheckOverTime(Attacker, _sphereDamageParameters, _origin, radius, in _damageDealtTo, _externalCheck);
#if UNITY_EDITOR
            spheresToDraw.Add(new SphereToDraw(_origin, radius));
#endif
        }
    }
}