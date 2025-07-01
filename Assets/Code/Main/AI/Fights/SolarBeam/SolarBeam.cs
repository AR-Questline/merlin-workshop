using System.Collections.Generic;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.VisualGraphUtils;
using Awaken.Utility.Debugging;
using UnityEngine;
using UnityEngine.VFX;

namespace Awaken.TG.Main.AI.Fights.SolarBeam {
    public class SolarBeam {
        SolarBeamData _data;
        ICharacter _owner;
        Transform _startBeam;
        VisualEffect _visualEffect;
        
        List<IAlive> _damagedTargets = new();
        
        public SolarBeam(SolarBeamData data, ICharacter owner, Transform startBeam) {
            _data = data;
            _owner = owner;
            _startBeam = startBeam;
            _visualEffect = startBeam.GetComponentInChildren<VisualEffect>();
            if (_visualEffect == null) {
                Log.Important?.Error($"SolarBeam: VisualEffect in {startBeam.gameObject.name} not found");
                return;
            }
            _visualEffect.SetFloat("Range", data.maxRange);
        }

        public void Update(float deltaTime) {
            ThrowCast(deltaTime);
        }

        void ThrowCast(float deltaTime) {
            float hitRange = _data.maxRange;
            bool hit = false;
            
            var startPosition = _startBeam.position + _startBeam.rotation * _data.raycastOffset;
            if (_data.pierceTargets) {
                List<HitResult> hitResults = _data.targetDetection.RaycastMultiHit(startPosition, _startBeam.forward, _data.maxRange);
                if (hitResults.Count > 0) {
                    foreach (var hitResult in hitResults) {
                        CheckCastResult(hitResult, deltaTime);
                    }
                    if (hitResults[^1].Prevented) {
                        hit = true;
                        hitRange = (hitResults[^1].Point - _startBeam.position).magnitude;
                    }
                }
            } else {
                HitResult hitResult = _data.targetDetection.Raycast(startPosition, _startBeam.forward, _data.maxRange);
                if (hitResult.Collider != null) {
                    CheckCastResult(hitResult, deltaTime);
                    hit = true;
                    hitRange = (hitResult.Point - _startBeam.position).magnitude;
                }
            }
            
            _visualEffect.SetBool("IsHit", hit);
            _visualEffect.SetFloat("Range", hitRange);
        }

        void CheckCastResult(HitResult hitResult, float deltaTime) {
            if (hitResult.Prevented) {
                return;
            }

            Collider hitCollider = hitResult.Collider;
            if (hitCollider == null) {
                return;
            }
            var iAlive = VGUtils.GetModel<IAlive>(hitCollider.gameObject);
            if (iAlive != _owner && iAlive != null) {
                OnAliveHit(iAlive, hitCollider, deltaTime);
            }
        }

        void OnAliveHit(IAlive alive, Collider collider, float deltaTime) {
            if (_data.damageData.isDamageOverTime) {
                var rawDamageOverTime = new RawDamageData(_data.rawDamageData);
                rawDamageOverTime.MultiplyMultModifier(deltaTime);
                DealDamage(alive, collider, rawDamageOverTime);
                return;
            }
            if (_damagedTargets.Contains(alive)) {
                return;
            }
            _damagedTargets.Add(alive);
            DealDamage(alive, collider, new RawDamageData(_data.rawDamageData));
        }

        void DealDamage(IAlive alive, Collider collider, RawDamageData rawDamageData) {
            Damage damage = new Damage(_data.damageData.Get(), _owner, alive, rawDamageData).WithHitCollider(collider);
            alive.HealthElement.TakeDamage(damage);
        }
    }
}
