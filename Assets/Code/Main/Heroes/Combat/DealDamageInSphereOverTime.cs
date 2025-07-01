using System.Collections.Generic;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Saving;
using Awaken.TG.MVC.Attributes;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Combat {
#if UNITY_EDITOR
    [SpawnsView(typeof(VDealDamageInSphereOverTimeVisualizer))]
#endif
    public partial class DealDamageInSphereOverTime : DealDamageInAreaOverTimeBase {
        public sealed override bool IsNotSaved => true;

        readonly SphereDamageParameters _sphereDamageParameters;
        
        protected override float Duration => _sphereDamageParameters.duration;
        
        public DealDamageInSphereOverTime(SphereDamageParameters parameters, Vector3 origin, ICharacter attacker = null) : base(parameters.defaultDelay, origin, attacker) {
            _sphereDamageParameters = parameters;
        }

        protected override void DealDamageInstant() {
            DamageUtils.DealDamageInSphereInstantaneous(Attacker, _sphereDamageParameters, _origin);
        }

        protected override void DealDamageOverTime(float percentage) {
            float radius = _sphereDamageParameters.endRadius * percentage;
            DamageUtils.DealDamageInSphereOverTime(Attacker, _sphereDamageParameters, _origin, radius, in _damageDealtTo);
#if UNITY_EDITOR
            spheresToDraw.Add(new SphereToDraw(_origin, radius));
#endif
        }

#if UNITY_EDITOR
        public struct SphereToDraw {
            public Vector3 Origin { get; }
            public float Radius { get; }

            public SphereToDraw(Vector3 origin, float radius) {
                this.Origin = origin;
                this.Radius = radius;
            }
        }

        public List<SphereToDraw> spheresToDraw = new();
#endif
    }
}