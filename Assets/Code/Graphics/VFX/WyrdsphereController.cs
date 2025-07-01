using System;
using UnityEngine;
using UnityEngine.VFX;

namespace Awaken.TG.Graphics.VFX {
    public class WyrdsphereController : MonoBehaviour
    {
        [SerializeField] [UnityEngine.Scripting.Preserve] private int _particleCount;
        public State wyrdspireState = State.Idle;
        public ParticleSpeed particleSpeed;
        
        private VisualEffect _visualEffect;
        private Light _wyrdspireLight;
        private Animator _animator;
        
        private bool _canUpdate;
        private bool _changedState;
        
        public enum State {
            Born,
            Idle,
            NoAura,
            Vulnerable,
            VulnerableNoAura,
            VulnerableNoAuraStunned,
            Stunned,
            StunnedNoAura,
            StunnedVulnerable,
            Buff,
            TakenDamage,
            AttackWeak,
            AttackStrong,
            AttackAoe,
            Heal,
            Block,
            Death,
            Dance
        }

        private void Start() {
            _visualEffect = gameObject.GetComponentInChildren<VisualEffect>();
            _wyrdspireLight = gameObject.GetComponentInChildren<Light>();
            _animator = gameObject.GetComponent<Animator>();
        }

        private void OnEnable() {
            _canUpdate = true;
        }

        private void OnValidate() {
            SetPlayRate();
            StartAnimation();
            _changedState = true;
        }

        private void Update() {
            SetActualPrimaryColorToLight();
            _particleCount = _visualEffect.aliveParticleCount;
        }

        private void StartAnimation() {
            if (_canUpdate) {
                if (_changedState) {
                    switch (wyrdspireState) {
                        case State.Born: 
                            _animator.SetTrigger("Wyrdsphere_Born");
                            break;
                        case State.Idle:
                            _animator.SetTrigger("Wyrdsphere_Idle");
                            break;
                        case State.NoAura:
                            _animator.SetTrigger("Wyrdsphere_NoAura");
                            break;
                        case State.Vulnerable:
                            _animator.SetTrigger("Wyrdsphere_Vulnerable");
                            break;
                        case State.VulnerableNoAura:
                            _animator.SetTrigger("Wyrdsphere_VulnerableNoAura");
                            break;
                        case State.VulnerableNoAuraStunned:
                            _animator.SetTrigger("Wyrdsphere_VulnerableNoAuraStunned");
                            break;
                        case State.Stunned:
                            _animator.SetTrigger("Wyrdsphere_Stunned");
                            break;
                        case State.StunnedNoAura:
                            _animator.SetTrigger("Wyrdsphere_StunnedNoAura");
                            break;
                        case State.StunnedVulnerable:
                            _animator.SetTrigger("Wyrdsphere_StunnedVulnerable");
                            break;
                        case State.Buff:
                            _animator.SetTrigger("Wyrdsphere_Buff");
                            break;
                        case State.TakenDamage:
                            _animator.SetTrigger("Wyrdsphere_TakenDamage");
                            break;
                        case State.AttackStrong:
                            _animator.SetTrigger("Wyrdsphere_AttackStrong");
                            break;
                        case State.AttackWeak:
                            _animator.SetTrigger("Wyrdsphere_AttackWeak");
                            break;
                        case State.AttackAoe:
                            _animator.SetTrigger("Wyrdsphere_AttackAoe");
                            break;
                        case State.Heal:
                            _animator.SetTrigger("Wyrdsphere_Heal");
                            break;
                        case State.Block:
                            _animator.SetTrigger("Wyrdsphere_Block");
                            break;
                        case State.Death:
                            _animator.SetTrigger("Wyrdsphere_Death");
                            break;
                        case State.Dance:
                            _animator.SetTrigger("Wyrdsphere_Dance");
                            break;
                    } 
                }
            }
        }

        private void SetActualPrimaryColorToLight() {
            Color acutalColor = _visualEffect.GetVector4("ColorPrimary");
            _wyrdspireLight.color = acutalColor;
        }
        
        [UnityEngine.Scripting.Preserve]
        private void SetIdlePlayRate() {
            _visualEffect.playRate = particleSpeed.idle;
        }

