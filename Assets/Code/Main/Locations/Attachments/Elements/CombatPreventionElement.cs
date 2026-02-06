using Awaken.TG.Assets;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Locations.Attachments.Attachment;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Elements {
    public partial class CombatPreventionElement : CombatPreventionElementBase, IRefreshedByAttachment<CombatPreventionAttachment>{
        public override ushort TypeForSerialization => SavedModels.CombatPreventionElement;

        CombatPreventionAttachment _spec;
        float _nextOverallVfxTime;
        float _nextPointVfxTime;
        [Saved] bool _enablePrevention;
        
        public bool Enabled => _enablePrevention;
        
        public void InitFromAttachment(CombatPreventionAttachment spec, bool isRestored) {
            _spec = spec;
        }

        protected override void OnInitialize() {
            _enablePrevention = true;
            base.OnInitialize();
        }
        
        protected override void InitElements(NpcElement npc) {
            Toggle(_enablePrevention);
        }

        public void Toggle(bool enabled) {
            _enablePrevention = enabled;
            
            if (enabled && ParentModel.TryGetElement<NpcElement>(out var npc)) {
                AddElements(npc);
            } else {
                RemoveElements();
            }
        }

        public override bool OnBeforeTakingFinalDamage(HealthElement healthElement, Damage damage) {
            if (!_enablePrevention) {
                return false;
            }
            
            if (_spec.BeingHitOverallVFX is { IsSet: true } && Time.time > _nextOverallVfxTime) {
                _nextOverallVfxTime = Time.time + _spec.OverallVfxCooldown;
                PrefabPool.InstantiateAndReturn(_spec.BeingHitOverallVFX, Vector3.zero, Quaternion.identity, parent: healthElement.ParentModel.ParentTransform).Forget();
            }
            if (_spec.BeingHitPointVFX is { IsSet: true } && damage.Position.HasValue && Time.time > _nextPointVfxTime) {
                _nextPointVfxTime = Time.time + _spec.PointVfxCooldown;
                var rotation = damage.Direction.HasValue ? Quaternion.LookRotation(damage.Direction.Value) : Quaternion.LookRotation(damage.DealerPosition - ParentModel.Coords);
                PrefabPool.InstantiateAndReturn(_spec.BeingHitPointVFX, damage.Position.Value, rotation).Forget();
            }
            return true;
        }
    }
}