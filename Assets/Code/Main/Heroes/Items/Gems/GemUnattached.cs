using Awaken.Utility;
using System.Collections.Generic;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes.Skills;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Skills;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.Heroes.Items.Gems {
    /// <summary>
    /// This element needs to spawn skills so that we can show description created from them
    /// But it doesn't implement IItemSkillOwner, to prevent those skills from execution by Item
    /// When inserted to an item, the Gem is destroyed and changes to <see cref="GemAttached"/>  
    /// </summary>
    public partial class GemUnattached : Element<Item>, IRefreshedByAttachment<GemAttachment>, ISkillOwner, ISkillProvider {
        public override ushort TypeForSerialization => SavedModels.GemUnattached;

        GemAttachment _spec;
        
        public IEnumerable<SkillReference> SkillRefs => _spec.Skills;
        public IEnumerable<Skill> Skills => Elements<Skill>().GetManagedEnumerator();
        public GemType GemType => _spec.Type;

        // Character for ISkillOwner
        public ICharacter Character => null;

        public void InitFromAttachment(GemAttachment spec, bool isRestored) {
            _spec = spec;
        }

        protected override void OnFullyInitialized() {
            foreach (var skillRef in _spec.Skills) {
                // Skills are not saved (see AllowElementSave)
                var skill = skillRef.CreateSkill();
                AddElement(skill);
            }
            ParentModel.RequestSetupTexts();
            base.OnFullyInitialized();
        }

        public override bool AllowElementSave(Element ele) {
            return false;
        }
    }

    public enum GemType {
        Weapon = 0,
        Armor = 1,
    }
}