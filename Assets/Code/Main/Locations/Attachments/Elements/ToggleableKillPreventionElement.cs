using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.Utility;
using JetBrains.Annotations;

namespace Awaken.TG.Main.Locations.Attachments.Elements {
    public partial class ToggleableKillPreventionElement : KillPreventionElement<IAlive> {
        public override ushort TypeForSerialization => SavedModels.ToggleableKillPreventionElement;

        bool _active;
        
        protected override IAlive GetAlive => ParentModel;
        protected override NpcElement TryGetNpc => ParentModel as NpcElement;
        
        [UsedImplicitly, UnityEngine.Scripting.Preserve]
        public static ToggleableKillPreventionElement Create(IAlive alive, bool startActive) {
            return Create(alive, startActive, true);
        }
        
        [UsedImplicitly, UnityEngine.Scripting.Preserve]
        public static ToggleableKillPreventionElement Create(IAlive alive, bool startActive, bool saved) {
            ToggleableKillPreventionElement element = alive.AddElement(new ToggleableKillPreventionElement());
            element.SetActive(startActive);
            if (!saved) {
                element.MarkedNotSaved = true;
            }
            return element;
        }
        
        public void SetActive(bool active) {
            _active = active;
        }
        
        public override bool OnBeforeTakingFinalDamage(HealthElement healthElement, Damage damage) {
            if (!_active) {
                return false;
            } 
            return base.OnBeforeTakingFinalDamage(healthElement, damage);
        }
    }
}
