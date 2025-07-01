using System.Collections.Generic;
using Awaken.TG.Assets;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Skills;
using Awaken.TG.Main.Utility.Audio;
using Awaken.TG.Main.VisualGraphUtils;
using FMODUnity;
using Unity.VisualScripting;
using UnityEngine;

namespace Awaken.TG.Main.AI.Fights.Projectiles {
    public partial class ProjectileWrapper {
         public void ApplyVariables(List<VSVariable> variables) {
            ApplyToProjectile(new ProjectileVariableOverride(variables));
        }

        public void ConfigureShootProjectile(VGUtils.ShootParams shootParams, Vector3 arrowVelocity) {
            ApplyToProjectile(new ConfigureShootProjectile(shootParams, arrowVelocity));
        }

        public void ConfigureShotProjectileSimple(Vector3 projectileVelocity, ICharacter shooter, EquipmentSlotType slotType, float fireStrength, ProjectileOffsetData? offsetParams, DamageType? damageType) {
            ApplyToProjectile(new ConfigureShotProjectileSimple(projectileVelocity, shooter, slotType, fireStrength, offsetParams, damageType));
        }

        public void ConfigureHomingProjectile(ICharacter shooter, Item shootingItem, DamageTypeData damageTypeData) {
            ApplyToProjectile(new ConfigureHomingProjectile(shooter, shootingItem, damageTypeData));
        }
        
        public void EnableLogic(bool enable) {
            ApplyToProjectile(new EnableLogic(enable));
        }

        [UnityEngine.Scripting.Preserve]
        public void DuplicateProjectile(Vector3 spawnOffset, Vector3 aimOffset, bool consumeAmmo = false, ShareableARAssetReference overrideLogicPrefab = null,
            ShareableARAssetReference overrideVisualPrefab = null, ProjectileLogicData? overrideLogicData = null, List<SkillReference> overrideSkills = null, float delayMove = 0f, float? damageMultiplier = null) {
            ApplyToProjectile(new ProjectileDuplicate(spawnOffset, aimOffset, consumeAmmo, overrideLogicPrefab, overrideVisualPrefab, overrideLogicData, overrideSkills, delayMove, damageMultiplier));
        }

        public void MultiplyBaseDamage(float value) {
            ApplyToProjectile(new ProjectileMultiplyBaseDamage(value));
        }
        
        public void SetAsSecondaryProjectile() {
            ApplyToProjectile(new SetAsSecondaryProjectile());
        }

        public void HomingProjectileSetTarget(ICharacter target, float aimAtHeight = 0.5f) {
            ApplyToProjectile(new ProjectileHomingSetTarget(target, aimAtHeight));
        }
    }
    
    internal class ConfigureShootProjectile : IApplicableToProjectile {
        readonly VGUtils.ShootParams _shootParams;
        readonly Vector3 _arrowVelocity;
        
        public ConfigureShootProjectile(VGUtils.ShootParams shootParams, Vector3 arrowVelocity) {
            _shootParams = shootParams;
            _arrowVelocity = arrowVelocity;
        }

        public void ApplyToProjectile(GameObject gameObject, Projectile projectile) {
            if (projectile is not DamageDealingProjectile ddp) {
                return;
            }
            
            ICharacter shooter = _shootParams.shooter;
            if (shooter is not { HasBeenDiscarded: false, IsAlive: true, IsDying: false }) {
                shooter = null;
            }
            
            Item equippedProjectile = null;
            if (_shootParams.projectileSlotType != null) {
                equippedProjectile = shooter?.Inventory.EquippedItem(_shootParams.projectileSlotType);
                if (equippedProjectile is { IsArrow: false } and { IsThrowable: false }) {
                    equippedProjectile = null;
                }
            }

            // --- Audio
            if (shooter?.OffHandWeapon != null) {
                shooter.OffHandWeapon.PlayAudioClip(ItemAudioType.ReleaseBow, false, new FMODParameter("ShootingForce", _shootParams.fireStrength));
            }

            Item item = _shootParams.item ?? shooter?.Inventory.EquippedItem(EquipmentSlotType.MainHand);
            ddp.SetVelocityAndForward(_arrowVelocity);
            ddp.owner = shooter;
            ddp.SetBaseDamageParams(item, equippedProjectile, _shootParams.fireStrength, _shootParams.damageTypeData, _shootParams.rawDamageData, _shootParams.damageAmount);
            if (projectile is Arrow a && equippedProjectile != null) {
                a.SetItemTemplate(equippedProjectile.Template);
            }
        }
    }

    internal class ConfigureShotProjectileSimple : IApplicableToProjectile {
        readonly Vector3 _projectileVelocity;
        readonly ICharacter _shooter;
        readonly EquipmentSlotType _slotType;
        readonly float _fireStrength;
        readonly ProjectileOffsetData? _offsetParams;
        readonly DamageType? _damageType;
        
        public ConfigureShotProjectileSimple(Vector3 projectileVelocity, ICharacter shooter, EquipmentSlotType slotType, float fireStrength, ProjectileOffsetData? offsetParams, DamageType? damageType) {
            _projectileVelocity = projectileVelocity;
            _shooter = shooter;
            _slotType = slotType;
            _fireStrength = fireStrength;
            _offsetParams = offsetParams;
            _damageType = damageType;
        }
        
