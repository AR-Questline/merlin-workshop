using Awaken.Utility;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Skills;
using Awaken.TG.Main.Utility.TokenTexts;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility.Attributes;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Heroes.Items.Gems {
    public partial class GemAttached : Element<Item>, IItemSkillOwner {
        public override ushort TypeForSerialization => SavedModels.GemAttached;

        [Saved] ItemTemplate _template;
        [Saved] List<SkillReference> _skillReferences;

        TokenText _descriptionToken;
        
        public string DisplayName { get; private set; }
        public string Description(Item gemItem) => _descriptionToken.GetValue(Hero.Current, gemItem);

        public ItemActionType Type => ItemActionType.Equip;
        public Item Item => ParentModel;
        public ICharacter Character => Item.Owner?.Character;
        public IEnumerable<Skill> Skills => Elements<Skill>().GetManagedEnumerator();
        public int PerformCount { get; set; }
        public ItemTemplate Template => _template;
        public List<SkillReference> SkillRefs => _skillReferences;

        [JsonConstructor, UnityEngine.Scripting.Preserve]
        GemAttached() {}
        
        public GemAttached(GemUnattached gem) {
            _template = gem.ParentModel.Template;
            _skillReferences = gem.SkillRefs.ToList();
        }

        public GemAttached(ItemTemplate template, List<SkillReference> skillReferences) {
            _template = template;
            _skillReferences = skillReferences;
        }

        protected override void OnInitialize() {
            DisplayName = _template.ItemName;
            _descriptionToken = new TokenText(_template.Description);
            this.ListenTo(Events.AfterFullyInitialized, InitSkills, this);
        }

        protected override void OnRestore() {
            DisplayName = _template.ItemName;
            _descriptionToken = new TokenText(_template.Description);
        }

        void InitSkills() {
            foreach (var skill in Skills) {
                skill.Learn();
            }
            
            if (Item.IsEquipped) {
                foreach (var skill in Skills) {
                    skill.Equip();
                }
            }
        }

        public Item RetrieveGem() {
            Item item = new(_template);
            World.Add(item);
            foreach (var skill in Skills) {
                skill.Unequip();
                skill.Forget();
            }
            Discard();
            return item;
        }
        
        public void Submit() { }
        public void AfterPerformed() { }
        public void Perform() { }
        public void Cancel() { }
    }
}