        private void SetPlayRate() {
            if (_canUpdate) {
                switch (wyrdspireState)
                {
                    case State.Born:
                        _visualEffect.playRate = particleSpeed.born;
                        break;
                    case State.Idle:
                        _visualEffect.playRate = particleSpeed.idle;
                        break;
                    case State.NoAura:
                        _visualEffect.playRate = particleSpeed.noAura;
                        break;
                    case State.Vulnerable:
                        _visualEffect.playRate = particleSpeed.vulnerable;
                        break;
                    case State.VulnerableNoAura:
                        _visualEffect.playRate = particleSpeed.vulnerableNoAura;
                        break;
                    case State.VulnerableNoAuraStunned:
                        _visualEffect.playRate = particleSpeed.vulnerableNoAuraStunned;
                        break;
                    case State.Stunned:
                        _visualEffect.playRate = particleSpeed.stunned;
                        break;
                    case State .StunnedNoAura:
                        _visualEffect.playRate = particleSpeed.stunnedNoAura;
                        break;
                    case State .StunnedVulnerable:
                        _visualEffect.playRate = particleSpeed.stunnedNoAura;
                        break;
                    case State.Buff:
                        _visualEffect.playRate = particleSpeed.buff;
                        break;
                    case State.TakenDamage:
                        _visualEffect.playRate = particleSpeed.takenDamage;
                        break;
                    case State.AttackStrong:
                        _visualEffect.playRate = particleSpeed.attackStrong;
                        break;
                    case State.AttackWeak:
                        _visualEffect.playRate = particleSpeed.attackWeak;
                        break;
                    case State.AttackAoe:
                        _visualEffect.playRate = particleSpeed.attackAoe;
                        break;
                    case State.Heal:
                        _visualEffect.playRate = particleSpeed.heal;
                        break;
                    case State.Block:
                        _visualEffect.playRate = particleSpeed.block;
                        break;
                    case State.Death:
                        _visualEffect.playRate = particleSpeed.death;
                        break;
                    case State.Dance:
                        _visualEffect.playRate = particleSpeed.dance;
                        break;
                }
            }
        }
        
        [Serializable]
        public class ParticleSpeed {
            public float born;
            public float idle;
            public float noAura;
            public float vulnerable;
            public float vulnerableNoAura;
            public float vulnerableNoAuraStunned;
            public float stunned;
            public float stunnedNoAura;
            [UnityEngine.Scripting.Preserve] public float stunnedVulnerable;
            public float buff;
            public float takenDamage;
            public float attackWeak;
            public float attackStrong;
            public float attackAoe;
            public float heal;
            public float block;
            public float death;
            public float dance;

            public ParticleSpeed(float born, float idle, float noAura, float vulnerable, float vulnerableNoAura, float vulnerableNoAuraStunned, float stunned, float stunnedNoAura, float stunnedVulnerable, float buff, float takenDamage, float attackWeak, float attackStrong, float attackAoe, float heal, float block, float death, float dance) {
                this.born = born;
                this.idle = idle;
                this.noAura = noAura;
                this.vulnerable = vulnerable;
                this.vulnerableNoAura = vulnerableNoAura;
                this.vulnerableNoAuraStunned = vulnerableNoAuraStunned;
                this.stunned = stunned;
                this.stunnedNoAura = stunnedNoAura;
                this.stunnedVulnerable = stunnedVulnerable;
                this.buff = buff;
                this.takenDamage = takenDamage;
                this.attackWeak = attackWeak;
                this.attackStrong = attackStrong;
                this.attackAoe = attackAoe;
                this.heal = heal;
                this.block = block;
                this.death = death;
                this.dance = dance;
            }

            public ParticleSpeed() {
                born = 1;
                idle = 1;
                noAura = 1;
                vulnerable = 1;
                vulnerableNoAura = 1;
                vulnerableNoAuraStunned = 1;
                stunned = 1;
                stunnedNoAura = 1;
                stunnedVulnerable = 1;
                buff = 1;
                takenDamage = 1;
                attackWeak = 1;
                attackStrong = 1;
                attackAoe = 1;
                heal = 1;
                block = 1;
                death = 1;
                dance = 1;
            }
        }
    }
}