        public void ApplyToProjectile(GameObject gameObject, Projectile projectile) {
            if (projectile is not DamageDealingProjectile ddp) {
                return;
            }

            ddp.SetVelocityAndForward(_projectileVelocity, _offsetParams);
            ddp.owner = _shooter;
            var slotType = _slotType ?? EquipmentSlotType.MainHand;
            var weapon = _shooter.Inventory.EquippedItem(slotType);
            var damageTypeData = _damageType.HasValue ? new DamageTypeData(_damageType.Value) : null;
            ddp.SetBaseDamageParams(weapon, null, _fireStrength, damageTypeData);

            Item equippedArrows = _shooter.Inventory.EquippedItem(EquipmentSlotType.Quiver);
            if (equippedArrows is { IsArrow: false }) {
                equippedArrows = null;
            }

            if (ddp is Arrow a && equippedArrows != null) {
                a.SetItemTemplate(equippedArrows.Template);
            }
        }
    }

    internal class ConfigureHomingProjectile : IApplicableToProjectile {
        readonly ICharacter _character;
        readonly Item _item;
        readonly DamageTypeData _damageTypeData;
        
        public ConfigureHomingProjectile(ICharacter character, Item item, DamageTypeData damageTypeData) {
            _character = character;
            _item = item;
            _damageTypeData = damageTypeData;
        }
        
        public void ApplyToProjectile(GameObject gameObject, Projectile projectile) {
            if (projectile is HomingProjectile hp) {
                hp.owner = _character;
                hp.SetBaseDamageParams(_item, null, 1, _damageTypeData);
            }
        }
    }
    
    internal class ProjectileMultiplyBaseDamage : IApplicableToProjectile {
        readonly float _multiplier;
        
        public ProjectileMultiplyBaseDamage(float multiplier) {
            _multiplier = multiplier;
        }
        
        public void ApplyToProjectile(GameObject gameObject, Projectile projectile) {
            if (projectile is DamageDealingProjectile ddp) {
                ddp.MultiplyBaseDamage(_multiplier);
            }
        }
    }
    
    internal class SetAsSecondaryProjectile : IApplicableToProjectile {
        public void ApplyToProjectile(GameObject gameObject, Projectile projectile) {
            if (projectile is DamageDealingProjectile ddp) {
                ddp.SetAsSecondaryProjectile();
            }
        }
    }
    
    internal class EnableLogic : IApplicableToProjectile {
        readonly bool _enable;
        
        public EnableLogic(bool enable) {
            _enable = enable;
        }
        
        public void ApplyToProjectile(GameObject gameObject, Projectile projectile) {
            if (projectile is DamageDealingProjectile ddp) {
                ddp.enabled = _enable;
                gameObject.GetComponent<Rigidbody>().isKinematic = !_enable;
            }
        }
    }
    
    internal class ProjectileDuplicate : IApplicableToProjectile {
        readonly Vector3 _spawnOffset;
        readonly Vector3 _aimOffset;
        readonly bool _consumeAmmo;
        readonly ShareableARAssetReference _overrideLogicPrefab;
        readonly ShareableARAssetReference _overrideVisualPrefab;
        readonly ProjectileLogicData? _overrideLogicData;
        readonly List<SkillReference> _overrideSkills;
        readonly float _delayMove;
        readonly float? _damageMultiplier;
        
        public ProjectileDuplicate(Vector3 spawnOffset, Vector3 aimOffset, bool consumeAmmo = false, ShareableARAssetReference overrideLogicPrefab = null,
            ShareableARAssetReference overrideVisualPrefab = null, ProjectileLogicData? overrideLogicData = null, List<SkillReference> overrideSkills = null, float delayMove = 0f, float? damageMultiplier = null) {
            _spawnOffset = spawnOffset;
            _aimOffset = aimOffset;
            _consumeAmmo = consumeAmmo;
            _overrideLogicPrefab = overrideLogicPrefab;
            _overrideVisualPrefab = overrideVisualPrefab;
            _overrideLogicData = overrideLogicData;
            _overrideSkills = overrideSkills;
            _delayMove = delayMove;
            _damageMultiplier = damageMultiplier;
        }
        public void ApplyToProjectile(GameObject gameObject, Projectile projectile) {
            if (projectile is DamageDealingProjectile ddp) {
                VGUtils.DuplicateProjectile(ddp, _spawnOffset, _aimOffset, _consumeAmmo, _overrideLogicPrefab,
                    _overrideVisualPrefab, _overrideLogicData, _overrideSkills, _delayMove, _damageMultiplier);
            }
        }
    }
    
    internal class ProjectileVariableOverride : IApplicableToProjectile {
        readonly List<VSVariable> _variables;
        
        public ProjectileVariableOverride(List<VSVariable> variables) {
            _variables = variables;
        }
        
        public void ApplyToProjectile(GameObject gameObject, Projectile projectile) {
            var variableDeclarations = Variables.Object(gameObject);
            foreach (var variable in _variables) {
                variableDeclarations.Set(variable.name, variable.value);
            }
        }
    }
    
    internal class ProjectileHomingSetTarget : IApplicableToProjectile {
        readonly ICharacter _target;
        readonly float _aimAtHeight;
        
        public ProjectileHomingSetTarget(ICharacter target, float aimAtHeight) {
            _target = target;
            _aimAtHeight = aimAtHeight;
        }
        
        public void ApplyToProjectile(GameObject gameObject, Projectile projectile) {
            if (projectile is HomingProjectile hp) {
                hp.SetTarget(_target, _aimAtHeight);
            }
        }
    }
}