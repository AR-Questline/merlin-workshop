using System.Collections.Generic;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Utils;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Combat {
    public abstract partial class DealDamageInAreaOverTimeBase : Element<IModel> {
        protected static float TickDuration => DamageUtils.SphereDamageTickDuration;

        protected readonly HashSet<HealthElement> _damageDealtTo = new();
        protected readonly Vector3 _origin;
        protected readonly WeakModelRef<ICharacter> _attacker;

        float _timePassed;
        float _tick;

        protected abstract float Duration { get; }
        protected IModel Attacker => _attacker.TryGet(out ICharacter attacker) ? attacker : ParentModel;

        protected DealDamageInAreaOverTimeBase(float? defaultDelay, Vector3 origin, ICharacter attacker = null) {
            _origin = origin;
            _tick = defaultDelay ?? TickDuration;
            _timePassed = -1;
            _attacker = new WeakModelRef<ICharacter>(attacker);
        }

        protected override void OnInitialize() {
            ParentModel.GetOrCreateTimeDependent().WithUpdate(ProcessUpdate);
        }

        void ProcessUpdate(float deltaTime) {
            _tick -= deltaTime;
            if (_tick <= 0) {
                // Start time passed after first tick
                if (_timePassed < 0) {
                    // If duration is less than tick duration, deal damage instantly
                    if (Duration < TickDuration) {
                        DealDamageInstant();
                        Discard();
                        return;
                    }
                    _timePassed = TickDuration;
                }
                _tick = TickDuration;
                DealDamage(_timePassed / Duration);
            }

            if (_timePassed < 0) {
                return;
            }

            _timePassed += deltaTime;
            if (_timePassed > Duration) {
                DealDamage(1);
                Discard();
            }
        }

        void DealDamage(float percentage) {
            DealDamageOverTime(percentage);
        }

        protected abstract void DealDamageInstant();
        protected abstract void DealDamageOverTime(float percentage);

        protected override void OnDiscard(bool fromDomainDrop) {
            ParentModel.GetTimeDependent()?.WithoutUpdate(ProcessUpdate);
        }
    }
}