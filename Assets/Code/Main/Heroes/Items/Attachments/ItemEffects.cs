using Awaken.Utility;
using System.Collections.Generic;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Skills;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.Heroes.Items.Attachments {
    public partial class ItemEffects : Element<Item>, IItemSkillOwner, IRefreshedByAttachment<IItemEffectsSpec>, INamed {
        public override ushort TypeForSerialization => SavedModels.ItemEffects;

        public ItemActionType Type { get; private set; }
        public int Priority() => -10000; // Should execute last as consume on use needs to be the last executed operation

        IItemEffectsSpec _spec;
        bool _consumeOnUse;

        public Item Item => ParentModel;
        public ICharacter Character => Item.Owner?.Character;
        public IEnumerable<Skill> Skills => Elements<Skill>().GetManagedEnumerator();
        
        public string DisplayName => (Item?.DisplayName ?? "Null") + " Effects";
        public string DebugName => (Item?.DebugName ?? "Null") + " Effects";
        public int PerformCount { get; set; }
        public bool CanBeCharged => _spec.CanBeCharged;
        public int MaxChargeSteps => _spec.MaxChargeSteps;

        public SkillState SkillState => new() {
            learned = Item.Owner is ICharacter,
            equipped = Item.IsEquipped
        };

        // === Initialization
        
        public void InitFromAttachment(IItemEffectsSpec spec, bool isRestored) {
            Type = spec.ActionType;
            _spec = spec;
            _consumeOnUse = _spec.ConsumeOnUse;
        }

        protected override void OnInitialize() {
            SkillInitialization.Initialize(this, _spec.SkillRefsFromSpec(this), SkillState);
        }
        
        protected override void OnRestore() {
            SkillInitialization.CustomRestore(this, _spec.SkillRefsFromSpec(this), SkillState);
        }
        
        protected override void OnFullyInitialized() {
            ParentModel.RequestSetupTexts();
            this.ListenTo(Events.AfterElementsCollectionModified, AfterSkillAdded, this);
        }

        void AfterSkillAdded(Element element) {
            if (!element.HasBeenDiscarded && element is Skill skill) {
                SkillState.Apply(skill);
            }
        }

        // === Operations
        public void Submit() { }
        public void AfterPerformed() {
            if (_consumeOnUse && !HasBeenDiscarded && !ParentModel.HasBeenDiscarded) {
                ParentModel.DecrementQuantityWithoutNotification();
            }
        }
        public void Perform() { }
        public void Cancel() { }
    }
}
