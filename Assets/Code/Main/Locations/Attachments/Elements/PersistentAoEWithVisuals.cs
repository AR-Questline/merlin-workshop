using Awaken.TG.Graphics.VFX;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Heroes.Statuses.Duration;
using Awaken.TG.Main.Locations.Attachments.Attachment;
using Awaken.TG.Main.Skills;
using Awaken.Utility;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Locations.Attachments.Elements {
    public partial class PersistentAoEWithVisuals : PersistentAoE, IRefreshedByAttachment<PersistentAoEWithVisualsAttachment> {
        public override ushort TypeForSerialization => SavedModels.PersistentAoEWithVisuals;

        float _discardParentOnEndDelay;
        
        public PersistentAoEWithVisuals(float? tick, IDuration duration, StatusTemplate statusTemplate, float buildupStrength,
            SkillVariablesOverride overrides, SphereDamageParameters? damageParameters, bool onlyOnGrounded, bool isRemovingOther, bool isRemovable, 
            bool canApplyToSelf, bool discardParentOnEnd, bool discardOnDamageDealerDeath)
            : base(tick, duration, statusTemplate, buildupStrength, overrides, damageParameters, onlyOnGrounded, isRemovingOther, isRemovable, 
                canApplyToSelf, discardParentOnEnd, discardOnDamageDealerDeath) {
        }

        [JsonConstructor, UnityEngine.Scripting.Preserve]
        PersistentAoEWithVisuals() { }
        
        public void InitFromAttachment(PersistentAoEWithVisualsAttachment spec, bool isRestored) {
            _discardParentOnEndDelay = spec.discardParentOnEndDelay;
        }

        protected override void End() {
            if (!DiscardParentOnEnd || _discardParentOnEndDelay <= 0) {
                base.End();
            }
            
            VFXUtils.StopVfx(ParentModel.ViewParent.gameObject);

            var parent = ParentModel;
            Discard();
            DelayedDiscardParent(parent).Forget();
        }

        async UniTaskVoid DelayedDiscardParent(Location parent) {
            parent.MarkedNotSaved = true;
            if (!await AsyncUtil.DelayTime(parent, _discardParentOnEndDelay)) {
                return;
            }
            parent.Discard();
        }
    }
}
