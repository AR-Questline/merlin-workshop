using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Saving;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Combat {
    public partial class DealDamageInConeOverTime : DealDamageInAreaOverTimeBase {
        public sealed override bool IsNotSaved => true;

        readonly ConeDamageParameters _coneDamageParameters;

        protected override float Duration => _coneDamageParameters.sphereDamageParameters.duration;
        
        public DealDamageInConeOverTime(ConeDamageParameters parameters, Vector3 origin, ICharacter attacker) : base(parameters.sphereDamageParameters.defaultDelay, origin, attacker) {
            _coneDamageParameters = parameters;
        }

        protected override void DealDamageInstant() {
            DamageUtils.DealDamageInConeInstantaneous(ParentModel, _coneDamageParameters, _origin);
        }

        protected override void DealDamageOverTime(float percentage) {
            float radius = _coneDamageParameters.sphereDamageParameters.endRadius * percentage;
            DamageUtils.DealDamageInConeOverTime(ParentModel, _coneDamageParameters, _origin, radius, in _damageDealtTo);
        }
    }
}