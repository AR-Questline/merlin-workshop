using Awaken.TG.MVC;
using JetBrains.Annotations;

namespace Awaken.TG.Main.Utility.Skills {
    public partial class DummySkillCharacter : DummyCharacter {
        public sealed override bool IsNotSaved => true;

        // === Constructing
        DummySkillCharacter() { }
        
        public static DummySkillCharacter GetOrCreateInstance {
            get {
                return Instance ??= ModelUtils.GetSingletonModel(() => new DummySkillCharacter());
            }
        }
        [CanBeNull] public static DummySkillCharacter Instance { get; private set; }

        // === LifeCycle
        protected override void OnDiscard(bool fromDomainDrop) {
            Instance = null;
        }
    }
}
